using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using InputMaster.Forms;

namespace InputMaster.TextEditor
{
  public sealed class TextEditorForm : ThemeForm
  {
    private readonly FileManager _fileManager;
    private readonly List<FileTab> _fileTabs = new List<FileTab>();
    private MyState _state;
    private readonly TabControlPlus _tabControl;
    /// <summary>
    /// This gives us time to save all data before closing the form.
    /// </summary>
    private bool _preventClose = true;

    public TextEditorForm(FileManager fileManager)
    {
      _fileManager = fileManager;
      Env.CommandCollection.AddActor(this);
      var stateHandler = Env.StateHandlerFactory.Create(new MyState(), nameof(TextEditorForm),
        StateHandlerFlags.Exportable | StateHandlerFlags.UseCipher);
      _fileManager.OpenFile = async (name) => await OpenFileAsync(name);
      Opacity = 0;
      SuspendLayout();
      _tabControl = new TabControlPlus
      {
        Dock = DockStyle.Fill
      };
      Controls.Add(_tabControl);
      FormBorderStyle = FormBorderStyle.None;
      KeyPreview = true;
      Text = Env.Config.TextEditorWindowTitle;
      WindowState = FormWindowState.Normal;
      StartPosition = FormStartPosition.Manual;
      Left = Screen.PrimaryScreen.WorkingArea.Left;
      Top = Screen.PrimaryScreen.WorkingArea.Top;
      Width = Screen.PrimaryScreen.WorkingArea.Width;
      Height = Screen.PrimaryScreen.WorkingArea.Height;
      ResumeLayout(false);
      var updatePanelTimer = new Timer
      {
        Interval = (int)Env.Config.UpdatePanelInterval.TotalMilliseconds,
        Enabled = true
      };
      updatePanelTimer.Tick += (s, e) =>
      {
        foreach (var fileTab in _fileTabs)
          fileTab.UpdatePanel();
      };
      KeyDown += async (s, e) =>
      {
        switch (e.KeyData)
        {
          case Keys.Control | Keys.N:
            e.Handled = true;
            await CreateNewFileAsync();
            break;
          case Keys.Control | Keys.Shift | Keys.F:
            e.Handled = true;
            await _fileManager.FindAllAsync();
            break;
          case Keys.Control | Keys.O:
            e.Handled = true;
            await OpenCustomFileAsync();
            break;
        }
      };
      FormClosing += async (s, e) =>
      {
        if (!_preventClose)
          return;
        e.Cancel = true;
        await Task.Yield();
        Application.Exit();
      };
      Env.App.Exiting += () =>
       {
         CloseAll();
         updatePanelTimer.Dispose();
         _preventClose = false;
         Close();
       };
      _tabControl.SelectedIndexChanged += (s, e) =>
      {
        var tab = _tabControl.SelectedTab;
        var fileTab = (FileTab)tab?.Tag;
        fileTab?.SelectRtb();
      };
      Env.App.AddSaveAction(async () =>
      {
        await Task.WhenAll(_fileTabs.Select(z => z.SaveAsync()));
        await stateHandler.SaveAsync();
      });
      Env.App.Run += async () =>
      {
        _state = await stateHandler.LoadAsync();
        Show();
      };
    }

    public bool TryGetPosition(string name, out RtbPosition position) => _state.Positions.TryGetValue(name, out position);

    public void UpdatePosition(string name, RtbPosition position)
    {
      _state.Positions[name] = position;
    }

    public void RemoveFileTab(FileTab fileTab)
    {
      _tabControl.TabPages.Remove(fileTab.TabPage);
      _fileTabs.Remove(fileTab);
      if (_fileTabs.Count == 0)
        Opacity = 0;
    }

    public void RemovePosition(string name)
    {
      _state.Positions.Remove(name);
    }

    private async Task OpenCustomFileAsync()
    {
      var name = await Helper.TryGetStringAsync("File name");
      if (name == null)
        return;
      Helper.RequireExistsFile(FileManager.GetFile(name));
      await OpenFileAsync(name);
    }

    private async Task OpenFileAsync(string name)
    {
      var fileTab = _fileTabs.FirstOrDefault(z => string.Equals(z.Name, name, StringComparison.OrdinalIgnoreCase));
      if (fileTab == null)
      {
        var tabPage = new TabPage();
        SuspendLayout();
        _tabControl.TabPages.Add(tabPage);
        fileTab = new FileTab(name, this, _fileManager, tabPage);
        await fileTab.InitializeAsync();
        _fileTabs.Add(fileTab);
        _tabControl.SelectTab(tabPage);
        fileTab.SelectRtb();
        ResumeLayout();
        Opacity = 1;
      }
      else
      {
        _tabControl.SelectTab(fileTab.TabPage);
      }
      if (_fileTabs.Count > Env.Config.MaxTextEditorTabs)
        await ((FileTab)_tabControl.GetLastTabPageInOrder().Tag).SaveAndCloseAsync();
    }

    private async Task CreateNewFileAsync()
    {
      var title = await Helper.TryGetLineAsync("Title of new file");
      if (title == null)
        return;
      var name = await Helper.TryGetLineAsync("Name of new file", Helper.GetRandomName(Env.Config.TextEditorFileNameLength));
      if (name == null)
        return;
      await _fileManager.CreateNewFileAsync(title.Trim(), "", name);
      await OpenFileAsync(name);
      _fileManager.CompileTextEditorMode();
    }

    public void CloseAll()
    {
      foreach (var fileTab in _fileTabs.ToList())
        fileTab.Close();
    }

    public async Task ExportToDirectoryAsync()
    {
      await Env.App.TriggerSaveAsync();
      await FileManager.ExportToDirectoryAsync();
    }

    [Command]
    private void ToggleTextEditor()
    {
      Visible = !Visible;
    }

    [Command]
    private async Task ImportTextFilesAsync()
    {
      await Env.App.TriggerSaveAsync();
      CloseAll();
      await _fileManager.ImportFromDirectoryAsync();
      Env.ShouldRestart = true;
      Application.Exit();
    }

    private class MyState : IState
    {
      public Dictionary<string, RtbPosition> Positions;

      public (bool, string message) Fix()
      {
        Positions = Positions ?? new Dictionary<string, RtbPosition>();
        return (true, "");
      }
    }
  }
}

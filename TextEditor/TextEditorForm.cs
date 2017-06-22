using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using InputMaster.Hooks;
using InputMaster.Forms;

namespace InputMaster.TextEditor
{
  internal sealed class TextEditorForm : ThemeForm
  {
    private readonly ModeHook ModeHook;
    private readonly FileManager FileManager;
    private readonly List<FileTab> FileTabs = new List<FileTab>();
    private readonly Dictionary<string, RtbPosition> Positions = new Dictionary<string, RtbPosition>(StringComparer.OrdinalIgnoreCase);
    private readonly MyState State;
    private readonly TabControlPlus TabControl;
    private bool IsClosing;
    private bool PreventClose = true;

    public TextEditorForm(ModeHook modeHook, FileManager fileManager)
    {
      ModeHook = modeHook;
      FileManager = fileManager;
      FileManager.OpenFile = async (file) => await OpenFileAsync(file);
      HideFirstInstant();
      SuspendLayout();
      TabControl = new TabControlPlus
      {
        Dock = DockStyle.Fill
      };
      Controls.Add(TabControl);
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
        foreach (var fileTab in FileTabs)
        {
          fileTab.UpdatePanel();
        }
      };
      State = new MyState(this);
      State.Load();
      Env.App.SaveTick += async () => await SaveAllAsync();
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
            await FileManager.FindAllAsync();
            break;
          case Keys.Control | Keys.Shift | Keys.I:
            e.Handled = true;
            await FileManager.ImportFromDirectoryAsync();
            break;
          case Keys.Control | Keys.Shift | Keys.E:
            e.Handled = true;
            await FileManager.ExportToDirectoryAsync();
            break;
          case Keys.Control | Keys.O:
            e.Handled = true;
            await OpenCustomFileAsync();
            break;
        }
      };
      FormClosing += async (s, e) =>
      {
        e.Cancel = PreventClose;
        if (IsClosing)
        {
          return;
        }
        IsClosing = true;
        await Try.ExecuteAsync(SaveAllAsync);
        await Try.ExecuteAsync(CloseAllAsync);
        updatePanelTimer.Dispose();
        PreventClose = false;
        await Task.Yield();
        Application.Exit();
      };
      TabControl.SelectedIndexChanged += (s, e) =>
      {
        var tab = TabControl.SelectedTab;
        var fileTab = (FileTab)tab?.Tag;
        fileTab?.SelectRtb();
      };
    }

    public bool TryGetPosition(string file, out RtbPosition position) => Positions.TryGetValue(file, out position);

    public async void StartAsync()
    {
      await Task.Yield();
      Show();
    }

    public void UpdatePosition(string file, RtbPosition position)
    {
      if (!Positions.TryGetValue(file, out var oldPosition))
      {
        Positions.Add(file, position);
        State.Changed = true;
        return;
      }
      if (position == oldPosition)
      {
        return;
      }
      Positions[file] = position;
      State.Changed = true;
    }

    public void RemoveFileTab(FileTab fileTab)
    {
      TabControl.TabPages.Remove(fileTab.TabPage);
      FileTabs.Remove(fileTab);
    }

    private void HideFirstInstant()
    {
      Opacity = 0;
      Shown += async (s, e) =>
      {
        await Task.Delay(TimeSpan.FromSeconds(1));
        Opacity = 1;
      };
    }

    private async Task OpenCustomFileAsync()
    {
      await Task.Yield();
      if (!Helper.TryGetString("File path", out var file))
      {
        return;
      }
      Helper.RequireExistsFile(file);
      await OpenFileAsync(file);
    }

    private async Task OpenFileAsync(string file)
    {
      var fileTab = FileTabs.FirstOrDefault(z => string.Equals(z.File, file, StringComparison.OrdinalIgnoreCase));
      if (fileTab == null)
      {
        var tabPage = new TabPage();
        SuspendLayout();
        TabControl.TabPages.Add(tabPage);
        fileTab = new FileTab(file, this, ModeHook, FileManager, tabPage);
        await fileTab.InitializeAsync();
        FileTabs.Add(fileTab);
        TabControl.SelectTab(tabPage);
        fileTab.SelectRtb();
        ResumeLayout();
      }
      else
      {
        TabControl.SelectTab(fileTab.TabPage);
      }
      if (FileTabs.Count > Env.Config.MaxTextEditorTabs)
      {
        await ((FileTab)TabControl.GetLastTabPageInOrder().Tag).CloseAsync();
      }
    }

    private async Task CreateNewFileAsync()
    {
      if (!Helper.TryGetString("Title of new file", out var title))
      {
        return;
      }
      var file = await FileManager.CreateNewFileAsync(title.Trim(), "");
      await OpenFileAsync(file);
      await FileManager.CompileTextEditorModeAsync();
    }

    public async Task SaveAllAsync()
    {
      foreach (var fileTab in FileTabs)
      {
        try
        {
          await fileTab.SaveAsync();
        }
        catch (Exception ex) when (!Helper.IsFatalException(ex))
        {
          Env.Notifier.WriteError(ex, "Failed to save a TextEditor text file" + Helper.GetBindingsSuffix(fileTab.Title, nameof(fileTab.Title)));
        }
      }
    }

    public async Task CloseAllAsync()
    {
      foreach (var item in FileTabs.ToArray())
      {
        await item.CloseAsync();
      }
    }

    private class MyState : State<TextEditorForm>
    {
      public MyState(TextEditorForm form) : base(nameof(TextEditorForm), form) { }

      protected override void Load(BinaryReader reader)
      {
        var count = reader.ReadInt32();
        for (var i = 0; i < count; i++)
        {
          var key = reader.ReadString();
          var position = new RtbPosition(reader);
          Parent.Positions.Add(key, position);
        }
      }

      protected override void Save(BinaryWriter writer)
      {
        writer.Write(Parent.Positions.Count);
        foreach (var pair in Parent.Positions)
        {
          writer.Write(pair.Key);
          pair.Value.Write(writer);
        }
      }
    }
  }
}

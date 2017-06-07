using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using InputMaster.Hooks;
using InputMaster.Parsers;
using InputMaster.Forms;

namespace InputMaster.TextEditor
{
  internal sealed class TextEditorForm : ThemeForm
  {
    private readonly ModeHook ModeHook;
    private readonly List<FileTab> FileTabs = new List<FileTab>();
    private readonly Dictionary<string, RtbPosition> Positions = new Dictionary<string, RtbPosition>(StringComparer.OrdinalIgnoreCase);
    private readonly MyState State;
    private readonly TabControlPlus TabControl;
    private bool Alive = true;
    private string Password;
    private bool WrongPassword;
    private readonly string DataDirName = "data";
    private readonly string NamesDirName = "names";
    private FileTab SelectedFileTab;

    public TextEditorForm(ModeHook modeHook)
    {
      HideFirstInstant();
      ModeHook = Helper.ForbidNull(modeHook, nameof(modeHook));
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
      var saveTimer = new Timer
      {
        Interval = (int)Env.Config.SaveTimerInterval.TotalMilliseconds,
        Enabled = true
      };
      State = new MyState(this);
      State.Load();
      SharedFileManager = new SharedFileManager(this);
      saveTimer.Tick += (s, e) =>
      {
        SaveAll();
      };
      Shown += async (s, e) =>
      {
        Directory.CreateDirectory(DataDir);
        Directory.CreateDirectory(NamesDir);
        await CompileTextEditorModeAsync(true);
        Started();
        HasStarted = true;
      };
      KeyDown += async (s, e) =>
      {
        if (WrongPassword)
        {
          ShowWrongPasswordError();
          return;
        }
        if (e.KeyData == (Keys.Control | Keys.N))
        {
          e.Handled = true;
          await CreateNewFileAsync();
        }
        else if (e.KeyData == (Keys.Control | Keys.Shift | Keys.F))
        {
          e.Handled = true;
          await FindAllAsync();
        }
        else if (e.KeyData == (Keys.Control | Keys.Shift | Keys.I))
        {
          e.Handled = true;
          await ImportFromDirectoryAsync();
        }
        else if (e.KeyData == (Keys.Control | Keys.Shift | Keys.E))
        {
          e.Handled = true;
          await ExportToDirectoryAsync();
        }
        else if (e.KeyData == (Keys.Control | Keys.O))
        {
          e.Handled = true;
          await OpenCustomFileAsync();
        }
      };
      FormClosing += (s, e) =>
      {
        if (!Alive)
        {
          return;
        }
        Alive = false;
        Try.Execute(SaveAll);
        Try.Execute(CloseAll);
        saveTimer.Dispose();
        updatePanelTimer.Dispose();
        Application.Exit();
      };
      TabControl.SelectedIndexChanged += (s, e) =>
      {
        var tab = TabControl.SelectedTab;
        SelectedFileTab = tab != null ? (FileTab)tab.Tag : null;
        SelectedFileTab?.SelectRtb();
      };
    }

    public SharedFileManager SharedFileManager { get; }

    public string AccountFile { get; private set; }

    public string SafePassword { get; private set; }

    public bool HasStarted { get; private set; }

    public string DataDir => GetDataDir(Env.Config.TextEditorDir);

    public string NamesDir => GetNamesDir(Env.Config.TextEditorDir);

    public event Action Started = delegate { };

    public event Action Saving = delegate { };

    public bool TryGetPosition(string file, out RtbPosition position) => Positions.TryGetValue(file, out position);

    public string GetDataFile(string nameOrDataFile)
    {
      return Path.Combine(GetDataDir(Path.GetDirectoryName(Path.GetDirectoryName(nameOrDataFile))), Path.GetFileName(nameOrDataFile));
    }

    public string GetNameFile(string nameOrDataFile)
    {
      return Path.Combine(GetNamesDir(Path.GetDirectoryName(Path.GetDirectoryName(nameOrDataFile))), Path.GetFileName(nameOrDataFile));
    }

    private string GetDataDir(string parentDir)
    {
      return Path.Combine(parentDir, DataDirName);
    }

    private string GetNamesDir(string parentDir)
    {
      return Path.Combine(parentDir, NamesDirName);
    }

    public async void Start()
    {
      await Task.Yield();
      CreatePassword();
      Show();
    }

    public string Decrypt(string file)
    {
      if (WrongPassword)
      {
        throw new InvalidPasswordException();
      }
      return Cipher.Decrypt(file, Password);
    }

    public void Encrypt(string file, string text)
    {
      Cipher.Encrypt(file, text, Password);
    }

    public int ExportToDirectory(string sourceDir, string targetDir)
    {
      var fromNamesDir = GetNamesDir(sourceDir);
      var count = 0;
      Directory.CreateDirectory(targetDir);
      foreach (var file in Directory.EnumerateFiles(fromNamesDir))
      {
        var title = Decrypt(file);
        var text = Decrypt(GetDataFile(file));
        var name = Helper.GetValidFileName(title, '_');
        var targetFile = new FileInfo(Path.Combine(targetDir, name + ".txt"));
        File.WriteAllText(targetFile.FullName, title + Environment.NewLine + text.Replace("\n", Environment.NewLine));
        count++;
      }
      return count;
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

    private void CreatePassword()
    {
      Password = Env.Config.KeyFile == null ? "" : File.ReadAllText(Env.Config.KeyFile.FullName);
      if (Env.Config.AskForPassword || Password == "")
      {
        Password += Helper.GetStringLine("Password", isPassword: true) ?? "";
      }
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
      var file = Helper.GetString("File path");
      if (!string.IsNullOrWhiteSpace(file))
      {
        Helper.RequireExistsFile(file);
        OpenFile(file);
      }
    }

    public async Task CompileTextEditorModeAsync(bool updateHotkeyFile = false)
    {
      var failCount = 0;
      HotkeyFile hotkeyFile = null;
      var hotkeyFilesFound = 0;
      var accountFilesFound = 0;
      var links = new List<FileLink>();
      SharedFileManager.Clear();
      await Task.Run(() =>
      {
        foreach (var file in Directory.EnumerateFiles(NamesDir))
        {
          var title = Decrypt(file);
          if (title == null)
          {
            failCount++;
            continue;
          }
          if (updateHotkeyFile && title.Contains("[hotkeys]"))
          {
            var text = Decrypt(GetDataFile(file));
            hotkeyFile = new HotkeyFile(nameof(TextEditorForm), text);
            hotkeyFilesFound++;
          }
          if (Env.Config.SharedFileRegex.IsMatch(title))
          {
            SharedFileManager.Add(new SharedFile(title, file, GetDataFile(file)));
          }
          if (title.Contains("[accounts]"))
          {
            accountFilesFound++;
            if (AccountFile == null)
            {
              AccountFile = GetDataFile(file);
            }
          }
          if (title.Contains("[safePassword]"))
          {
            SafePassword = Decrypt(GetDataFile(file));
          }
          links.Add(new FileLink { Title = title, File = file });
        }
      });
      if (failCount > 0)
      {
        Env.Notifier.WriteWarning($"Password incorrect. Failed to decrypt {failCount} files.");
        WrongPassword = true;
      }
      if (accountFilesFound > 1)
      {
        Env.Notifier.WriteError("Multiple account files found.");
      }
      else if (accountFilesFound == 0 && !WrongPassword)
      {
        var file = CreateNewFile("[accounts]", "[]");
        AccountFile = GetDataFile(file);
      }
      if (hotkeyFilesFound > 1)
      {
        Env.Notifier.WriteError("Multiple hotkey files found.");
      }
      Env.Parser.UpdateParseAction(nameof(TextEditorForm), parserOutput =>
      {
        var mode = parserOutput.AddMode(new Mode(Env.Config.TextEditorModeName, true));
        foreach (var link in links)
        {
          var file = link.File;
          var title = link.Title;
          var chordText = Env.Config.GetChordText(title) ?? title;
          var chord = Env.Config.DefaultChordReader.CreateChord(new LocatedString(chordText));

          void Action(Combo combo)
          {
            OpenFile(file);
            if (Env.Config.TextEditorDesktopHotkey != Combo.None)
            {
              Env.CreateInjector().Add(Env.Config.TextEditorDesktopHotkey).Run();
            }
          }

          var hotkey = new ModeHotkey(chord, Action, title);
          mode.AddHotkey(hotkey);
        }
      });
      if (hotkeyFile != null)
      {
        Env.Parser.UpdateHotkeyFile(hotkeyFile);
      }
      Env.Parser.Run();
    }

    private async Task ImportFromDirectoryAsync()
    {
      var path = Helper.GetString("Please give a directory from which to import all text files.");
      var count = 0;
      if (!string.IsNullOrWhiteSpace(path))
      {
        var dir = new DirectoryInfo(path);
        foreach (var file in dir.GetFiles("*.txt"))
        {
          var s = File.ReadAllText(file.FullName).Replace("\r\n", "\n");
          var i = s.IndexOf('\n');
          var failed = false;
          if (i == -1)
          {
            failed = true;
          }
          else
          {
            var title = s.Substring(0, i).Trim();
            if (title.Length == 0)
            {
              failed = true;
            }
            else
            {
              var text = s.Substring(i + 1);
              CreateNewFile(title, text);
              count++;
            }
          }
          if (failed)
          {
            Env.Notifier.WriteError($"Cannot import '{file.FullName}', file is in incorrect format.");
          }
        }
        Env.Notifier.Write($"Imported {count} files from '{path}'.");
        await CompileTextEditorModeAsync(true);
      }
    }

    private void ShowWrongPasswordError()
    {
      Env.Notifier.WriteError("Cannot complete action (wrong password).");
    }

    private async Task ExportToDirectoryAsync()
    {
      var path = Helper.GetString("Please give a directory to which to export all files.");
      if (!string.IsNullOrWhiteSpace(path))
      {
        var count = await Task.Run(() => ExportToDirectory(Env.Config.TextEditorDir, path));
        Env.Notifier.Write($"Exported {count} files to '{path}'.");
      }
    }

    private void OpenFile(string file)
    {
      var fileTab = FileTabs.FirstOrDefault(z => string.Equals(z.File, file, StringComparison.OrdinalIgnoreCase));
      if (fileTab == null)
      {
        var tabPage = new TabPage();
        SuspendLayout();
        TabControl.TabPages.Add(tabPage);
        fileTab = new FileTab(file, this, ModeHook, tabPage);
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
        (TabControl.GetLastTabPageInOrder().Tag as FileTab).Close();
      }
    }

    private async Task CreateNewFileAsync()
    {
      var title = Helper.GetString("Title of new file");
      if (!string.IsNullOrWhiteSpace(title))
      {
        var file = CreateNewFile(title.Trim(), "");
        OpenFile(file);
      }
      await CompileTextEditorModeAsync();
    }

    public string CreateNewFile(string title, string text)
    {
      var i = 0;
      while (true)
      {
        var file = Path.Combine(NamesDir, i.ToString());
        if (!File.Exists(file))
        {
          Encrypt(file, title);
          Encrypt(GetDataFile(file), text);
          return file;
        }
        i++;
      }
    }

    public void SaveAll()
    {
      foreach (var fileTab in FileTabs)
      {
        try
        {
          fileTab.Save();
        }
        catch (Exception ex) when (!Helper.IsCriticalException(ex))
        {
          Env.Notifier.WriteError(ex, "Failed to save a TextEditor text file" + Helper.GetBindingsSuffix(fileTab.Title, nameof(fileTab.Title)));
        }
      }
      Saving();
    }

    private async Task FindAllAsync()
    {
      var s = Helper.GetString("Find All", "");
      if (string.IsNullOrEmpty(s))
      {
        return;
      }
      var r = Helper.GetRegex(s, RegexOptions.IgnoreCase);
      var log = await Task.Run(() =>
      {
        var sb = new StringBuilder();
        foreach (var dataFile in Directory.GetFiles(DataDir))
        {
          var text = Decrypt(dataFile);
          var count = r.Matches(text).Count;
          if (count > 0)
          {
            sb.AppendLine($"{Decrypt(GetNameFile(dataFile))}  ({count} match{(count != 1 ? "es" : "")})");
          }
        }
        return sb;
      });
      Helper.ShowSelectableText(log.Length == 0 ? "No matches found!" : log.ToString());
    }

    public void CloseAll()
    {
      foreach (var item in FileTabs.ToArray())
      {
        item.Close();
      }
    }

    private class FileLink
    {
      public string File { get; set; }
      public string Title { get; set; }
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

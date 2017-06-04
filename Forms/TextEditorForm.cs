using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using InputMaster.Hooks;
using InputMaster.Parsers;

namespace InputMaster.Forms
{
  internal sealed class TextEditorForm : ThemeForm
  {
    private readonly ModeHook ModeHook;
    private readonly List<FileTab> FileTabs = new List<FileTab>();
    private readonly Dictionary<string, RtbPosition> Positions = new Dictionary<string, RtbPosition>();
    private readonly MyState State;
    private readonly TabControlPlus TabControl;
    private bool Alive = true;
    private string Password;
    private bool WrongPassword;

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
      Text = Config.TextEditorWindowTitle;
      WindowState = FormWindowState.Normal;
      StartPosition = FormStartPosition.Manual;
      Left = Screen.PrimaryScreen.WorkingArea.Left;
      Top = Screen.PrimaryScreen.WorkingArea.Top;
      Width = Screen.PrimaryScreen.WorkingArea.Width;
      Height = Screen.PrimaryScreen.WorkingArea.Height;
      ResumeLayout(false);
      var updatePanelTimer = new Timer
      {
        Interval = (int)Config.UpdatePanelInterval.TotalMilliseconds,
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
        Interval = (int)Config.SaveTimerInterval.TotalMilliseconds,
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
        DataDir.Create();
        NamesDir.Create();
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
    }

    public SharedFileManager SharedFileManager { get; }

    public FileInfo AccountFile { get; private set; }

    public string SafePassword { get; private set; }

    public bool HasStarted { get; private set; }

    private DirectoryInfo DataDir => GetDataDir(Config.TextEditorDir);

    private DirectoryInfo NamesDir => GetNamesDir(Config.TextEditorDir);

    public event Action Started = delegate { };

    public event Action Saving = delegate { };

    private static FileInfo GetDataFile(FileInfo file)
    {
      return new FileInfo(Path.Combine(GetDataDir(file.Directory.Parent).FullName, file.Name));
    }

    private static FileInfo GetNameFile(FileInfo file)
    {
      return new FileInfo(Path.Combine(GetNamesDir(file.Directory.Parent).FullName, file.Name));
    }

    private static DirectoryInfo GetDataDir(DirectoryInfo parentDir)
    {
      return new DirectoryInfo(Path.Combine(parentDir.FullName, "data"));
    }

    private static DirectoryInfo GetNamesDir(DirectoryInfo parentDir)
    {
      return new DirectoryInfo(Path.Combine(parentDir.FullName, "names"));
    }

    public async void Start()
    {
      await Task.Yield();
      CreatePassword();
      Show();
    }

    public string Decrypt(FileInfo file)
    {
      if (WrongPassword)
      {
        throw new InvalidPasswordException();
      }
      return Cipher.Decrypt(file, Password);
    }

    public void Encrypt(FileInfo file, string text)
    {
      Cipher.Encrypt(file, text, Password);
    }

    public int ExportToDirectory(DirectoryInfo sourceDir, DirectoryInfo targetDir)
    {
      var fromNamesDir = GetNamesDir(sourceDir);
      var count = 0;
      targetDir.Create();
      foreach (var file in fromNamesDir.GetFiles())
      {
        var title = Decrypt(file);
        var text = Decrypt(GetDataFile(file));
        var name = Helper.GetValidFileName(title, '_');
        var targetFile = new FileInfo(Path.Combine(targetDir.FullName, name + ".txt"));
        File.WriteAllText(targetFile.FullName, title + Environment.NewLine + text.Replace("\n", Environment.NewLine));
        count++;
      }
      return count;
    }

    private void CreatePassword()
    {
      Password = Config.KeyFile == null ? "" : File.ReadAllText(Config.KeyFile.FullName);
      if (Config.AskForPassword || Password == "")
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
      var s = Helper.GetString("File path");
      if (!string.IsNullOrWhiteSpace(s))
      {
        var file = new FileInfo(s);
        Helper.RequireExists(file);
        OpenFile(file);
      }
    }

    private async Task CompileTextEditorModeAsync(bool updateHotkeyFile = false)
    {
      var failCount = 0;
      HotkeyFile hotkeyFile = null;
      var hotkeyFilesFound = 0;
      var accountFilesFound = 0;
      var links = new List<FileLink>();
      SharedFileManager.Clear();
      await Task.Run(() =>
      {
        foreach (var file in NamesDir.GetFiles())
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
          if (Config.SharedFileRegex.IsMatch(title))
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
        var mode = parserOutput.AddMode(new Mode(Config.TextEditorModeName, true));
        foreach (var link in links)
        {
          var file = link.File;
          var title = link.Title;
          var chordText = Config.GetChordText(title) ?? title;
          var chord = Config.DefaultChordReader.CreateChord(new LocatedString(chordText));

          void Action(Combo combo)
          {
            OpenFile(file);
            if (Config.TextEditorDesktopHotkey != Combo.None)
            {
              Env.CreateInjector().Add(Config.TextEditorDesktopHotkey).Run();
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
        var count = await Task.Run(() => ExportToDirectory(Config.TextEditorDir, new DirectoryInfo(path)));
        Env.Notifier.Write($"Exported {count} files to '{path}'.");
      }
    }

    private void OpenFile(FileInfo file)
    {
      var fileTab = FileTabs.FirstOrDefault(z => z.File.FullName.ToLower() == file.FullName.ToLower());
      if (fileTab == null)
      {
        FileTabs.Add(new FileTab(file, this));
      }
      else
      {
        fileTab.Select();
      }
      if (FileTabs.Count > Config.MaxTextEditorTabs)
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

    public FileInfo CreateNewFile(string title, string text)
    {
      var i = 0;
      while (true)
      {
        var file = new FileInfo(Path.Combine(NamesDir.FullName, i.ToString()));
        if (!file.Exists)
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
        foreach (var dataFile in DataDir.GetFiles())
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

    private class FileTab : IDisposable
    {
      private readonly TextEditorForm Form;
      private readonly TabPage TabPage;
      private readonly RichTextBoxPlus Panel;
      private readonly RichTextBoxPlus Rtb;
      private readonly List<IDisposable> Disposables = new List<IDisposable>();
      private bool Changed;
      private bool ShouldUpdatePanel;

      public FileTab(FileInfo file, TextEditorForm form)
      {
        File = file;
        Form = form;
        Title = Form.Decrypt(File);
        var text = Form.Decrypt(GetDataFile(File));
        Form.SuspendLayout();
        TabPage = new TabPage(Title) { Tag = this };
        Disposables.Add(TabPage);
        Form.TabControl.TabPages.Add(TabPage);

        var splitContainer = new SplitContainer
        {
          Dock = DockStyle.Fill,
          Parent = TabPage,
          SplitterWidth = 1,
          SplitterDistance = Config.SplitterDistance,
          TabStop = false
        };

        Panel = new RichTextBoxPlus
        {
          Dock = DockStyle.Fill,
          Parent = splitContainer.Panel1,
          BorderStyle = BorderStyle.None,
          ReadOnly = true,
          WordWrap = false,
          TabStop = false
        };

        Rtb = new RichTextBoxPlus
        {
          Dock = DockStyle.Fill,
          Parent = splitContainer.Panel2,
          AcceptsTab = true,
          BorderStyle = BorderStyle.None,
          Text = text
        };

        UpdatePanel(true);
        if (Form.Positions.TryGetValue(file.FullName.ToLower(), out var rtbPosition))
        {
          Rtb.LoadPosition(rtbPosition);
        }

        Form.TabControl.SelectedIndexChanged += Form_TabControl_SelectedIndexChanged;
        if (Form.TabControl.SelectedTab == TabPage)
        {
          Rtb.Select();
        }
        else
        {
          Form.TabControl.SelectTab(TabPage);
        }
        Changed = false;
        Form.ResumeLayout();

        Rtb.TextChanged += (s, e) =>
        {
          Changed = true;
          ShouldUpdatePanel = true;
        };

        Rtb.KeyDown += async (s, e) =>
        {
          if (e.Handled)
          {
            return;
          }
          switch (e.KeyData)
          {
            case Keys.F6:
              e.Handled = true;
              OpenMode(MoveSelectedLines);
              break;
            case Keys.F6 | Keys.Shift:
              e.Handled = true;
              OpenMode(MoveCaretToSection);
              break;
            case Keys.Control | Keys.PageUp:
              e.Handled = true;
              MoveCaretToTopOfSection();
              break;
            case Keys.Control | Keys.PageDown:
              e.Handled = true;
              MoveCaretToEndOfSection();
              break;
            case Keys.Control | Keys.W:
              e.Handled = true;
              Close();
              break;
            case Keys.F2:
              e.Handled = true;
              await RenameAsync();
              break;
            case Keys.F5:
              e.Handled = true;
              Compile();
              break;
          }
        };

        Panel.MouseClick += (s, e) =>
        {
          Rtb.Focus();
          MoveCaretToSection(Panel.GetCurrentLine());
        };
      }

      public FileInfo File { get; }
      public string Title { get; private set; }

      /// <summary>
      /// Returns the position of the special section character, or -1 on failure.
      /// </summary>
      private static int GetStartOfSection(string padded, string section)
      {
        var s = $"\n{Config.TextEditorSectionIdentifier} {section}\n";
        var i = padded.IndexOf(s, StringComparison.Ordinal);
        return i == -1 ? -1 : i + 1;
      }

      /// <summary>
      /// Returns an index i such that on position i - 1 the last non-whitespace character of the section is found, or -1 on failure.
      /// </summary>
      private static int GetAppendIndex(string padded, string section)
      {
        var i = GetStartOfSection(padded, section);
        return i == -1 ? -1 : GetAppendIndex(padded, i);
      }

      /// <summary>
      /// Returns an index i such that on position i - 1 the last non-whitespace character of the section containing the given index is found, or -1 on failure.
      /// </summary>
      private static int GetAppendIndex(string padded, int index)
      {
        var i = padded.IndexOf($"\n{Config.TextEditorSectionIdentifier} ", index, StringComparison.Ordinal);
        i = i == -1 ? padded.Length : i;
        for (; i >= 2; i--)
        {
          if (padded[i - 1] != ' ' && padded[i - 1] != '\n')
          {
            return i;
          }
        }
        return -1;
      }

      private static string GetPaddedText(string text)
      {
        return "\n" + text + "\n";
      }

      public void Select()
      {
        Form.TabControl.SelectTab(TabPage);
      }

      public void UpdatePanel(bool force = false)
      {
        if (!force && !ShouldUpdatePanel)
        {
          return;
        }
        ShouldUpdatePanel = false;
        var sb = new StringBuilder();
        foreach (var line in Rtb.Lines)
        {
          if (line.Length >= 3 && line.StartsWith(Config.TextEditorSectionIdentifier + " "))
          {
            var s = line.Substring(2);
            if (!string.IsNullOrWhiteSpace(s))
            {
              sb.AppendLine(s);
            }
          }
        }
        var text = sb.ToString();
        if (Panel.Text != text)
        {
          Panel.Text = text;
        }
      }

      public void Save()
      {
        if (Changed)
        {
          Form.Encrypt(GetDataFile(File), Rtb.Text);
          Changed = false;
        }
      }

      public void Close()
      {
        Save();
        Form.Positions[File.FullName.ToLower()] = Rtb.GetPosition();
        Form.State.Changed = true;
        Form.FileTabs.Remove(this);
        Form.TabControl.SelectedIndexChanged -= Form_TabControl_SelectedIndexChanged;
        Form.TabControl.TabPages.Remove(TabPage);
        Dispose();
      }

      public void Dispose()
      {
        foreach (var disposable in Disposables)
        {
          disposable.Dispose();
        }
      }

      private void Form_TabControl_SelectedIndexChanged(object sender, EventArgs e)
      {
        if (Form.TabControl.SelectedTab == TabPage)
        {
          Rtb.Select();
        }
      }

      private void MoveCaretToSection(string section)
      {
        MoveCaretToSection(GetPaddedText(Rtb.Text), section);
      }

      private void MoveSelectedLines(string section)
      {
        Rtb.SuspendPainting();
        var padded = GetPaddedText(Rtb.Text);
        var i = Rtb.SelectionStart + 1;
        var j = i + Math.Max(0, Rtb.SelectionLength - 1);
        var cutIndex = Helper.GetLineStart(padded, i);
        var k = Helper.GetLineEnd(padded, j);
        var copyLength = k - cutIndex;
        var cutLength = copyLength + (k < padded.Length ? 1 : 0);
        var copiedText = padded.Substring(cutIndex, copyLength);
        Rtb.Select(cutIndex - 1, cutLength);
        Rtb.SelectedText = "";
        padded = GetPaddedText(Rtb.Text);
        var insertIndex = GetAppendIndex(padded, section);
        if (insertIndex == -1)
        {
          Rtb.SelectedText = copiedText + "\n";
        }
        else
        {
          Rtb.Select(insertIndex - 1, 0);
          var lengthBefore = Rtb.TextLength;
          Rtb.SelectedText = "\n" + copiedText;
          Rtb.Select(cutIndex - 1 + (insertIndex < cutIndex ? Rtb.TextLength - lengthBefore : 0), 0);
        }
        Rtb.ResumePainting();
      }

      private void MoveCaretToSection(string padded, string section)
      {
        var index = GetStartOfSection(padded, section);
        if (index != -1)
        {
          Rtb.Select(index - 1, 0);
          Rtb.ScrollToCaret();
          var i = GetAppendIndex(padded, index);
          Debug.Assert(i != -1);
          Rtb.Select(i - 1, 0);
        }
      }

      private void MoveCaretToEndOfSection()
      {
        var padded = GetPaddedText(Rtb.Text);
        var i = GetAppendIndex(padded, Rtb.SelectionStart + 1);
        if (i != -1)
        {
          Rtb.Select(i - 1, 0);
        }
      }

      private void MoveCaretToTopOfSection()
      {
        var padded = GetPaddedText(Rtb.Text);
        var i = padded.LastIndexOf($"\n{Config.TextEditorSectionIdentifier} ", Rtb.SelectionStart + 1, StringComparison.Ordinal);
        if (i == -1)
        {
          Rtb.Select(0, 0);
        }
        else
        {
          i++;
          Rtb.Select(i - 1, 0);
          Rtb.ScrollToCaret();
          var j = Helper.GetLineEnd(padded, i) + 1;
          if (j >= padded.Length)
          {
            j = padded.Length - 1;
          }
          Rtb.Select(j - 1, 0);
        }
      }

      private void OpenMode(Action<string> action)
      {
        var sections = Panel.Lines.Where(z => !string.IsNullOrEmpty(z));
        var mode = new Mode(Title, true);
        foreach (var section in sections)
        {
          var chordText = Helper.GetChordText(section);
          if (chordText != null)
          {
            var chord = Config.DefaultChordReader.CreateChord(new LocatedString(chordText));
            mode.AddHotkey(new ModeHotkey(chord, combo => action(section), section));
          }
        }
        Form.ModeHook.EnterMode(mode);
      }

      private async Task RenameAsync()
      {
        var newTitle = Helper.GetString("New Name", Title);
        if (!string.IsNullOrEmpty(newTitle) && newTitle != Title)
        {
          Title = newTitle;
          TabPage.Text = Title;
          Form.Encrypt(File, Title);
          await Form.CompileTextEditorModeAsync();
        }
      }

      private void Compile()
      {
        if (Title.Contains("[hotkeys]"))
        {
          Env.Parser.UpdateHotkeyFile(new HotkeyFile(nameof(TextEditorForm), Rtb.Text));
          Env.Parser.Run();
        }
      }
    }

    private class FileLink
    {
      public FileInfo File { get; set; }
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

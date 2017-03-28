using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using InputMaster.Parsers;
using InputMaster.Hooks;
using System.Threading.Tasks;
using System.Threading;
using Timer = System.Windows.Forms.Timer;

namespace InputMaster.Forms
{
  class TextEditorForm : ThemeForm, IDisposable
  {
    private readonly ModeHook ModeHook;
    private readonly List<FileTab> FileTabs = new List<FileTab>();
    private readonly Dictionary<string, RtbPosition> Positions = new Dictionary<string, RtbPosition>();
    private readonly MyState State;
    private readonly TabControlPlus TabControl;
    private readonly IContainer Components;
    private readonly Parser Parser;
    private bool Alive = true;
    private string PassPhrase;
    private bool WrongPassPhrase;

    public TextEditorForm(Brain brain, ModeHook modeHook, Parser parser)
    {
      HideFirstInstant();
      ModeHook = Helper.ForbidNull(modeHook, nameof(modeHook));
      Parser = Helper.ForbidNull(parser, nameof(parser));
      SuspendLayout();
      Components = new Container();
      TabControl = new TabControlPlus
      {
        Dock = DockStyle.Fill,
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
      var saveTimer = new Timer
      {
        Enabled = true,
        Interval = (int)Config.SaveTimerInterval.TotalMilliseconds
      };
      Components.Add(saveTimer);
      State = new MyState(this);

      saveTimer.Tick += (s, e) =>
      {
        SaveAll();
      };

      Shown += async (s, e) =>
      {
        DataDir.Create();
        NamesDir.Create();
        State.Load();
        await CompileTextEditorModeAsync(true);
      };

      KeyDown += (s, e) =>
      {
        if (e.KeyData == (Keys.Control | Keys.N))
        {
          Run(CreateNewFileAsync);
          e.Handled = true;
        }
        else if (e.KeyData == (Keys.Control | Keys.Shift | Keys.F))
        {
          Run(FindAllAsync);
          e.Handled = true;
        }
        else if (e.KeyData == (Keys.Control | Keys.Shift | Keys.I))
        {
          e.Handled = true;
          Run(ImportFromDirectoryAsync);
        }
        else if (e.KeyData == (Keys.Control | Keys.Shift | Keys.E))
        {
          e.Handled = true;
          Run(ExportToDirectoryAsync);
        }
      };

      brain.Exiting += () =>
      {
        Alive = false;
        Try.Execute(SaveAll);
        Try.Execute(CloseAll);
        Try.Execute(State.Save);
        Try.Execute(Close);
      };

      FormClosing += (s, e) =>
      {
        if (Alive)
        {
          e.Cancel = true;
          Env.Notifier.RequestExit();
        }
      };
    }

    public FileInfo PasswordFile { get; private set; }

    public void Start()
    {
      Run(() =>
      {
        if (Config.UseCipher)
        {
          CreatePassPhrase();
        }
        Show();
      });
    }

    private void CreatePassPhrase()
    {
      PassPhrase = Config.KeyFile == null ? "" : File.ReadAllText(Config.KeyFile.FullName);
      if (Config.AskForPassword || PassPhrase == "")
      {
        PassPhrase += Helper.GetStringLine("Password", isPassword: true) ?? "";
      }
    }

    private void HideFirstInstant()
    {
      Opacity = 0;

      var timeout = new Timeout();

      Shown += (s, e) =>
      {
        timeout.Start(TimeSpan.FromSeconds(1));
      };

      timeout.Elapsed += () =>
      {
        Opacity = 1;
        timeout.Dispose();
        timeout = null;
      };

      Disposed += (s, e) =>
      {
        timeout?.Dispose();
      };
    }

    private FileTab CurrentFileTab
    {
      get
      {
        var i = TabControl.SelectedIndex;
        if (TabControl.TabCount == 0 || i == -1)
        {
          return null;
        }
        else
        {
          return FileTabs[i];
        }
      }
    }

    private DirectoryInfo DataDir { get { return GetDataDir(Config.TextEditorDir); } }

    private DirectoryInfo NamesDir { get { return GetNamesDir(Config.TextEditorDir); } }

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

    public new void Dispose()
    {
      Components.Dispose();
      base.Dispose();
    }

    private async Task CompileTextEditorModeAsync(bool updateHotkeyFile = false)
    {
      var failCount = 0;
      HotkeyFile hotkeyFile = null;
      var multipleHotkeyFilesFound = false;
      PasswordFile = null;
      var multiplePasswordsFilesFound = false;
      List<FileLink> links = new List<FileLink>();
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
          links.Add(new FileLink { Title = title, File = file });
          if (updateHotkeyFile && title.Contains("[hotkeys]"))
          {
            if (hotkeyFile != null)
            {
              multipleHotkeyFilesFound = true;
            }
            else
            {
              var text = Decrypt(GetDataFile(file));
              hotkeyFile = new HotkeyFile(nameof(TextEditorForm), text);
            }
          }
          if (title.Contains("[passwords]"))
          {
            if (PasswordFile != null)
            {
              multiplePasswordsFilesFound = true;
            }
            else
            {
              PasswordFile = GetDataFile(file);
            }
          }
        }
      });
      if (failCount > 0)
      {
        Env.Notifier.WriteWarning($"Password incorrect. Failed to decrypt {failCount} files.");
        WrongPassPhrase = true;
      }
      if (multiplePasswordsFilesFound)
      {
        Env.Notifier.WriteWarning("Multiple passwords files found. Only one is supported.");
      }
      if (multipleHotkeyFilesFound)
      {
        Env.Notifier.WriteWarning("Multiple hotkey files found. Only one is supported.");
      }
      Parser.UpdateParseAction(nameof(TextEditorForm), (parserOutput) =>
      {
        var mode = parserOutput.AddMode(new Mode(Config.TextEditorModeName, true));
        foreach (var link in links)
        {
          var file = link.File;
          var title = link.Title;
          var chordText = Helper.GetChordText(title);
          if (chordText == null)
          {
            chordText = title;
          }
          var chord = Config.DefaultChordReader.CreateChord(new LocatedString(chordText));
          Action<Combo> action = (combo) =>
          {
            OpenFile(file);
            if (Config.TextEditorDesktopHotkey != Combo.None)
            {
              Env.CreateInjector().Add(Config.TextEditorDesktopHotkey).Run();
            }
          };
          var hotkey = new ModeHotkey(chord, action, title);
          mode.AddHotkey(hotkey);
        }
      });
      if (hotkeyFile != null)
      {
        Parser.UpdateHotkeyFile(hotkeyFile);
      }
      Parser.Parse();
    }

    private async Task ImportFromDirectoryAsync()
    {
      if (WrongPassPhrase)
      {
        ShowWrongPasswordError();
        return;
      }
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

    private void Run(Func<Task> action)
    {
      Task.Factory.StartNew(async () =>
      {
        try
        {
          await action();
        }
        catch (Exception ex)
        {
          Helper.HandleFatalException(ex);
        }
      }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private void Run(Action action)
    {
      Task.Factory.StartNew(() =>
      {
        try
        {
          action();
        }
        catch (Exception ex)
        {
          Helper.HandleFatalException(ex);
        }
      }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.FromCurrentSynchronizationContext());
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
      if (WrongPassPhrase)
      {
        ShowWrongPasswordError();
        return;
      }
      var title = Helper.GetString("Title of new file");
      if (!string.IsNullOrWhiteSpace(title))
      {
        var file = CreateNewFile(title.Trim(), "");
        OpenFile(file);
      }
      await CompileTextEditorModeAsync(false);
    }

    private FileInfo CreateNewFile(string title, string text)
    {
      int i = 0;
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

    private void SaveAll()
    {
      foreach (var fileTab in FileTabs)
      {
        Helper.TryCatchLog(fileTab.Save);
      }
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

    private void CloseAll()
    {
      foreach (var item in FileTabs.ToArray())
      {
        item.Close();
      }
    }

    private void Encrypt(FileInfo file, string text)
    {
      if (Config.UseCipher)
      {
        Cipher.Encrypt(file, text, PassPhrase);
      }
      else
      {
        File.WriteAllText(file.FullName, text);
      }
    }

    public string Decrypt(FileInfo file)
    {
      if (Config.UseCipher)
      {
        return Cipher.Decrypt(file, PassPhrase);
      }
      else
      {
        return File.ReadAllText(file.FullName);
      }
    }

    private class MyState : State
    {
      private TextEditorForm Form;

      public MyState(TextEditorForm form) : base(nameof(TextEditorForm))
      {
        Form = form;
      }

      protected override void Load(BinaryReader reader)
      {
        var n = reader.ReadInt32();
        for (int i = 0; i < n; i++)
        {
          var key = reader.ReadString();
          var position = new RtbPosition(reader);
          Form.Positions.Add(key, position);
        }
      }

      protected override void Save(BinaryWriter writer)
      {
        writer.Write(Form.Positions.Count);
        foreach (var pair in Form.Positions)
        {
          writer.Write(pair.Key);
          pair.Value.Write(writer);
        }
      }
    }

    private class FileTab : IDisposable
    {
      private readonly TextEditorForm Form;
      private readonly TabPage TabPage;
      private readonly RichTextBoxPlus Panel;
      private readonly RichTextBoxPlus Rtb;
      private readonly List<IDisposable> Disposables = new List<IDisposable>();
      private string Title;
      private bool Changed;

      public FileTab(FileInfo file, TextEditorForm form)
      {
        File = file;
        Form = form;
        Title = Form.Decrypt(File);
        var text = Form.Decrypt(GetDataFile(File));
        Form.SuspendLayout();
        TabPage = new TabPage(Title);
        TabPage.Tag = this;
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

        UpdatePanel();
        RtbPosition rtbPosition;
        if (Form.Positions.TryGetValue(file.FullName.ToLower(), out rtbPosition))
        {
          Rtb.LoadPosition(rtbPosition);
        }
        var updatePanelTimeout = new Timeout();
        updatePanelTimeout.Elapsed += UpdatePanel;
        Disposables.Add(updatePanelTimeout);

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

        Rtb.SelectionChanged += (s, e) =>
        {
          Form.State.Changed = true;
        };

        Rtb.TextChanged += (s, e) =>
        {
          Changed = true;
          updatePanelTimeout.Start(Config.UpdatePanelDelay);
        };

        Rtb.KeyDown += (s, e) =>
        {
          if (e.Handled)
          {
            return;
          }
          var handled = true;
          switch (e.KeyData)
          {
            case Keys.F6:
              OpenMode(MoveSelectedLines);
              break;
            case Keys.F6 | Keys.Shift:
              OpenMode(MoveCaretToSection);
              break;
            case Keys.Control | Keys.PageUp:
              MoveCaretToTopOfSection();
              break;
            case Keys.Control | Keys.PageDown:
              MoveCaretToEndOfSection();
              break;
            case Keys.Control | Keys.W:
              Close();
              break;
            case Keys.F2:
              Rename();
              break;
            case Keys.F5:
              Compile();
              break;
            default:
              handled = false;
              break;
          }
          e.Handled = handled;
        };

        Panel.MouseClick += (s, e) =>
        {
          Rtb.Focus();
          MoveCaretToSection(Panel.GetCurrentLine());
        };
      }

      public FileInfo File { get; }

      /// <summary>
      /// Returns the position of the special section character, or -1 on failure.
      /// </summary>
      private static int GetStartOfSection(string padded, string section)
      {
        var s = $"\n{Config.SectionChar} {section}\n";
        var i = padded.IndexOf(s);
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
        var i = padded.IndexOf($"\n{Config.SectionChar} ", index);
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
        var i = padded.LastIndexOf($"\n{Config.SectionChar} ", Rtb.SelectionStart + 1);
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
            mode.AddHotkey(new ModeHotkey(chord, (combo) => { action(section); }, section));
          }
        }
        Form.ModeHook.EnterMode(mode);
      }

      private void Rename()
      {
        var newTitle = Helper.GetString("New Name", Title);
        if (!string.IsNullOrEmpty(newTitle) && newTitle != Title)
        {
          Title = newTitle;
          TabPage.Text = Title;
          Form.Encrypt(File, Title);
          Form.Run(async () => { await Form.CompileTextEditorModeAsync(false); });
        }
      }

      private void UpdatePanel()
      {
        var sb = new StringBuilder();
        foreach (var line in Rtb.Lines)
        {
          if (line.Length >= 3 && line.StartsWith(Config.SectionChar + " "))
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

      private void Compile()
      {
        if (Title.Contains("[hotkeys]"))
        {
          Form.Parser.UpdateHotkeyFile(new HotkeyFile(nameof(TextEditorForm), Rtb.Text));
          Form.Parser.Parse();
        }
      }
    }

    private class FileLink
    {
      public FileInfo File { get; set; }
      public string Title { get; set; }
    }
  }
}

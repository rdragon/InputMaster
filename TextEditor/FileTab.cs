using InputMaster.Forms;
using InputMaster.Hooks;
using InputMaster.Parsers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InputMaster.TextEditor
{
  internal class FileTab
  {
    private readonly TextEditorForm TextEditorForm;
    private readonly ModeHook ModeHook;
    private readonly FileManager FileManager;
    private RichTextBoxPlus Panel;
    private RichTextBoxPlus Rtb;
    private bool Changed;
    private bool ShouldUpdatePanel;

    public FileTab(string file, TextEditorForm textEditorForm, ModeHook modeHook, FileManager fileManager, TabPage tabPage)
    {
      File = file;
      TextEditorForm = textEditorForm;
      FileManager = fileManager;
      TabPage = tabPage;
      ModeHook = modeHook;
    }

    public string File { get; }
    public TabPage TabPage { get; }
    public string Title { get; private set; }

    public async Task InitializeAsync()
    {
      Title = await Env.Cipher.DecryptAsync(File);
      var text = await Env.Cipher.DecryptAsync(FileManager.GetDataFile(File));
      TabPage.Text = Title;
      TabPage.Tag = this;
      var splitContainer = new SplitContainer
      {
        Dock = DockStyle.Fill,
        Parent = TabPage,
        SplitterWidth = 1,
        SplitterDistance = Env.Config.SplitterDistance,
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
      if (TextEditorForm.TryGetPosition(File, out var rtbPosition))
      {
        Rtb.LoadPosition(rtbPosition);
      }

      Changed = false;

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
            await CloseAsync();
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

    /// <summary>
    /// Returns the position of the special section character, or -1 on failure.
    /// </summary>
    private static int GetStartOfSection(string padded, string section)
    {
      var s = $"\n{Constants.TextEditorSectionIdentifier} {section}\n";
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
      var i = padded.IndexOf($"\n{Constants.TextEditorSectionIdentifier} ", index, StringComparison.Ordinal);
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
        if (line.Length >= 3 && line.StartsWith(Constants.TextEditorSectionIdentifier + " "))
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

    public Task SaveAsync()
    {
      if (!Changed)
      {
        return Task.CompletedTask;
      }
      Changed = false;
      return Env.Cipher.EncryptAsync(FileManager.GetDataFile(File), Rtb.Text);
    }

    public async Task CloseAsync()
    {
      await SaveAsync();
      TextEditorForm.UpdatePosition(File, Rtb.GetPosition());
      TextEditorForm.RemoveFileTab(this);
      TabPage.Dispose();
    }

    public void SelectRtb()
    {
      Rtb.Select();
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
      var i = padded.LastIndexOf($"\n{Constants.TextEditorSectionIdentifier} ", Rtb.SelectionStart + 1, StringComparison.Ordinal);
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
          var chord = Env.Config.DefaultChordReader.CreateChord(new LocatedString(chordText));
          mode.AddHotkey(new ModeHotkey(chord, combo => action(section), section));
        }
      }
      ModeHook.EnterMode(mode);
    }

    private async Task RenameAsync()
    {
      var newTitle = Helper.GetString("New Name", Title);
      if (!string.IsNullOrEmpty(newTitle) && newTitle != Title)
      {
        Title = newTitle;
        TabPage.Text = Title;
        await Env.Cipher.EncryptAsync(File, Title);
        await FileManager.CompileTextEditorModeAsync();
      }
    }

    private void Compile()
    {
      if (Title.Contains(Constants.HotkeyFileTag))
      {
        Env.Parser.UpdateHotkeyFile(new HotkeyFile(nameof(TextEditor.TextEditorForm), Rtb.Text));
        Env.Parser.Run();
      }
    }
  }
}

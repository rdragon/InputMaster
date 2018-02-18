using InputMaster.Forms;
using InputMaster.Parsers;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InputMaster.TextEditor
{
  public class FileTab
  {
    public string Name { get; }
    public TabPage TabPage { get; }
    private readonly TextEditorForm _textEditorForm;
    private readonly FileManager _fileManager;
    private RichTextBoxPlus _panel;
    private RichTextBoxPlus _rtb;
    private bool _changed;
    private bool _shouldUpdatePanel;

    public FileTab(string name, TextEditorForm textEditorForm, FileManager fileManager, TabPage tabPage)
    {
      Name = name;
      _textEditorForm = textEditorForm;
      _fileManager = fileManager;
      TabPage = tabPage;
    }

    public string File => FileManager.GetFile(Name);
    public string Title => _fileManager.GetTitle(Name);
    public string Text => _rtb.Text;

    public async Task InitializeAsync()
    {
      var text = new TitleTextPair(await Env.Cipher.DecryptFileAsync(File)).Text;
      TabPage.Text = Helper.StripTags(Title);
      TabPage.Tag = this;
      var splitContainer = new SplitContainer
      {
        Dock = DockStyle.Fill,
        Parent = TabPage,
        SplitterWidth = 1,
        SplitterDistance = Env.Config.SplitterDistance,
        TabStop = false
      };

      _panel = new RichTextBoxPlus
      {
        Dock = DockStyle.Fill,
        Parent = splitContainer.Panel1,
        BorderStyle = BorderStyle.None,
        ReadOnly = true,
        WordWrap = false,
        TabStop = false
      };

      _rtb = new RichTextBoxPlus
      {
        Dock = DockStyle.Fill,
        Parent = splitContainer.Panel2,
        AcceptsTab = true,
        BorderStyle = BorderStyle.None,
        ReadOnly = Env.FlagManager.HasFlag("ReadOnly") || Program.ReadOnly,
        Text = text
      };

      UpdatePanel(true);
      if (_textEditorForm.TryGetPosition(Name, out var rtbPosition))
        _rtb.LoadPosition(rtbPosition);

      _changed = false;

      _rtb.TextChanged += (s, e) =>
      {
        _changed = true;
        _shouldUpdatePanel = true;
      };

      _rtb.KeyDown += async (s, e) =>
      {
        if (e.Handled)
          return;
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
            await SaveAsync();
            Close();
            break;
          case Keys.F2:
            e.Handled = true;
            await Rename();
            break;
          case Keys.F5:
            e.Handled = true;
            Compile();
            break;
        }
      };

      _panel.MouseClick += (s, e) =>
      {
        _rtb.Focus();
        MoveCaretToSection(_panel.GetCurrentLine());
      };

      Env.FlagManager.FlagsChanged += FlagsChanged;
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
    /// Returns an index i such that on position i - 1 the last non-whitespace character of the section containing the given index is found,
    /// or -1 on failure.
    /// </summary>
    private static int GetAppendIndex(string padded, int index)
    {
      var i = padded.IndexOf($"\n{Constants.TextEditorSectionIdentifier} ", index, StringComparison.Ordinal);
      i = i == -1 ? padded.Length : i;
      for (; i >= 2; i--)
        if (padded[i - 1] != ' ' && padded[i - 1] != '\n')
          return i;
      return -1;
    }

    private static string GetPaddedText(string text)
    {
      return "\n" + text + "\n";
    }

    public void UpdatePanel(bool force = false)
    {
      if (!force && !_shouldUpdatePanel)
        return;
      _shouldUpdatePanel = false;
      var sb = new StringBuilder();
      foreach (var line in _rtb.Lines)
      {
        if (line.Length >= 3 && line.StartsWith(Constants.TextEditorSectionIdentifier + " "))
        {
          var s = line.Substring(2);
          if (!string.IsNullOrWhiteSpace(s))
            sb.Append($"{s}\n");
        }
      }
      var text = sb.ToString();
      if (_panel.Text != text)
        _panel.Text = text;
    }

    public async Task SaveAsync()
    {
      _textEditorForm.UpdatePosition(Name, _rtb.GetPosition());
      if (!_changed)
        return;
      await Env.Cipher.EncryptToFileAsync(File, new TitleTextPair(Title, Text).ToString());
      _changed = false;
    }

    public async Task SaveAndCloseAsync()
    {
      await SaveAsync();
      Close();
    }

    private void Delete()
    {
      _changed = false;
      _fileManager.DeleteFile(Name);
      _textEditorForm.RemovePosition(Name);
      Close();
    }

    public void Close()
    {
      Env.FlagManager.FlagsChanged -= FlagsChanged;
      _textEditorForm.RemoveFileTab(this);
      TabPage.Dispose();
    }

    public void SelectRtb()
    {
      _rtb.Select();
    }

    private void MoveCaretToSection(string section)
    {
      MoveCaretToSection(GetPaddedText(Text), section);
    }

    private void MoveSelectedLines(string section)
    {
      _rtb.SuspendPainting();
      var padded = GetPaddedText(Text);
      var i = _rtb.SelectionStart + 1;
      var j = i + Math.Max(0, _rtb.SelectionLength - 1);
      var cutIndex = Helper.GetLineStart(padded, i);
      var k = Helper.GetLineEnd(padded, j);
      var copyLength = k - cutIndex;
      var cutLength = copyLength + (k < padded.Length ? 1 : 0);
      var copiedText = padded.Substring(cutIndex, copyLength);
      _rtb.Select(cutIndex - 1, cutLength);
      _rtb.SelectedText = "";
      padded = GetPaddedText(Text);
      var insertIndex = GetAppendIndex(padded, section);
      if (insertIndex == -1)
        _rtb.SelectedText = copiedText + "\n";
      else
      {
        _rtb.Select(insertIndex - 1, 0);
        var lengthBefore = _rtb.TextLength;
        _rtb.SelectedText = "\n" + copiedText;
        _rtb.Select(cutIndex - 1 + (insertIndex < cutIndex ? _rtb.TextLength - lengthBefore : 0), 0);
      }
      _rtb.ResumePainting();
    }

    private void MoveCaretToSection(string padded, string section)
    {
      var index = GetStartOfSection(padded, section);
      if (index != -1)
      {
        _rtb.Select(index - 1, 0);
        _rtb.ScrollToCaret();
        var i = GetAppendIndex(padded, index);
        Helper.RequireTrue(i != -1);
        _rtb.Select(i - 1, 0);
      }
    }

    private void MoveCaretToEndOfSection()
    {
      var padded = GetPaddedText(Text);
      var i = GetAppendIndex(padded, _rtb.SelectionStart + 1);
      if (i != -1)
        _rtb.Select(i - 1, 0);
    }

    private void MoveCaretToTopOfSection()
    {
      var padded = GetPaddedText(Text);
      var i = padded.LastIndexOf($"\n{Constants.TextEditorSectionIdentifier} ", _rtb.SelectionStart + 1, StringComparison.Ordinal);
      if (i == -1)
        _rtb.Select(0, 0);
      else
      {
        i++;
        _rtb.Select(i - 1, 0);
        _rtb.ScrollToCaret();
        var j = Helper.GetLineEnd(padded, i) + 1;
        if (j >= padded.Length)
          j = padded.Length - 1;
        _rtb.Select(j - 1, 0);
      }
    }

    private void OpenMode(Action<string> action)
    {
      var sections = _panel.Lines.Where(z => !string.IsNullOrEmpty(z));
      var mode = new Mode(Title, true);
      foreach (var section in sections)
      {
        if (Helper.TryGetChordText(section, out var chordText))
        {
          var chord = Env.Config.DefaultChordReader.CreateChord(new LocatedString(chordText));
          mode.AddHotkey(new ModeHotkey(chord, combo => action(section), section));
        }
      }
      Env.ModeHook.EnterMode(mode);
    }

    private async Task Rename()
    {
      var title = await Helper.TryGetLineAsync($"New Title ({Name})", Title);
      if (title == null)
        return;
      if (string.IsNullOrWhiteSpace(title))
      {
        if (MessageBox.Show("Are you sure you want to delete this file?", "Delete file", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
          Delete();
          _fileManager.CompileTextEditorMode();
        }
        return;
      }
      TabPage.Text = Helper.StripTags(title);
      _fileManager.SetTitle(Name, title);
      _fileManager.CompileTextEditorMode();
      _changed = true;
    }

    private void Compile()
    {
      if (Name == Path.GetFileName(Env.Config.TextEditorHotkeyFile))
      {
        Env.Parser.UpdateHotkeyFile(new HotkeyFile(nameof(TextEditor.TextEditorForm), Text));
        Env.Parser.Run();
      }
    }

    private void FlagsChanged()
    {
      _rtb.ReadOnly = Env.FlagManager.HasFlag("ReadOnly") || Program.ReadOnly;
    }
  }
}

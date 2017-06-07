using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using InputMaster.Win32;

namespace InputMaster.Forms
{
  /// <summary>
  /// Fixes the following <see cref="RichTextBox"/> bugs:
  /// 1) "Chinese Character Bug": After pressing Alt, the next key is sometimes replaced by a seemingly random Unicode
  /// character (most of the times a Chinese character). For example: inside a <see cref="RichTextBox"/> hold left Alt,
  /// press three times the left arrow key, and release left Alt. Whatever character you type next, it will be replaced
  /// by the character '5' (on my machine at least).
  /// 
  /// Other features:
  /// 1) A properly disabled <see cref="RichTextBox.AutoWordSelection"/>. By default this functionality is enabled for
  /// a RichTextBox, contradicting the default value of <see cref="RichTextBox.AutoWordSelection"/>.
  /// 2) Pressing Tab inserts two spaces.
  /// 3) Uses <see cref="Helper.StartProcess"/> when a link is clicked.
  /// 4) A number of standard hotkeys are supported, like Ctrl + F and Alt + Up.
  /// 5) Easy saving and loading of cursor and scrollbar position.
  /// 6) No delay while scrolling.
  /// 7) Edit multiple lines simultaneously with Alt + Lmb.
  /// 8) Does not select a trailing space on double click.
  /// 9) Does not stop at the character '_' on double click.
  /// </summary>
  internal class RichTextBoxPlus : RichTextBox
  {
    private const int EmGetEventMask = (int)WindowMessage.User + 59;
    private const int EmSetEventMask = (int)WindowMessage.User + 69;
    private const int EmGetScrollPos = (int)WindowMessage.User + 221;
    private const int EmSetScrollPos = (int)WindowMessage.User + 222;
    private const int SbThumbPosition = 4;
    private const int SbBottom = 7;
    private const int MkControl = 8;

    /// <summary>
    /// Speed at which the control can be scrolled with the mouse wheel.
    /// </summary>
    private readonly int ScrollDelta = 200;
    private Regex FindRegex;
    private Regex FindPrevRegex;
    private string LastSearchString = "";
    private NativePoint ScrollPointBackup;
    private IntPtr EventMask;

    public RichTextBoxPlus()
    {
      var multiEditBrain = new MyMultiEditBrain(this);
      AcceptsTab = true;

      // A hack to properly disable AutoWordSelection. It is first set to true in the constructor, and sometime after the constructor it is set back to false.
      AutoWordSelection = true;

      KeyDown += (s, e) =>
      {
        if (e.Handled)
        {
          return;
        }
        e.Handled = true;
        switch (e.KeyData)
        {
          case Keys.F | Keys.Control:
            ShowFindDialog();
            break;
          case Keys.F3:
            FindNext(Math.Min(TextLength, SelectionStart + 1));
            break;
          case Keys.Shift | Keys.F3:
            FindPrev(SelectionStart - 1);
            break;
          case Keys.F7:
            Tidy();
            break;
          case Keys.V | Keys.Control:
          case Keys.Insert | Keys.Shift:
            if (CanPaste(DataFormats.GetFormat("Text")))
            {
              var e1 = new PastingEventArgs();
              Pasting(e1);
              if (!e1.Handled)
              {
                Paste(DataFormats.GetFormat("Text"));
              }
            }
            else
            {
              e.Handled = false;
            }
            break;
          case Keys.Shift | Keys.Delete:
            RemoveSelectedLines();
            break;
          case Keys.Control | Keys.D:
            DuplicateSelectionOrLine();
            break;
          case Keys.Alt | Keys.Up:
            MoveSelectedLines(ArrowDirection.Up);
            break;
          case Keys.Alt | Keys.Down:
            MoveSelectedLines(ArrowDirection.Down);
            break;
          default:
            e.Handled = false;
            break;
        }
      };

      KeyPress += (s, e) =>
      {
        if (e.KeyChar == '\t')
        {
          SelectedText = "  ";
          e.Handled = true;
        }
      };

      KeyUp += async (s, e) =>
      {
        // A hack to fix the "Chinese Character Bug".
        if (e.KeyData.HasFlag(Keys.Alt))
        {
          await Task.Yield();
          if (Parent != null)
          {
            Parent.Focus();
            Focus();
          }
        }
      };

      Disposed += (s, e) =>
      {
        multiEditBrain.Dispose();
      };

      LinkClicked += (s, e) =>
      {
        Helper.StartProcess(e.LinkText);
      };

      MouseDown += (s, e) =>
      {
        if (AutoWordSelection)
        {
          AutoWordSelection = false;
        }
      };
    }

    private event Action<PastingEventArgs> Pasting = delegate { };

    private static void ShowWrappedMessage()
    {
      Env.Notifier.Write("Wrapped");
    }

    /// <summary>
    /// todo: check why this is needed, instead of just using suspendlayout
    /// </summary>
    public void SuspendPainting()
    {
      NativeMethods.SendMessage(Handle, EmGetScrollPos, IntPtr.Zero, ref ScrollPointBackup);
      NativeMethods.SendMessage(Handle, (int)WindowMessage.SetRedraw, 0, 0);
      EventMask = NativeMethods.SendMessage(Handle, EmGetEventMask, 0, 0);
    }

    public void ResumePainting()
    {
      NativeMethods.SendMessage(Handle, EmSetScrollPos, IntPtr.Zero, ref ScrollPointBackup);
      NativeMethods.SendMessage(Handle, EmSetEventMask, IntPtr.Zero, EventMask);
      NativeMethods.SendMessage(Handle, (int)WindowMessage.SetRedraw, 1, 0);
      Invalidate();
    }

    public string GetCurrentLine()
    {
      var text = Text;
      var i = Helper.GetLineStart(text, SelectionStart);
      var j = Helper.GetLineEnd(text, SelectionStart);
      return text.Substring(i, j - i);
    }

    public RtbPosition GetPosition()
    {
      return new RtbPosition(SelectionStart, GetScrollPosition());
    }

    public void LoadPosition(RtbPosition position)
    {
      SelectionStart = position.SelectionStart;
      SetScrollPosition(position.ScrollPosition);
    }

    public void ScrollToBottom()
    {
      var msg = new Message { HWnd = Handle, WParam = new IntPtr(SbBottom), LParam = IntPtr.Zero, Msg = (int)WindowMessage.VerticalScroll };
      base.WndProc(ref msg);
    }

    public void ShowFindDialog()
    {
      var s = Helper.GetString("Find", string.IsNullOrEmpty(SelectedText) ? LastSearchString : SelectedText);
      if (!string.IsNullOrEmpty(s))
      {
        LastSearchString = s;
        FindRegex = Helper.GetRegex(s, RegexOptions.IgnoreCase);
        FindPrevRegex = Helper.GetRegex(s, RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
        FindNext(SelectionStart);
      }
    }

    protected override void WndProc(ref Message m)
    {
      // Remove the delay of the default scrolling behaviour.
      if (m.Msg == (int)WindowMessage.MouseWheel && (m.WParam.ToInt32() & MkControl) == 0)
      {
        SetScrollPosition(GetScrollPosition() + ScrollDelta * (Helper.GetMouseWheelDelta(m) < 0 ? 1 : -1));
      }
      else if (m.Msg == (int)WindowMessage.LeftMouseButtonDoubleClick)
      {
        SelectCurrentWord();
      }
      else
      {
        base.WndProc(ref m);
      }
    }

    private void SelectCurrentWord()
    {
      var i = SelectionStart;
      var text = Text;
      var j = i;
      while (j < text.Length && ParserConfig.IsIdentifierCharacter(text[j]))
      {
        j++;
      }
      while (i >= 0 && ParserConfig.IsIdentifierCharacter(text[i]))
      {
        i--;
      }
      Select(i + 1, j - (i + 1));
    }

    /// <summary>
    /// Replaces tabs by spaces, removes trailing whitespace, limits the number of adjacent empty lines to two and appends 50 empty lines at the end
    /// </summary>
    private void Tidy()
    {
      var i = GetLineFromCharIndex(SelectionStart);
      SuspendPainting();
      var text = Text;
      text = Regex.Replace(text, " +$", "", RegexOptions.Multiline);
      text = Regex.Replace(text, @"\n\n\n+", "\n\n");
      SelectAll();
      SelectedText = text;
      if (i < Lines.Length)
      {
        Select(Helper.GetLineEnd(text, GetFirstCharIndexFromLine(i)), 0);
      }
      ResumePainting();
    }

    private void RemoveSelectedLines()
    {
      var text = Text;
      var i = Helper.GetLineStart(text, SelectionStart);
      var j = Helper.GetLineEnd(text, SelectionStart + Math.Max(0, SelectionLength - 1));
      Select(i, Math.Min(j + 1, text.Length) - i);
      SelectedText = "";
    }

    private void DuplicateSelectionOrLine()
    {
      if (SelectionLength == 0)
      {
        var text = Text;
        var ss = SelectionStart;
        var i = Helper.GetLineStart(text, ss);
        var j = Helper.GetLineEnd(text, ss);
        if (j == text.Length)
        {
          Select(i, j - i);
          SelectedText = SelectedText + "\n" + SelectedText;
        }
        else
        {
          Select(i, j - i + 1);
          SelectedText = SelectedText + SelectedText;
        }
        Select(ss, 0);
      }
      else
      {
        var sl = SelectionLength;
        SelectedText = SelectedText + SelectedText;
        SelectionLength = sl;
      }
    }

    private void MoveSelectedLines(ArrowDirection dir)
    {
      var text = Text;
      var ss = SelectionStart;
      var sl = SelectionLength;
      var a = Helper.GetLineStart(text, ss);
      var b = Helper.GetLineEnd(text, ss + Math.Max(0, sl - 1));
      int i; // start of a line
      int j; // end of a line
      int k; // end of a line
      if (dir == ArrowDirection.Up)
      {
        if (a == 0)
        {
          return;
        }
        i = Helper.GetLineStart(text, a - 1);
        j = a - 1;
        k = b;
        ss -= a - i;
      }
      else
      {
        if (b == text.Length)
        {
          return;
        }
        i = a;
        j = b;
        k = Helper.GetLineEnd(text, b + 1);
        ss += k + 1 - (j + 1); // Length of 'v' below.
      }
      var l = Math.Min(k + 1, text.Length);
      Select(i, l - i);
      var u = text.Substring(i, j + 1 - i);
      var v = text.Substring(j + 1, k - (j + 1)) + "\n";
      SelectedText = v + u;
      Select(ss, sl);
    }

    private bool FindNext(int startAt)
    {
      if (FindRegex != null)
      {
        var m = FindRegex.Match(Text, startAt);
        if (m.Success)
        {
          SelectionStart = m.Index;
          SelectionLength = m.Length;
          return true;
        }
        if (startAt > 0)
        {
          var found = FindNext(0);
          if (found)
          {
            ShowWrappedMessage();
          }
          return found;
        }
        ShowNotFoundMessage();
      }
      return false;
    }

    private bool FindPrev(int startAt)
    {
      Debug.Assert(startAt >= -1);
      if (FindPrevRegex != null)
      {
        var m = FindPrevRegex.Match(Text, 0, startAt + 1);
        if (m.Success)
        {
          SelectionStart = m.Index;
          SelectionLength = m.Length;
          return true;
        }
        if (startAt < TextLength - 1)
        {
          var found = FindPrev(TextLength - 1);
          if (found)
          {
            ShowWrappedMessage();
          }
          return found;
        }
        ShowNotFoundMessage();
      }
      return false;
    }

    private void ShowNotFoundMessage()
    {
      Env.Notifier.Write($"No match found for '{LastSearchString}'.");
    }

    private int GetScrollPosition()
    {
      return NativeMethods.GetScrollPos(Handle, Orientation.Vertical);
    }

    private void SetScrollPosition(int scrollPosition)
    {
      var low = SbThumbPosition;
      var high = Math.Max(0, scrollPosition);
      var wParam = new IntPtr(high << 16 | low);
      var msg = new Message { HWnd = Handle, WParam = wParam, LParam = IntPtr.Zero, Msg = (int)WindowMessage.VerticalScroll };
      base.WndProc(ref msg);
    }

    /// <summary>
    /// Should be used with a fixed-width font. Also, each character inside the <see cref="RichTextBox"/> should occupy exactly one cell (except for the newline character), and therefore tab characters or characters outside of the Basic Multilingual Plane are not allowed (no checking is done).
    /// </summary>
    private class MyMultiEditBrain : IDisposable
    {
      private readonly RichTextBoxPlus Rtb;
      private readonly Timer Timer = new Timer { Interval = 100 };
      private bool WaitingForMouseUp;
      private bool On;
      private int A;
      private int B;
      private int Counter;

      public MyMultiEditBrain(RichTextBoxPlus rtb)
      {
        Rtb = rtb;

        Rtb.MouseDown += (s, e) =>
        {
          if (e.Button == MouseButtons.Left && ModifierKeys.HasFlag(Keys.Alt))
          {
            WaitingForMouseUp = true;
          }
        };

        Rtb.TextChanged += (s, e) =>
        {
          if (On)
          {
            TurnOff();
          }
        };

        Rtb.SelectionChanged += (s, e) =>
        {
          if (On)
          {
            TurnOff();
          }
        };

        Rtb.MouseUp += (s, e) =>
        {
          if (e.Button == MouseButtons.Left && WaitingForMouseUp)
          {
            WaitingForMouseUp = false;
            var text = Rtb.Text;
            A = Rtb.SelectionStart;
            var column = A - Helper.GetLineStart(text, A);
            var i = Helper.GetLineStart(text, Rtb.SelectionStart + Math.Max(0, Rtb.SelectionLength - 1));
            var j = Helper.GetLineEnd(text, i);
            B = i + column;
            if (B > A)
            {
              if (B > j)
              {
                Rtb.Select(j, 0);
                Rtb.SelectedText = new string(' ', B - j);
              }
              Rtb.Select(A, 0);
              Counter = 0;
              Timer.Start();
              On = true;
            }
          }
        };

        Rtb.KeyDown += (s, e) =>
        {
          if (On)
          {
            if (e.KeyCode == Keys.Back)
            {
              e.Handled = true;
              if (e.KeyData == Keys.Back)
              {
                Modify(ModifyType.Backspace);
              }
            }
            else if (e.KeyCode == Keys.Delete)
            {
              e.Handled = true;
              if (e.KeyData == Keys.Delete)
              {
                Modify(ModifyType.Delete);
              }
            }
            else if (e.KeyData == Keys.Escape)
            {
              TurnOff();
            }
          }
        };

        Rtb.KeyPress += (s, e) =>
        {
          if (On)
          {
            e.Handled = true;
            if (!char.IsControl(e.KeyChar))
            {
              Modify(ModifyType.String, e.KeyChar.ToString());
            }
          }
        };

        Rtb.Pasting += async e =>
        {
          if (On)
          {
            e.Handled = true;
            var s = await Helper.GetClipboardTextAsync();
            if (s.IndexOf('\n') == -1)
            {
              Modify(ModifyType.String, s);
            }
            else
            {
              Env.Notifier.WriteError("Can't paste text containing multiple lines while multi-edit is active.");
            }
          }
        };

        Timer.Tick += (s, e) =>
        {
          Counter++;
          On = false;
          Rtb.Select(Counter % 2 == 0 ? A : B, 0);
          On = true;
        };
      }

      private void Modify(ModifyType type, string str = null)
      {
        var text = Rtb.Text;
        var column = A - Helper.GetLineStart(text, A);
        if (!(type == ModifyType.Backspace && column == 0))
        {
          On = false;
          var i = Helper.GetLineStart(text, A);
          Rtb.Select(i, Helper.GetLineEnd(text, B) - i);
          var lines = Rtb.SelectedText.Split('\n');
          for (var p = 0; p < lines.Length; p++)
          {
            if (type == ModifyType.Backspace)
            {
              if (column <= lines[p].Length)
              {
                lines[p] = lines[p].Substring(0, column - 1) + lines[p].Substring(column);
              }
            }
            else if (type == ModifyType.Delete)
            {
              if (column < lines[p].Length)
              {
                lines[p] = lines[p].Substring(0, column) + lines[p].Substring(column + 1);
              }
            }
            else
            {
              var n = column - lines[p].Length;
              if (n > 0)
              {
                lines[p] += new string(' ', n);
              }
              Debug.Assert(str != null);
              lines[p] = lines[p].Substring(0, column) + str + lines[p].Substring(column);
            }
          }
          Rtb.SelectedText = string.Join("\n", lines);
          B = column + Helper.GetLineStart(Rtb.Text, Rtb.SelectionStart + Rtb.SelectionLength - 1);
          if (type == ModifyType.Backspace)
          {
            A--;
            B--;
          }
          else if (type == ModifyType.String)
          {
            A += str.Length;
            B += str.Length;
          }
          Rtb.Select(A, 0);
          Counter = 0;
          Timer.Stop();
          Timer.Start();
          On = true;
        }
      }

      private void TurnOff()
      {
        On = false;
        Timer.Stop();
      }

      public void Dispose()
      {
        Timer.Dispose();
      }

      private enum ModifyType
      {
        Backspace, Delete, String
      }
    }

    private class PastingEventArgs
    {
      public bool Handled { get; set; }
    }
  }
}

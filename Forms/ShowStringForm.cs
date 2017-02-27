using System;
using System.Windows.Forms;

namespace InputMaster.Forms
{
  /// <summary>
  /// Simple form for displaying selectable text.
  /// </summary>
  partial class ShowStringForm : ThemeForm
  {
    public ShowStringForm(string text, bool scrollToBottom)
    {
      InitializeComponent();
      StartPosition = FormStartPosition.CenterScreen;
      Rtb.Text = text;
      if (scrollToBottom)
      {
        Rtb.ScrollToBottom();
      }
    }

    protected override void OnShown(EventArgs e)
    {
      base.OnShown(e);
      ForceToForeground();
      TopMost = true;
    }

    private void Rtb_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter || e.KeyData == (Keys.W | Keys.Control))
      {
        Close();
        e.Handled = true;
      }
    }
  }
}

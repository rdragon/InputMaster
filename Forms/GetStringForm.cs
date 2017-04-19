using System;
using System.Windows.Forms;

namespace InputMaster.Forms
{
  /// <summary>
  /// Simple form for getting string data from the user.
  /// </summary>
  partial class GetStringForm : ThemeForm
  {
    public GetStringForm(string title, string defaultValue, bool selectAll)
    {
      InitializeComponent();
      Text = Helper.ForbidNull(title, nameof(title));
      RichTextBox.Text = defaultValue ?? "";
      if (selectAll)
      {
        RichTextBox.SelectAll();
      }
    }

    public string GetValue()
    {
      if (DialogResult == DialogResult.OK)
      {
        return RichTextBox.Text;
      }
      else
      {
        return null;
      }
    }

    private void Button_Click(object sender, EventArgs e)
    {
      DialogResult = DialogResult.OK;
    }

    private void RichTextBox_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.KeyData == Keys.Escape)
      {
        e.Handled = true;
        DialogResult = DialogResult.Abort;
      }
      else if (e.KeyData == Keys.Return)
      {
        e.Handled = true;
        DialogResult = DialogResult.OK;
      }
    }

    private void GetStringForm_Shown(object sender, EventArgs e)
    {
      ForceToForeground();
      TopMost = true;
    }
  }
}

using System;
using System.Windows.Forms;

namespace InputMaster.Forms
{
  /// <summary>
  /// Simple form for getting string data from the user.
  /// </summary>
  internal sealed partial class GetStringForm : ThemeForm
  {
    public GetStringForm(string title, string defaultValue, bool selectAll)
    {
      InitializeComponent();
      Text = title;
      RichTextBox.Text = defaultValue;
      if (selectAll)
      {
        RichTextBox.SelectAll();
      }
    }

    public bool TryGetValue(out string value)
    {
      if (DialogResult == DialogResult.OK)
      {
        value = RichTextBox.Text;
        return true;
      }
      value = null;
      return false;
    }

    private void Button_Click(object sender, EventArgs e)
    {
      DialogResult = DialogResult.OK;
    }

    private void RichTextBox_KeyDown(object sender, KeyEventArgs e)
    {
      switch (e.KeyData)
      {
        case Keys.Escape:
          e.Handled = true;
          DialogResult = DialogResult.Abort;
          break;
        case Keys.Return:
          e.Handled = true;
          DialogResult = DialogResult.OK;
          break;
      }
    }

    private void GetStringForm_Shown(object sender, EventArgs e)
    {
      ForceToForeground();
      TopMost = true;
    }
  }
}

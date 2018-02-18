using System;
using System.Windows.Forms;

namespace InputMaster.Forms
{
  public sealed partial class GetStringLineForm : ThemeForm
  {
    public GetStringLineForm(string title, string defaultValue, bool passwordForm)
    {
      InitializeComponent();
      Text = title;
      TextBox.Text = defaultValue;
      TextBox.SelectAll();
      Button.Height = TextBox.Height;
      if (passwordForm)
        TextBox.PasswordChar = '*';
    }

    public bool TryGetValue(out string value)
    {
      value = DialogResult == DialogResult.OK ? TextBox.Text.Trim() : null;
      return value != null;
    }

    private void Button_Click(object sender, EventArgs e)
    {
      DialogResult = DialogResult.OK;
    }

    private void TextBox_KeyDown(object sender, KeyEventArgs e)
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

    private void GetStringLineForm_Shown(object sender, EventArgs e)
    {
      ForceToForeground();
      TopMost = true;
    }
  }
}

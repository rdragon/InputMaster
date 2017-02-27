using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InputMaster.Forms
{
  partial class GetStringLineForm : ThemeForm
  {
    public GetStringLineForm(string title, string defaultValue = null, bool passwordForm = false)
    {
      InitializeComponent();
      Text = Helper.ForbidNull(title, nameof(title));
      TextBox.Text = defaultValue ?? "";
      TextBox.SelectAll();
      Button.Height = TextBox.Height;

      if (passwordForm)
      {
        TextBox.PasswordChar = '*';
      }
    }

    public string GetValue()
    {
      if (DialogResult == DialogResult.OK)
      {
        return TextBox.Text;
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

    private void TextBox_KeyDown(object sender, KeyEventArgs e)
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

    private void GetStringLineForm_Shown(object sender, EventArgs e)
    {
      ForceToForeground();
      TopMost = true;
    }
  }
}

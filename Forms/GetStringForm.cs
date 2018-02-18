using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InputMaster.Forms
{
  /// <summary>
  /// Simple form for getting string data from the user.
  /// </summary>
  public sealed partial class GetStringForm : ThemeForm
  {
    private readonly bool _startWithFindDialog;
    private readonly bool _forceForeground;

    public GetStringForm(string title, string defaultValue, bool selectAll, bool startWithFindDialog, bool forceForeground,
      bool containsJson)
    {
      InitializeComponent();
      Text = title + " - InputMaster";
      RichTextBox.ContainsJson = containsJson;
      RichTextBox.Text = defaultValue;
      if (selectAll)
        RichTextBox.SelectAll();
      _startWithFindDialog = startWithFindDialog;
      _forceForeground = forceForeground;
    }

    public bool TryGetValue(out string value)
    {
      value = DialogResult == DialogResult.OK ? RichTextBox.Text : null;
      return !string.IsNullOrWhiteSpace(value);
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

    private async void GetStringForm_Shown(object sender, EventArgs e)
    {
      if (_forceForeground)
      {
        ForceToForeground();
        TopMost = true;
      }
      if (_startWithFindDialog)
      {
        await Task.Delay(50);
        await RichTextBox.ShowFindDialog();
      }
    }
  }
}

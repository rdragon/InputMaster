using System;
using System.Windows.Forms;

namespace InputMaster.Forms
{
  class ThemeForm : Form
  {
    public ThemeForm()
    {
      Load += (s, e) =>
      {
        ApplyThemeRecursive(this);
        SetHandlerRecursive(this);
      };
    }

    private void SetHandlerRecursive(Control c)
    {
      c.ControlAdded += SomeControlAdded;
      foreach (Control child in c.Controls)
      {
        SetHandlerRecursive(child);
      }
    }

    private void SomeControlAdded(object sender, ControlEventArgs e)
    {
      ApplyThemeRecursive(e.Control);
      SetHandlerRecursive(e.Control);
    }

    public void ApplyThemeRecursive(Control control)
    {
      if (control is RichTextBox || control is TextBox)
      {
        control.ForeColor = Config.ForegroundColor;
        control.BackColor = Config.BackgroundColor;
        control.Font = Config.Font;
      }
      else if (control is SplitContainer)
      {
        control.BackColor = Config.BackgroundColor;
      }
      else if (control is Label)
      {
        control.Font = Config.Font;
      }

      foreach (Control child in control.Controls)
      {
        ApplyThemeRecursive(child);
      }
    }

    public void ForceToForeground()
    {
      Helper.SetForegroundWindowForce(Handle);
    }
  }
}

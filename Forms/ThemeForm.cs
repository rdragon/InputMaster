using System.Windows.Forms;

namespace InputMaster.Forms
{
  public abstract class ThemeForm : Form
  {
    protected ThemeForm()
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

    private static void ApplyThemeRecursive(Control control)
    {
      if (control is RichTextBox || control is TextBox)
      {
        control.ForeColor = Env.Config.ForegroundColor;
        control.BackColor = Env.Config.BackgroundColor;
        control.Font = Env.Config.Font;
      }
      else if (control is SplitContainer)
      {
        control.BackColor = Env.Config.BackgroundColor;
      }
      else if (control is Label)
      {
        control.Font = Env.Config.Font;
      }

      foreach (Control child in control.Controls)
      {
        ApplyThemeRecursive(child);
      }
    }

    protected void ForceToForeground()
    {
      Helper.SetForegroundWindowForce(Handle);
    }
  }
}

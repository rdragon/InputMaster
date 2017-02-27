using InputMaster.Win32;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InputMaster.Forms
{
  class NotifyForm : ThemeForm
  {
    private readonly Label Label = new Label { AutoSize = true, Location = new Point(9, 9) };
    private readonly Queue<string> Messages = new Queue<string>();
    private string PersistentText;

    public NotifyForm()
    {
      SuspendLayout();
      Controls.Add(Label);
      AutoSize = true;
      AutoSizeMode = AutoSizeMode.GrowAndShrink;
      FormBorderStyle = FormBorderStyle.None;
      ShowInTaskbar = false;
      Text = Config.NotifierWindowTitle;
      TopMost = true;
      StartPosition = FormStartPosition.Manual;
      Left = 99999;
      ResumeLayout(false);

      Shown += (s, e) =>
      {
        // This method is used instead of setting `this.Enabled` to false, so that the visuals are not affected.
        NativeMethods.EnableWindow(Handle, false);
      };

      SizeChanged += (s, e) =>
      {
        UpdateFormPosition();
      };
    }

    public void Write(string text)
    {
      Helper.ForbidNull(text, nameof(text));
      Messages.Enqueue(text);
      UpdateLabel();
      Task.Delay(Config.NotifierTextLifetime)
        .ContinueWith(t => DequeueMessage(), TaskScheduler.FromCurrentSynchronizationContext());
    }

    public void SetPersistentText(string text)
    {
      PersistentText = text;
      UpdateLabel();
    }

    private void UpdateLabel()
    {
      StringBuilder sb = new StringBuilder();
      if (!string.IsNullOrEmpty(PersistentText))
      {
        sb.AppendLine(PersistentText);
      }
      foreach (var text in Messages)
      {
        sb.AppendLine(text);
      }
      Label.Text = sb.ToString();
    }

    private void UpdateFormPosition()
    {
      if (Label.Text.Length == 0)
      {
        Left = 99999;
      }
      else if (!string.IsNullOrEmpty(PersistentText))
      {
        Location = new Point(Screen.PrimaryScreen.WorkingArea.Left, Screen.PrimaryScreen.WorkingArea.Top);
      }
      else
      {
        Location = new Point(Screen.PrimaryScreen.WorkingArea.Left, Screen.PrimaryScreen.WorkingArea.Bottom - Height);
      }
    }

    private void DequeueMessage()
    {
      Messages.Dequeue();
      UpdateLabel();
    }
  }
}

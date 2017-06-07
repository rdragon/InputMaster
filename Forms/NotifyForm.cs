using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using InputMaster.Win32;

namespace InputMaster.Forms
{
  internal sealed class NotifyForm : ThemeForm, INotifier
  {
    private readonly Label Label = new Label { AutoSize = true, Location = new Point(9, 9) };
    private readonly Queue<string> Messages = new Queue<string>();
    private readonly StringBuilder Log = new StringBuilder();
    private string PersistentText;
    private bool Alive = true;

    public NotifyForm()
    {
      SuspendLayout();
      Controls.Add(Label);
      AutoSize = true;
      AutoSizeMode = AutoSizeMode.GrowAndShrink;
      FormBorderStyle = FormBorderStyle.None;
      ShowInTaskbar = false;
      Text = Env.Config.NotifierWindowTitle;
      TopMost = true;
      StartPosition = FormStartPosition.Manual;
      Left = 99999;
      ResumeLayout(false);

      Shown += (s, e) =>
      {
        // This method is used instead of setting `this.Enabled` to false, so that the visuals are not affected.
        NativeMethods.EnableWindow(Handle, false);

        File.WriteAllText(Env.Config.WindowHandleFile, Handle.ToString());
      };

      SizeChanged += (s, e) =>
      {
        UpdateFormPosition();
      };

      FormClosing += (s, e) =>
      {
        if (!Alive)
        {
          return;
        }
        Alive = false;
        Application.Exit();
        File.Delete(Env.Config.WindowHandleFile);
      };
    }

    public ISynchronizeInvoke SynchronizingObject => this;

    private static string AppendTimestamp(string text)
    {
      var date = DateTime.Now.ToString(Env.Config.LogDateTimeFormat);
      return $"{date} {text}";
    }

    public void Write(string message)
    {
      message = message ?? "";
      WriteToLog(message);
      Messages.Enqueue(message);
      UpdateLabel();
      Task.Delay(Env.Config.NotifierTextLifetime)
        .ContinueWith(t => DequeueMessage(), TaskScheduler.FromCurrentSynchronizationContext());
    }

    public void WriteWarning(string message)
    {
      message = $"Warning: {message}";
      WriteToFile(message);
      Write(message);
    }

    public void WriteError(string message)
    {
      message = $"Error: {message}";
      WriteToFile(message);
      Write(message);
    }

    public void SetPersistentText(string text)
    {
      PersistentText = text;
      UpdateLabel();
    }

    public string GetLog()
    {
      return Log.ToString();
    }

    public void CaptureForeground()
    {
      if (!Alive)
      {
        return;
      }
      Helper.SetForegroundWindowForce(Handle);
    }

    private void WriteToLog(string message)
    {
      message = AppendTimestamp(message);
      Log.Append($"{message}\n");
    }

    private void WriteToFile(string message)
    {
      message = AppendTimestamp(message);
      File.AppendAllLines(Env.Config.ErrorLogFile, new[] { message });
    }

    private void UpdateLabel()
    {
      if (!Alive)
      {
        return;
      }
      var sb = new StringBuilder();
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

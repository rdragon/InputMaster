using System;
using System.Text;
using InputMaster.Forms;
using System.ComponentModel;

namespace InputMaster
{
  class Notifier : INotifier, IDisposable
  {
    private readonly NotifyForm Form = new NotifyForm();
    private readonly StringBuilder Log = new StringBuilder();
    private bool Alive = true;

    public Notifier(Brain brain)
    {
      // Allow this class to decide when the form closes.
      Form.FormClosing += (s, e) =>
      {
        if (Alive)
        {
          e.Cancel = true;
          RequestingExit();
        }
      };

      brain.Exiting += () =>
      {
        Alive = false;
        Try.Execute(Form.Close);
      };

      // Directly show form so that any warnings during startup will be written to form.
      Form.Show();
    }

    public IntPtr WindowHandle => Form.Handle;
    public ISynchronizeInvoke SynchronizingObject => Form;

    public event Action RequestingExit = delegate { };

    public void CaptureForeground()
    {
      if (Alive)
      {
        Form.ForceToForeground();
      }
    }

    public void RequestExit()
    {
      RequestingExit();
    }

    public void Disable()
    {
      Alive = false;
    }

    public void Write(string text)
    {
      if (text != null)
      {
        var date = DateTime.Now.ToString(Config.LogDateTimeFormat);
        Log.Append($"{date} {text}\n");
        if (Alive) Form.Write(text);
      }
    }

    public void ShowLog()
    {
      Helper.ShowSelectableText(Log.ToString());
    }

    public void SetPersistentText(string text)
    {
      if (Alive) Form.SetPersistentText(text);
    }

    public void WriteWarning(string text)
    {
      Helper.ForbidNullOrEmpty(text, nameof(text));
      WriteToErrorLogFile("Warning: " + text);
    }

    public void WriteError(string text)
    {
      Helper.ForbidNullOrEmpty(text, nameof(text));
      WriteToErrorLogFile("Error: " + text);
    }

    private void WriteToErrorLogFile(string text)
    {
      if (Alive)
      {
        var date = DateTime.Now.ToString(Config.LogDateTimeFormat);
        using (var stream = Config.ErrorLogFile.AppendText())
        {
          stream.WriteLine($"{date} {text}");
        }
        Write(text);
      }
      else
      {
        Try.SetException(new InvalidOperationException("Failed to write the following text to error log file: " + text));
      }
    }

    /// <summary>
    /// Note: all methods remain safe to call after this method has returned.
    /// </summary>
    public void Dispose()
    {
      Alive = false;
      Form.Dispose();
    }
  }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using InputMaster.Win32;
using InputMaster.Forms;

namespace InputMaster.Instances
{
  public sealed class NotifyForm : ThemeForm, INotifier
  {
    private readonly Label _label = new Label { AutoSize = true, Location = new Point(9, 9) };
    private readonly Queue<string> _messages = new Queue<string>();
    private readonly StringBuilder _log = new StringBuilder();
    private string _persistentText;
    private State _state = State.Running;

    public NotifyForm()
    {
      SuspendLayout();
      Controls.Add(_label);
      AutoSize = true;
      AutoSizeMode = AutoSizeMode.GrowAndShrink;
      FormBorderStyle = FormBorderStyle.None;
      ShowInTaskbar = false;
      Text = Env.Config.NotifierWindowTitle;
      TopMost = true;
      StartPosition = FormStartPosition.Manual;
      Left = 99999;
      if (Program.ReadOnly)
        BackColor = Color.FromArgb(255, 204, 0);
      ResumeLayout(false);

      Shown += async (s, e) =>
      {
        try
        {
          // This method is used instead of setting `this.Enabled` to false, so that the visuals are not affected.
          NativeMethods.EnableWindow(Handle, false);
          Directory.CreateDirectory(Env.Config.DataDir);
          Directory.CreateDirectory(Env.Config.CacheDir);
          if (!File.Exists(Env.Config.HotkeyFile) && !Program.ReadOnly)
            File.WriteAllText(Env.Config.HotkeyFile, "");
          if (!Program.ReadOnly)
            File.WriteAllText(Env.Config.WindowHandleFile, Handle.ToString());
          await new Factory(this).Run();
        }
        catch (Exception ex)
        {
          await Helper.HandleExceptionAsync(new FatalException("Error during startup.", ex));
        }
      };

      SizeChanged += (s, e) =>
      {
        UpdateFormPosition();
      };

      FormClosing += async (s, e) =>
      {
        if (_state == State.CanBeClosed)
          return;
        e.Cancel = true;
        if (_state == State.WaitingToClose)
          return;
        _state = State.WaitingToClose;
        await Task.Yield();
        await ExitAsync();
      };
    }

    private async Task ExitAsync()
    {
      await Env.App.TriggerSaveAsync();
      await Env.App.TriggerExitAsync();
      if (!Program.ReadOnly)
        File.Delete(Env.Config.WindowHandleFile);
      _state = State.CanBeClosed;
      Application.Exit();
    }

    public ISynchronizeInvoke SynchronizingObject => this;

    private static string AppendTimestamp(string text)
    {
      var date = DateTime.Now.ToString(Env.Config.LogDateTimeFormat);
      return $"{date} {text}";
    }

    public void Info(string message)
    {
      message = message ?? "";
      WriteToLog(message);
      _messages.Enqueue(message);
      UpdateLabel();
      Task.Delay(Env.Config.NotifierTextLifetime)
        .ContinueWith(t => DequeueMessage(), TaskScheduler.FromCurrentSynchronizationContext());
    }

    public void Warning(string message)
    {
      message = $"Warning: {message}";
      WriteToFile(message);
      Info(message);
    }

    public void Error(string message)
    {
      Error(message, true);
    }

    public void LogError(string message)
    {
      Error(message, false);
    }

    private void Error(string message, bool showToUser)
    {
      message = $"Error: {message}";
      WriteToFile(message);
      if (showToUser)
        Info(message);
    }

    public void SetPersistentText(string text)
    {
      _persistentText = text;
      UpdateLabel();
    }

    public string GetLog()
    {
      return _log.ToString();
    }

    public void CaptureForeground()
    {
      if (_state != State.Running)
        return;
      Helper.SetForegroundWindowForce(Handle);
    }

    private void WriteToLog(string message)
    {
      message = AppendTimestamp(message);
      _log.Append($"{message}\n");
    }

    private static void WriteToFile(string message)
    {
      if (Program.ReadOnly)
        return;
      message = AppendTimestamp(message);
      File.AppendAllLines(Env.Config.ErrorLogFile, new[] { message });
    }

    private void UpdateLabel()
    {
      if (_state != State.Running)
        return;
      var sb = new StringBuilder();
      if (!string.IsNullOrEmpty(_persistentText))
        sb.Append($"{_persistentText}\n");
      foreach (var text in _messages)
        sb.Append($"{text}\n");
      _label.Text = sb.ToString();
    }

    private void UpdateFormPosition()
    {
      if (_label.Text.Length == 0)
        Left = 99999;
      else if (!string.IsNullOrEmpty(_persistentText))
        Location = new Point(Screen.PrimaryScreen.WorkingArea.Left, Screen.PrimaryScreen.WorkingArea.Top);
      else
        Location = new Point(Screen.PrimaryScreen.WorkingArea.Left, Screen.PrimaryScreen.WorkingArea.Bottom - Height);
    }

    private void DequeueMessage()
    {
      _messages.Dequeue();
      UpdateLabel();
    }

    private enum State
    {
      Running, WaitingToClose, CanBeClosed
    }
  }
}

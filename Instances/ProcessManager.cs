using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace InputMaster.Instances
{
  public class ProcessManager : IProcessManager
  {
    private readonly HashSet<MyProcess> _processes = new HashSet<MyProcess>();
    private readonly Timer _timer = new Timer { Interval = (int)Env.Config.ProcessManagerInterval.TotalMilliseconds };

    public ProcessManager()
    {
      _timer.Tick += (s, e) =>
      {
        foreach (var process in _processes.ToArray())
        {
          process.KillIfTimedOut();
          if (process.Process.HasExited)
          {
            process.Process.Dispose();
            _processes.Remove(process);
          }
        }
      };
      Env.App.Run += () => _timer.Enabled = true;
      Env.App.Exiting += async () =>
      {
        _timer.Dispose();
        foreach (var process in _processes)
        {
          await Try.Execute(process.KillIfRunning);
          process.Process.Dispose();
        }
        _processes.Clear();
      };
    }

    public void StartHiddenProcess(string file, string arguments = "", TimeSpan? timeout = null)
    {
      var startInfo = new ProcessStartInfo(file, arguments ?? "") { WindowStyle = ProcessWindowStyle.Hidden };
      var process = Process.Start(startInfo);
      if (process != null)
      {
        var timeoutLength = timeout.GetValueOrDefault(TimeSpan.MaxValue);
        var timeoutDate = timeoutLength == TimeSpan.MaxValue ? DateTime.MaxValue : DateTime.Now.Add(timeoutLength);
        _processes.Add(new MyProcess(process, timeoutLength, timeoutDate));
      }
    }

    private class MyProcess
    {
      public Process Process { get; }
      private readonly TimeSpan _timeoutLength;
      private readonly DateTime _timeoutDate;

      public MyProcess(Process process, TimeSpan timeoutLength, DateTime timeoutDate)
      {
        Process = process;
        _timeoutLength = timeoutLength;
        _timeoutDate = timeoutDate;
      }

      public void KillIfTimedOut()
      {
        if (_timeoutDate < DateTime.Now)
          KillIfRunning();
      }

      public void KillIfRunning()
      {
        if (Process.HasExited)
          return;
        Process.Kill();
        var s = "Killed process" + Helper.GetBindingsSuffix(Process.StartInfo.FileName, nameof(Process.StartInfo.FileName),
          Process.StartInfo.Arguments, nameof(Process.StartInfo.Arguments));
        if (_timeoutDate < DateTime.Now)
          s += " Reason: timed out after " + _timeoutLength + ".";
        Env.Notifier.Error(s);
      }
    }
  }
}

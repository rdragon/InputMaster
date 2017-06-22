using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace InputMaster.Instances
{
  internal class ProcessManager : IProcessManager
  {
    private readonly HashSet<MyProcess> Processes = new HashSet<MyProcess>();
    private readonly Timer Timer = new Timer { Interval = (int)Env.Config.ProcessManagerInterval.TotalMilliseconds, Enabled = true };

    public ProcessManager()
    {
      Timer.Tick += (s, e) =>
      {
        foreach (var process in Processes.ToArray())
        {
          process.KillIfTimedOut();
          if (process.Process.HasExited)
          {
            process.Process.Dispose();
            Processes.Remove(process);
          }
        }
      };
      Env.App.Exiting += () =>
      {
        Timer.Dispose();
        foreach (var process in Processes)
        {
          Try.Execute(process.KillIfRunning);
          process.Process.Dispose();
        }
        Processes.Clear();
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
        Processes.Add(new MyProcess(process, timeoutLength, timeoutDate));
      }
    }

    private class MyProcess
    {
      private readonly TimeSpan TimeoutLength;
      private readonly DateTime TimeoutDate;

      public MyProcess(Process process, TimeSpan timeoutLength, DateTime timeoutDate)
      {
        Process = process;
        TimeoutLength = timeoutLength;
        TimeoutDate = timeoutDate;
      }

      public Process Process { get; }

      public void KillIfTimedOut()
      {
        if (TimeoutDate < DateTime.Now)
        {
          KillIfRunning();
        }
      }

      public void KillIfRunning()
      {
        if (Process.HasExited)
        {
          return;
        }
        Process.Kill();
        var s = "Killed process" + Helper.GetBindingsSuffix(Process.StartInfo.FileName, nameof(Process.StartInfo.FileName), Process.StartInfo.Arguments, nameof(Process.StartInfo.Arguments));
        if (TimeoutDate < DateTime.Now)
        {
          s += " Reason: timed out after " + TimeoutLength + ".";
        }
        Env.Notifier.WriteError(s);
      }
    }
  }
}

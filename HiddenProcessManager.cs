using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace InputMaster
{
  class HiddenProcessManager : IDisposable
  {
    private readonly Timer Timer = new Timer { Interval = (int)Config.ProcessManagerInterval.TotalMilliseconds };
    private List<HiddenProcess> HiddenProcesses = new List<HiddenProcess>();

    public HiddenProcessManager(Brain brain)
    {
      Timer.Tick += (s, e) =>
      {
        var newProcesses = new List<HiddenProcess>();
        foreach (var hiddenProcess in HiddenProcesses)
        {
          hiddenProcess.Update();
          if (hiddenProcess.HasExited)
          {
            hiddenProcess.Dispose();
          }
          else
          {
            newProcesses.Add(hiddenProcess);
          }
        }
        HiddenProcesses = newProcesses;
      };

      brain.Exiting += () =>
      {
        foreach (var hiddenProcess in HiddenProcesses)
        {
          Try.Execute(hiddenProcess.Kill);
          hiddenProcess.Dispose();
        }
      };

      Timer.Start();
    }

    public void StartHiddenProcess(FileInfo file, string arguments = "", TimeSpan? timeout = null)
    {
      var startInfo = new ProcessStartInfo(Helper.ForbidNull(file, nameof(file)).FullName, arguments ?? "");
      startInfo.WindowStyle = ProcessWindowStyle.Hidden;
      var process = Process.Start(startInfo);
      if (process != null)
      {
        var timeoutLength = timeout.GetValueOrDefault(TimeSpan.MaxValue);
        var timeoutDate = timeoutLength == TimeSpan.MaxValue ? DateTime.MaxValue : DateTime.Now.Add(timeoutLength);
        HiddenProcesses.Add(new HiddenProcess(process, timeoutLength, timeoutDate));
      }
    }

    public void Dispose()
    {
      Timer.Dispose();
      foreach (var hiddenProcess in HiddenProcesses)
      {
        hiddenProcess.Dispose();
      }
    }

    private class HiddenProcess : IDisposable
    {
      private Process Process;
      private TimeSpan TimeoutLength;
      private DateTime TimeoutDate;

      public HiddenProcess(Process process, TimeSpan timeoutLength, DateTime timeoutDate)
      {
        Process = process;
        TimeoutLength = timeoutLength;
        TimeoutDate = timeoutDate;
      }

      public bool HasExited { get { return Process.HasExited; } }

      public void Update()
      {
        if (!Process.HasExited && DateTime.Now > TimeoutDate)
        {
          Kill();
        }
      }

      public void Kill()
      {
        if (!Process.HasExited)
        {
          Process.Kill();
          var s = "Killed process" + Helper.GetBindingsSuffix(Process.StartInfo.FileName, nameof(Process.StartInfo.FileName), Process.StartInfo.Arguments, nameof(Process.StartInfo.Arguments));
          if (DateTime.Now > TimeoutDate)
          {
            s += " Reason: timed out after " + TimeoutLength + ".";
          }
          Env.Notifier.WriteError(s);
        }
      }

      public void Dispose()
      {
        Process.Dispose();
      }
    }
  }
}

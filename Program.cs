using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

[assembly: InternalsVisibleTo("unitTests")]
[assembly: CLSCompliant(true)]

namespace InputMaster
{
  static class Program
  {
    public static bool ShouldRestart { get; set; }

    [STAThread]
    private static void Main(string[] arguments)
    {
      try
      {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        Application.ThreadException += (s, e) =>
        {
          Try.SetException(new WrappedException("Unhandled Windows Forms exception (only innermost exception is being shown).", e.Exception));
          if (Env.Notifier != null)
          {
            Env.Notifier.RequestExit();
          }
          else
          {
            Application.Exit();
          }
        };

        var exitFlag = arguments.Length == 1 && arguments[0] == "exit";
        if (!exitFlag && arguments.Length > 0)
        {
          ShowError("Expecting no arguments or the single argument 'exit'.");
        }
        else
        {
          Run(exitFlag);
        }
      }
      catch (Exception ex)
      {
        Try.SetException(ex);
      }
      finally
      {
        Try.Execute(RestartIfNeeded);
        Try.ShowException();
      }
    }

    private static void ShowError(string text)
    {
      MessageBox.Show("Error", text, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private static void Run(bool exitFlag)
    {
      using (var mutex = new SafeMutex("InputMasterSingleInstance"))
      {
        try
        {
          if (!mutex.Acquire())
          {
            Helper.RequireExists(Config.WindowHandleFile);
            var handle = new IntPtr(Convert.ToInt64(File.ReadAllText(Config.WindowHandleFile.FullName)));
            Helper.CloseWindow(handle);
            if (!mutex.Acquire(Config.ExitRunningInputMasterTimeout))
            {
              throw new TimeoutException("Running InputMaster instance could not be closed.");
            }
          }
          if (!exitFlag)
          {
            new Brain().Start();
          }
        }
        finally
        {
          if (Env.Notifier != null)
          {
            Try.Execute(Env.Notifier.RequestExit);
            Try.Execute(Env.Notifier.Disable);
          }
          Try.Execute(mutex.Release);
        }
      }
    }

    public static void RestartIfNeeded()
    {
      if (ShouldRestart || Debugger.IsAttached)
      {
        var path = Application.ExecutablePath;
        if (Debugger.IsAttached)
        {
          path = path.Replace(".vshost", "");
          path = path.Replace("Debug", "Release");
        }
        if (File.Exists(path))
        {
          var process = Process.Start(path);
          if (process != null)
          {
            process.Dispose();
          }
        }
      }
    }
  }
}

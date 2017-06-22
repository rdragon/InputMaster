using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;
using InputMaster.Win32;

[assembly: InternalsVisibleTo("UnitTests")]
[assembly: CLSCompliant(true)]

namespace InputMaster
{
  internal class Program
  {
    private Mutex Mutex;
    private bool MutexAcquired;

    private Program()
    {
      ConfigHelper.SetConfig();
    }

    [STAThread]
    private static void Main(string[] arguments)
    {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
      Application.ThreadException += (s, e) =>
      {
        // Warning: the argument only contains the innermost exception (see http://stackoverflow.com/questions/347502/why-does-the-inner-exception-reach-the-threadexception-handler-and-not-the-actual).
        Helper.HandleException(e.Exception);
      };
      Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
      new Program().Run(arguments);
    }

    /// <summary>
    /// Returns whether application should exit.
    /// </summary>
    private static bool HandleArguments(string[] arguments)
    {
      if (arguments.Length == 0)
      {
        return false;
      }
      if (arguments.Length > 1 || arguments[0] != "exit")
      {
        ShowError("Expecting no arguments or the single argument 'exit'.");
      }
      return true;
    }

    private static void CloseOtherInstance()
    {
      if (!File.Exists(Env.Config.WindowHandleFile))
      {
        throw new FileNotFoundException($"File '{Env.Config.WindowHandleFile}' not found.");
      }
      var text = File.ReadAllText(Env.Config.WindowHandleFile);
      if (!long.TryParse(text, out var handle))
      {
        throw new FatalException($"Failed to parse contents of '{Env.Config.WindowHandleFile}' as long.");
      }
      NativeMethods.SendNotifyMessage(new IntPtr(handle), WindowMessage.Close, IntPtr.Zero, IntPtr.Zero);
    }

    private static void RestartIfNeeded()
    {
      if (!Env.ShouldRestart && !Debugger.IsAttached)
      {
        return;
      }
      var path = Application.ExecutablePath;
      if (Debugger.IsAttached)
      {
        path = path.Replace(".vshost", "");
        path = path.Replace("Debug", "Release");
      }
      if (File.Exists(path))
      {
        Process.Start(path)?.Dispose();
      }
    }

    private static void ShowError(string text)
    {
      MessageBox.Show("Error", text, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void Run(string[] arguments)
    {
      try
      {
        Mutex = new Mutex(false, "4dy9fbflg2ct");
        AcquireMutex();
        MutexAcquired = true;
        if (HandleArguments(arguments))
        {
          return;
        }
        Application.Run(Env.Config.CreateMainForm());
      }
      catch (Exception ex)
      {
        Try.HandleFatalException(ex);
      }
      finally
      {
        Try.Execute(OnExit);
        Try.ShowFatalExceptionIfExists();
      }
    }

    private void AcquireMutex()
    {
      if (AcquireMutex(TimeSpan.Zero))
      {
        return;
      }
      try
      {
        CloseOtherInstance();
        if (!AcquireMutex(Env.Config.ExitOtherInputMasterTimeout))
        {
          throw new FatalException("Timeout while waiting for mutex to be released.");
        }
      }
      catch (Exception ex)
      {
        throw new FatalException("Failed to close other InputMaster instance.", ex);
      }
    }

    private bool AcquireMutex(TimeSpan timespan)
    {
      try
      {
        return Mutex.WaitOne(timespan);
      }
      catch (AbandonedMutexException)
      {
        return true;
      }
    }

    private void OnExit()
    {
      if (MutexAcquired)
      {
        Mutex.ReleaseMutex();
      }
      Mutex?.Dispose();
      RestartIfNeeded();
    }
  }
}

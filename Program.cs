using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;
using InputMaster.Forms;
using InputMaster.Win32;

[assembly: InternalsVisibleTo("unitTests")]
[assembly: CLSCompliant(true)]

namespace InputMaster
{
  internal class Program
  {
    private Mutex Mutex;
    private bool MutexAcquired;

    [STAThread]
    private static void Main(string[] arguments)
    {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
      Application.ThreadException += (s, e) =>
      {
        // Warning: the argument only contains the innermost exception (see http://stackoverflow.com/questions/347502/why-does-the-inner-exception-reach-the-threadexception-handler-and-not-the-actual).
        Helper.HandleAnyException(e.Exception);
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
      Config.WindowHandleFile.Refresh();
      if (!Config.WindowHandleFile.Exists)
      {
        throw new FileNotFoundException($"File '{Config.WindowHandleFile.FullName}' not found.");
      }
      var text = File.ReadAllText(Config.WindowHandleFile.FullName);
      if (!long.TryParse(text, out var handle))
      {
        throw new Exception($"Failed to parse contents of '{Config.WindowHandleFile.FullName}' as long.");
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
        Mutex = new Mutex(false, "InputMasterSingleInstance");//4dy9fbflg2ct
        AcquireMutex();
        MutexAcquired = true;
        if (HandleArguments(arguments))
        {
          return;
        }
        var notifyForm = new NotifyForm();
        notifyForm.Shown += (s, e) =>
        {
          new Factory(notifyForm).Run();
        };
        Application.Run(notifyForm);
      }
      catch (Exception ex)
      {
        Try.SetException(ex);
      }
      finally
      {
        Try.Execute(OnExit);
        Try.ShowException();
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
        if (!AcquireMutex(Config.ExitOtherInputMasterTimeout))
        {
          throw new Exception("Timeout while waiting for mutex to be released.");
        }
      }
      catch (Exception ex)
      {
        throw new Exception("Failed to close other InputMaster instance.", ex);
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

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using InputMaster.Win32;

[assembly: InternalsVisibleTo("UnitTests")]
[assembly: CLSCompliant(true)]

namespace InputMaster
{
  public class Program
  {
    public static bool Reset { get; private set; }
    public static bool ReadOnly { get; private set; }
    private Mutex Mutex;
    private bool MutexAcquired;

    [STAThread]
    private static void Main(string[] arguments)
    {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
      Application.ThreadException += async (s, e) =>
      {
        // Warning: the argument only contains the innermost exception (see http://stackoverflow.com/questions/347502/why-does-the-inner-exception-reach-the-threadexception-handler-and-not-the-actual).
        await Helper.HandleExceptionAsync(e.Exception);
      };
      Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
      new Program().Run(arguments);
    }

    /// <summary>
    /// Returns whether application should exit.
    /// </summary>
    private static Result HandleArguments(string[] arguments)
    {
      if (arguments.Length == 0)
        return Result.Run;
      if (1 < arguments.Length)
        throw new ArgumentException($"Expecting zero or one arguments, but found {arguments.Length} arguments.");
      if (arguments[0] == "exit")
        return Result.CloseAfterMutex;
      if (arguments[0] == Env.Config.ResetCommandLineArgument)
      {
        Reset = true;
        return Result.Run;
      }
      if (arguments[0] == Constants.ReadOnlyCommandLineArgument)
      {
        ReadOnly = true;
        return Result.Run;
      }
      if (arguments[0] == Constants.StartReadOnlyCommandLineArgument)
      {
        Helper.StartReadOnlyAsync().Wait();
        Thread.Sleep(500);
        Env.ShouldRestart = true;
        return Result.CloseNow;
      }
      throw new ArgumentException($"Unknown command line argument '{arguments[0]}'.");
    }

    private static void CloseOtherInstance()
    {
      if (!File.Exists(Env.Config.WindowHandleFile))
        throw new FileNotFoundException($"File '{Env.Config.WindowHandleFile}' not found.");
      var text = File.ReadAllText(Env.Config.WindowHandleFile);
      if (!long.TryParse(text, out var handle))
        throw new FatalException($"Failed to parse contents of '{Env.Config.WindowHandleFile}' as long.");
      NativeMethods.SendNotifyMessage(new IntPtr(handle), WindowMessage.Close, IntPtr.Zero, IntPtr.Zero);
    }

    private static void RestartIfNeeded()
    {
      if (!Env.ShouldRestart && !Debugger.IsAttached)
        return;
      var file = Path.Combine(Env.Config.InputMasterPublishDir, Env.Config.InputMasterFileName);
      if (File.Exists(file))
        Process.Start(file, Env.RestartArguments)?.Dispose();
    }

    private void Run(string[] arguments)
    {
      ConfigHelper.SetConfig();
      try
      {
        var result = HandleArguments(arguments);
        if (result == Result.CloseNow)
          return;
        if (!ReadOnly)
        {
          Mutex = new Mutex(false, "4dy9fbflg2ct_");
          AcquireMutex();
          MutexAcquired = true;
        }
        if (result == Result.CloseAfterMutex)
          return;
        Application.Run(Env.Config.CreateMainForm());
      }
      catch (Exception ex)
      {
        Try.HandleFatalException(ex).Wait();
      }
      finally
      {
        Try.Execute(OnExit).Wait();
      }
    }

    private void AcquireMutex()
    {
      if (AcquireMutex(TimeSpan.Zero))
        return;
      try
      {
        CloseOtherInstance();
        if (!AcquireMutex(Env.Config.ExitOtherInputMasterTimeout))
          throw new FatalException("Timeout while waiting for mutex to be released.");
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
        Mutex.ReleaseMutex();
      Mutex?.Dispose();
      RestartIfNeeded();
    }

    private enum Result { CloseNow, CloseAfterMutex, Run }
  }
}

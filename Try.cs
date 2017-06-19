using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InputMaster
{
  internal static class Try
  {
    private static Exception FatalException;

    /// <summary>
    /// Execute the action and capture any exception thrown during the execution.
    /// </summary>
    /// <param name="action"></param>
    public static void Execute(Action action)
    {
      try
      {
        action();
      }
      catch (Exception ex)
      {
        HandleFatalException(ex);
      }
    }

    public static async Task ExecuteAsync(Func<Task> action)
    {
      try
      {
        await action();
      }
      catch (Exception ex)
      {
        HandleFatalException(ex);
      }
    }

    public static void HandleFatalException(Exception exception)
    {
      FatalException = FatalException ?? exception;
      // Try to log the exception.
      try
      {
        Env.Notifier.WriteError(exception.ToString());
      }
      // ReSharper disable once EmptyGeneralCatchClause
      catch (Exception)
      {
        // Nothing can be done here as there has already been thrown a fatal exception, and logging does not seem to work.
      }
    }

    public static Action Wrap(Action action)
    {
      return () => { Execute(action); };
    }

    public static void ShowFatalExceptionIfExists()
    {
      if (FatalException == null)
      {
        return;
      }
      MessageBox.Show(FatalException.ToString(), "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    public static void ThrowFatalExceptionIfExists()
    {
      if (FatalException == null)
      {
        return;
      }
      throw FatalException;
    }
  }
}

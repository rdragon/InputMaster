using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InputMaster
{
  public static class Try
  {
    private static bool _fatalExceptionHandled;

    /// <summary>
    /// Execute the action and capture any exception thrown during the execution.
    /// </summary>
    /// <param name="action"></param>
    public static async Task Execute(Action action)
    {
      try
      {
        action();
      }
      catch (Exception ex)
      {
        await HandleFatalException(ex);
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
        await HandleFatalException(ex);
      }
    }

    public static async Task HandleFatalException(Exception exception)
    {
      if (_fatalExceptionHandled)
        return;
      _fatalExceptionHandled = true;
      try
      {
        Env.Notifier?.LogError(exception.ToString());
      }
      catch (Exception)
      {
        // Nothing can be done here as there has already been thrown a fatal exception, and logging does not seem to work.
      }
      try
      {
        await Helper.ShowSelectableTextAsync("Fatal error", exception.ToString());
      }
      catch (Exception)
      {
        // We are not interested in this exception.
      }
      Application.Exit();
    }

    public static Action Wrap(Action action)
    {
      return async () => { await Execute(action); };
    }

    public static Func<Task> Wrap(Func<Task> action)
    {
      return () => { return ExecuteAsync(action); };
    }
  }
}

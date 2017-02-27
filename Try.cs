using System;
using System.Threading;
using System.Windows.Forms;

namespace InputMaster
{
  static class Try
  {
    private static Exception Exception;

    public static void Execute(Action action)
    {
      try
      {
        action();
      }
      catch (Exception ex)
      {
        SetException(ex);
      }
    }

    public static void SetException(Exception exception)
    {
      if (exception != null && Interlocked.CompareExchange(ref Exception, exception, null) == null)
      {
        try
        {
          Env.Notifier.WriteError(exception.ToString());
        }
        catch (Exception) { }
      }
    }

    public static Action Wrap(Action action)
    {
      return () => { Execute(action); };
    }

    public static void ShowException()
    {
      var exception = Exception;
      if (exception != null)
      {
        MessageBox.Show("Fatal Error: " + exception, "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    public static void ThrowException()
    {
      var exception = Exception;
      if (exception != null)
      {
        throw exception;
      }
    }
  }
}

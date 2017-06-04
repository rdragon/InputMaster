﻿using System;
using System.Threading;
using System.Windows.Forms;

namespace InputMaster
{
  internal static class Try
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
      try
      {
        if (exception != null && Interlocked.CompareExchange(ref Exception, exception, null) == null)
        {
          Env.Notifier.WriteError(exception.ToString());
        }
      }
      // ReSharper disable once EmptyGeneralCatchClause
      catch (Exception) { }
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
        MessageBox.Show($"Fatal Error: {exception}", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

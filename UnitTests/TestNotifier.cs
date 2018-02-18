using System;
using System.ComponentModel;
using System.Text;
using InputMaster;

namespace UnitTests
{
  public class TestNotifier : INotifier
  {
    private readonly StringBuilder Log = new StringBuilder();

    public ISynchronizeInvoke SynchronizingObject => throw new NotImplementedException();

    public void Info(string message) { }

    public void Warning(string message)
    {
      Log.Append($"Warning: {message}\n");
    }

    public void Error(string message)
    {
      Log.Append($"Error: {message}\n");
    }

    public void LogError(string message)
    {
      Error(message);
    }

    public void SetPersistentText(string text) { }

    public void CaptureForeground() { }

    public string GetLog()
    {
      return Log.ToString();
    }
  }
}

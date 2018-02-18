using System;
using System.ComponentModel;
using System.Text;
using InputMaster;

namespace UnitTests
{
  public class TestNotifier : INotifier
  {
    public ISynchronizeInvoke SynchronizingObject => throw new NotImplementedException();
    private readonly StringBuilder _log = new StringBuilder();

    public void Info(string message) { }

    public void Warning(string message)
    {
      _log.Append($"Warning: {message}\n");
    }

    public void Error(string message)
    {
      _log.Append($"Error: {message}\n");
    }

    public void LogError(string message)
    {
      Error(message);
    }

    public void SetPersistentText(string text) { }

    public void CaptureForeground() { }

    public string GetLog()
    {
      return _log.ToString();
    }
  }
}

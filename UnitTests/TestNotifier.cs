using System;
using System.ComponentModel;
using System.Text;
using InputMaster;

namespace UnitTests
{
  internal class TestNotifier : INotifier
  {
    private readonly StringBuilder Log = new StringBuilder();

    public ISynchronizeInvoke SynchronizingObject => throw new NotImplementedException();

    public void Write(string text) { }

    public void WriteWarning(string text)
    {
      Log.Append($"Warning: {text}\n");
    }

    public void WriteError(string text)
    {
      Log.Append($"Error: {text}\n");
    }

    public void SetPersistentText(string text) { }

    public void CaptureForeground() { }

    public string GetLog()
    {
      return Log.ToString();
    }
  }
}

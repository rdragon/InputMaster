using InputMaster;
using System.Text;

namespace UnitTests
{
  class FakeNotifier : INotifier
  {
    private StringBuilder Log = new StringBuilder();

    public int LogLength => Log.Length;

    public string GetLog()
    {
      return Log.ToString();
    }

    public void Write(string text) { }

    public void WriteWarning(string text)
    {
      Log.Append($"Warning: {text}\n");
    }

    public void WriteError(string text)
    {
      Log.Append($"Error: {text}\n");
    }

    public void ShowLog() { }

    public void SetPersistentText(string text) { }

    public void CaptureForeground() { }

    public void Disable() { }

    public void RequestExit() { WriteError("Exit requested."); }
  }
}

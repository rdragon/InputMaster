using InputMaster;
using System.Text;

namespace UnitTests
{
  class FakeNotifier : INotifier
  {
    private StringBuilder Log = new StringBuilder();

    public int LogLength { get { return Log.Length; } }

    public string GetLog()
    {
      return Log.ToString();
    }

    public void Write(string text) { }

    public void WriteWarning(string text)
    {
      Log.AppendLine("Warning: " + text);
    }

    public void WriteError(string text)
    {
      Log.AppendLine("Error: " + text);
    }

    public void ShowLog() { }

    public void SetPersistentText(string text) { }

    public void CaptureForeground() { }

    public void Disable() { }

    public void RequestExit() { WriteError("Exit requested."); }
  }
}

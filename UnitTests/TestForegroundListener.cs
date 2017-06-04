using InputMaster;

namespace UnitTests
{
  internal class TestForegroundListener : IForegroundListener
  {
    public string NewWindowTitle { get; set; }
    public string NewProcessName { get; set; }
    public string ForegroundWindowTitle { get; private set; }
    public string ForegroundProcessName { get; private set; }

    public void Update()
    {
      if (NewWindowTitle != null)
      {
        ForegroundWindowTitle = NewWindowTitle;
        NewWindowTitle = null;
        Env.StateCounter++;
      }
      if (NewProcessName != null)
      {
        ForegroundProcessName = NewProcessName;
        NewProcessName = null;
        Env.StateCounter++;
      }
    }

    public void Reset()
    {
      NewProcessName = "";
      NewWindowTitle = "";
      Update();
    }
  }
}

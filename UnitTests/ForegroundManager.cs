using InputMaster;

namespace UnitTests
{
  class ForegroundManager : ForegroundListenerBase
  {
    private string NewWindowTitle;
    private string NewProcessName;

    public ForegroundManager(FlagManager flagManager, IParserOutputProvider parserOutputProvider) : base(flagManager, parserOutputProvider)
    {
      FlagManager = flagManager;
    }

    public FlagManager FlagManager { get; }

    public override void Update()
    {
      if (NewWindowTitle != null)
      {
        ForegroundWindowTitle = NewWindowTitle;
        NewWindowTitle = null;
        Counter++;
      }
      if (NewProcessName != null)
      {
        ForegroundProcessName = NewProcessName;
        NewProcessName = null;
        Counter++;
      }
    }

    public void SetProcessName(string name)
    {
      NewProcessName = name;
    }

    public void SetWindowTitle(string title)
    {
      NewWindowTitle = title;
    }

    public void Reset()
    {
      NewProcessName = "";
      NewWindowTitle = "";
      FlagManager.ClearFlags();
    }
  }
}

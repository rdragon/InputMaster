using System;

namespace InputMaster
{
  abstract class ForegroundListenerBase : IForegroundListener
  {
    private readonly IFlagViewer FlagViewer;

    public ForegroundListenerBase(IFlagViewer flagViewer, IParserOutputProvider parserOutputProvider)
    {
      FlagViewer = Helper.ForbidNull(flagViewer, nameof(flagViewer));
      FlagViewer.FlagsChanged += () => { Counter++; FlagsChanged(); };
      parserOutputProvider.NewParserOutput += (parserOutput) =>
      {
        if (!parserOutput.AdditionalModesOutput)
        {
          DynamicHotkeyCollection = parserOutput.DynamicHotkeyCollection;
        }
      };
    }

    public event Action FlagsChanged = delegate { };

    public string ForegroundWindowTitle { get; protected set; } = "";
    public string ForegroundProcessName { get; protected set; } = "";
    public DynamicHotkeyCollection DynamicHotkeyCollection { get; private set; } = new DynamicHotkeyCollection();
    public int Counter { get; protected set; }

    public bool IsFlagSet(string flag)
    {
      return FlagViewer.IsFlagSet(flag);
    }

    public abstract void Update();

    public string GetForegroundInfoSuffix()
    {
      return Helper.GetBindingsSuffix(ForegroundProcessName, nameof(ForegroundProcessName), ForegroundWindowTitle, nameof(ForegroundWindowTitle));
    }
  }
}

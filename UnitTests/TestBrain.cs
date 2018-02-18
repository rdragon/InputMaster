using System.Text;
using InputMaster;
using InputMaster.Actors;
using InputMaster.Hooks;
using InputMaster.Parsers;
using InputMaster.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
  public class TestBrain : Actor
  {
    private readonly TestOutputHandler _outputHandler;
    private readonly InputReader _inputReader = new InputReader(InputReaderFlags.AllowHoldRelease | InputReaderFlags.AllowCustomModifier |
      InputReaderFlags.AllowMultiplier);
    private TestInjector _primaryHookInjector;
    private TestPrimaryHook _primaryHook;
    private InputRelay _inputRelay;
    private InputHook _inputHook;
    private ComboRelay _comboRelay;
    private ComboHook _comboHook;
    private TestForegroundListener _foregroundListener;

    public TestBrain(TestOutputHandler outputHandler)
    {
      _outputHandler = outputHandler;
    }

    public void Run()
    {
      _foregroundListener = (TestForegroundListener)Env.ForegroundListener;
      _comboHook = new ComboHook();
      _comboRelay = new ComboRelay(Env.ModeHook, _comboHook);
      _inputHook = new InputHook(_comboRelay);
      _inputRelay = new InputRelay(_inputHook);
      _primaryHook = new TestPrimaryHook(_inputRelay);
      _primaryHookInjector = new TestInjector(_primaryHook);
      Env.AddActor(new MiscActor());
      Env.Parser.UpdateHotkeyFile(new HotkeyFile(nameof(TestBrain), Helper.RemoveCarriageReturns(Resources.Tests)));
    }

    [Command(CommandTypes.ExecuteAtParseTime | CommandTypes.Chordless | CommandTypes.TopLevelOnly)]
    public void Reset(ExecuteAtParseTimeData data)
    {
      data.ParserOutput.Clear();
    }

    [Command(CommandTypes.ExecuteAtParseTime | CommandTypes.Chordless | CommandTypes.TopLevelOnly)]
    public void Test(ExecuteAtParseTimeData data, LocatedString toSimulate, string expectedOutput, string processName = null,
      string windowTitle = null, string flag = null)
    {
      Env.Parser.FireNewParserOutput(data.ParserOutput);
      _primaryHook.Reset();
      _outputHandler.Reset();
      _foregroundListener.Reset();
      Env.FlagManager.ClearFlags();
      if (!string.IsNullOrEmpty(processName))
        _foregroundListener.NewProcessName = processName;
      if (!string.IsNullOrEmpty(windowTitle))
        _foregroundListener.NewWindowTitle = windowTitle;
      if (!string.IsNullOrEmpty(flag))
        Env.FlagManager.ToggleFlag(flag);
      _primaryHookInjector.CreateInjector().Add(toSimulate, _inputReader).Run();
      var output = _outputHandler.GetStateInfo();
      var state = _primaryHook.GetStateInfo();
      var errorLog = Env.Notifier.GetLog();
      if (expectedOutput != output || errorLog.Length > 0 || state.Length > 0)
      {
        var sb = new StringBuilder($"\n\n{data.LocatedName.Location}:\n");
        if (expectedOutput != output)
        {
          var expected = expectedOutput.Length == 0 ? "(no output)" : expectedOutput;
          var actual = output.Length == 0 ? "(no output)" : output;
          sb.Append($"Expected output: {expected}\n");
          sb.Append($"Actual output: {actual}\n\n");
        }
        if (errorLog.Length > 0)
          sb.Append($"Unexpected error(s):\n{errorLog}\n\n");
        if (state.Length > 0)
          sb.Append($"Invalid state after simulation:\n{state}\n\n");
        Assert.Fail(sb.ToString());
      }
    }
  }
}

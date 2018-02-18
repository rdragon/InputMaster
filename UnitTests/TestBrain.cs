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
    private readonly TestOutputHandler OutputHandler;
    private readonly InputReader InputReader = new InputReader(InputReaderFlags.AllowHoldRelease | InputReaderFlags.AllowCustomModifier | InputReaderFlags.AllowMultiplier);
    private TestInjector PrimaryHookInjector;
    private TestPrimaryHook PrimaryHook;
    private InputRelay InputRelay;
    private InputHook InputHook;
    private ComboRelay ComboRelay;
    private ComboHook ComboHook;
    private TestForegroundListener ForegroundListener;

    public TestBrain(TestOutputHandler outputHandler)
    {
      OutputHandler = outputHandler;
    }

    public void Run()
    {
      ForegroundListener = (TestForegroundListener)Env.ForegroundListener;
      ComboHook = new ComboHook();
      ComboRelay = new ComboRelay(Env.ModeHook, ComboHook);
      InputHook = new InputHook(ComboRelay);
      InputRelay = new InputRelay(InputHook);
      PrimaryHook = new TestPrimaryHook(InputRelay);
      PrimaryHookInjector = new TestInjector(PrimaryHook);
      Env.AddActor(new MiscActor());
      Env.Parser.UpdateHotkeyFile(new HotkeyFile(nameof(TestBrain), Helper.RemoveCarriageReturns(Resources.Tests)));
    }

    [Command(CommandTypes.ExecuteAtParseTime | CommandTypes.Chordless | CommandTypes.TopLevelOnly)]
    public void Reset(ExecuteAtParseTimeData data)
    {
      data.ParserOutput.Clear();
    }

    [Command(CommandTypes.ExecuteAtParseTime | CommandTypes.Chordless | CommandTypes.TopLevelOnly)]
    public void Test(ExecuteAtParseTimeData data, LocatedString toSimulate, string expectedOutput, string processName = null, string windowTitle = null, string flag = null)
    {
      Env.Parser.FireNewParserOutput(data.ParserOutput);
      PrimaryHook.Reset();
      OutputHandler.Reset();
      ForegroundListener.Reset();
      Env.FlagManager.ClearFlags();
      if (!string.IsNullOrEmpty(processName))
        ForegroundListener.NewProcessName = processName;
      if (!string.IsNullOrEmpty(windowTitle))
        ForegroundListener.NewWindowTitle = windowTitle;
      if (!string.IsNullOrEmpty(flag))
        Env.FlagManager.ToggleFlag(flag);
      PrimaryHookInjector.CreateInjector().Add(toSimulate, InputReader).Run();
      var output = OutputHandler.GetStateInfo();
      var state = PrimaryHook.GetStateInfo();
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

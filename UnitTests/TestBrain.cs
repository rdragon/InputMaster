using System.Text;
using InputMaster;
using InputMaster.Hooks;
using InputMaster.Parsers;
using InputMaster.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
  internal class TestBrain : Actor
  {
    private readonly TestOutputHandler OutputHandler;
    private readonly InputReader InputReader = new InputReader(InputReaderFlags.AllowHoldRelease | InputReaderFlags.AllowCustomModifier | InputReaderFlags.AllowMultiplier);
    private TestInjector PrimaryHookInjector;
    private TestPrimaryHook PrimaryHook;
    private InputRelay InputRelay;
    private InputHook InputHook;
    private ComboRelay ComboRelay;
    private ComboHook ComboHook;
    private ModeHook ModeHook;
    private TestForegroundListener ForegroundListener;

    public TestBrain(TestOutputHandler outputHandler)
    {
      OutputHandler = outputHandler;
    }

    public void Run()
    {
      ForegroundListener = Env.ForegroundListener as TestForegroundListener;
      ModeHook = new ModeHook();
      ComboHook = new ComboHook();
      ComboRelay = new ComboRelay(ModeHook, ComboHook);
      InputHook = new InputHook(ComboRelay);
      InputRelay = new InputRelay(InputHook);
      PrimaryHook = new TestPrimaryHook(InputRelay);
      PrimaryHookInjector = new TestInjector(PrimaryHook);
      Env.AddActor(new MiscActor());
      Env.Parser.UpdateHotkeyFile(new HotkeyFile(nameof(TestBrain), Resources.Tests.Replace("\r\n", "\n")));
      Env.Parser.EnableOnce();
    }

    [CommandTypes(CommandTypes.ExecuteAtParseTime | CommandTypes.Chordless | CommandTypes.Visible | CommandTypes.TopLevelOnly)]
    public void Reset(ExecuteAtParseTimeData data)
    {
      data.ParserOutput.Clear();
    }

    [CommandTypes(CommandTypes.ExecuteAtParseTime | CommandTypes.Chordless | CommandTypes.Visible | CommandTypes.TopLevelOnly)]
    public void Test(ExecuteAtParseTimeData data, LocatedString toSimulate, string expectedOutput, string processName = null, string windowTitle = null, string flag = null)
    {
      Env.Parser.FireNewParserOutput(data.ParserOutput);
      PrimaryHook.Reset();
      OutputHandler.Reset();
      ForegroundListener.Reset();
      Env.FlagManager.ClearFlags();
      if (!string.IsNullOrEmpty(processName))
      {
        ForegroundListener.NewProcessName = processName;
      }
      if (!string.IsNullOrEmpty(windowTitle))
      {
        ForegroundListener.NewWindowTitle = windowTitle;
      }
      if (!string.IsNullOrEmpty(flag))
      {
        Env.FlagManager.ToggleFlag(flag);
      }
      PrimaryHookInjector.CreateInjector().Add(toSimulate, InputReader).Run();
      var output = OutputHandler.GetStateInfo();
      var state = PrimaryHook.GetStateInfo();
      var errorLog = Env.Notifier.GetLog();
      if (expectedOutput != output || errorLog.Length > 0 || state.Length > 0)
      {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine(data.LocatedName.Location + ":");
        if (expectedOutput != output)
        {
          sb.AppendLine("Expected output: " + (expectedOutput.Length == 0 ? "(no output)" : expectedOutput));
          sb.AppendLine("Actual output: " + (output.Length == 0 ? "(no output)" : output));
          sb.AppendLine();
        }
        if (errorLog.Length > 0)
        {
          sb.AppendLine("Unexpected error(s):");
          sb.AppendLine(errorLog);
          sb.AppendLine();
        }
        if (state.Length > 0)
        {
          sb.AppendLine("Invalid state after simulation:");
          sb.AppendLine(state);
          sb.AppendLine();
        }
        Assert.Fail(sb.ToString());
      }
    }
  }
}

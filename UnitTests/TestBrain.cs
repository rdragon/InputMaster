using InputMaster.Parsers;
using InputMaster.Hooks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using InputMaster;
using InputMaster.Properties;
using System.IO;

namespace UnitTests
{
  class TestBrain
  {
    private readonly FakeNotifier Notifier = new FakeNotifier();
    private readonly StringBuilder Output = new StringBuilder();
    private ForegroundManager ForegroundManager;
    private FakeInjectorFactory SourceFactory;
    private InputReader SourceInputReader;
    private InputReader SinkInputReader;
    private ParserOutputProvider ParserOutputProvider;
    private InputRelay InputRelay;

    public void Run()
    {
      var assertFailed = false;
      Env.Notifier = Notifier;
      try
      {
        SourceFactory = new FakeInjectorFactory();
        SourceFactory.Injecting += (input, down) =>
        {
          var e = new InputArgs(input, down);
          InputRelay.Handle(e);
          if (!e.Capture)
          {
            Env.CreateInjector().Add(input, down).Run();
          }
        };
        var sinkFactory = new FakeInjectorFactory();
        Env.InjectorPrototype = sinkFactory.CreateInjector();
        var outputHandler = new OutputHandler(Output);
        sinkFactory.Injecting += outputHandler.Handle;
        SourceInputReader = new InputReader(InputReaderFlags.AllowHoldRelease | InputReaderFlags.AllowCustomModifier | InputReaderFlags.AllowMultiplier);
        SinkInputReader = new InputReader(InputReaderFlags.AllowMultiplier);
        var commandCollection = new CommandCollection();
        var parser = new Parser(commandCollection);
        var flagManager = new FlagManager(parser);
        ParserOutputProvider = new ParserOutputProvider();
        ForegroundManager = new ForegroundManager(flagManager, ParserOutputProvider);
        Env.ForegroundListener = ForegroundManager;
        var modeHook = new ModeHook(ParserOutputProvider);
        var comboHook = new ComboHook(ParserOutputProvider);
        var comboRelay = new ComboRelay(modeHook, comboHook);
        var inputHook = new InputHook(comboRelay);
        InputRelay = new InputRelay(inputHook);
        var actor = new Actor();
        commandCollection.AddActors(this, flagManager, ForegroundManager, modeHook, comboHook, inputHook, InputRelay, actor);
        parser.UpdateHotkeyFile(new HotkeyFile(nameof(TestBrain), Resources.Tests.Replace("\r\n", "\n")));
        parser.Parse();
      }
      catch (Exception ex) when (Helper.HasAssertFailed(ex))
      {
        assertFailed = true;
        throw;
      }
      catch (Exception ex) when (!Helper.IsCriticalException(ex))
      {
        Notifier.WriteError(ex);
      }
      finally
      {
        if (!assertFailed && Notifier.LogLength > 0)
        {
          Assert.Fail("Unexpected output: " + Notifier.GetLog());
        }
        Try.ThrowException();
      }
    }

    [CommandTypes(CommandTypes.ExecuteAtParseTime | CommandTypes.Chordless | CommandTypes.Visible | CommandTypes.TopLevelOnly)]
    public void Reset(ExecuteAtParseTimeData data)
    {
      data.ParserOutput.Clear();
    }

    [CommandTypes(CommandTypes.ExecuteAtParseTime | CommandTypes.Chordless | CommandTypes.Visible | CommandTypes.TopLevelOnly)]
    public void Test(ExecuteAtParseTimeData data, LocatedString toSimulate, string expectedOutput, string processName = null, string windowTitle = null, string flag = null)
    {
      ParserOutputProvider.SetParserOutput(data.ParserOutput);
      InputRelay.Reset();
      ForegroundManager.Reset();
      if (!string.IsNullOrEmpty(processName))
      {
        ForegroundManager.SetProcessName(processName);
      }
      if (!string.IsNullOrEmpty(windowTitle))
      {
        ForegroundManager.SetWindowTitle(windowTitle);
      }
      if (!string.IsNullOrEmpty(flag))
      {
        ForegroundManager.FlagManager.ToggleFlag(flag);
      }
      Output.Clear();
      SourceFactory.CreateInjector().Add(toSimulate, SourceInputReader).Run();
      var output = Output.ToString();
      var state = InputRelay.GetStateInfo();
      if (expectedOutput != output || Notifier.LogLength > 0 || state.Length > 0)
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
        if (Notifier.LogLength > 0)
        {
          sb.AppendLine("Unexpected output:");
          sb.AppendLine(Notifier.GetLog());
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

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using InputMaster.Parsers;

namespace InputMaster
{
  [CommandTypes(CommandTypes.Visible)]
  internal class MiscActor : Actor
  {
    public void ExitInputMaster()
    {
      Application.Exit();
    }

    public void RestartInputMaster()
    {
      Env.ShouldRestart = true;
      Application.Exit();
    }

    public static void RunElevated([AllowSpaces] string filePath, [AllowSpaces]string arguments = "")
    {
      Helper.StartProcess(filePath, arguments);
    }

    public static void EditElevated([AllowSpaces]string filePath)
    {
      if (Config.PreprocessorReplaces.TryGetValue("Notepadpp", out var exePath))
      {
        RunElevated(exePath, $"-multiInst \"{filePath}\"");
      }
      else
      {
        Env.Notifier.WriteError("Notepad++ path not set.");
      }
    }

    public static void WriteLine([AllowSpaces] string text)
    {
      Env.Notifier.Write(text);
    }

    public static void PrintDate(TimeSpan? timeDifference = null)
    {
      var d = timeDifference.GetValueOrDefault(TimeSpan.Zero);
      Env.CreateInjector().Add(DateTime.Now.Add(d).ToString("yyyy-MM-dd"), new InputReader(InputReaderFlags.ParseLiteral)).Run();
    }

    public static void PrintFormattedDate([AllowSpaces] string format)
    {
      Env.CreateInjector().Add(DateTime.Now.ToString(format), new InputReader(InputReaderFlags.ParseLiteral)).Run();
    }

    public static void Send([AllowSpaces] Action action)
    {
      action();
    }

    public void SendDynamic([AllowSpaces] LocatedString located)
    {
      Env.CreateInjector().Add(located, new InputReader(Config.DefaultInputReader.Flags | InputReaderFlags.AllowDynamicHotkey)).Run();
    }

    [CommandTypes(CommandTypes.Chordless | CommandTypes.ExecuteAtParseTime | CommandTypes.StandardSectionOnly)]
    public void Define(ExecuteAtParseTimeData data, LocatedString token, [AllowSpaces] LocatedString argument)
    {
      var name = Helper.ReadIdentifierTokenString(token);
      void Action(IInjectorStream<object> stream)
      {
        stream.Add(argument, Config.DefaultInputReader);
      }
      Action(Env.CreateInjector()); // Test if argument is in correct format.
      data.ParserOutput.DynamicHotkeyCollection.AddDynamicHotkey(name, Action, data.Section.AsStandardSection);
    }

    [CommandTypes(CommandTypes.Chordless | CommandTypes.ExecuteAtParseTime | CommandTypes.ModeOnly)]
    public void IncludeMode(ExecuteAtParseTimeData data, LocatedString modeName)
    {
      data.Section.AsMode.IncludeMode(modeName.Value);
    }

    [CommandTypes(CommandTypes.Chordless | CommandTypes.ExecuteAtParseTime | CommandTypes.TopLevelOnly)]
    public void MutualExclusiveFlags(ExecuteAtParseTimeData data, [AllowSpaces]string names)
    {
      data.ParserOutput.FlagSets.Add(new HashSet<string>(names.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)));
    }

    public static void PrintInput(HotkeyTrigger trigger)
    {
      Env.CreateInjector().Add(trigger.Combo.ToString(), Config.LiteralInputReader).Run();
    }

    public static void WriteMousePosition()
    {
      Env.Notifier.Write(Cursor.Position.X + ", " + Cursor.Position.Y);
    }

    public static void Beep()
    {
      Helper.Beep();
    }

    public static void ShowLog()
    {
      Helper.ShowSelectableText(Env.Notifier.GetLog());
    }

    public static void Nothing(string dummy = "") { }

    public void PutComputerToSleep()
    {
      if (Env.FlagManager.IsSet("NoStandby"))
      {
        Env.Notifier.Write("Standby suppressed.");
      }
      else
      {
        Application.SetSuspendState(PowerState.Suspend, false, true);
      }
    }

    [CommandTypes(CommandTypes.Chordless | CommandTypes.ExecuteAtParseTime)]
    public static void ComposeHelper(ExecuteAtParseTimeData data, [AllowSpaces] LocatedString locatedArgument)
    {
      foreach (var s in locatedArgument.Split(" "))
      {
        if (s.Length == 1)
        {
          throw new ParseException(s, "Expecting more than one character.");
        }
        if (s.Length > 0)
        {
          var chord = Config.DefaultChordReader.CreateChord(s.Substring(0, s.Value.Length - 1));
          var action = Env.CreateInjector().Add(s.Value[s.Length - 1].ToString(), Config.DefaultInputReader).Compile();
          data.ParserOutput.AddHotkey(data.Section, chord, combo => action(), null);
        }
      }
    }

    public static void Standby()
    {
      if (Env.FlagManager.IsSet("NoStandby"))
      {
        Env.Notifier.Write("Standby suppressed.");
      }
      else
      {
        Application.SetSuspendState(PowerState.Suspend, false, true);
      }
    }
  }
}

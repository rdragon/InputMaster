using InputMaster.Parsers;
using System;
using System.Windows.Forms;

namespace InputMaster
{
  [CommandTypes(CommandTypes.Visible)]
  class Actor
  {

    public void ExitInputMaster()
    {
      Env.Notifier.RequestExit();
    }

    public void RestartInputMaster()
    {
      Program.ShouldRestart = true;
      ExitInputMaster();
    }

    public static void Run([AllowSpaces] string filePath, [AllowSpaces]string arguments = "")
    {
      Helper.StartProcess(filePath, arguments);
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
      Action<IInjectorStream<object>> action = (stream) =>
      {
        stream.Add(argument, Config.DefaultInputReader);
      };
      action(Env.CreateInjector()); // Test if argument is in correct format.
      data.ParserOutput.DynamicHotkeyCollection.AddDynamicHotkey(name, action, data.Section.AsStandardSection);
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
      Env.Notifier.ShowLog();
    }

    public static void Nothing(string dummy = "") { }

    public void PutComputerToSleep()
    {
      if (Env.ForegroundListener.IsFlagSet("NoStandby"))
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
      foreach (var s in locatedArgument.Split(' '))
      {
        if (s.Length == 1)
        {
          throw new ParseException(s, "Expecting more than one character.");
        }
        else if (s.Length > 0)
        {
          var chord = Config.DefaultChordReader.CreateChord(s.Substring(0, s.Value.Length - 1));
          var action = Env.CreateInjector().Add(s.Value[s.Length - 1].ToString(), Config.DefaultInputReader).Compile();
          data.ParserOutput.AddHotkey(data.Section, chord, (combo) => action(), null);
        }
      }
    }

    public static void Standby()
    {
      if (Env.ForegroundListener.IsFlagSet("NoStandby"))
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

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using InputMaster.Parsers;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace InputMaster.Actors
{
  public class MiscActor : Actor
  {
    [Command]
    private static void ExitInputMaster()
    {
      Application.Exit();
    }

    [Command]
    private static void RestartInputMaster([AllowSpaces]string arguments = "")
    {
      Env.ShouldRestart = true;
      Env.RestartArguments = arguments;
      Application.Exit();
    }

    [Command]
    private static Task RunElevated([AllowSpaces] string filePath, [AllowSpaces]string arguments = "")
    {
      return Helper.StartProcessAsync(filePath, arguments);
    }

    [Command]
    private static void EditElevated([AllowSpaces]string filePath)
    {
      RunElevated(Env.Config.Notepadpp, $"-multiInst \"{filePath}\"");
    }

    [Command]
    private static void WriteLine([AllowSpaces] string text)
    {
      Env.Notifier.Info(text);
    }

    [Command]
    private static void PrintDate(TimeSpan? timeDifference = null)
    {
      var d = timeDifference.GetValueOrDefault(TimeSpan.Zero);
      Env.CreateInjector().Add(DateTime.Now.Add(d).ToString("yyyy-MM-dd"), new InputReader(InputReaderFlags.ParseLiteral)).Run();
    }

    [Command]
    private static void PrintFormattedDate([AllowSpaces] string format)
    {
      Env.CreateInjector().Add(DateTime.Now.ToString(format), new InputReader(InputReaderFlags.ParseLiteral)).Run();
    }

    [Command]
    private static void Send([AllowSpaces] Action action)
    {
      action();
    }

    [Command]
    public static void SendDynamic([AllowSpaces] LocatedString located)
    {
      Env.CreateInjector().Add(located, new InputReader(Env.Config.DefaultInputReader.Flags | InputReaderFlags.AllowDynamicHotkey)).Run();
    }

    [Command(CommandTypes.Chordless | CommandTypes.ExecuteAtParseTime | CommandTypes.StandardSectionOnly)]
    private static void Define(ExecuteAtParseTimeData data, LocatedString token, [AllowSpaces] LocatedString argument)
    {
      var name = Helper.ReadIdentifierTokenString(token);
      void Action(IInjectorStream<object> stream)
      {
        stream.Add(argument, Env.Config.DefaultInputReader);
      }
      Action(Env.CreateInjector()); // Test if argument is in correct format.
      data.ParserOutput.DynamicHotkeyCollection.AddDynamicHotkey(name, Action, (StandardSection)data.Section);
    }

    [Command(CommandTypes.Chordless | CommandTypes.ExecuteAtParseTime | CommandTypes.ModeOnly)]
    private static void IncludeMode(ExecuteAtParseTimeData data, LocatedString modeName)
    {
      ((Mode)data.Section).IncludeMode(modeName.Value);
    }

    [Command(CommandTypes.Chordless | CommandTypes.ExecuteAtParseTime | CommandTypes.TopLevelOnly)]
    private static void MutualExclusiveFlags(ExecuteAtParseTimeData data, [AllowSpaces]string names)
    {
      data.ParserOutput.FlagSets.Add(new HashSet<string>(names.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)));
    }

    [Command]
    private static void PrintInput(HotkeyTrigger trigger)
    {
      Env.CreateInjector().Add(trigger.Combo.ToString(), Env.Config.LiteralInputReader).Run();
    }

    [Command]
    private static void WriteMousePosition()
    {
      Env.Notifier.Info(Cursor.Position.X + ", " + Cursor.Position.Y);
    }

    [Command]
    private static void Beep()
    {
      Helper.Beep();
    }

    [Command]
    private static void ShowLog()
    {
      Helper.ShowSelectableText(Env.Notifier.GetLog());
    }

    [Command]
    private static void Nothing(string dummy = "") { }

    [Command]
    private static void PutComputerToSleep()
    {
      if (Env.FlagManager.HasFlag("NoStandby"))
        Env.Notifier.Info("Standby suppressed.");
      else
        Application.SetSuspendState(PowerState.Suspend, false, true);
    }

    [Command(CommandTypes.Chordless | CommandTypes.ExecuteAtParseTime)]
    private static void ComposeHelper(ExecuteAtParseTimeData data, [AllowSpaces] LocatedString locatedArgument)
    {
      foreach (var s in locatedArgument.Split(" "))
      {
        if (s.Length == 1)
          throw new ParseException(s, "Expecting more than one character.");
        if (s.Length > 0)
        {
          var chord = Env.Config.DefaultChordReader.CreateChord(s.Substring(0, s.Value.Length - 1));
          var action = Env.CreateInjector().Add(s.Value[s.Length - 1].ToString(), Env.Config.DefaultInputReader).Compile();
          data.ParserOutput.AddHotkey(data.Section, chord, combo => action(), null);
        }
      }
    }

    [Command]
    private static void Standby()
    {
      if (Env.FlagManager.HasFlag("NoStandby"))
        Env.Notifier.Info("Standby suppressed.");
      else
        Application.SetSuspendState(PowerState.Suspend, false, true);
    }

    [Command]
    public static async Task SaveScreen()
    {
      var bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
      var graphics = Graphics.FromImage(bitmap);
      graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
      var name = await Helper.TryGetStringAsync("Name", Helper.GetValidFileName(DateTime.Now.ToString(), '-'));
      if (name == null)
        return;
      Directory.CreateDirectory(Env.Config.ScreenshotsDir);
      bitmap.Save(Path.Combine(Env.Config.ScreenshotsDir, name + ".jpg"), ImageFormat.Jpeg);
    }
  }
}

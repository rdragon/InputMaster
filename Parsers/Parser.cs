using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InputMaster.Parsers
{
  public class Parser : IParser
  {
    public event Action<ParserOutput> NewParserOutput = delegate { };
    private static readonly Regex CommentRegex = new Regex($"{Regex.Escape(Constants.CommentIdentifier)}.*$", RegexOptions.Multiline);
    private static readonly Regex PreprocessorReplaceRegex = new Regex($@"{Constants.SpecialChar}\((?<ident>{Constants.InnerIdentifierTokenPattern})\)");
    private readonly Dictionary<string, HotkeyFile> HotkeyFiles = new Dictionary<string, HotkeyFile>();
    private readonly Dictionary<string, ParseAction> ParseActions = new Dictionary<string, ParseAction>();
    private DynamicHotkeyCollection DynamicHotkeyCollection = new DynamicHotkeyCollection();

    public Parser()
    {
      if (Env.RunningUnitTests)
        return;
      Env.App.Run += async () =>
      {
        var hotkeyFileWatcher = new FileChangedWatcher(Env.Config.HotkeyFile);
        Env.App.Exiting += hotkeyFileWatcher.Dispose;
        hotkeyFileWatcher.TextChanged += text =>
        {
          UpdateHotkeyFile(new HotkeyFile("default", text));
          Run();
        };
        await hotkeyFileWatcher.RaiseChangedEventAsync();
      };
    }

    public static string RunPreprocessor(string text)
    {
      var sb = new StringBuilder(text.Length * 2);
      var i = 0;
      while (true)
      {
        var match = PreprocessorReplaceRegex.Match(text, i);
        if (match.Success)
        {
          sb.Append(text.Substring(i, match.Index - i));
          i = match.Index + match.Length;
          var name = match.Groups["ident"].Value;
          if (!Env.Config.TryGetPreprocessorReplace(name, out var s))
          {
            Env.Notifier.Warning($"Use of undefined preprocessor variable '{name}'.");
            s = "(undefined)";
          }
          sb.Append(s);
        }
        else
        {
          break;
        }
      }
      sb.Append(text.Substring(i));
      return sb.ToString();
    }

    private static Func<Combo, T> CreateFunc<T>(object actor, MethodInfo methodInfo, object[] arguments, bool insertTrigger)
    {
      if (insertTrigger)
        return combo => (T)methodInfo.Invoke(actor, new object[] { new HotkeyTrigger(combo) }.Concat(arguments).ToArray());
      return combo => (T)methodInfo.Invoke(actor, arguments);
    }

    private static Action<Combo> CreateAction(object actor, MethodInfo methodInfo, object[] arguments, bool insertTrigger)
    {
      if (methodInfo.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null || methodInfo.ReturnType == typeof(Task) ||
        methodInfo.ReturnType.IsGenericType && methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
      {
        if (methodInfo.ReturnType == typeof(Task))
        {
          var func = CreateFunc<Task>(actor, methodInfo, arguments, insertTrigger);
          return async combo =>
          {
            await func(combo);
          };
        }
        Env.Notifier.Warning($"Async function '{actor.GetType()}.{methodInfo.Name}' has a return type other than 'Task'. Any exceptions thrown by this function will cause the program to exit.");
      }
      var func1 = CreateFunc<object>(actor, methodInfo, arguments, insertTrigger);
      return combo => func1(combo);
    }

    private static Action<Combo> CreateAction(IEnumerable<Action<Combo>> actions)
    {
      var actionArray = actions.ToArray();
      return combo =>
      {
        foreach (var action in actionArray)
          action(combo);
      };
    }

    public void UpdateHotkeyFile(HotkeyFile hotkeyFile)
    {
      HotkeyFiles[hotkeyFile.Name] = hotkeyFile;
    }

    public void UpdateParseAction(string name, ParseAction action)
    {
      ParseActions[name] = action;
    }

    public void Run()
    {
      var parserOutput = new ParserOutput();
      try
      {
        foreach (var hotkeyFile in HotkeyFiles)
        {
          var text = CommentRegex.Replace(hotkeyFile.Value.Text, "");
          new MyCharReader(new LocatedString(RunPreprocessor(text), new Location(1, 1)), parserOutput).Run();
        }
        foreach (var action in ParseActions)
          action.Value(parserOutput);
        foreach (var mode in parserOutput.Modes)
          mode.ResolveIncludes(parserOutput);
      }
      catch (Exception ex) when (!Helper.IsFatalException(ex))
      {
        Env.Notifier.WriteError(ex, "Failed to parse hotkeys.");
        return;
      }
      FireNewParserOutput(parserOutput);
    }

    public void GetAction(string name, out Action<IInjectorStream<object>> action)
    {
      action = null;
      if (DynamicHotkeyCollection.TryGetValue(name, out var sortedSet))
      {
        foreach (var dynamicHotkey in sortedSet)
        {
          if (dynamicHotkey.Enabled)
            action = dynamicHotkey.Action;
        }
        if (action != null)
          return;
      }
      throw new ParseException($"No dynamic hotkey named '{name}' found" + Helper.GetBindingsSuffix(
        Env.ForegroundListener.ForegroundProcessName, nameof(Env.ForegroundListener.ForegroundProcessName),
        Env.ForegroundListener.ForegroundWindowTitle, nameof(Env.ForegroundListener.ForegroundWindowTitle),
        string.Join("|", Env.FlagManager.GetFlags()), "Flags"));
    }

    public bool IsDynamicHotkey(string name) => DynamicHotkeyCollection.ContainsKey(name);

    public void FireNewParserOutput(ParserOutput parserOutput)
    {
      DynamicHotkeyCollection = parserOutput.DynamicHotkeyCollection;
      NewParserOutput(parserOutput);
    }

    private class MyCharReader : CharReader
    {
      private readonly Stack<Section> Sections = new Stack<Section>();
      private readonly ParserOutput ParserOutput;
      private readonly InputReader ChordReader = Env.Config.DefaultChordReader;
      private readonly InputReader ModeChordReader = Env.Config.DefaultModeChordReader;
      private Chord Chord;

      public MyCharReader(LocatedString text, ParserOutput parserOutput) : base(text)
      {
        ParserOutput = parserOutput;
      }

      public void Run()
      {
        Sections.Clear();
        Sections.Push(new StandardSection());
        while (!EndOfStream)
        {
          ReadMany(' ');
          if (EndOfLine)
          {
            Read();
          }
          else
          {
            HandleColumn(Location.Column);
            if (TryRead(Constants.SectionIdentifier))
              ReadSectionHeader();
            else if (TryRead(Constants.SpecialCommandIdentifier))
              ReadSpecialCommand();
            else
              ReadHotkey();
          }
        }
      }

      private void HandleColumn(int column)
      {
        if (Sections.Peek().Column == -1)
        {
          var section = Sections.Pop();
          if (column > Sections.Peek().Column)
          {
            section.Column = column;
            Sections.Push(section);
          }
        }
        while (column < Sections.Peek().Column)
          Sections.Pop();
        if (column > Sections.Peek().Column)
          throw new ParseException(Location, "Unexpected indentation.");
      }

      private static Regex CreateRegex(LocatedString argument, RegexOptions regexOptions)
      {
        try
        {
          return Helper.GetRegex(argument.Value, regexOptions, fullMatchIfLiteral: true);
        }
        catch (Exception ex) when (ex is ArgumentException)
        {
          throw new ParseException(argument.Location, ex);
        }
      }

      private static Exception CreateException(CommandToken token, string message)
      {
        return new ParseException(token.LocatedName, message);
      }

      private void ReadSectionHeader()
      {
        if (Sections.Peek() is Mode)
          throw new ParseException(Location, "Cannot start section inside mode.");
        ReadSome(' ');
        var sectionType = ReadIdentifier();
        ReadSome(' ');
        var argument = ReadArguments().Require(targetCount: 1, delimiter: Constants.ArgumentDelimiter);
        Read('\n');
        var section = CreateSection(sectionType, argument);
        if (section is Mode mode)
        {
          if (Sections.Count > 1)
            throw new ParseException(sectionType.Location, "A mode is only valid at the top level.");
          section = ParserOutput.AddMode(mode);
        }
        Sections.Push(section);
      }

      private Section CreateSection(LocatedString type, LocatedString argument)
      {
        if (type.Value == nameof(RegexSectionType.Process) || type.Value == nameof(RegexSectionType.Window))
        {
          var sectionType = (RegexSectionType)Enum.Parse(typeof(RegexSectionType), type.Value);
          var regexOptions = sectionType == RegexSectionType.Process ? RegexOptions.IgnoreCase : RegexOptions.None;
          return new RegexSection((StandardSection)Sections.Peek(), CreateRegex(argument, regexOptions), sectionType);
        }
        if (type.Value == Constants.FlagSectionIdentifier)
          return new FlagSection((StandardSection)Sections.Peek(), argument.Value);
        if (type.Value == Constants.InputModeSectionIdentifier)
          return new Mode(argument.Value, false);
        if (type.Value == Constants.ComposeModeSectionIdentifier)
          return new Mode(argument.Value, true);
        throw new ParseException(type.Location, $"Unrecognized section type '{type.Value}'.");
      }

      private LocatedString ReadArguments()
      {
        return ReadUntil('\n').TrimEnd();
      }

      private void ReadHotkey()
      {
        Chord = ReadChord();
        ReadMany(' ');
        BeginReadCommands();
      }

      private void ReadSpecialCommand()
      {
        ReadSome(' ');
        Chord = null;
        BeginReadCommands();
      }

      private void BeginReadCommands()
      {
        var tokens = new List<CommandToken>();
        if (EndOfLine && Chord != null)
        {
          tokens.AddRange(ReadCommands());
          if (tokens.Count == 0)
            throw new ParseException(Location, "Expecting a command.");
        }
        else
        {
          tokens.Add(ReadCommand());
        }
        foreach (var token in tokens)
        {
          if (token.HasFlag(CommandTypes.Chordless) && Chord != null)
            throw CreateException(token, "This command cannot be bound to a chord.");
          if (!token.HasFlag(CommandTypes.Chordless) && Chord == null)
            throw CreateException(token, "This command has to be bound to a chord.");
          if (token.HasFlag(CommandTypes.TopLevelOnly) && Sections.Count > 1)
            throw CreateException(token, "This command is only valid at the top level.");
          if (token.HasFlag(CommandTypes.ModeOnly) && !(Sections.Peek() is Mode))
            throw CreateException(token, $"This command is only valid in a {Constants.InputModeSectionIdentifier} or {Constants.ComposeModeSectionIdentifier} section.");
          if (token.HasFlag(CommandTypes.ComposeModeOnly) && (!(Sections.Peek() is Mode mode) || !mode.IsComposeMode))
            throw CreateException(token, $"This command is only valid in a {Constants.ComposeModeSectionIdentifier} section.");
          if (token.HasFlag(CommandTypes.InputModeOnly) && (!(Sections.Peek() is Mode mode1) || !mode1.IsInputMode))
            throw CreateException(token, $"This command is only valid in a {Constants.InputModeSectionIdentifier} section.");
          if (token.HasFlag(CommandTypes.StandardSectionOnly) && !(Sections.Peek() is StandardSection))
            throw CreateException(token, "This command is only valid in a standard section.");
          if (token.HasFlag(CommandTypes.ExecuteAtParseTime) && tokens.Count > 1)
            throw CreateException(token, "This command cannot be combined with other commands.");
        }
        if (tokens[0].HasFlag(CommandTypes.ExecuteAtParseTime))
        {
          tokens[0].Action(Combo.None);
        }
        else
        {
          var action = tokens.Count > 1 ? CreateAction(tokens.Select(z => z.Action)) : tokens[0].Action;
          var description = Chord + " " + tokens[0].LocatedName.Value + " " + tokens[0].LocatedArguments.Value;
          ParserOutput.AddHotkey(Sections.Peek(), Chord, action, description);
        }
      }

      private CommandToken ReadCommand()
      {
        var locatedName = ReadIdentifier();
        if (!EndOfLine)
          Require(' ');
        ReadMany(' ');
        var locatedArguments = ReadArguments();
        Read('\n');
        var command = Env.CommandCollection.GetCommand(locatedName);
        var action = GetAction(command, locatedName, locatedArguments);
        return new CommandToken(command, locatedName, locatedArguments, action);
      }

      private IEnumerable<CommandToken> ReadCommands()
      {
        var commandTokens = new List<CommandToken>();
        var column = -1;
        while (!EndOfStream)
        {
          ReadMany(' ');
          if (EndOfLine)
            Read();
          else
          {
            if (column == -1)
            {
              if (Location.Column <= Sections.Peek().Column)
                throw new ParseException(Location, "Incorrect indentation.");
              column = Location.Column;
            }
            else if (Location.Column > column)
            {
              throw new ParseException(Location, "Unexpected indentation.");
            }
            else if (Location.Column < column)
            {
              break;
            }
            Read(Constants.MultipleCommandsIdentifier);
            ReadSome(' ');
            commandTokens.Add(ReadCommand());
          }
        }
        return commandTokens;
      }

      private Action<Combo> GetAction(Command command, LocatedString locatedName, LocatedString locatedArguments)
      {
        var parameters = new List<ParameterInfo>(command.MethodInfo.GetParameters());
        var arguments = new List<object>();
        var insertTrigger = false;
        if (command.CommandTypes.HasFlag(CommandTypes.ExecuteAtParseTime) && parameters.Count > 0 && parameters[0].ParameterType == typeof(ExecuteAtParseTimeData))
        {
          arguments.Add(new ExecuteAtParseTimeData(ParserOutput, Sections.Peek(), Chord, locatedName));
          parameters.RemoveAt(0);
        }
        if (!command.CommandTypes.HasFlag(CommandTypes.ExecuteAtParseTime) && parameters.Count > 0 && parameters[0].ParameterType == typeof(HotkeyTrigger))
        {
          insertTrigger = true;
          parameters.RemoveAt(0);
        }
        arguments.AddRange(locatedArguments.ReadArguments(parameters));
        return CreateAction(command.Actor, command.MethodInfo, arguments.ToArray(), insertTrigger);
      }

      private Chord ReadChord()
      {
        var locatedString = ReadUntil(' ', '\n');
        return (Sections.Peek() is Mode ? ModeChordReader : ChordReader).CreateChord(locatedString);
      }
    }
  }

  public delegate void ParseAction(ParserOutput parserOutput);
}

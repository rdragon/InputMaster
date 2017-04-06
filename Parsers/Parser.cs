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
  class Parser : IParserOutputProvider
  {
    private readonly CommandCollection CommandCollection;
    private readonly Dictionary<string, HotkeyFile> HotkeyFiles = new Dictionary<string, HotkeyFile>();
    private readonly Dictionary<string, ParseAction> ParseActions = new Dictionary<string, ParseAction>();

    public Parser(CommandCollection commandCollection, FileChangedWatcher hotkeyFileWatcher = null)
    {
      CommandCollection = Helper.ForbidNull(commandCollection, nameof(commandCollection));
      if (hotkeyFileWatcher != null)
      {
        hotkeyFileWatcher.TextChanged += (text) =>
        {
          UpdateHotkeyFile(new HotkeyFile("default", text));
          Parse();
        };
      }
    }

    public event Action<ParserOutput> NewParserOutput = delegate { };

    public void UpdateHotkeyFile(HotkeyFile hotkeyFile)
    {
      HotkeyFiles[hotkeyFile.Name] = hotkeyFile;
    }

    public void UpdateParseAction(string name, ParseAction action)
    {
      ParseActions[name] = action;
    }

    public void Parse()
    {
      var parserOutput = new ParserOutput();
      try
      {
        foreach (var hotkeyFile in HotkeyFiles)
        {
          new MyCharReader(new LocatedString(StripComments(hotkeyFile.Value.Text), new Location(1, 1)), this, parserOutput).Run();
        }
        foreach (var action in ParseActions)
        {
          action.Value(parserOutput);
        }
      }
      catch (Exception ex) when (!Helper.IsCriticalException(ex))
      {
        Env.Notifier.WriteError(ex, $"Error during parsing.");
        return;
      }
      NewParserOutput(parserOutput);
    }

    private string StripComments(string text)
    {
      var sb = new StringBuilder(text.Length);
      for (int i = 0; i < text.Length; i++)
      {
        if (text[i] == Config.CommentStart)
        {
          while (i + 1 < text.Length && text[i + 1] != '\n')
          {
            i++;
          }
        }
        else
        {
          sb.Append(text[i]);
        }
      }
      return sb.ToString();
    }

    private class MyCharReader : CharReader
    {
      private readonly Stack<Section> Sections = new Stack<Section>();
      private readonly ParserOutput ParserOutput;
      private readonly Parser Parser;
      private readonly InputReader ChordReader = Config.DefaultChordReader;
      private readonly InputReader ModeChordReader = Config.DefaultModeChordReader;
      private Chord Chord;

      public MyCharReader(LocatedString text, Parser parser, ParserOutput parserOutput) : base(text)
      {
        Parser = parser;
        ParserOutput = parserOutput;
      }

      public ParserOutput Run()
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
            if (Current == Config.SectionChar)
            {
              ReadSectionHeader();
            }
            else if (At(Config.SpecialCommandPrefix))
            {
              ReadSpecialCommand();
            }
            else
            {
              ReadHotkey();
            }
          }
        }
        return ParserOutput;
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
        {
          Sections.Pop();
        }
        if (column > Sections.Peek().Column)
        {
          throw new ParseException(Location, "Unexpected indentation.");
        }
      }

      private static Regex CreateRegex(LocatedString argument, RegexOptions regexOptions)
      {
        try
        {
          return Helper.GetRegex(argument.Value, regexOptions, fullMatchIfLiteral: true);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is ArgumentOutOfRangeException)
        {
          throw new ParseException(argument.Location, ex);
        }
      }

      private void ReadSectionHeader()
      {
        if (Sections.Peek().IsMode)
        {
          throw new ParseException(Location, $"Cannot start section inside mode.");
        }
        Read(Config.SectionChar);
        ReadSome(' ');
        var sectionType = ReadIdentifier();
        ReadSome(' ');
        var argument = ReadArguments().Require(targetCount: 1, delimiter: Config.ArgumentDelimiter);
        Read('\n');
        var section = CreateSection(sectionType, argument);
        if (section.IsMode)
        {
          if (Sections.Count > 1)
          {
            throw new ParseException(sectionType.Location, "A mode is only valid at the top level.");
          }
          section = ParserOutput.AddMode(section.AsMode);
        }
        Sections.Push(section);
      }

      private Section CreateSection(LocatedString type, LocatedString argument)
      {
        switch (type.Value)
        {
          case nameof(RegexSectionType.Process):
          case nameof(RegexSectionType.Window):
            var sectionType = (RegexSectionType)Enum.Parse(typeof(RegexSectionType), type.Value);
            var regexOptions = sectionType == RegexSectionType.Process ? RegexOptions.IgnoreCase : RegexOptions.None;
            return new RegexSection(Sections.Peek().AsStandardSection, CreateRegex(argument, regexOptions), sectionType);
          case Config.FlagSectionIdentifier:
            return new FlagSection(Sections.Peek().AsStandardSection, argument.Value);
          case Config.InputModeSectionIdentifier:
            return new Mode(argument.Value, false);
          case Config.ComposeModeSectionIdentifier:
            return new Mode(argument.Value, true);
          default:
            throw new ParseException(type.Location, $"Unrecognized section type '{type.Value}'.");
        }
      }

      private LocatedString ReadArguments()
      {
        return ReadUntil('\n').TrimEnd();
      }

      private void ReadHotkey()
      {
        Chord = ReadChord();
        Forbid(Config.ArgumentDelimiter);
        ReadMany(' ');
        BeginReadCommands();
      }

      private void ReadSpecialCommand()
      {
        Read(Config.SpecialCommandPrefix);
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
          {
            throw new ParseException(Location, "Expecting a command.");
          }
        }
        else
        {
          tokens.Add(ReadCommand());
        }
        foreach (var token in tokens)
        {
          if (token.HasFlag(CommandTypes.Chordless) && Chord != null)
          {
            throw CreateException(token, "This command cannot be bound to a chord.");
          }
          else if (!token.HasFlag(CommandTypes.Chordless) && Chord == null)
          {
            throw CreateException(token, "This command has to be bound to a chord.");
          }
          else if (token.HasFlag(CommandTypes.TopLevelOnly) && Sections.Count > 1)
          {
            throw CreateException(token, "This command is only valid at the top level.");
          }
          else if (token.HasFlag(CommandTypes.ModeOnly) && !Sections.Peek().IsMode)
          {
            throw CreateException(token, $"This command is only valid in a {Config.InputModeSectionIdentifier} or {Config.ComposeModeSectionIdentifier} section.");
          }
          else if (token.HasFlag(CommandTypes.ComposeModeOnly) && (!Sections.Peek().IsMode || !Sections.Peek().AsMode.IsComposeMode))
          {
            throw CreateException(token, $"This command is only valid in a {Config.ComposeModeSectionIdentifier} section.");
          }
          else if (token.HasFlag(CommandTypes.InputModeOnly) && (!Sections.Peek().IsMode || Sections.Peek().AsMode.IsComposeMode))
          {
            throw CreateException(token, $"This command is only valid in a {Config.InputModeSectionIdentifier} section.");
          }
          else if (token.HasFlag(CommandTypes.StandardSectionOnly) && !Sections.Peek().IsStandardSection)
          {
            throw CreateException(token, "This command is only valid in a standard section.");
          }
          else if (token.HasFlag(CommandTypes.ExecuteAtParseTime) && tokens.Count > 1)
          {
            throw CreateException(token, "This command cannot be combined with other commands.");
          }
        }
        if (tokens[0].HasFlag(CommandTypes.ExecuteAtParseTime))
        {
          tokens[0].Action(Combo.None);
        }
        else
        {
          Action<Combo> action;
          if (tokens.Count > 1)
          {
            action = CreateAction(tokens.Select(z => z.Action));
          }
          else
          {
            action = tokens[0].Action;
          }
          var description = Chord + " " + tokens[0].LocatedName.Value + " " + tokens[0].LocatedArguments.Value;
          ParserOutput.AddHotkey(Sections.Peek(), Chord, action, description);
        }
      }

      private Exception CreateException(CommandToken token, string message)
      {
        return new ParseException(token.LocatedName, message);
      }

      private CommandToken ReadCommand()
      {
        var locatedName = ReadIdentifier();
        if (!EndOfLine)
        {
          Require(' ');
        }
        ReadMany(' ');
        var locatedArguments = ReadArguments();
        Read('\n');
        var command = Parser.CommandCollection.GetCommand(locatedName);
        var action = GetAction(command, locatedName, locatedArguments);
        return new CommandToken(command, locatedName, locatedArguments, action);
      }

      private IEnumerable<CommandToken> ReadCommands()
      {
        var commandTokens = new List<CommandToken>();
        int column = -1;
        while (!EndOfStream)
        {
          ReadMany(' ');
          if (EndOfLine)
          {
            Read();
          }
          else
          {
            if (column == -1)
            {
              if (Location.Column <= Sections.Peek().Column)
              {
                break;
              }
              else
              {
                column = Location.Column;
              }
            }
            else if (Location.Column > column)
            {
              throw new ParseException(Location, "Unexpected indentation.");
            }
            else if (Location.Column < column)
            {
              break;
            }
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
          arguments.Add(new ExecuteAtParseTimeData(ParserOutput, Sections.Peek(), Chord, locatedName, locatedArguments));
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

      private Action<Combo> CreateAction(object actor, MethodInfo methodInfo, object[] arguments, bool insertTrigger)
      {
        if (methodInfo.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null)
        {
          if (methodInfo.ReturnType == typeof(Task))
          {
            var func = CreateFunc<Task>(actor, methodInfo, arguments, insertTrigger);
            return async (combo) =>
            {
              try
              {
                await func(combo);
              }
              catch (Exception ex)
              {
                if (Helper.IsCriticalException(ex))
                {
                  Helper.HandleFatalException(ex);
                }
                else
                {
                  Env.Notifier.WriteError(ex, Helper.GetUnhandledExceptionWarningMessage());
                }
              }
            };
          }
          else
          {
            Env.Notifier.WriteWarning($"Async function '{actor.GetType().ToString()}.{methodInfo.Name}' has a return type other than 'Task'. Exceptions thrown by this function will not be catched.");
          }
        }
        var func1 = CreateFunc<object>(actor, methodInfo, arguments, insertTrigger);
        return (combo) => func1(combo);
      }

      private Func<Combo, T> CreateFunc<T>(object actor, MethodInfo methodInfo, object[] arguments, bool insertTrigger)
      {
        if (insertTrigger)
        {
          return (combo) =>
          {
            return (T)methodInfo.Invoke(actor, new object[] { new HotkeyTrigger(combo) }.Concat(arguments).ToArray());
          };
        }
        else
        {
          return (combo) =>
          {
            return (T)methodInfo.Invoke(actor, arguments);
          };
        }
      }

      private Action<Combo> CreateAction(IEnumerable<Action<Combo>> actions)
      {
        var actionArray = actions.ToArray();
        return (combo) =>
        {
          foreach (var action in actionArray)
          {
            action(combo);
          }
        };
      }

      private Chord ReadChord()
      {
        var locatedString = ReadUntil(' ', '\n', Config.ArgumentDelimiter);
        return (Sections.Peek().IsMode ? ModeChordReader : ChordReader).CreateChord(locatedString);
      }
    }
  }

  delegate void ParseAction(ParserOutput parserOutput);
}

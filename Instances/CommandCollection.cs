using System;
using System.Collections.Generic;
using System.Reflection;
using InputMaster.Parsers;

namespace InputMaster.Instances
{
  public class CommandCollection : ICommandCollection
  {
    private readonly Dictionary<string, Command> _commands = new Dictionary<string, Command>();

    public void AddActor(object actor)
    {
      var actorType = actor.GetType();
      foreach (var methodInfo in actorType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Instance |
        BindingFlags.Public | BindingFlags.NonPublic))
      {
        if (!TryGetCommandTypes(methodInfo, out var commandTypes))
          continue;
        var name = methodInfo.Name;
        if (_commands.ContainsKey(name))
          throw new AmbiguousMatchException("Duplicate command found" + Helper.GetBindingsSuffix(name, nameof(name)));
        _commands[name] = new Command(actor, methodInfo, commandTypes);
      }
    }

    private static bool TryGetCommandTypes(MemberInfo memberInfo, out CommandTypes commandTypes)
    {
      if (Attribute.IsDefined(memberInfo, typeof(CommandAttribute)))
      {
        commandTypes = ((CommandAttribute)memberInfo.GetCustomAttribute(typeof(CommandAttribute))).CommandTypes;
        return true;
      }
      commandTypes = CommandTypes.None;
      return false;
    }

    public Command GetCommand(LocatedString locatedName)
    {
      if (_commands.TryGetValue(locatedName.Value, out var command) || _commands.TryGetValue(locatedName.Value + "Async", out command))
        return command;
      throw new ParseException(locatedName, "Command not found.");
    }
  }
}

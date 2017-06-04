using System;
using System.Collections.Generic;
using System.Reflection;
using InputMaster.Parsers;

namespace InputMaster
{
  internal class CommandCollection : ICommandCollection
  {
    private readonly Dictionary<string, Command> Commands = new Dictionary<string, Command>();

    public void AddActor(object actor)
    {
      var actorType = actor.GetType();
      var actorCommandTypes = GetCommandTypes(actorType);
      foreach (var methodInfo in actorType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance))
      {
        var commandTypes = actorCommandTypes | GetCommandTypes(methodInfo);
        if (!commandTypes.HasFlag(CommandTypes.Visible) || commandTypes.HasFlag(CommandTypes.Invisible))
        {
          continue;
        }
        var name = methodInfo.Name;
        if (Commands.ContainsKey(name))
        {
          throw new AmbiguousMatchException("Duplicate command found" + Helper.GetBindingsSuffix(name, nameof(name)));
        }
        Commands[name] = new Command(actor, methodInfo, commandTypes);
      }
    }

    private static CommandTypes GetCommandTypes(MemberInfo memberInfo)
    {
      if (Attribute.IsDefined(memberInfo, typeof(CommandTypesAttribute)))
      {
        return ((CommandTypesAttribute)memberInfo.GetCustomAttribute(typeof(CommandTypesAttribute))).CommandTypes;
      }
      return CommandTypes.None;
    }

    public Command GetCommand(LocatedString locatedName)
    {
      if (Commands.TryGetValue(locatedName.Value, out var command))
      {
        return command;
      }
      throw new ParseException(locatedName, "Command not found.");
    }
  }
}

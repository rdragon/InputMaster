using System;

namespace InputMaster.Parsers
{
  internal class CommandToken
  {
    private readonly Command Command;

    public CommandToken(Command command, LocatedString locatedName, LocatedString locatedArguments, Action<Combo> action)
    {
      Command = command;
      LocatedName = locatedName;
      LocatedArguments = locatedArguments;
      Action = action;
    }

    public LocatedString LocatedName { get; }
    public LocatedString LocatedArguments { get; }
    public Action<Combo> Action { get; }

    public bool HasFlag(CommandTypes commandTypes)
    {
      return Command.CommandTypes.HasFlag(commandTypes);
    }

    public override string ToString()
    {
      return LocatedName.ToString();
    }
  }
}

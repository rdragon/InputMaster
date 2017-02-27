using System;

namespace InputMaster.Parsers
{
  class CommandToken
  {
    public Command Command { get; }
    public LocatedString LocatedName { get; }
    public LocatedString LocatedArguments { get; }
    public Action<Combo> Action { get; }

    public CommandToken(Command command, LocatedString locatedName, LocatedString locatedArguments, Action<Combo> action)
    {
      Command = command;
      LocatedName = locatedName;
      LocatedArguments = locatedArguments;
      Action = action;
    }

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

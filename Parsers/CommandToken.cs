using System;

namespace InputMaster.Parsers
{
  public class CommandToken
  {
    public LocatedString LocatedName { get; }
    public LocatedString LocatedArguments { get; }
    public Action<Combo> Action { get; }
    private readonly Command _command;

    public CommandToken(Command command, LocatedString locatedName, LocatedString locatedArguments, Action<Combo> action)
    {
      _command = command;
      LocatedName = locatedName;
      LocatedArguments = locatedArguments;
      Action = action;
    }

    public bool HasFlag(CommandTypes commandTypes)
    {
      return _command.CommandTypes.HasFlag(commandTypes);
    }

    public override string ToString()
    {
      return LocatedName.ToString();
    }
  }
}

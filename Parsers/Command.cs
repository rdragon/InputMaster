using System.Reflection;

namespace InputMaster.Parsers
{
  internal class Command
  {
    public object Actor { get; }
    public MethodInfo MethodInfo { get; }
    public CommandTypes CommandTypes { get; }

    public Command(object actor, MethodInfo methodInfo, CommandTypes commandTypes)
    {
      Actor = actor;
      MethodInfo = methodInfo;
      CommandTypes = commandTypes;
    }
  }
}

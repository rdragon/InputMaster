using System;

namespace InputMaster
{
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
  sealed class CommandTypesAttribute : Attribute
  {
    public CommandTypes CommandTypes { get; }

    public CommandTypesAttribute(CommandTypes types)
    {
      CommandTypes = types;
    }
  }

  [AttributeUsage(AttributeTargets.Parameter)]
  public sealed class ValidRangeAttribute : Attribute
  {
    public int Minimum { get; }
    public int Maximum { get; }

    public ValidRangeAttribute(int minimum, int maximum)
    {
      Minimum = minimum;
      Maximum = maximum;
    }
  }

  [AttributeUsage(AttributeTargets.Parameter)]
  public sealed class ValidFlagsAttribute : Attribute
  {
    public string FlagsString { get; }

    public ValidFlagsAttribute(string flagsString)
    {
      FlagsString = flagsString;
    }
  }

  [AttributeUsage(AttributeTargets.Parameter)]
  public sealed class AllowSpacesAttribute : Attribute { }
}

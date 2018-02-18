using System;

namespace InputMaster
{
  [AttributeUsage(AttributeTargets.Method)]
  public sealed class CommandAttribute : Attribute
  {
    public CommandTypes CommandTypes { get; }

    public CommandAttribute() { }

    public CommandAttribute(CommandTypes types)
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

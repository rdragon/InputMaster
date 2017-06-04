﻿using System;
using JetBrains.Annotations;

namespace InputMaster
{
  [MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
  internal sealed class CommandTypesAttribute : Attribute
  {
    public CommandTypes CommandTypes { get; }

    public CommandTypesAttribute(CommandTypes types)
    {
      CommandTypes = types;
    }
  }

  [AttributeUsage(AttributeTargets.Parameter)]
  internal sealed class ValidRangeAttribute : Attribute
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
  internal sealed class ValidFlagsAttribute : Attribute
  {
    public string FlagsString { get; }

    public ValidFlagsAttribute(string flagsString)
    {
      FlagsString = flagsString;
    }
  }

  [AttributeUsage(AttributeTargets.Parameter)]
  internal sealed class AllowSpacesAttribute : Attribute { }
}

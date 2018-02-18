using System;

namespace InputMaster
{
  [Flags]
  public enum CommandTypes
  {
    None = 0,
    ModeOnly = 1,
    InputModeOnly = 2,
    ComposeModeOnly = 4,
    StandardSectionOnly = 8,
    TopLevelOnly = 16,
    Chordless = 32,
    ExecuteAtParseTime = 128
  }

  public enum BlueprintShape
  {
    Straight, Stairs, Spiral, L
  }

  [Flags]
  public enum StateHandlerFlags
  {
    None = 0, UseCipher = 1, UserEditable = 2, SavePeriodically = 4, Exportable = 8
  }
}

using InputMaster.Rik; // Remove this line.
using System;
using System.Collections.Generic;

namespace InputMaster
{
  [Flags]
  public enum Modifiers
  {
    None = 0,
    Shift = 1,
    Ctrl = 2,
    Alt = 4,
    Win = 8
    , Caps = 16 // Remove this line.
  }

  public enum DynamicHotkeyEnum { Copy, Paste }

  public static class ConfigHelper
  {
    public static Modifiers StandardModifiers { get; } = Modifiers.Shift | Modifiers.Ctrl | Modifiers.Alt | Modifiers.Win;

    public static IReadOnlyCollection<(Input, Modifiers)> ModifierKeys { get; } = new List<(Input, Modifiers)>()
    {
      (Input.LShift, Modifiers.Shift),
      (Input.RShift, Modifiers.Shift),
      (Input.LCtrl, Modifiers.Ctrl),
      (Input.RCtrl, Modifiers.Ctrl),
      (Input.LAlt, Modifiers.Alt),
      (Input.RAlt, Modifiers.Alt),
      (Input.LWin, Modifiers.Win),
      (Input.RWin, Modifiers.Win)
      , (Input.CapsLock, Modifiers.Caps) // Remove this line.
      , (Input.Menu, Modifiers.Caps) // Remove this line.
    }.AsReadOnly();

    public static void SetConfig()
    {
      Env.Config = new /**/ConfigRik/**/(); // Config
      Env.Config.Initialize();
    }
  }
}

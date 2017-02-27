using System;
using Microsoft.Win32.SafeHandles;
using InputMaster.Win32;

namespace InputMaster
{
  [Flags]
  enum CommandTypes
  {
    None = 0,
    /// <summary>
    /// Overwrites <see cref="Visible"/>.
    /// </summary>
    ModeOnly = 1,
    InputModeOnly = 2,
    ComposeModeOnly = 4,
    StandardSectionOnly = 8,
    TopLevelOnly = 16,
    Chordless = 32,
    Visible = 64,
    ExecuteAtParseTime = 128,
    Invisible = 256,
  }

  class HookHandle : SafeHandleZeroOrMinusOneIsInvalid
  {
    public HookHandle() : base(true) { }

    public HookHandle(bool ownsHandle) : base(ownsHandle) { }

    protected override bool ReleaseHandle()
    {
      return NativeMethods.UnhookWindowsHookEx(handle);
    }
  }

  class HotkeyTrigger
  {
    public Combo Combo { get; }

    public HotkeyTrigger(Combo combo)
    {
      Combo = combo;
    }
  }

  public class AmbiguousHotkeyException : Exception
  {
    public AmbiguousHotkeyException() { }
    public AmbiguousHotkeyException(string message) : base(message) { }
    public AmbiguousHotkeyException(string message, Exception innerException) : base(message, innerException) { }
  }

  public class WrappedException : Exception
  {
    public WrappedException() { }
    public WrappedException(string message) : base(message) { }
    public WrappedException(string message, Exception innerException) : base(message, innerException) { }
  }
}

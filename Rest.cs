using System;
using InputMaster.Win32;
using JetBrains.Annotations;
using Microsoft.Win32.SafeHandles;

namespace InputMaster
{
  [Flags]
  internal enum CommandTypes
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
    Invisible = 256
  }

  [UsedImplicitly]
  internal class HookHandle : SafeHandleZeroOrMinusOneIsInvalid
  {
    public HookHandle() : base(true) { }

    protected override bool ReleaseHandle()
    {
      return NativeMethods.UnhookWindowsHookEx(handle);
    }
  }

  internal class HotkeyTrigger
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

  public class InvalidPasswordException : Exception
  {
    public InvalidPasswordException() { }
    public InvalidPasswordException(string message) : base(message) { }
    public InvalidPasswordException(string message, Exception innerException) : base(message, innerException) { }
  }

  public class WrappedException : Exception
  {
    public WrappedException() { }
    public WrappedException(string message) : base(message) { }
    public WrappedException(string message, Exception innerException) : base(message, innerException) { }
  }
}

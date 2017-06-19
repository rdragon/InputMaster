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
    ModeOnly = 1,
    InputModeOnly = 2,
    ComposeModeOnly = 4,
    StandardSectionOnly = 8,
    TopLevelOnly = 16,
    Chordless = 32,
    ExecuteAtParseTime = 128
  }

  [Flags]
  internal enum Warnings
  {
    None = 0,
    MissingAccountFile = 1,
    MissingSharedPasswordFile = 2
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
    public HotkeyTrigger(Combo combo)
    {
      Combo = combo;
    }

    public Combo Combo { get; }
  }

  internal class AmbiguousHotkeyException : Exception
  {
    public AmbiguousHotkeyException(string message, Exception innerException = null) : base(message, innerException) { }
  }

  internal class WrappedException : Exception
  {
    public WrappedException(string message, Exception innerException = null) : base(message, innerException) { }
  }

  internal class FatalException : Exception
  {
    public FatalException(string message, Exception innerException = null) : base(message, innerException) { }
  }

  internal class DecryptionFailedException : Exception
  {
    public DecryptionFailedException(string message, Exception innerException = null) : base(message, innerException) { }
  }
}

using System;
using System.Runtime.InteropServices;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace InputMaster.Win32
{
  [StructLayout(LayoutKind.Sequential)]
  internal struct NativePoint
  {
    public int X;
    public int Y;
  }

  internal enum ShowWindowArgument
  {
    Minimize = 6,
    Restore = 9
  }

  internal enum WindowMessage
  {
    Close = 0x0010,
    KeyDown = 0x0100,
    SystemKeyDown = 0x0104,
    VerticalScroll = 0x0115,
    MouseMove = 0x0200,
    LeftMouseButtonDown = 0x0201,
    LeftMouseButtonUp = 0x0202,
    LeftMouseButtonDoubleClick = 0x0203,
    RightMouseButtonDown = 0x0204,
    RightMouseButtonUp = 0x0205,
    RightMouseButtonDoubleClick = 0x0206,
    MiddleMouseButtonDown = 0x0207,
    MiddleMouseButtonUp = 0x0208,
    MiddleMouseButtonDoubleClick = 0x0209,
    MouseWheel = 0x020A,
    SetRedraw = 0x000B,
    User = 0x0400
  }

  #region Hook
  internal delegate IntPtr HookProcedureFunction(int code, IntPtr lParam, IntPtr wParam);

  // KBDLLHOOKSTRUCT
  [StructLayout(LayoutKind.Sequential)]
  internal struct KeyProcedureData
  {
    public Input VirtualKey;
    public int ScanCode;
    public KeyProcedureDataFlags Flags;
    public int _time;
    public IntPtr _extraInfo;
  }

  // MSLLHOOKSTRUCT
  [StructLayout(LayoutKind.Sequential)]
  internal struct MouseProcedureData
  {
    public NativePoint _mousePosition;
    public int MouseData;
    public MouseProcedureDataFlags Flags;
    public int _time;
    public IntPtr _extraInfo;
  }

  [Flags]
  internal enum KeyProcedureDataFlags
  {
    Extended = 0x01,
    Injected = 0x10
  }

  [Flags]
  internal enum MouseProcedureDataFlags
  {
    Injected = 0x01
  }

  internal enum HookType
  {
    LowLevelKeyboardHook = 13,
    LowLevelMouseHook = 14
  }
  #endregion

  #region SendInput
  [StructLayout(LayoutKind.Sequential)]
  internal struct NativeInput
  {
    public InputType Type;
    public InputData Data;
  }

  internal enum InputType { Mouse, Key }

  [StructLayout(LayoutKind.Explicit)]
  internal struct InputData
  {
    [FieldOffset(0)]
    public KeyInput KeyInput;

    [FieldOffset(0)]
    public MouseInput MouseInput;
  }

  [StructLayout(LayoutKind.Sequential)]
  internal struct KeyInput
  {
    public short VirtualKeyCode;
    public short ScanCode;
    public KeyFlags Flags;
    public int _time;
    public IntPtr _extraInfo;
  }

  [Flags]
  internal enum KeyFlags
  {
    ExtendedKey = 0x0001,
    KeyUp = 0x0002,
    Unicode = 0x0004,
  }

  [StructLayout(LayoutKind.Sequential)]
  internal struct MouseInput
  {
    public int _x;
    public int _y;
    public int MouseData;
    public MouseFlags Flags;
    public int _time;
    public IntPtr _extraInfo;
  }

  [Flags]
  internal enum MouseFlags
  {
    LeftDown = 0x0002,
    LeftUp = 0x0004,
    RightDown = 0x0008,
    RightUp = 0x0010,
    MiddleDown = 0x0020,
    MiddleUp = 0x0040,
    Wheel = 0x0800
  }
  #endregion
}

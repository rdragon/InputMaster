using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using InputMaster.Win32;

namespace InputMaster.Hooks
{
  /// <summary>
  /// Installs a hook procedure into the two Windows hook chains that monitor low-level keyboard and mouse events.
  /// </summary>
  internal class PrimaryHook
  {
    private readonly IInputHook TargetHook;
    private readonly HookProcedureFunction HookProcedureFunction;
    private HookHandle HookHandle;
    private HookHandle MouseHookHandle;

    public PrimaryHook(IInputHook targetHook)
    {
      HookProcedureFunction = HookProcedure; // So the garbage collector does not free the procedure.
      TargetHook = targetHook;
      Env.App.Exiting += () =>
      {
        TargetHook.Reset();
        HookHandle?.Dispose();
        MouseHookHandle?.Dispose();
      };
    }

    private static InputArgs ReadMessage(WindowMessage message, IntPtr data)
    {
      return message.IsMouseMessage() ? ReadMouseMessage(message, data) : Env.Config.KeyboardLayout.ReadKeyboardMessage(message, data);
    }

    private static InputArgs ReadMouseMessage(WindowMessage message, IntPtr data)
    {
      var mouseProcedureData = (MouseProcedureData)Marshal.PtrToStructure(data, typeof(MouseProcedureData));
      if (mouseProcedureData.Flags.HasFlag(MouseProcedureDataFlags.Injected) || message == WindowMessage.MouseMove)
      {
        return null;
      }
      var down = false;
      switch (message)
      {
        case WindowMessage.LeftMouseButtonDown:
        case WindowMessage.RightMouseButtonDown:
        case WindowMessage.MiddleMouseButtonDown:
        case WindowMessage.MouseWheel:
          down = true;
          break;
      }
      Input input;
      switch (message)
      {
        case WindowMessage.LeftMouseButtonDown:
        case WindowMessage.LeftMouseButtonUp:
          input = Input.Lmb;
          break;
        case WindowMessage.RightMouseButtonDown:
        case WindowMessage.RightMouseButtonUp:
          input = Input.Rmb;
          break;
        case WindowMessage.MiddleMouseButtonDown:
        case WindowMessage.MiddleMouseButtonUp:
          input = Input.Mmb;
          break;
        case WindowMessage.MouseWheel:
          input = (mouseProcedureData.MouseData >> 16) > 0 ? Input.WheelUp : Input.WheelDown;
          break;
        default:
          throw new ArgumentException("Unrecognized mouse message.", nameof(message));
      }
      return new InputArgs(input, down);
    }

    public void Register()
    {
      HookHandle = NativeMethods.SetWindowsHookEx(HookType.LowLevelKeyboardHook, HookProcedureFunction, IntPtr.Zero, 0);
      if (HookHandle.IsInvalid)
      {
        throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to register low-level keyboard hook.");
      }
      MouseHookHandle = NativeMethods.SetWindowsHookEx(HookType.LowLevelMouseHook, HookProcedureFunction, IntPtr.Zero, 0);
      if (MouseHookHandle.IsInvalid)
      {
        throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to register low-level mouse hook.");
      }
    }

    private IntPtr HookProcedure(int code, IntPtr wParam, IntPtr lParam)
    {
      var captured = false;
      try
      {
        if (code >= 0)
        {
          var e = ReadMessage((WindowMessage)wParam, lParam);
          if (e != null)
          {
            if (Env.Config.CaptureLmb && e.Input == Input.Lmb)
            {
              captured = true;
            }
            TargetHook.Handle(e);
            captured = e.Capture;
          }
        }
      }
      catch (Exception ex) // This is the last place to handle any exceptions thrown during the hook procedure.
      {
        Helper.HandleAnyException(ex);
      }
      return captured ? new IntPtr(-1) : NativeMethods.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
    }
  }
}

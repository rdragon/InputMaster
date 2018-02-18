using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using InputMaster.Win32;

namespace InputMaster.Hooks
{
  /// <summary>
  /// Installs a hook procedure into the two Windows hook chains that monitor low-level keyboard and mouse events.
  /// </summary>
  public class PrimaryHook
  {
    private readonly IInputHook _targetHook;
    private readonly HookProcedureFunction _hookProcedureFunction;
    private HookHandle _hookHandle;
    private HookHandle _mouseHookHandle;

    public PrimaryHook(IInputHook targetHook)
    {
      _hookProcedureFunction = HookProcedure; // So the garbage collector does not free the procedure.
      _targetHook = targetHook;
      Env.App.Run += Register;
      Env.App.Exiting += _targetHook.Reset;
      Env.App.Unhook += () =>
      {
        _hookHandle?.Dispose();
        _mouseHookHandle?.Dispose();
      };
    }

    private static bool TryReadMessage(WindowMessage message, IntPtr data, out InputArgs inputArgs)
    {
      return message.IsMouseMessage()
        ? TryReadMouseMessage(message, data, out inputArgs)
        : Env.Config.KeyboardLayout.TryReadKeyboardMessage(message, data, out inputArgs);
    }

    private static bool TryReadMouseMessage(WindowMessage message, IntPtr data, out InputArgs inputArgs)
    {
      var mouseProcedureData = (MouseProcedureData)Marshal.PtrToStructure(data, typeof(MouseProcedureData));
      if (mouseProcedureData.Flags.HasFlag(MouseProcedureDataFlags.Injected) || message == WindowMessage.MouseMove)
      {
        inputArgs = null;
        return false;
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
          input = mouseProcedureData.MouseData >> 16 > 0 ? Input.WheelUp : Input.WheelDown;
          break;
        default:
          throw new ArgumentException("Unrecognized mouse message.", nameof(message));
      }
      inputArgs = new InputArgs(input, down);
      return true;
    }

    public void Register()
    {
      _hookHandle = NativeMethods.SetWindowsHookEx(HookType.LowLevelKeyboardHook, _hookProcedureFunction, IntPtr.Zero, 0);
      if (_hookHandle.IsInvalid)
        throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to register low-level keyboard hook.");
      _mouseHookHandle = NativeMethods.SetWindowsHookEx(HookType.LowLevelMouseHook, _hookProcedureFunction, IntPtr.Zero, 0);
      if (_mouseHookHandle.IsInvalid)
        throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to register low-level mouse hook.");
    }

    private IntPtr HookProcedure(int code, IntPtr wParam, IntPtr lParam)
    {
      var captured = false;
      try
      {
        if (code >= 0 && TryReadMessage((WindowMessage)wParam, lParam, out var e))
        {
          if (Env.Config.CaptureLmb && e.Input == Input.Lmb)
            captured = true;
          _targetHook.Handle(e);
          captured = e.Capture;
        }
      }
      catch (Exception ex) // This is the last place to handle any exceptions thrown during the hook procedure.
      {
        Helper.AwaitTask(Helper.HandleExceptionAsync(ex));
      }
      return captured ? new IntPtr(-1) : NativeMethods.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
    }
  }
}

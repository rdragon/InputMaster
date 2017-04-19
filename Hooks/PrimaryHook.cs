using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using InputMaster.Win32;

namespace InputMaster.Hooks
{
  /// <summary>
  /// Installs a hook procedure into the two Windows hook chains that monitor low-level keyboard and mouse events.
  /// </summary>
  class PrimaryHook : IDisposable
  {
    /// <summary>
    /// For debugging purposes.
    /// </summary>
    private static PrimaryHook Instance;

    private readonly IInputHook TargetHook;
    private readonly HookProcedureFunction HookProcedureFunction;
    private HookHandle HookHandle;
    private HookHandle MouseHookHandle;

    public PrimaryHook(Brain brain, IInputHook targetHook)
    {
      HookProcedureFunction = HookProcedure; // So the garbage collector does not free the procedure.
      TargetHook = targetHook;
      Instance = this;

      brain.Exiting += Reset;
    }

    public static void Unhook()
    {
      Instance?.Dispose();
    }

    private static InputArgs ReadMessage(WindowMessage message, IntPtr data)
    {
      if (message.IsMouseMessage())
      {
        return ReadMouseMessage(message, data);
      }
      else
      {
        return ReadKeyboardMessage(message, data);
      }
    }

    private static InputArgs ReadKeyboardMessage(WindowMessage message, IntPtr data)
    {
      var keyProcedureData = (KeyProcedureData)Marshal.PtrToStructure(data, typeof(KeyProcedureData));
      if (keyProcedureData.Flags.HasFlag(KeyProcedureDataFlags.Injected))
      {
        return null; // Ignore injected messages (e.g. the events we simulate).
      }
      else
      {
        var down = message == WindowMessage.KeyDown || message == WindowMessage.SystemKeyDown;
        var input = keyProcedureData.VirtualKey;
        if (keyProcedureData.Flags.HasFlag(KeyProcedureDataFlags.Extended))
        {
          switch (input)
          {
            case Input.Cancel: input = Input.Pause; break;
            case Input.Enter: input = Input.NumEnter; break;
            case Input.Pause: input = Input.NumLock; break;
          }
        }
        else
        {
          switch (input)
          {
            case Input.Del: input = Input.Dec; break;
            case Input.Ins: input = Input.Num0; break;
            case Input.End: input = Input.Num1; break;
            case Input.Down: input = Input.Num2; break;
            case Input.PgDn: input = Input.Num3; break;
            case Input.Left: input = Input.Num4; break;
            case Input.Clear: input = Input.Num5; break;
            case Input.Right: input = Input.Num6; break;
            case Input.Home: input = Input.Num7; break;
            case Input.Up: input = Input.Num8; break;
            case Input.PgUp: input = Input.Num9; break;
          }
        }
        return new InputArgs(input, down);
      }
    }

    private static InputArgs ReadMouseMessage(WindowMessage message, IntPtr data)
    {
      var mouseProcedureData = (MouseProcedureData)Marshal.PtrToStructure(data, typeof(MouseProcedureData));
      if (mouseProcedureData.Flags.HasFlag(MouseProcedureDataFlags.Injected) || message == WindowMessage.MouseMove)
      {
        return null;
      }
      else
      {
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

    public void Reset()
    {
      TargetHook.Reset();
    }

    public void Dispose()
    {
      if (HookHandle != null && !HookHandle.IsInvalid && !HookHandle.IsClosed)
      {
        HookHandle.Dispose();
      }
      if (MouseHookHandle != null && !MouseHookHandle.IsInvalid && !MouseHookHandle.IsClosed)
      {
        MouseHookHandle.Dispose();
      }
    }

    private IntPtr HookProcedure(int code, IntPtr wParam, IntPtr lParam)
    {
      bool captured = false;
      if (code >= 0)
      {
        try
        {
          var e = ReadMessage((WindowMessage)wParam, lParam);
          if (e != null)
          {
            if (Config.CaptureLmb && e.Input == Input.Lmb)
            {
              captured = true;
            }
            else
            {
              TargetHook.Handle(e);
              captured = e.Capture;
            }
          }
        }
        catch (Exception ex)
        {
          if (Helper.IsCriticalException(ex))
          {
            Helper.HandleFatalException(ex, "during the hook procedure");
          }
          else
          {
            Env.Notifier.WriteError(ex, Helper.GetUnhandledExceptionWarningMessage("during the hook procedure"));
          }
        }
      }
      return captured ? new IntPtr(-1) : NativeMethods.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
    }
  }
}

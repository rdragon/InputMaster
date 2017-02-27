using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using InputMaster.Win32;

namespace InputMaster
{
  class Injector : IInjector
  {
    private List<NativeInput> NativeInputs = new List<NativeInput>();

    private static void Inject(NativeInput[] myInputs)
    {
      var n = NativeMethods.SendInput(myInputs.Length, myInputs, Marshal.SizeOf(typeof(NativeInput)));
      if (n < myInputs.Length)
      {
        var prefix = n == 0 ? $"Failed to inject all {myInputs.Length} events." : $"Only {n}/{myInputs.Length} events successfully injected.";
        Env.Notifier.WriteError(prefix + " Error code: " + Marshal.GetLastWin32Error());
      }
    }

    private static NativeInput GetNativeInput(Input input, bool down)
    {
      if (input.IsMouseInput())
      {
        return GetNativeMouse(input, down);
      }
      else
      {
        return GetNativeKey(input, down);
      }
    }

    private static NativeInput GetNativeKey(Input input, bool down)
    {
      bool extended =
        input == Input.Left || input == Input.Right || input == Input.Up || input == Input.Down ||
        input == Input.Ins || input == Input.Home || input == Input.PgUp || input == Input.PgDn || input == Input.End || input == Input.Del ||
        input == Input.NumEnter || input == Input.Div || input == Input.NumLock ||
        input == Input.RAlt || input == Input.RCtrl || input == Input.RShift || input == Input.RWin || input == Input.LWin;
      if (input == Input.NumEnter)
      {
        input = Input.Enter;
      }
      return new NativeInput
      {
        Type = InputType.Key,
        Data = new InputData
        {
          KeyInput = new KeyInput
          {
            VirtualKeyCode = (short)input,
            Flags = (extended ? KeyFlags.ExtendedKey : 0) | (down ? 0 : KeyFlags.KeyUp),
          }
        }
      };
    }

    private static NativeInput GetNativeMouse(Input input, bool down)
    {
      MouseFlags flags;
      int data = 0;
      switch (input)
      {
        case Input.Lmb:
          flags = down ? MouseFlags.LeftDown : MouseFlags.LeftUp;
          break;
        case Input.Rmb:
          flags = down ? MouseFlags.RightDown : MouseFlags.RightUp;
          break;
        case Input.Mmb:
          flags = down ? MouseFlags.MiddleDown : MouseFlags.MiddleUp;
          break;
        case Input.WheelDown:
          flags = MouseFlags.Wheel;
          data = -120;
          break;
        case Input.WheelUp:
          flags = MouseFlags.Wheel;
          data = 120;
          break;
        default:
          throw new ArgumentException("Unrecognized mouse input.", nameof(input));
      }
      return new NativeInput
      {
        Type = InputType.Mouse,
        Data = new InputData
        {
          MouseInput = new MouseInput
          {
            Flags = flags,
            MouseData = data
          }
        }
      };
    }

    private static NativeInput GetNativeChar(char c, bool down)
    {
      return new NativeInput
      {
        Type = InputType.Key,
        Data = new InputData
        {
          KeyInput = new KeyInput
          {
            ScanCode = (short)c,
            Flags = (down ? 0 : KeyFlags.KeyUp) | KeyFlags.Unicode
          }
        }
      };
    }

    public IInjector Add(Input input, bool down)
    {
      NativeInputs.Add(GetNativeInput(input, down));
      return this;
    }

    public IInjector Add(char c)
    {
      NativeInputs.Add(GetNativeChar(c, true));
      NativeInputs.Add(GetNativeChar(c, false));
      return this;
    }

    public void Run()
    {
      Inject(NativeInputs.ToArray());
    }

    public Action Compile()
    {
      var inputs = NativeInputs.ToArray();
      return () => Inject(inputs);
    }

    public IInjector CreateInjector()
    {
      return new Injector();
    }
  }
}

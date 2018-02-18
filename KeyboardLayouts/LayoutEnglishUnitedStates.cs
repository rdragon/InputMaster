using InputMaster.Win32;
using System;
using System.Runtime.InteropServices;

namespace InputMaster.KeyboardLayouts
{
  public class LayoutEnglishUnitedStates : KeyboardLayout
  {
    public LayoutEnglishUnitedStates()
    {
      var chars = "`-=[];'\\,./0123456789abcdefghijklmnopqrstuvwxyz";
      var shiftedChars = "~_+{}:\"|<>?)!@#$%^&*(ABCDEFGHIJKLMNOPQRSTUVWXYZ";
      var inputs = new[] { Input.Grave, Input.Dash, Input.Is, Input.LBracket, Input.RBracket, Input.Semicolon, Input.Quote, Input.Backslash, Input.Comma, Input.Period, Input.Slash, Input.D0, Input.D1, Input.D2, Input.D3, Input.D4, Input.D5, Input.D6, Input.D7, Input.D8, Input.D9, Input.A, Input.B, Input.C, Input.D, Input.E, Input.F, Input.G, Input.H, Input.I, Input.J, Input.K, Input.L, Input.M, Input.N, Input.O, Input.P, Input.Q, Input.R, Input.S, Input.T, Input.U, Input.V, Input.W, Input.X, Input.Y, Input.Z };
      foreach (var tuple in Helper.Zip3(inputs, chars, shiftedChars))
      {
        AddKey(tuple.Item1, tuple.Item2, tuple.Item3);
      }
      AddKey(Input.Space, ' ');
    }

    public override bool TryReadKeyboardMessage(WindowMessage message, IntPtr data, out InputArgs inputArgs)
    {
      var keyProcedureData = (KeyProcedureData)Marshal.PtrToStructure(data, typeof(KeyProcedureData));
      if (keyProcedureData.Flags.HasFlag(KeyProcedureDataFlags.Injected))
      {
        inputArgs = null;
        return false; // Ignore injected messages (e.g. the events we simulate).
      }
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
      inputArgs = new InputArgs(input, down);
      return true;
    }
  }
}

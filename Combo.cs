using System;

namespace InputMaster
{
  struct Combo : IEquatable<Combo>
  {
    public static readonly Combo None = new Combo();
    public static readonly Combo Backspace = new Combo(Input.Bs);

    public Input Input { get; }
    public Modifiers Modifiers { get; }

    public Combo(Input input, Modifiers modifiers = Modifiers.None)
    {
      Input = input;
      Modifiers = modifiers;
    }

    public static bool TryConvertFromChar(char c, out Combo combo)
    {
      int i;
      if (c == ' ')
      {
        combo = new Combo(Input.Space);
      }
      else if ((i = Config.Keyboard.IndexOf(c)) >= 0)
      {
        combo = new Combo(Config.KeyboardInputs[i]);
      }
      else if ((i = Config.ShiftedKeyboard.IndexOf(c)) >= 0)
      {
        combo = new Combo(Config.KeyboardInputs[i], Modifiers.Shift);
      }
      else
      {
        combo = None;
      }
      return combo != None;
    }

    public static bool operator ==(Combo combo1, Combo combo2)
    {
      return combo1.Equals(combo2);
    }

    public static bool operator !=(Combo combo1, Combo combo2)
    {
      return !combo1.Equals(combo2);
    }

    public override bool Equals(object obj)
    {
      return obj is Combo && Equals((Combo)obj);
    }

    public bool Equals(Combo other)
    {
      return Input == other.Input && Modifiers == other.Modifiers;
    }

    public override int GetHashCode()
    {
      return ((int)Input << 16) | (int)Modifiers;
    }

    public override string ToString()
    {
      var modifiers = Modifiers;
      string text;
      var i = Array.IndexOf(Config.KeyboardInputs, Input);
      if (i >= 0)
      {
        if (modifiers.HasFlag(Modifiers.Shift))
        {
          modifiers &= ~Modifiers.Shift;
          text = Config.ShiftedKeyboard[i].ToString();
        }
        else
        {
          text = Config.Keyboard[i].ToString();
        }
      }
      else
      {
        text = Config.TokenStart + Input.ToString() + Config.TokenEnd;
      }
      return modifiers.ToTokenString() + text;
    }

    public bool ModeEquals(Combo other)
    {
      if (Equals(other))
      {
        return true;
      }
      else if (other != None && Input == Input.Any && other.Modifiers.HasFlag(Modifiers) && !other.Input.IsModifierKey() && !other.Input.IsMouseInput())
      {
        return true;
      }
      else if (other != None && other.Input == Input.Any && Modifiers.HasFlag(other.Modifiers) && !Input.IsModifierKey() && !Input.IsMouseInput())
      {
        return true;
      }
      else
      {
        return false;
      }
    }
  }
}

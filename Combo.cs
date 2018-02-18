using System;

namespace InputMaster
{
  public struct Combo : IEquatable<Combo>
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
      combo = Env.Config.KeyboardLayout.GetCombo(c);
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
      return Env.Config.KeyboardLayout.ConvertComboToString(this);
    }

    public bool ModeEquals(Combo other)
    {
      if (Equals(other))
      {
        return true;
      }
      if (other != None && Input == Input.Any && other.Modifiers.HasFlag(Modifiers) && !other.Input.IsModifierKey() && !other.Input.IsMouseInput())
      {
        return true;
      }
      return other != None && other.Input == Input.Any && Modifiers.HasFlag(other.Modifiers) && !Input.IsModifierKey() && !Input.IsMouseInput();
    }
  }
}

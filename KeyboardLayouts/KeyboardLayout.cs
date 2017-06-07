using System;
using System.Collections.Generic;
using InputMaster.Win32;

namespace InputMaster.KeyboardLayouts
{
  internal abstract class KeyboardLayout : IKeyboardLayout
  {
    private Dictionary<char, Combo> Combos = new Dictionary<char, Combo>();
    private Dictionary<Input, CharacterKey> CharacterKeys = new Dictionary<Input, CharacterKey>();

    protected void AddKey(Input input, char c, char? shiftedChar = null)
    {
      Combos.Add(c, new Combo(input));
      if (shiftedChar.HasValue)
      {
        Combos.Add(shiftedChar.Value, new Combo(input, Modifiers.Shift));
        CharacterKeys.Add(input, new CharacterKey(input, c, shiftedChar.Value));
      }
    }

    public Combo GetCombo(char c) => Combos.TryGetValue(c, out var combo) ? combo : Combo.None;

    public string ConvertComboToString(Combo combo)
    {
      var modifiers = combo.Modifiers;
      string text;
      if (CharacterKeys.TryGetValue(combo.Input, out var printableKey))
      {
        if (modifiers.HasFlag(Modifiers.Shift))
        {
          modifiers &= ~Modifiers.Shift;
          text = printableKey.ShiftedChar.ToString();
        }
        else
        {
          text = printableKey.Char.ToString();
        }
      }
      else
      {
        text = ParserConfig.TokenStart + combo.Input.ToString() + ParserConfig.TokenEnd;
      }
      return modifiers.ToTokenString() + text;
    }

    public bool IsCharacterKey(Input input)
    {
      return CharacterKeys.ContainsKey(input);
    }

    public abstract InputArgs ReadKeyboardMessage(WindowMessage message, IntPtr data);
  }
}

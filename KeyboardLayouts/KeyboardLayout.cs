using System;
using System.Collections.Generic;
using InputMaster.Win32;

namespace InputMaster.KeyboardLayouts
{
  public abstract class KeyboardLayout : IKeyboardLayout
  {
    private readonly Dictionary<char, Combo> _combos = new Dictionary<char, Combo>();
    private readonly Dictionary<Input, (char, char)> _characterKeys = new Dictionary<Input, (char, char)>();

    protected void AddKey(Input input, char c, char? shiftedChar = null)
    {
      _combos.Add(c, new Combo(input));
      if (shiftedChar.HasValue)
      {
        _combos.Add(shiftedChar.Value, new Combo(input, Modifiers.Shift));
        _characterKeys.Add(input, (c, shiftedChar.Value));
      }
    }

    public Combo GetCombo(char c) => _combos.TryGetValue(c, out var combo) ? combo : Combo.None;

    public string ConvertComboToString(Combo combo)
    {
      var modifiers = combo.Modifiers;
      string text;
      if (_characterKeys.TryGetValue(combo.Input, out var printableKey))
      {
        if (modifiers.HasFlag(Modifiers.Shift))
        {
          modifiers &= ~Modifiers.Shift;
          text = printableKey.Item2.ToString();
        }
        else
          text = printableKey.Item1.ToString();
      }
      else
        text = Constants.TokenStart + combo.Input.ToString() + Constants.TokenEnd;
      return modifiers.ToTokenString() + text;
    }

    public bool IsCharacterKey(Input input)
    {
      return _characterKeys.ContainsKey(input);
    }

    public abstract bool TryReadKeyboardMessage(WindowMessage message, IntPtr data, out InputArgs inputArgs);
  }
}

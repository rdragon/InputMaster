using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using InputMaster.Actors;

namespace InputMaster.Parsers
{
  public class InputReader
  {
    private readonly List<Combo> _combos = new List<Combo>();
    private IInjectorStream<object> _injectorStream;

    public InputReader(InputReaderFlags flags)
    {
      Flags = flags;
    }

    public InputReaderFlags Flags { get; }

    public Chord CreateChord(LocatedString locatedString)
    {
      if ((Flags & (InputReaderFlags.AllowCustomCharacter | InputReaderFlags.AllowHoldRelease | InputReaderFlags.AllowMultiplier |
        InputReaderFlags.AllowDynamicHotkey)) != 0)
      {
        throw new InvalidOperationException();
      }
      _combos.Clear();
      new MyCharReader(this, locatedString, true).Run();
      var chord = new Chord(_combos);
      _combos.Clear();
      return chord;
    }

    public void AddToInjectorStream<T>(IInjectorStream<T> injectorStream, LocatedString locatedString)
    {
      if (Flags.HasFlag(InputReaderFlags.AllowKeywordAny))
        throw new InvalidOperationException();
      _injectorStream = (IInjectorStream<object>)injectorStream;
      new MyCharReader(this, locatedString, false).Run();
    }

    private class MyCharReader : CharReader
    {
      private static readonly Regex TokenRegex = new Regex("^" + Constants.TokenPattern);
      private readonly InputReader _inputReader;
      private readonly bool _createChord;
      private readonly bool _parseLiteral;
      private Modifiers _modifiers;
      private PressType _pressType;
      private int _multiplier = 1;
      private LocatedString _locatedString;

      public MyCharReader(InputReader inputReader, LocatedString locatedString, bool createChord) : base(locatedString)
      {
        _inputReader = inputReader;
        _createChord = createChord;
        _parseLiteral = inputReader.Flags.HasFlag(InputReaderFlags.ParseLiteral);
      }

      public void Run()
      {
        try
        {
          RunInner();
        }
        catch (ParseException ex) when (!ex.HasLocation)
        {
          throw new ParseException(_locatedString, ex);
        }
      }

      private ParseException CreateException(string text)
      {
        return new ParseException(_locatedString, text);
      }

      private void RunInner()
      {
        while (!EndOfStream)
        {
          _locatedString = new LocatedString(Current.ToString(), Location);
          if (!_parseLiteral && At(TokenRegex))
            HandleToken();
          else
            Add(Read().ToString());
        }
        if (_multiplier != 1 || _modifiers != Modifiers.None || _pressType != PressType.None)
          ForbidEndOfStream();
      }

      private void HandleToken()
      {
        _locatedString = ReadToken(out var text);
        if (text[0] >= '0' && text[0] <= '9')
          HandleMultiplier(text);
        else if (Enum.TryParse(text, out Modifiers modifiers) && modifiers != Modifiers.None)
          HandleModifiers(modifiers);
        else if (Enum.TryParse(text, out Input input) && input != Input.None)
          Add(input);
        else if (Env.Config.TryGetCustomInput(text, out input))
          Add(input);
        else if (Env.Config.TryGetCustomCombo(text, out var combo))
        {
          HandleModifiers(combo.Modifiers);
          Add(combo.Input);
        }
        else if (Enum.TryParse(text, out PressType pressType) && pressType != PressType.None)
          HandleHoldRelease(pressType);
        else if (Env.Parser.IsDynamicHotkey(text))
          HandleDynamicHotkey(text);
        else
          throw CreateException("Unrecognized token.");
      }

      private void HandleMultiplier(string text)
      {
        if (!_inputReader.Flags.HasFlag(InputReaderFlags.AllowMultiplier))
          throw CreateException("No multiplier allowed at this position.");
        var s = text.Substring(0, text.Length - 1);
        if (int.TryParse(s, out var multiplier))
          _multiplier *= multiplier;
        else
          throw CreateException($"Cannot convert '{s}' to an int.");
      }

      private void HandleModifiers(Modifiers modifiers)
      {
        if (modifiers.HasCustomModifiers() && !_inputReader.Flags.HasFlag(InputReaderFlags.AllowCustomModifier))
          throw CreateException("No custom modifiers allowed at this location.");
        _modifiers |= modifiers;
        CheckConflict();
      }

      private void HandleHoldRelease(PressType pressType)
      {
        if (!_inputReader.Flags.HasFlag(InputReaderFlags.AllowHoldRelease))
          throw CreateException("Cannot use a hold/release token at this location.");
        if (_pressType != PressType.None)
          throw CreateException("Unexpected token.");
        _pressType = pressType;
        CheckConflict();
      }

      private void HandleDynamicHotkey(string text)
      {
        if (!_inputReader.Flags.HasFlag(InputReaderFlags.AllowDynamicHotkey))
          throw CreateException($"Cannot use a dynamic hotkey token at this location (use '{nameof(MiscActor.SendDynamic)}' instead).");
        ForbidModifierHoldRelease();
        Env.Parser.GetAction(text, out var action);
        for (var i = 0; i < _multiplier; i++)
          action.Invoke(_inputReader._injectorStream);
        _multiplier = 1;
      }

      private void Add(string str)
      {
        foreach (var c in str)
        {
          if (Combo.TryConvertFromChar(c, out var combo))
          {
            _modifiers |= combo.Modifiers;
            CheckConflict();
            Add(combo.Input);
          }
          else
            Add(c);
        }
      }

      private void Add(char c)
      {
        if (!_inputReader.Flags.HasFlag(InputReaderFlags.AllowCustomCharacter) || _createChord)
          throw CreateException("No custom characters allowed at this location.");
        if (_modifiers != Modifiers.None)
          throw CreateException($"No modifiers allowed before character '{c}'.");
        if (_pressType != PressType.None)
          throw CreateException($"Token '{Helper.CreateTokenString(_pressType.ToString())}' not allowed before character '{c}'.");
        _inputReader._injectorStream.Add(c, _multiplier);
        _multiplier = 1;
      }

      private void Add(Input input)
      {
        if (input == Input.Any && !_inputReader.Flags.HasFlag(InputReaderFlags.AllowKeywordAny))
          throw CreateException($"Input '{input.ToTokenString()}' not allowed at this location.");
        if (_pressType != PressType.None)
        {
          Helper.RequireTrue(!_createChord && _multiplier == 1 && _modifiers == Modifiers.None);
          _inputReader._injectorStream.Add(input, _pressType == PressType.Hold);
          _pressType = PressType.None;
        }
        else
        {
          if (_createChord)
          {
            Helper.RequireTrue(_multiplier == 1);
            _inputReader._combos.Add(new Combo(input, _modifiers));
          }
          else
          {
            _inputReader._injectorStream.Add(input, _modifiers, _multiplier);
            _multiplier = 1;
          }
          _modifiers = Modifiers.None;
        }
      }

      private void ForbidModifierHoldRelease()
      {
        if (_pressType != PressType.None || _modifiers != Modifiers.None)
          throw CreateException("No modifiers allowed before this token.");
      }

      private void CheckConflict()
      {
        if (_pressType != PressType.None && (_modifiers != Modifiers.None || _multiplier != 1))
          throw CreateException("Cannot combine hold/release with modifier or multiplier.");
      }
    }

    private enum PressType
    {
      None, Hold, Release
    }
  }
}

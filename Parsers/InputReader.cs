﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace InputMaster.Parsers
{
  class InputReader
  {
    private readonly List<Combo> Combos = new List<Combo>();
    private IInjectorStream<object> InjectorStream;

    public InputReader(InputReaderFlags flags)
    {
      Flags = flags;
    }

    public InputReaderFlags Flags { get; }

    public Chord CreateChord(LocatedString locatedString)
    {
      if ((Flags & (InputReaderFlags.AllowCustomCharacter | InputReaderFlags.AllowHoldRelease | InputReaderFlags.AllowMultiplier | InputReaderFlags.AllowDynamicHotkey)) != 0)
      {
        throw new InvalidOperationException();
      }
      Combos.Clear();
      new MyCharReader(this, locatedString, true).Run();
      var chord = new Chord(Combos);
      Combos.Clear();
      return chord;
    }

    public void AddToInjectorStream<T>(IInjectorStream<T> injectorStream, LocatedString locatedString)
    {
      if (Flags.HasFlag(InputReaderFlags.AllowKeywordAny))
      {
        throw new InvalidOperationException();
      }
      InjectorStream = (IInjectorStream<object>)injectorStream;
      new MyCharReader(this, locatedString, false).Run();
    }

    private class MyCharReader : CharReader
    {
      private static readonly Regex TokenRegex = new Regex("^" + Config.TokenPattern);
      private readonly InputReader InputReader;
      private readonly bool CreateChord;
      private readonly bool ParseLiteral;
      private Modifiers Modifiers;
      private PressType PressType;
      private int Multiplier = 1;
      private LocatedString LocatedString;

      public MyCharReader(InputReader inputReader, LocatedString locatedString, bool createChord) : base(locatedString)
      {
        InputReader = inputReader;
        CreateChord = createChord;
        ParseLiteral = inputReader.Flags.HasFlag(InputReaderFlags.ParseLiteral);
      }

      public void Run()
      {
        try
        {
          RunInner();
        }
        catch (ParseException ex) when (!ex.HasLocation)
        {
          throw new ParseException(LocatedString, ex);
        }
      }

      private new ParseException CreateException(string text)
      {
        return new ParseException(LocatedString, text);
      }

      private void RunInner()
      {
        while (!EndOfStream)
        {
          LocatedString = new LocatedString(Current.ToString(), Location);
          if (!ParseLiteral && At(TokenRegex))
          {
            HandleToken();
          }
          else
          {
            Add(Read().ToString());
          }
        }
        if (Multiplier != 1 || Modifiers != Modifiers.None || PressType != PressType.None)
        {
          ForbidEndOfStream();
        }
      }

      private void HandleToken()
      {
        string text;
        LocatedString = ReadToken(out text);
        Modifiers modifiers;
        Input input;
        Combo combo;
        PressType pressType;
        DynamicHotkeyEnum dynamicHotkey;
        if (text[0] >= '0' && text[0] <= '9')
        {
          HandleMultiplier(text);
        }
        else if (Enum.TryParse(text, out modifiers) && modifiers != Modifiers.None)
        {
          HandleModifiers(modifiers);
        }
        else if (Enum.TryParse(text, out input) && input != Input.None)
        {
          Add(input);
        }
        else if (Config.CustomInputs.TryGetValue(text, out input))
        {
          Add(input);
        }
        else if (Config.CustomCombos.TryGetValue(text, out combo))
        {
          HandleModifiers(combo.Modifiers);
          Add(combo.Input);
        }
        else if (Enum.TryParse(text, out pressType) && pressType != PressType.None)
        {
          HandleHoldRelease(pressType);
        }
        else if (Enum.TryParse(text, out dynamicHotkey) && dynamicHotkey != DynamicHotkeyEnum.None)
        {
          HandleDynamicHotkey(text);
        }
        else if (Config.HandleCustomToken(text, InputReader.InjectorStream))
        {
          HandleCustomToken(text);
        }
        else
        {
          throw CreateException("Unrecognized token.");
        }
      }

      private void HandleMultiplier(string text)
      {
        if (!InputReader.Flags.HasFlag(InputReaderFlags.AllowMultiplier))
        {
          throw CreateException($"No multiplier allowed at this position.");
        }
        var s = text.Substring(0, text.Length - 1);
        int multiplier;
        if (int.TryParse(s, out multiplier))
        {
          Multiplier *= multiplier;
        }
        else
        {
          throw CreateException($"Cannot convert '{s}' to an int.");
        }
      }

      private void HandleModifiers(Modifiers modifiers)
      {
        if (modifiers.HasCustomModifiers() && !InputReader.Flags.HasFlag(InputReaderFlags.AllowCustomModifier))
        {
          throw CreateException("No custom modifiers allowed at this location.");
        }
        Modifiers |= modifiers;
        CheckConflict();
      }

      private void HandleHoldRelease(PressType pressType)
      {
        if (!InputReader.Flags.HasFlag(InputReaderFlags.AllowHoldRelease))
        {
          throw CreateException("Cannot use a hold/release token at this location.");
        }
        if (PressType != PressType.None)
        {
          throw CreateException("Unexpected token.");
        }
        PressType = pressType;
        CheckConflict();
      }

      private void HandleDynamicHotkey(string text)
      {
        if (!InputReader.Flags.HasFlag(InputReaderFlags.AllowDynamicHotkey))
        {
          throw CreateException($"Cannot use a dynamic hotkey token at this location (use '{nameof(Actor.SendDynamic)}' instead).");
        }
        ForbidModifierHoldRelease();
        for (int i = 0; i < Multiplier; i++)
        {
          Env.ForegroundListener.DynamicHotkeyCollection.GetAction(text)?.Invoke(InputReader.InjectorStream);
        }
        Multiplier = 1;
      }

      private void HandleCustomToken(string text)
      {
        if (!InputReader.Flags.HasFlag(InputReaderFlags.AllowCustomToken))
        {
          throw CreateException("No custom tokens allowed at this location.");
        }
        ForbidModifierHoldRelease();
        for (int i = 1; i < Multiplier; i++)
        {
          Config.HandleCustomToken(text, InputReader.InjectorStream);
        }
        Multiplier = 1;
      }

      private void Add(string str)
      {
        foreach (var c in str)
        {
          Combo combo;
          if (Combo.TryConvertFromChar(c, out combo))
          {
            Modifiers |= combo.Modifiers;
            CheckConflict();
            Add(combo.Input);
          }
          else
          {
            Add(c);
          }
        }
      }

      private void Add(char c)
      {
        if (!InputReader.Flags.HasFlag(InputReaderFlags.AllowCustomCharacter) || CreateChord)
        {
          throw CreateException($"No custom characters allowed at this location.");
        }
        if (Modifiers != Modifiers.None)
        {
          throw CreateException($"No modifiers allowed before character '{c}'.");
        }
        if (PressType != PressType.None)
        {
          throw CreateException($"Token '{Helper.CreateTokenString(PressType.ToString())}' not allowed before character '{c}'.");
        }
        InputReader.InjectorStream.Add(c, Multiplier);
        Multiplier = 1;
      }

      private void Add(Input input)
      {
        if (input == Input.Any && !InputReader.Flags.HasFlag(InputReaderFlags.AllowKeywordAny))
        {
          throw CreateException($"Input '{input.ToTokenString()}' not allowed at this location.");
        }
        if (PressType != PressType.None)
        {
          Debug.Assert(!CreateChord && Multiplier == 1 && Modifiers == Modifiers.None);
          InputReader.InjectorStream.Add(input, PressType == PressType.Hold);
          PressType = PressType.None;
        }
        else
        {
          if (CreateChord)
          {
            Debug.Assert(Multiplier == 1);
            InputReader.Combos.Add(new Combo(input, Modifiers));
          }
          else
          {
            InputReader.InjectorStream.Add(input, Modifiers, Multiplier);
            Multiplier = 1;
          }
          Modifiers = Modifiers.None;
        }
      }

      private void ForbidModifierHoldRelease()
      {
        if (PressType != PressType.None || Modifiers != Modifiers.None)
        {
          throw CreateException($"No modifiers allowed before this token.");
        }
      }

      private void CheckConflict()
      {
        if (PressType != PressType.None && (Modifiers != Modifiers.None || Multiplier != 1))
        {
          throw CreateException($"Cannot combine hold/release with modifier or multiplier.");
        }
      }
    }
  }

  [Flags]
  enum InputReaderFlags
  {
    None = 0,
    AllowKeywordAny = 1,
    AllowCustomModifier = 2,
    AllowHoldRelease = 4,
    AllowCustomCharacter = 8,
    ParseLiteral = 16,
    AllowMultiplier = 32,
    AllowDynamicHotkey = 64,
    AllowCustomToken = 128
  }

  enum PressType
  {
    None, Hold, Release
  }
}

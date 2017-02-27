using System;
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
          new ParseException(LocatedString, ex);
        }
      }

      private new ParseException CreateException(string text)
      {
        return new ParseException(LocatedString, text);
      }

      private void RunInner()
      {
        var tokenRegex = new Regex("^" + Config.TokenPattern);
        var unicodeRegex = new Regex($@"^UNICODE\[[0-9a-fA-F]+\]");
        while (!EndOfStream)
        {
          LocatedString = new LocatedString(Current.ToString(), Location);
          if (!ParseLiteral && At(tokenRegex))
          {
            string text;
            var token = ReadToken(out text);
            LocatedString = token;
            Modifiers modifiers;
            Input input;
            PressType pressType;
            DynamicHotkeyEnum dynamicHotkey;
            if (text[0] >= '0' && text[0] <= '9')
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
            else if (Enum.TryParse(text, out modifiers) && modifiers != Modifiers.None)
            {
              if (modifiers.HasCustomModifiers() && !InputReader.Flags.HasFlag(InputReaderFlags.AllowCustomModifier))
              {
                throw CreateException("No custom modifiers allowed at this location.");
              }
              Modifiers |= modifiers;
              CheckConflict();
            }
            else if (Enum.TryParse(text, out input) && input != Input.None)
            {
              Add(input);
            }
            else if (Enum.TryParse(text, out pressType) && pressType != PressType.None)
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
            }
            else if (Enum.TryParse(text, out dynamicHotkey) && dynamicHotkey != DynamicHotkeyEnum.None)
            {
              if (!InputReader.Flags.HasFlag(InputReaderFlags.AllowDynamicHotkey))
              {
                throw CreateException($"Cannot use a dynamic hotkey token at this location (use '{nameof(Actor.SendDynamic)}' instead).");
              }
              Env.ForegroundListener.DynamicHotkeyCollection.GetAction(text)?.Invoke(InputReader.InjectorStream);
            }
            else
            {
              throw CreateException($"Unrecognized token.");
            }
          }
          else if (!ParseLiteral && At(unicodeRegex))
          {
            string text;
            LocatedString = ReadUnicodeToken(out text);
            int code;
            if (int.TryParse(text, System.Globalization.NumberStyles.AllowHexSpecifier, null, out code))
            {
              string s;
              try
              {
                s = char.ConvertFromUtf32(code);
              }
              catch (ArgumentOutOfRangeException)
              {
                throw CreateException($"The hexadecimal value '{text}' is not a valid unicode code point.");
              }
              Add(s);
            }
            else
            {
              throw CreateException($"Cannot convert hexadecimal value '{text}' to an int.");
            }
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

      private void CheckConflict()
      {
        if (PressType != PressType.None && ((Modifiers != Modifiers.None || Multiplier != 1)))
        {
          throw CreateException($"Cannot combine hold/release with modifier or multiplier.");
        }
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
    AllowDynamicHotkey = 64
  }

  enum PressType
  {
    None, Hold, Release
  }
}

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using InputMaster.Actors;

namespace InputMaster.Parsers
{
  internal class InputReader
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
      private static readonly Regex TokenRegex = new Regex("^" + Constants.TokenPattern);
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

      private ParseException CreateException(string text)
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
        LocatedString = ReadToken(out var text);
        if (text[0] >= '0' && text[0] <= '9')
        {
          HandleMultiplier(text);
        }
        else if (Enum.TryParse(text, out Modifiers modifiers) && modifiers != Modifiers.None)
        {
          HandleModifiers(modifiers);
        }
        else if (Enum.TryParse(text, out Input input) && input != Input.None)
        {
          Add(input);
        }
        else if (Env.Config.TryGetCustomInput(text, out input))
        {
          Add(input);
        }
        else if (Env.Config.TryGetCustomCombo(text, out var combo))
        {
          HandleModifiers(combo.Modifiers);
          Add(combo.Input);
        }
        else if (Enum.TryParse(text, out PressType pressType) && pressType != PressType.None)
        {
          HandleHoldRelease(pressType);
        }
        else if (Env.Parser.IsDynamicHotkey(text))
        {
          HandleDynamicHotkey(text);
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
          throw CreateException("No multiplier allowed at this position.");
        }
        var s = text.Substring(0, text.Length - 1);
        if (int.TryParse(s, out var multiplier))
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
          throw CreateException($"Cannot use a dynamic hotkey token at this location (use '{nameof(MiscActor.SendDynamic)}' instead).");
        }
        ForbidModifierHoldRelease();
        Env.Parser.GetAction(text, out var action);
        for (var i = 0; i < Multiplier; i++)
        {
          action.Invoke(InputReader.InjectorStream);
        }
        Multiplier = 1;
      }

      private void Add(string str)
      {
        foreach (var c in str)
        {
          if (Combo.TryConvertFromChar(c, out var combo))
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
          throw CreateException("No custom characters allowed at this location.");
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
          Helper.RequireTrue(!CreateChord && Multiplier == 1 && Modifiers == Modifiers.None);
          InputReader.InjectorStream.Add(input, PressType == PressType.Hold);
          PressType = PressType.None;
        }
        else
        {
          if (CreateChord)
          {
            Helper.RequireTrue(Multiplier == 1);
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
          throw CreateException("No modifiers allowed before this token.");
        }
      }

      private void CheckConflict()
      {
        if (PressType != PressType.None && (Modifiers != Modifiers.None || Multiplier != 1))
        {
          throw CreateException("Cannot combine hold/release with modifier or multiplier.");
        }
      }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    private enum PressType
    {
      None, Hold, Release
    }
  }

  [Flags]
  internal enum InputReaderFlags
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
}

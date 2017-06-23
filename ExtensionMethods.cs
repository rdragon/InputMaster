﻿using System;
using System.Collections.Generic;
using System.Text;
using InputMaster.Parsers;
using InputMaster.Win32;
using System.Linq;

namespace InputMaster
{
  internal static class ExtensionMethods
  {
    public static bool IsMouseMessage(this WindowMessage message)
    {
      switch (message)
      {
        case WindowMessage.MouseMove:
        case WindowMessage.LeftMouseButtonDown:
        case WindowMessage.LeftMouseButtonUp:
        case WindowMessage.LeftMouseButtonDoubleClick:
        case WindowMessage.RightMouseButtonDown:
        case WindowMessage.RightMouseButtonUp:
        case WindowMessage.RightMouseButtonDoubleClick:
        case WindowMessage.MiddleMouseButtonDown:
        case WindowMessage.MiddleMouseButtonUp:
        case WindowMessage.MiddleMouseButtonDoubleClick:
        case WindowMessage.MouseWheel:
          return true;
        default:
          return false;
      }
    }

    public static bool IsCharacterKey(this Input input)
    {
      return input == Input.Space || Env.Config.KeyboardLayout.IsCharacterKey(input);
    }

    public static bool IsMouseInput(this Input input)
    {
      switch (input)
      {
        case Input.Lmb:
        case Input.Rmb:
        case Input.Mmb:
        case Input.WheelDown:
        case Input.WheelUp:
          return true;
        default:
          return false;
      }
    }

    public static Modifiers ToModifier(this Input input)
    {
      return Env.Config.TryGetModifier(input, out var modifier) ? modifier : Modifiers.None;
    }

    public static bool IsModifierKey(this Input input)
    {
      return input.ToModifier() != Modifiers.None;
    }

    public static bool IsStandardModifierKey(this Input input)
    {
      return input.ToModifier().ToStandardModifiers() != Modifiers.None;
    }

    public static string ToTokenString(this Input input, bool shiftDown = false)
    {
      return new Combo(input, shiftDown ? Modifiers.Shift : Modifiers.None).ToString();
    }

    public static string ToTokenString(this Modifiers modifiers)
    {
      var sb = new StringBuilder();
      foreach (var modifier in Helper.Modifiers.Where(z => modifiers.HasFlag(z)))
      {
        sb.Append(Helper.CreateTokenString(modifier.ToString()));
      }
      return sb.ToString();
    }

    public static Modifiers ToStandardModifiers(this Modifiers modifiers)
    {
      return modifiers & ConfigHelper.StandardModifiers;
    }

    public static bool HasCustomModifiers(this Modifiers modifiers)
    {
      return (modifiers & ~ConfigHelper.StandardModifiers) != 0;
    }

    public static void WriteError(this INotifier notifier, Exception ex, string text)
    {
      notifier.WriteError(text + " " + ex);
    }

    public static void WriteError(this INotifier notifier, Exception ex)
    {
      notifier.WriteError(ex.ToString());
    }

    public static T Add<T>(this IInjectorStream<T> stream, Input input, int count = 1)
    {
      Helper.RequireInInterval(count, nameof(count), 0, 9999);
      for (var i = 0; i < count; i++)
      {
        stream.Add(input, true);
        stream.Add(input, false);
      }
      return (T)stream;
    }

    public static T Add<T>(this IInjectorStream<T> stream, char c, int count = 1)
    {
      Helper.RequireInInterval(count, nameof(count), 0, 9999);
      for (var i = 0; i < count; i++)
      {
        stream.Add(c);
      }
      return (T)stream;
    }

    public static T Add<T>(this IInjectorStream<T> stream, Modifiers modifiers, bool down)
    {
      foreach (var modifier in Helper.Modifiers.Where(z => modifiers.HasFlag(z)))
      {
        if (Env.Config.TryGetModifierKey(modifier, out var input))
        {
          stream.Add(input, down);
        }
      }
      return (T)stream;
    }

    public static T Add<T>(this IInjectorStream<T> stream, Input input, Modifiers modifiers, int count = 1)
    {
      Helper.RequireInInterval(count, nameof(count), 0, 9999);
      if (count > 0)
      {
        stream.Add(modifiers, true);
        stream.Add(input, count);
        stream.Add(modifiers, false);
      }
      return (T)stream;
    }

    public static T Add<T>(this IInjectorStream<T> stream, Combo combo, int count = 1)
    {
      Helper.RequireInInterval(count, nameof(count), 0, 9999);
      return stream.Add(combo.Input, combo.Modifiers, count);
    }

    public static T Add<T>(this IInjectorStream<T> stream, IEnumerable<Combo> combos)
    {
      foreach (var combo in combos)
      {
        stream.Add(combo);
      }
      return (T)stream;
    }

    public static T Add<T>(this IInjectorStream<T> stream, string text, InputReader inputReader)
    {
      return stream.Add(new LocatedString(text), inputReader);
    }

    public static T Add<T>(this IInjectorStream<T> stream, LocatedString locatedString, InputReader inputReader)
    {
      inputReader.AddToInjectorStream(stream, locatedString);
      return (T)stream;
    }

    public static T Add<T>(this IInjectorStream<T> stream, Action<IInjectorStream<object>> action) where T : class
    {
      action(stream);
      return (T)stream;
    }

    public static bool TryGetAction(this IParser parser, DynamicHotkeyEnum key, bool complainIfNotFound, out Action<IInjectorStream<object>> action)
    {
      return parser.TryGetAction(key.ToString(), complainIfNotFound, out action);
    }
  }
}

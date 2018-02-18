﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace InputMaster
{
  public class HotkeyCollection
  {
    private readonly Dictionary<Chord, SortedSet<Hotkey>> _dictionary = new Dictionary<Chord, SortedSet<Hotkey>>();

    public int MaxChordLength { get; private set; }

    public void AddHotkey(Chord chord, Action<Combo> action, StandardSection section)
    {
      if (!_dictionary.TryGetValue(chord, out var set))
      {
        set = new SortedSet<Hotkey>(Hotkey.SectionComparer);
        _dictionary[chord] = set;
      }
      if (set.Any(z => z.Section.CompareTo(section) == 0))
        throw new AmbiguousHotkeyException("Ambiguous hotkey found" + Helper.GetBindingsSuffix(chord, nameof(chord)));
      var added = set.Add(new Hotkey(action, section));
      Helper.RequireTrue(added);
      if (chord.Length > Math.Max(Env.Config.MaxChordLength, MaxChordLength))
      {
        Env.Notifier.Error($"Chord with length '{chord.Length}' found, while maximum allowed length is '{Env.Config.MaxChordLength}'. " +
          $"To change the maximum allowed length, update the config variable '{nameof(Env.Config.MaxChordLength)}'.");
      }
      MaxChordLength = Math.Max(MaxChordLength, chord.Length);
    }

    public bool TryGetAction(Chord chord, out Action<Combo> action)
    {
      action = null;
      if (_dictionary.TryGetValue(chord, out var set))
      {
        foreach (var item in set)
        {
          if (item.Enabled)
            action = item.Action;
        }
      }
      return action != null;
    }

    public void Clear()
    {
      _dictionary.Clear();
      MaxChordLength = 0;
    }
  }
}

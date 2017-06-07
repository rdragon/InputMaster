using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace InputMaster
{
  internal class HotkeyCollection
  {
    private readonly Dictionary<Chord, SortedSet<Hotkey>> Dictionary = new Dictionary<Chord, SortedSet<Hotkey>>();

    public int MaxChordLength { get; private set; }

    public void AddHotkey(Chord chord, Action<Combo> action, StandardSection section)
    {
      if (!Dictionary.TryGetValue(chord, out var set))
      {
        set = new SortedSet<Hotkey>(Hotkey.SectionComparer);
        Dictionary[chord] = set;
      }
      if (set.Any(z => z.Section.CompareTo(section) == 0))
      {
        throw new AmbiguousHotkeyException();
      }
      var added = set.Add(new Hotkey(action, section));
      Debug.Assert(added);
      if (chord.Length > Math.Max(Env.Config.MaxChordLength, MaxChordLength))
      {
        Env.Notifier.WriteError($"Chord with length '{chord.Length}' found, while maximum allowed length is '{Env.Config.MaxChordLength}'. To change the maximum allowed length, update the config variable '{nameof(Env.Config.MaxChordLength)}'.");
      }
      MaxChordLength = Math.Max(MaxChordLength, chord.Length);
    }

    public Action<Combo> TryGetAction(Chord chord)
    {
      Action<Combo> action = null;
      if (Dictionary.TryGetValue(chord, out var set))
      {
        foreach (var item in set)
        {
          if (item.Enabled)
          {
            action = item.Action;
          }
        }
      }
      return action;
    }

    public void Clear()
    {
      Dictionary.Clear();
      MaxChordLength = 0;
    }
  }
}

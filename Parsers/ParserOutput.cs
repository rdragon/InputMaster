using System;
using System.Collections.Generic;
using System.Linq;

namespace InputMaster.Parsers
{
  public class ParserOutput
  {
    public HotkeyCollection HotkeyCollection { get; } = new HotkeyCollection();
    public List<Mode> Modes { get; } = new List<Mode>();
    public DynamicHotkeyCollection DynamicHotkeyCollection { get; } = new DynamicHotkeyCollection();
    public List<HashSet<string>> FlagSets { get; } = new List<HashSet<string>>();

    public void Clear()
    {
      Modes.Clear();
      HotkeyCollection.Clear();
      DynamicHotkeyCollection.Clear();
    }

    public void AddHotkey(Section section, Chord chord, Action<Combo> action, string description)
    {
      if (section is Mode mode)
      {
        if (!mode.IsComposeMode && chord.Length > 1)
          throw new ParseException($"Cannot use a chord with multiple keys inside a normal mode section. " +
            $"Use a '{Constants.ComposeModeSectionIdentifier}' section instead.");
        if (!mode.IsComposeMode && chord.Length == 1 && chord.First().Modifiers != Modifiers.None)
          throw new ParseException($"Cannot use modifiers inside a normal mode section. " +
            $"Use a '{Constants.ComposeModeSectionIdentifier}' section instead.");
        mode.AddHotkey(new ModeHotkey(chord, action, description));
      }
      else
        HotkeyCollection.AddHotkey(chord, action, (StandardSection)section);
    }

    public Mode AddMode(Mode mode)
    {
      var otherMode = Modes.FirstOrDefault(z => z.Name == mode.Name);
      if (otherMode != null && otherMode.IsComposeMode != mode.IsComposeMode)
        throw new ParseException($"Incompatible definitions of mode '{mode.Name}' found.");
      if (otherMode != null)
        mode = otherMode;
      else
        Modes.Add(mode);
      return mode;
    }
  }
}

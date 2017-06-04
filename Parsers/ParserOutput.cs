using System;
using System.Collections.Generic;
using System.Linq;

namespace InputMaster.Parsers
{
  internal class ParserOutput
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
      if (section.IsMode)
      {
        var mode = section.AsMode;
        if (!mode.IsComposeMode && chord.Length > 1)
        {
          throw new ParseException($"Cannot use a chord with multiple keys inside a normal mode section. Use a '{Config.ComposeModeSectionIdentifier}' section instead.");
        }
        if (!mode.IsComposeMode && chord.Length == 1 && chord.First().Modifiers != Modifiers.None)
        {
          throw new ParseException($"Cannot use modifiers inside a normal mode section. Use a '{Config.ComposeModeSectionIdentifier}' section instead.");
        }
        section.AsMode.AddHotkey(new ModeHotkey(chord, action, description));
      }
      else
      {
        HotkeyCollection.AddHotkey(chord, action, section.AsStandardSection);
      }
    }

    public Mode AddMode(Mode mode)
    {
      var otherMode = Modes.FirstOrDefault(z => z.Name == mode.Name);
      if (otherMode != null && otherMode.IsComposeMode != mode.IsComposeMode)
      {
        throw new ParseException($"Incompatible definitions of mode '{mode.Name}' found.");
      }
      if (otherMode != null)
      {
        mode = otherMode;
      }
      else
      {
        Modes.Add(mode);
      }
      return mode;
    }
  }
}

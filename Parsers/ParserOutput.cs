using System;
using System.Collections.Generic;
using System.Linq;

namespace InputMaster.Parsers
{
  class ParserOutput
  {
    public ParserOutput()
    {
      Modes = new List<Mode>();
      HotkeyCollection = new HotkeyCollection();
      DynamicHotkeyCollection = new DynamicHotkeyCollection();
    }

    public HotkeyCollection HotkeyCollection { get; }
    public List<Mode> Modes { get; }
    public DynamicHotkeyCollection DynamicHotkeyCollection { get; }

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

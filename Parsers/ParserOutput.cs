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

    public ParserOutput(IEnumerable<Mode> modes)
    {
      Modes = new List<Mode>(modes);
      AdditionalModesOutput = true;
    }

    public HotkeyCollection HotkeyCollection { get; }
    public List<Mode> Modes { get; }
    public DynamicHotkeyCollection DynamicHotkeyCollection { get; }
    public bool AdditionalModesOutput { get; }

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
  }
}

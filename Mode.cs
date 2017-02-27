using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace InputMaster
{
  class Mode : Section
  {
    private List<ModeHotkey> Hotkeys = new List<ModeHotkey>();

    public Mode(string name, bool isComposeMode)
    {
      Name = Helper.ForbidNull(name, nameof(name));
      IsComposeMode = isComposeMode;
    }

    public string Name { get; }
    public bool IsComposeMode { get; }

    public void AddHotkey(ModeHotkey modeHotkey)
    {
      Debug.Assert(IsComposeMode || (modeHotkey.Chord.Length == 1 && modeHotkey.Chord.First().Modifiers == Modifiers.None));
      Hotkeys.Add(modeHotkey);
    }

    public void CheckAmbiguity()
    {
      var chords = GetHotkeys().Select(z => z.Chord);
      foreach (var chord in chords)
      {
        foreach (var smallerChord in chords.Where(z => z.Length < chord.Length))
        {
          if (chord.HasPrefix(smallerChord))
          {
            Env.Notifier.Write($"Warning: mode '{Name}' has ambiguous chord '{smallerChord}'.");
          }
        }
      }
    }

    public IEnumerable<ModeHotkey> GetHotkeys()
    {
      return Hotkeys.AsReadOnly();
    }
  }
}

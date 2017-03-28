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
      foreach (var chord in Hotkeys.Select(z => z.Chord))
      {
        var small = modeHotkey.Chord;
        var big = chord;
        if (small.Length > big.Length)
        {
          Helper.Swap(ref small, ref big);
        }
        if (big.HasPrefix(small))
        {
          Env.Notifier.WriteWarning($"Mode '{Name}' has ambiguous chord '{small}'.");
        }
      }
      Hotkeys.Add(modeHotkey);
    }

    public IEnumerable<ModeHotkey> GetHotkeys()
    {
      return Hotkeys.AsReadOnly();
    }
  }
}

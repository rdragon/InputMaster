using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using InputMaster.Parsers;

namespace InputMaster
{
  internal class Mode : Section
  {
    private readonly List<ModeHotkey> Hotkeys = new List<ModeHotkey>();
    private readonly List<string> Includes = new List<string>();
    private MyIncludeState IncludeState = MyIncludeState.Idle;
    private bool HasAmbiguousChord;

    public Mode(string name, bool isComposeMode)
    {
      Name = Helper.ForbidNull(name, nameof(name));
      IsComposeMode = isComposeMode;
    }

    public string Name { get; }
    public bool IsComposeMode { get; }

    public void AddHotkey(ModeHotkey modeHotkey, bool hideWarningMessage = false)
    {
      Debug.Assert(IsComposeMode || modeHotkey.Chord.Length == 1 && modeHotkey.Chord.First().Modifiers == Modifiers.None);
      foreach (var chord in Hotkeys.Select(z => z.Chord))
      {
        var small = modeHotkey.Chord;
        var big = chord;
        if (small.Length > big.Length)
        {
          (small, big) = (big, small);
        }
        if (big.HasPrefix(small))
        {
          if (!hideWarningMessage)
          {
            Env.Notifier.WriteWarning($"Mode '{Name}' has ambiguous chord '{small}'.");
            HasAmbiguousChord = true;
          }
          break;
        }
      }
      Hotkeys.Add(modeHotkey);
    }

    public IEnumerable<ModeHotkey> GetHotkeys()
    {
      return Hotkeys.AsReadOnly();
    }

    public void IncludeMode(string modeName)
    {
      Includes.Add(modeName);
    }

    public void ResolveIncludes(ParserOutput parserOutput)
    {
      switch (IncludeState)
      {
        case MyIncludeState.Idle:
          IncludeState = MyIncludeState.Running;
          foreach (var name in Includes)
          {
            var mode = parserOutput.Modes.Find(z => z.Name == name);
            if (mode == null)
            {
              throw new ParseException($"Cannot find mode '{name}' (an include of mode '{Name}').");
            }
            if (mode.IsComposeMode != IsComposeMode)
            {
              throw new ParseException($"Cannot include mode '{name}' in mode '{Name}' as they are not of the same kind.");
            }
            mode.ResolveIncludes(parserOutput);
            foreach (var hotkey in mode.Hotkeys)
            {
              AddHotkey(hotkey, mode.HasAmbiguousChord);
            }
          }
          IncludeState = MyIncludeState.Done;
          break;
        case MyIncludeState.Running:
          throw new ParseException($"Cyclic include detected at mode '{Name}'.");
      }
    }

    enum MyIncludeState { Idle, Running, Done }
  }
}

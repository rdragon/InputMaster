using System.Collections.Generic;
using System.Linq;
using InputMaster.Parsers;

namespace InputMaster
{
  public class Mode : Section
  {
    private readonly List<ModeHotkey> _hotkeys = new List<ModeHotkey>();
    private readonly List<string> _includes = new List<string>();
    private MyIncludeState _includeState = MyIncludeState.Idle;
    private bool _hasAmbiguousChord;

    public Mode(string name, bool isComposeMode)
    {
      Name = name;
      IsComposeMode = isComposeMode;
    }

    public string Name { get; }
    public bool IsComposeMode { get; }
    public bool IsInputMode => !IsComposeMode;

    public void AddHotkey(ModeHotkey modeHotkey, bool hideWarningMessage = false)
    {
      Helper.RequireTrue(IsComposeMode || modeHotkey.Chord.Length == 1 && modeHotkey.Chord.First().Modifiers == Modifiers.None);
      foreach (var chord in _hotkeys.Select(z => z.Chord))
      {
        var small = modeHotkey.Chord;
        var big = chord;
        if (small.Length > big.Length)
          (small, big) = (big, small);
        if (big.HasPrefix(small))
        {
          if (!hideWarningMessage)
          {
            Env.Notifier.Warning($"Mode '{Name}' has ambiguous chord '{small}'.");
            _hasAmbiguousChord = true;
          }
          break;
        }
      }
      _hotkeys.Add(modeHotkey);
    }

    public IEnumerable<ModeHotkey> GetHotkeys()
    {
      return _hotkeys.AsReadOnly();
    }

    public void IncludeMode(string modeName)
    {
      _includes.Add(modeName);
    }

    public void ResolveIncludes(ParserOutput parserOutput)
    {
      switch (_includeState)
      {
        case MyIncludeState.Idle:
          _includeState = MyIncludeState.Running;
          foreach (var name in _includes)
          {
            var mode = parserOutput.Modes.Find(z => z.Name == name);
            if (mode == null)
              throw new ParseException($"Cannot find mode '{name}' (an include of mode '{Name}').");
            if (mode.IsComposeMode != IsComposeMode)
              throw new ParseException($"Cannot include mode '{name}' in mode '{Name}' as they are not of the same kind.");
            mode.ResolveIncludes(parserOutput);
            foreach (var hotkey in mode._hotkeys)
              AddHotkey(hotkey, mode._hasAmbiguousChord);
          }
          _includeState = MyIncludeState.Done;
          break;
        case MyIncludeState.Running:
          throw new ParseException($"Cyclic include detected at mode '{Name}'.");
      }
    }

    private enum MyIncludeState { Idle, Running, Done }
  }
}

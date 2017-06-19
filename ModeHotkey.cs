using System;

namespace InputMaster
{
  internal class ModeHotkey
  {
    public ModeHotkey(Chord chord, Action<Combo> action, string description)
    {
      Chord = chord;
      Action = action;
      Description = description;
    }

    public Chord Chord { get; }
    public Action<Combo> Action { get; }
    public string Description { get; }
  }
}

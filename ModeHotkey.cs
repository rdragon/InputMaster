using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InputMaster
{
  class ModeHotkey
  {
    public Chord Chord { get; }
    public Action<Combo> Action { get; }
    public string Description { get; }

    public ModeHotkey(Chord chord, Action<Combo> action, string description)
    {
      Chord = chord;
      Action = action;
      Description = description;
    }
  }
}

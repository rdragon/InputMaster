using System;
using System.Collections.Generic;

namespace InputMaster
{
  class Hotkey
  {
    public static IComparer<Hotkey> SectionComparer = new MySectionComparer();

    public Action<Combo> Action { get; }
    public StandardSection Section { get; }
    public bool Enabled => Section.IsEnabled;

    public Hotkey(Action<Combo> action, StandardSection section)
    {
      Action = action;
      Section = section;
    }

    private class MySectionComparer : IComparer<Hotkey>
    {
      public int Compare(Hotkey x, Hotkey y)
      {
        if (x == null)
        {
          return y == null ? 0 : -1;
        }
        else
        {
          return y == null ? 1 : x.Section.CompareTo(y.Section);
        }
      }
    }
  }
}

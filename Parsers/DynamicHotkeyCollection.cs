using System;
using System.Collections.Generic;

namespace InputMaster.Parsers
{
  public class DynamicHotkeyCollection : Dictionary<string, SortedSet<DynamicHotkey>>
  {
    public void AddDynamicHotkey(string name, Action<IInjectorStream<object>> action, StandardSection section)
    {
      if (!ContainsKey(name))
      {
        this[name] = new SortedSet<DynamicHotkey>();
      }
      this[name].Add(new DynamicHotkey(action, section));
    }
  }
}

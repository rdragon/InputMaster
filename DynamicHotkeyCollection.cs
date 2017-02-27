using System;
using System.Collections.Generic;

namespace InputMaster
{
  class DynamicHotkeyCollection
  {
    private Dictionary<string, SortedSet<DynamicHotkey>> Dictionary = new Dictionary<string, SortedSet<DynamicHotkey>>();

    public void AddDynamicHotkey(string name, Action<IInjectorStream<object>> action, StandardSection section)
    {
      if (!Dictionary.ContainsKey(name))
      {
        Dictionary[name] = new SortedSet<DynamicHotkey>();
      }
      Dictionary[name].Add(new DynamicHotkey(action, section));
    }

    public Action<IInjectorStream<object>> GetAction(DynamicHotkeyEnum key)
    {
      return GetAction(key.ToString());
    }

    public Action<IInjectorStream<object>> GetAction(string name)
    {
      var suffix = "";
      SortedSet<DynamicHotkey> set;
      if (Dictionary.TryGetValue(name, out set))
      {
        Action<IInjectorStream<object>> action = null;
        foreach (var dynamicHotkey in set)
        {
          if (dynamicHotkey.Enabled)
          {
            action = dynamicHotkey.Action;
          }
        }
        if (action != null)
        {
          return action;
        }
        suffix = " in this context" + Env.ForegroundListener.GetForegroundInfoSuffix();
      }
      Env.Notifier.WriteError($"No dynamic hotkey named '{name}' found{suffix}");
      return null;
    }

    public void Clear()
    {
      Dictionary.Clear();
    }
  }
}

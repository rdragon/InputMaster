using System;

namespace InputMaster
{
  class DynamicHotkey : IComparable<DynamicHotkey>
  {
    public Action<IInjectorStream<object>> Action { get; }
    public StandardSection Section { get; }
    public bool Enabled => Section.IsEnabled;

    public DynamicHotkey(Action<IInjectorStream<object>> action, StandardSection section)
    {
      Action = action;
      Section = section;
    }

    public int CompareTo(DynamicHotkey other)
    {
      return Section.CompareTo(other.Section);
    }
  }
}

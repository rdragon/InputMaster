using System;

namespace InputMaster
{
  internal class DynamicHotkey : IComparable<DynamicHotkey>
  {
    private readonly StandardSection Section;
    public bool Enabled => Section.IsEnabled;

    public DynamicHotkey(Action<IInjectorStream<object>> action, StandardSection section)
    {
      Action = action;
      Section = section;
    }

    public Action<IInjectorStream<object>> Action { get; }

    public int CompareTo(DynamicHotkey other)
    {
      return Section.CompareTo(other.Section);
    }
  }
}

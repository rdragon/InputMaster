using System;

namespace InputMaster.Parsers
{
  public class DynamicHotkey : IComparable<DynamicHotkey>
  {
    public bool Enabled => _section.IsEnabled;
    private readonly StandardSection _section;

    public DynamicHotkey(Action<IInjectorStream<object>> action, StandardSection section)
    {
      Action = action;
      _section = section;
    }

    public Action<IInjectorStream<object>> Action { get; }

    public int CompareTo(DynamicHotkey other)
    {
      return _section.CompareTo(other._section);
    }
  }
}

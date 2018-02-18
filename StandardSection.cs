using System;
using System.Text.RegularExpressions;
using System.Threading;

namespace InputMaster
{
  public class StandardSection : Section
  {
    private static int IdCounter;

    private readonly StandardSection Parent;
    private int Counter = -1;
    private bool Enabled;
    private readonly int Id;

    protected StandardSection(StandardSection parent)
    {
      Parent = parent;
      Id = Interlocked.Increment(ref IdCounter);
    }

    public StandardSection()
    {
      Column = 1;
      Id = Interlocked.Increment(ref IdCounter);
    }

    public bool IsEnabled
    {
      get
      {
        if (Counter < Env.StateCounter)
        {
          Counter = Env.StateCounter;
          Enabled = (IsTopLevel || Parent.IsEnabled) && ComputeEnabled();
        }
        return Enabled;
      }
    }

    private bool IsTopLevel => Parent == null;

    protected virtual bool ComputeEnabled()
    {
      return true;
    }

    private int GetDepth(int d = 0)
    {
      if (IsTopLevel)
      {
        return d;
      }
      return Parent.GetDepth(d + 1);
    }

    public int CompareTo(StandardSection other)
    {
      if (other == null)
      {
        return 1;
      }
      var x = GetDepth() - other.GetDepth();
      return x == 0 ? Id - other.Id : x;
    }
  }

  public class FlagSection : StandardSection
  {
    private readonly string Flag;

    public FlagSection(StandardSection parent, string text) : base(parent)
    {
      Flag = text;
    }

    protected override bool ComputeEnabled()
    {
      return Env.FlagManager.HasFlag(Flag);
    }
  }

  public class RegexSection : StandardSection
  {
    private readonly Regex Regex;
    private readonly RegexSectionType Type;

    public RegexSection(StandardSection parent, Regex regex, RegexSectionType type) : base(parent)
    {
      Regex = regex;
      if (!Enum.IsDefined(typeof(RegexSectionType), type))
      {
        throw new ArgumentOutOfRangeException(nameof(type));
      }
      Type = type;
    }

    protected override bool ComputeEnabled()
    {
      string s;
      switch (Type)
      {
        case RegexSectionType.Window: s = Env.ForegroundListener.ForegroundWindowTitle; break;
        default: s = Env.ForegroundListener.ForegroundProcessName; break;
      }
      return Regex.IsMatch(s);
    }
  }

  public enum RegexSectionType { Window, Process }
}

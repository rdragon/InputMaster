using System;
using System.Text.RegularExpressions;
using System.Threading;

namespace InputMaster
{
  public class StandardSection : Section
  {
    private static int _idCounter;
    private readonly StandardSection _parent;
    private readonly int _id;
    private bool _enabled;
    private int _counter = -1;

    protected StandardSection(StandardSection parent)
    {
      _parent = parent;
      _id = Interlocked.Increment(ref _idCounter);
    }

    public StandardSection()
    {
      Column = 1;
      _id = Interlocked.Increment(ref _idCounter);
    }

    private bool IsTopLevel => _parent == null;
    public bool IsEnabled
    {
      get
      {
        if (_counter < Env.StateCounter)
        {
          _counter = Env.StateCounter;
          _enabled = (IsTopLevel || _parent.IsEnabled) && ComputeEnabled();
        }
        return _enabled;
      }
    }

    protected virtual bool ComputeEnabled()
    {
      return true;
    }

    private int GetDepth(int d = 0)
    {
      if (IsTopLevel)
        return d;
      return _parent.GetDepth(d + 1);
    }

    public int CompareTo(StandardSection other)
    {
      if (other == null)
        return 1;
      var x = GetDepth() - other.GetDepth();
      return x == 0 ? _id - other._id : x;
    }
  }
}

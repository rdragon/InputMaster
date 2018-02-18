using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InputMaster.Instances
{
  public class FlagManager : Actor, IFlagManager
  {
    private MyState _state;
    private List<HashSet<string>> _flagSets; // Sets of mutual exclusive flags.

    private FlagManager()
    {
      Env.Parser.NewParserOutput += parserOutput => _flagSets = parserOutput.FlagSets;
    }

    public static Task<FlagManager> GetFlagManagerAsync()
    {
      return new FlagManager().InitializeAsync();
    }

    private async Task<FlagManager> InitializeAsync()
    {
      var stateHandler = Env.StateHandlerFactory.Create(new MyState(), Path.Combine(Env.Config.CacheDir, nameof(FlagManager)),
        StateHandlerFlags.SavePeriodically);
      _state = await stateHandler.LoadAsync();
      return this;
    }

    public event Action FlagsChanged = delegate { };

    public bool HasFlag(string flag)
    {
      return _state.Flags.Contains(flag);
    }

    public IEnumerable<string> GetFlags()
    {
      return _state.Flags.ToList();
    }

    public void ClearFlags()
    {
      if (!_state.Flags.Any())
        return;
      _state.Flags.Clear();
      Env.StateCounter++;
      FlagsChanged();
    }

    public override string ToString()
    {
      return string.Join(", ", _state.Flags);
    }

    [Command]
    public void SetFlag(string flag)
    {
      if (!HasFlag(flag))
        ToggleFlag(flag);
    }

    [Command]
    public void ClearFlag(string flag)
    {
      if (HasFlag(flag))
        ToggleFlag(flag);
    }

    [Command]
    public void ToggleFlag(string flag)
    {
      ToggleFlag(flag, true);
    }

    [Command]
    public async Task ToggleCustomFlagAsync()
    {
      var flag = await Helper.TryGetStringAsync("Flag");
      if (flag != null)
        ToggleFlag(flag);
    }

    private void ToggleFlag(string flag, bool raiseEvent)
    {
      if (_flagSets != null && !_state.Flags.Contains(flag))
      {
        foreach (var otherFlag in _flagSets.Where(z => z.Contains(flag)).SelectMany(z => z).Where(z => z != flag))
          if (_state.Flags.Contains(otherFlag))
            ToggleFlag(otherFlag, false);
      }
      _state.Flags.SymmetricExceptWith(new[] { flag });
      Env.StateCounter++;
      if (raiseEvent)
      {
        var text = _state.Flags.Contains(flag) ? "Enabled" : "Disabled";
        Env.Notifier.Info($"{text} flag '{flag}'.");
        FlagsChanged();
      }
    }

    public class MyState : IState
    {
      public HashSet<string> Flags { get; set; }

      public (bool, string message) Fix()
      {
        Flags = Flags ?? new HashSet<string>();
        return (true, "");
      }
    }
  }
}

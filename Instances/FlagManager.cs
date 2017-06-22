using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InputMaster.Instances
{
  internal class FlagManager : Actor, IFlagManager
  {
    private readonly HashSet<string> Flags = new HashSet<string>();
    private readonly MyState State;
    private List<HashSet<string>> FlagSets;

    public FlagManager()
    {
      Env.Parser.NewParserOutput += parserOutput => FlagSets = parserOutput.FlagSets;
      State = new MyState(this);
      State.Load();
    }

    public event Action FlagsChanged = delegate { };

    public bool IsSet(string flag)
    {
      return Flags.Contains(flag);
    }

    public IEnumerable<string> GetFlags()
    {
      return Flags.ToList();
    }

    public void ClearFlags()
    {
      Flags.Clear();
      State.Changed = true;
      FlagsChanged();
    }

    public override string ToString()
    {
      return string.Join(", ", Flags);
    }

    [Command]
    public void SetFlag(string flag)
    {
      if (!IsSet(flag))
      {
        ToggleFlag(flag);
      }
    }

    [Command]
    public void ClearFlag(string flag)
    {
      if (IsSet(flag))
      {
        ToggleFlag(flag);
      }
    }

    [Command]
    public void ToggleFlag(string flag)
    {
      ToggleFlag(flag, true);
    }

    [Command]
    public async Task SetCustomFlagAsync()
    {
      await Task.Yield();
      if (!Helper.TryGetString("Flag", out var s))
      {
        return;
      }
      SetFlag(s);
    }

    [Command]
    public async Task ClearCustomFlagAsync()
    {
      await Task.Yield();
      if (!Helper.TryGetString("Flag", out var s))
      {
        return;
      }
      ClearFlag(s);
    }

    private void ToggleFlag(string flag, bool raiseEvent)
    {
      if (FlagSets != null && !Flags.Contains(flag))
      {
        foreach (var otherFlag in FlagSets.Where(z => z.Contains(flag)).SelectMany(z => z).Where(z => z != flag))
        {
          if (Flags.Contains(otherFlag))
          {
            ToggleFlag(otherFlag, false);
          }
        }
      }
      Flags.SymmetricExceptWith(new[] { flag });
      State.Changed = true;
      if (raiseEvent)
      {
        var text = Flags.Contains(flag) ? "Enabled" : "Disabled";
        Env.Notifier.Write($"{text} flag '{flag}'.");
        FlagsChanged();
      }
    }

    private class MyState : State<FlagManager>
    {
      public MyState(FlagManager flagManager) : base(nameof(FlagManager), flagManager) { }

      protected override void Load(BinaryReader reader)
      {
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
          Parent.Flags.Add(reader.ReadString());
        }
      }

      protected override void Save(BinaryWriter writer)
      {
        foreach (var flag in Parent.Flags)
        {
          writer.Write(flag);
        }
      }
    }
  }
}

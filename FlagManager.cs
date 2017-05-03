using InputMaster.Parsers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InputMaster
{
  class FlagManager : IFlagViewer
  {
    private readonly HashSet<string> Flags = new HashSet<string>();
    private List<HashSet<string>> FlagSets;

    public FlagManager(Parser parser, Brain brain = null)
    {
      if (parser != null)
      {
        parser.NewParserOutput += (parserOutput) => FlagSets = parserOutput.FlagSets;
      }
      if (brain != null)
      {
        var state = new MyStateManager(this);
        state.Load();
        state.Changed = true;
        brain.Exiting += Try.Wrap(state.Save);
      }
    }

    public event Action FlagsChanged = delegate { };

    public bool IsFlagSet(string flag)
    {
      return Flags.Contains(flag);
    }

    public void ClearFlags()
    {
      Flags.Clear();
    }

    public override string ToString()
    {
      return string.Join(", ", Flags);
    }

    [CommandTypes(CommandTypes.Visible)]
    public void SetFlag(string flag)
    {
      if (!IsFlagSet(flag))
      {
        ToggleFlag(flag);
      }
    }

    [CommandTypes(CommandTypes.Visible)]
    public void ClearFlag(string flag)
    {
      if (IsFlagSet(flag))
      {
        ToggleFlag(flag);
      }
    }

    [CommandTypes(CommandTypes.Visible)]
    public void ToggleFlag(string flag)
    {
      ToggleFlag(flag, true);
    }

    [CommandTypes(CommandTypes.Visible)]
    public async Task SetCustomFlag()
    {
      await Task.Yield();
      var s = Helper.GetString("Flag");
      if (!string.IsNullOrWhiteSpace(s))
      {
        SetFlag(s);
      }
    }

    [CommandTypes(CommandTypes.Visible)]
    public async Task ClearCustomFlag()
    {
      await Task.Yield();
      var s = Helper.GetString("Flag");
      if (!string.IsNullOrWhiteSpace(s))
      {
        ClearFlag(s);
      }
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
      Flags.SymmetricExceptWith(new string[] { flag });
      if (raiseEvent)
      {
        var text = Flags.Contains(flag) ? "Enabled" : "Disabled";
        Env.Notifier.Write($"{text} flag '{flag}'.");
        FlagsChanged();
      }
    }

    private class MyStateManager : State
    {
      private readonly FlagManager FlagManager;

      public MyStateManager(FlagManager flagManager) : base(nameof(FlagManager))
      {
        FlagManager = flagManager;
      }

      protected override void Load(BinaryReader reader)
      {
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
          FlagManager.Flags.Add(reader.ReadString());
        }
      }

      protected override void Save(BinaryWriter writer)
      {
        foreach (var flag in FlagManager.Flags)
        {
          writer.Write(flag);
        }
      }
    }
  }
}

using System;
using System.Collections.Generic;
using System.IO;

namespace InputMaster
{
  class FlagManager : IFlagViewer
  {
    private readonly HashSet<string> Flags = new HashSet<string>();

    public FlagManager(Brain brain = null)
    {
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
      Flags.SymmetricExceptWith(new string[] { flag });
      FlagsChanged();
      var text = Flags.Contains(flag) ? "Enabled" : "Disabled";
      Env.Notifier.Write($"{text} flag '{flag}'.");
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

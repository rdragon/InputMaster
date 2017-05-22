using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;

namespace InputMaster.Extensions
{
  [CommandTypes(CommandTypes.Visible)]
  class VarActor
  {
    private readonly Dictionary<string, string> Dict = new Dictionary<string, string>();
    private readonly ForegroundInteractor ForegroundInteractor;

    public VarActor(Brain brain, ForegroundInteractor foregroundInteractor)
    {
      Helper.ForbidNull(brain, nameof(brain));
      Helper.ForbidNull(foregroundInteractor, nameof(foregroundInteractor));
      ForegroundInteractor = foregroundInteractor;
      var state = new MyStateManager(nameof(VarActor), this);
      state.Load();
      state.Changed = true;
      brain.Exiting += Try.Wrap(state.Save);
    }

    public async Task SetVar(string name, string value = null)
    {
      await SetVarFormatted(name, "{0}", value);
    }

    public async Task SetVarFormatted(string name, [AllowSpaces]string format, string value = null)
    {
      if (value == null)
      {
        value = await ForegroundInteractor.GetSelectedText();
      }
      Dict[name] = string.Format(format, value);
      Env.Notifier.Write($"{name}: {Dict[name]}");
    }

    public void PrintVar(string name)
    {
      if (Dict.TryGetValue(name, out var value))
      {
        Env.CreateInjector().Add(value, Config.LiteralInputReader).Run();
      }
      else
      {
        Env.Notifier.Write($"Var {name} not found.");
      }
    }

    public async Task ExportVars()
    {
      await ForegroundInteractor.Paste(JsonConvert.SerializeObject(Dict, Formatting.Indented).Replace(Environment.NewLine, "\n"));
    }

    public async Task ImportVars(string text = null)
    {
      if (text == null)
      {
        text = await ForegroundInteractor.GetSelectedText();
      }
      var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
      foreach (var pair in dict)
      {
        await SetVar(pair.Key, pair.Value);
      }
    }

    private class MyStateManager : State
    {
      private readonly VarActor Parent;

      public MyStateManager(string name, VarActor parent) : base(name, Config.DataDir)
      {
        Parent = parent;
      }

      protected override void Load(BinaryReader reader)
      {
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
          Parent.Dict.Add(reader.ReadString(), reader.ReadString());
        }
      }

      protected override void Save(BinaryWriter writer)
      {
        foreach (var pair in Parent.Dict)
        {
          writer.Write(pair.Key);
          writer.Write(pair.Value);
        }
      }
    }
  }
}

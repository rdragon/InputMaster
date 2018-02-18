using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace InputMaster.Actors
{
  public class VarActor : Actor
  {
    private MyState _state;
    private string _currentGroup = "default";

    private VarActor() { }

    private async Task<VarActor> InitializeAsync()
    {
      var stateHandler = Env.StateHandlerFactory.Create(new MyState(), nameof(VarActor),
          StateHandlerFlags.Exportable | StateHandlerFlags.UserEditable | StateHandlerFlags.SavePeriodically);
      _state = await stateHandler.LoadAsync();
      Env.App.AddSaveAction(stateHandler.SaveAsync);
      return this;
    }

    public static Task<VarActor> GetVarActorAsync()
    {
      return new VarActor().InitializeAsync();
    }

    [Command]
    private void SetVarGroup(string name)
    {
      _currentGroup = name;
      if (!_state.Dict.ContainsKey(_currentGroup))
        _state.Dict[_currentGroup] = new Dictionary<string, string>();
      Env.Notifier.Info($"CurrentGroup: {_currentGroup}");
    }

    [Command]
    private Task SetVarAsync(string name, string value = null)
    {
      return SetVarFormattedAsync(name, "{0}", value);
    }

    [Command]
    private async Task SetVarFormattedAsync(string name, [AllowSpaces]string format, string value = null)
    {
      if (value == null)
        value = await ForegroundInteractor.GetSelectedTextAsync();
      _state.Dict[_currentGroup][name] = string.Format(format, value);
      Env.Notifier.Info($"{name}: {_state.Dict[_currentGroup][name]}  [{_currentGroup}]");
    }

    [Command]
    private void PrintVar(string name)
    {
      if (_state.Dict[_currentGroup].TryGetValue(name, out var value))
        Env.CreateInjector().Add(value, Env.Config.LiteralInputReader).Run();
      else
        Env.Notifier.Info($"Var {name} not found.");
    }

    [Command]
    private Task ExportVars()
    {
      return ForegroundInteractor.PasteAsync(Helper.JsonSerialize(_state.Dict[_currentGroup], Formatting.Indented));
    }

    [Command]
    private async Task ImportVarsAsync(string text = null)
    {
      text = text ?? await ForegroundInteractor.GetSelectedTextAsync();
      var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
      foreach (var pair in dict)
        await SetVarAsync(pair.Key, pair.Value);
    }

    public class MyState : IState
    {
      public Dictionary<string, Dictionary<string, string>> Dict { get; set; }

      public (bool, string message) Fix()
      {
        Dict = Dict ?? new Dictionary<string, Dictionary<string, string>>();
        return (true, "");
      }
    }
  }
}

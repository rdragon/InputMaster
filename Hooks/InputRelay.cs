using System;

namespace InputMaster.Hooks
{
  /// <summary>
  /// Very simple hook that can be switched on and off by pressing a key. It also has the capability to capture all events until a dedicated
  /// key is pressed.
  /// </summary>
  public class InputRelay : Actor, IInputHook
  {
    private readonly IInputHook _targetHook;
    private bool _enabled;
    private bool _toggleKeyIsDown;
    private bool _captureAll;
    private Tuple<Input, Input> _simulatedInput;

    public InputRelay(IInputHook targetHook)
    {
      _targetHook = targetHook;
      _enabled = true;
    }

    public void Handle(InputArgs e)
    {
      if (_captureAll)
      {
        e.Capture = true;
        if (e.Input == Env.Config.CloseKey)
          _captureAll = false;
      }
      else if (e.Input == Env.Config.ToggleHookKey)
      {
        if (e.Down && !_toggleKeyIsDown)
        {
          var enabled = _enabled;
          Reset();
          _enabled = !enabled;
        }
        else if (!e.Down)
          _toggleKeyIsDown = false;
      }
      else if (_enabled)
      {
        if (_simulatedInput != null && e.Up && e.Input == _simulatedInput.Item1)
        {
          ReleaseSimulatedInput();
          e.Capture = true;
        }
        _targetHook.Handle(e);
      }
    }

    public void Reset()
    {
      _targetHook.Reset();
      _toggleKeyIsDown = false;
      ReleaseSimulatedInput();
      _enabled = true;
    }

    public string GetStateInfo()
    {
      var s = _targetHook.GetStateInfo();
      return !_toggleKeyIsDown && _enabled && !_captureAll ? s :
        s + nameof(InputRelay) + Helper.GetBindingsSuffix(
          _toggleKeyIsDown, nameof(_toggleKeyIsDown),
          _enabled, nameof(_enabled),
          _captureAll, nameof(_captureAll)) + '\n';
    }

    [Command]
    public void CaptureAllInput()
    {
      _captureAll = true;
    }

    /// <summary>
    /// Like <see cref="Actors.MiscActor.Send(Action)"/>, but only accepts a single <see cref="Input"/> as argument, and will release the
    /// given input when the hotkey key that triggered the event is released. Also, when the hotkey key is being held down, no additional
    /// injections are made (if this is not desired, an additional parameter should be added to the function which controls this behaviour).
    /// </summary>
    [Command]
    public void SimulateInput(HotkeyTrigger trigger, Input input)
    {
      if (_simulatedInput != null)
      {
        if (_simulatedInput.Item2 != input)
          Env.Notifier.Error("Already simulating a key.");
      }
      else if (input.IsStandardModifierKey())
        Env.Notifier.Error("Simulating a standard modifier key is not supported.");
      else
      {
        _simulatedInput = new Tuple<Input, Input>(trigger.Combo.Input, input);
        Env.CreateInjector().Add(input, true).Run();
      }
    }

    private void ReleaseSimulatedInput()
    {
      if (_simulatedInput == null)
        return;
      Env.CreateInjector().Add(_simulatedInput.Item2, false).Run();
      _simulatedInput = null;
    }
  }
}

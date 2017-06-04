using System;

namespace InputMaster.Hooks
{
  /// <summary>
  /// Very simple hook that can be switched on and off by pressing a key. It also has the capability to capture all events until <see cref="Config.CloseKey"/> is pressed.
  /// </summary>
  internal class InputRelay : Actor, IInputHook
  {
    private bool Enabled;
    private readonly IInputHook TargetHook;
    private bool ToggleKeyIsDown;
    private bool CaptureAll;
    private Tuple<Input, Input> SimulatedInput;

    public InputRelay(IInputHook targetHook)
    {
      TargetHook = targetHook;
      Enabled = true;
    }

    public void Handle(InputArgs e)
    {
      if (CaptureAll)
      {
        e.Capture = true;
        if (e.Input == Config.CloseKey)
        {
          CaptureAll = false;
        }
      }
      else if (e.Input == Config.ToggleHookKey)
      {
        if (e.Down && !ToggleKeyIsDown)
        {
          var enabled = Enabled;
          Reset();
          Enabled = !enabled;
        }
        else if (!e.Down)
        {
          ToggleKeyIsDown = false;
        }
      }
      else if (Enabled)
      {
        if (SimulatedInput != null && e.Up && e.Input == SimulatedInput.Item1)
        {
          ReleaseSimulatedInput();
          e.Capture = true;
        }
        TargetHook.Handle(e);
      }
    }

    public void Reset()
    {
      TargetHook.Reset();
      ToggleKeyIsDown = false;
      ReleaseSimulatedInput();
      Enabled = true;
    }

    public string GetStateInfo()
    {
      var s = TargetHook.GetStateInfo();
      if (ToggleKeyIsDown || !Enabled || CaptureAll)
      {
        s += nameof(InputRelay) + Helper.GetBindingsSuffix(
          ToggleKeyIsDown, nameof(ToggleKeyIsDown),
          Enabled, nameof(Enabled),
          CaptureAll, nameof(CaptureAll)) + '\n';
      }
      return s;
    }

    [CommandTypes(CommandTypes.Visible)]
    public void CaptureAllInput()
    {
      CaptureAll = true;
    }

    /// <summary>
    /// Like <see cref="MiscActor.Send(Action)"/>, but only accepts a single <see cref="Input"/> as argument, and will release the given input when the hotkey key that triggered the event is released.
    /// Also, when the hotkey key is being held down, no additional injections are made (if this is not desired, an additional parameter should be added to the function which controls this behaviour).
    /// </summary>
    [CommandTypes(CommandTypes.Visible)]
    public void SimulateInput(HotkeyTrigger trigger, Input input)
    {
      if (SimulatedInput != null)
      {
        if (SimulatedInput.Item2 != input)
        {
          Env.Notifier.WriteError("Already simulating a key.");
        }
      }
      else if (input.IsStandardModifierKey())
      {
        Env.Notifier.WriteError("Simulating a standard modifier key is not supported.");
      }
      else
      {
        SimulatedInput = new Tuple<Input, Input>(trigger.Combo.Input, input);
        Env.CreateInjector().Add(input, true).Run();
      }
    }

    private void ReleaseSimulatedInput()
    {
      if (SimulatedInput != null)
      {
        Env.CreateInjector().Add(SimulatedInput.Item2, false).Run();
        SimulatedInput = null;
      }
    }
  }
}

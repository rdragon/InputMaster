using System;
using System.Collections.Generic;
using System.Linq;
using InputMaster.Parsers;

namespace InputMaster.Hooks
{
  public class InputHook : Actor, IInputHook
  {
    private readonly HashSet<Input> _captured = new HashSet<Input>();
    private readonly IComboHook _targetHook;
    /// <summary>
    /// All virtually active modifiers. Some can have a key that is up (the stuck modifiers).
    /// </summary>
    private Modifiers _modifiers;
    /// <summary>
    /// These virtually active modifiers have a key that is up.
    /// </summary>
    private Modifiers _stuckModifiers;
    /// <summary>
    /// Virtually active modifiers that will become stuck modifiers when the corresponding key is released.
    /// </summary>
    private Modifiers _almostStuckModifiers;
    /// <summary>
    /// Modifiers that the system sees as active and which will be released according to <see cref="_timeOfRelease"/>.
    /// </summary>
    private Modifiers _modifiersToRelease;
    /// <summary>
    /// Specifies when to release <see cref="_modifiersToRelease"/>.
    /// </summary>
    private ReleaseTime _timeOfRelease;
    /// <summary>
    /// All non-modifier keys for which the key down was captured and for which the next key up event will be captured.
    /// </summary>
    private Action _backspaceAction;

    public InputHook(IComboHook targetHook)
    {
      _targetHook = targetHook;
    }

    public void Handle(InputArgs e)
    {
      var eventInjected = false; // There is one case in which we inject the given event ourselves.
      var isModifier = e.Input.IsModifierKey();
      var standardModifiers = _modifiers.ToStandardModifiers();

      // Try to release the modifiers to release.
      if (_modifiersToRelease != Modifiers.None && _timeOfRelease == ReleaseTime.AtNextEvent)
      {
        Env.CreateInjector().Add(_modifiersToRelease, false).Run();
        _modifiersToRelease = Modifiers.None;
      }

      // When there are still modifiers to release, all down events are captured (ignored). This makes things easier, as we can now assume
      // that there are no modifiers to release for down events that are not yet captured.
      if (_modifiersToRelease != Modifiers.None && e.Down)
        e.Capture = true;

      // Handle a down event.
      if (e.Down && !e.Capture)
        HandleDownEvent(e);

      // If there are standard modifiers virtually active and a non-modifier key down event is not captured, these modifiers need to be
      // injected.
      if (e.Down && !isModifier && !e.Capture && standardModifiers != Modifiers.None)
      {
        // A mouse event is handled separately.
        if (e.Input.IsMouseInput())
        {
          // Inject the key down events of the modifiers. These injections occur during the hook procedure of the mouse event, and will
          // therefore arrive in time.
          Env.CreateInjector().Add(standardModifiers, true).Run();

          _modifiersToRelease |= standardModifiers;

          // Specify when to release the modifiers.
          _timeOfRelease = e.Input == Input.WheelDown || e.Input == Input.WheelUp ? ReleaseTime.AtNextEvent : ReleaseTime.AfterMouseUpEvent;
        }
        else
        {
          e.Capture = true;
          eventInjected = true;
          // Inject the modifier down events, the key down event, and the modifier up events.
          Env.CreateInjector().Add(standardModifiers, true).Add(e.Input, true).Add(standardModifiers, false).Run();
        }
      }

      // For a non-modifier key down, or a modifier key down that is captured, the stuck modifiers are reset and the almost stuck modifiers
      // are cleared.
      if (e.Down && (!isModifier || e.Capture))
      {
        DeactivateStuckModifiers();
        _almostStuckModifiers = Modifiers.None;
      }

      // Update time of release.
      if (_modifiersToRelease != Modifiers.None && _timeOfRelease == ReleaseTime.AfterMouseUpEvent && e.Up && e.Input.IsMouseInput())
        _timeOfRelease = ReleaseTime.AtNextEvent;

      // A modifier key that is not captured affects the virtual modifier state.
      if (isModifier && !e.Capture)
        UpdateVirtualModifiers(e);

      // Capture all modifiers and some non-modifier key up events.
      if (isModifier)
      {
        // Capture all modifiers. Only injected modifiers will get through.
        e.Capture = true;
      }
      else
      {
        // For a non-modifier key down event, add the key to the captured keys.
        if (e.Capture && e.Down && !eventInjected)
          _captured.Add(e.Input);
        // For a non-modifier key up event for which the key down was captured, also capture the key up.
        else if (e.Up && _captured.Contains(e.Input))
        {
          e.Capture = true;
          _captured.Remove(e.Input);
        }
      }
    }

    private void HandleDownEvent(InputArgs e)
    {
      var combo = new Combo(e.Input, _modifiers);
      var b = _backspaceAction;
      _backspaceAction = null;
      // If there is an action pending for backspace, execute it.
      if (b != null && combo == Combo.Backspace)
      {
        b();
        e.Capture = true;
      }
      // Clear the stuck modifiers with the close key.
      else if (_stuckModifiers != Modifiers.None && e.Input == Env.Config.CloseKey)
        e.Capture = true;
      // Otherwise, if there are no modifiers needed to release, let the next hook decide what to do.
      else if (_modifiersToRelease == Modifiers.None)
      {
        var comboArgs = new ComboArgs(combo);
        _targetHook.Handle(comboArgs);
        e.Capture = comboArgs.Capture;
        // Capture a key down event that does not trigger an action when custom modifiers are active.
        e.Capture = e.Capture || _modifiers.HasCustomModifiers() && !e.Input.IsModifierKey();
      }
    }

    private void UpdateVirtualModifiers(InputArgs e)
    {
      var modifier = e.Input.ToModifier();
      if (e.Down)
      {
        // A modifier key down event has two cases:
        //   1) If this key was in the stuck state, it is no longer stuck, but instead it is again almost stuck.
        //   2) If this modifier was not yet active, then make it active and set it in the almost stuck state.
        if (_stuckModifiers.HasFlag(modifier))
        {
          _stuckModifiers &= ~modifier;
          _almostStuckModifiers |= modifier;
        }
        else if (!_modifiers.HasFlag(modifier))
        {
          _modifiers |= modifier;
          _almostStuckModifiers |= modifier;
        }
      }
      else
      {
        // For a modifier key up event, add the modifier to the stuck keys when it was almost stuck, but make it inactive otherwise.
        if (_almostStuckModifiers.HasFlag(modifier))
        {
          _stuckModifiers |= modifier;
          _almostStuckModifiers &= ~modifier;
        }
        else
          _modifiers &= ~modifier;
      }
    }

    private void DeactivateStuckModifiers()
    {
      _modifiers &= ~_stuckModifiers;
      _stuckModifiers = Modifiers.None;
    }

    public void Reset()
    {
      _modifiers = Modifiers.None;
      _stuckModifiers = Modifiers.None;
      _almostStuckModifiers = Modifiers.None;
      _modifiersToRelease = Modifiers.None;
      _captured.Clear();
      ResetStandardModifierKeys();
      _targetHook.Reset();
    }

    public string GetStateInfo()
    {
      var s = _targetHook.GetTestStateInfo();
      if ((_modifiers | _stuckModifiers | _almostStuckModifiers) != Modifiers.None || _captured.Count > 0)
      {
        s += nameof(InputHook) + Helper.GetBindingsSuffix(_modifiers, nameof(_modifiers), _stuckModifiers, nameof(_stuckModifiers),
          _almostStuckModifiers, nameof(_almostStuckModifiers), _captured.Count, nameof(_captured)) + '\n';
      }
      return s;
    }

    [Command(CommandTypes.ExecuteAtParseTime)]
    public void Replace(ExecuteAtParseTimeData data, [AllowSpaces] LocatedString argument)
    {
      Replace(data, argument, false);
    }

    [Command(CommandTypes.ExecuteAtParseTime)]
    public void ReplaceOp(ExecuteAtParseTimeData data, [AllowSpaces] LocatedString argument)
    {
      Replace(data, argument, true);
    }

    private static void ResetStandardModifierKeys()
    {
      ConfigHelper
        .ModifierKeys.Select(z => z.Item1)
        .Where(z => z.IsStandardModifierKey())
        .Aggregate(Env.CreateInjector(), (x, y) => x.Add(y, false))
        .Run();
    }

    private void Replace(ExecuteAtParseTimeData data, LocatedString argument, bool surroundWithSpaces)
    {
      var chord = data.Chord;
      int backspaceCount, deleteCount;
      if (data.Section is Mode)
      {
        backspaceCount = 0;
        deleteCount = 0;
      }
      else
      {
        var count = new InputCounter(false).Add(chord.Take(chord.Length - 1));
        backspaceCount = count.LeftCount;
        deleteCount = count.RightCount;
      }
      var replaceAction = Env.CreateInjector()
        .Add(Input.Bs, backspaceCount)
        .Add(Input.Del, deleteCount)
        .Add(Input.Space, surroundWithSpaces ? 1 : 0)
        .Add(argument, Env.Config.DefaultInputReader)
        .Add(Input.Space, surroundWithSpaces ? 1 : 0)
        .Compile();
      var count1 = new InputCounter(true).Add(argument, Env.Config.DefaultInputReader);
      var backspaceAction = Env.CreateInjector()
        .Add(Input.Bs, count1.LeftCount + (surroundWithSpaces ? 2 : 0))
        .Add(Input.Del, count1.RightCount)
        .Compile();
      var description = chord + " " + nameof(Replace) + " " + argument.Value;
      void Action(Combo dummy)
      {
        _backspaceAction = backspaceAction;
        replaceAction();
      }
      data.ParserOutput.AddHotkey(data.Section, chord, Action, description);
    }

    private enum ReleaseTime
    {
      AfterMouseUpEvent, AtNextEvent
    }
  }
}

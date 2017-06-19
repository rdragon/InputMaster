using System;
using System.Collections.Generic;
using System.Linq;
using InputMaster.Parsers;

namespace InputMaster.Hooks
{
  internal class InputHook : Actor, IInputHook
  {
    /// <summary>
    /// All virtually active modifiers. Some can have a key that is up (the stuck modifiers).
    /// </summary>
    private Modifiers Modifiers;
    /// <summary>
    /// These virtually active modifiers have a key that is up.
    /// </summary>
    private Modifiers StuckModifiers;
    /// <summary>
    /// Virtually active modifiers that will become stuck modifiers when the corresponding key is released.
    /// </summary>
    private Modifiers AlmostStuckModifiers;
    /// <summary>
    /// Modifiers that the system sees as active and which will be released according to <see cref="TimeOfRelease"/>. There is no connection with the virtually active modifiers.
    /// </summary>
    private Modifiers ModifiersToRelease;
    /// <summary>
    /// Specifies when to release <see cref="ModifiersToRelease"/>.
    /// </summary>
    private ReleaseTime TimeOfRelease;
    /// <summary>
    /// All non-modifier keys for which the key down was captured and for which the next key up event will be captured.
    /// </summary>
    private readonly HashSet<Input> Captured = new HashSet<Input>();
    private readonly IComboHook TargetHook;
    private Action BackspaceAction;

    public InputHook(IComboHook targetHook)
    {
      TargetHook = targetHook;
    }


    public void Handle(InputArgs e)
    {
      if (ModifiersToRelease != Modifiers.None && TimeOfRelease == ReleaseTime.AtNextEvent)
      {
        Env.CreateInjector().Add(ModifiersToRelease, false).Run();
        ModifiersToRelease = Modifiers.None;
      }

      if (ModifiersToRelease != Modifiers.None && e.Down)
      {
        e.Capture = true;
      }

      // Handle a key down event.
      if (e.Down && !e.Capture)
      {
        var combo = new Combo(e.Input, Modifiers);
        var b = BackspaceAction;
        BackspaceAction = null;
        // If there is an action pending for backspace, execute it.
        if (b != null && combo == Combo.Backspace)
        {
          b();
          e.Capture = true;
        }
        // Clear the stuck modifiers with the close key.
        else if (StuckModifiers != Modifiers.None && e.Input == Env.Config.CloseKey)
        {
          e.Capture = true;
        }
        // Otherwise, if there are no modifiers needed to release, let the next hook decide what to do.
        else if (ModifiersToRelease == Modifiers.None)
        {
          var comboArgs = new ComboArgs(combo);
          TargetHook.Handle(comboArgs);
          e.Capture = comboArgs.Capture;
          // Capture a key down event that does not trigger an action when custom modifiers are active.
          e.Capture = e.Capture || Modifiers.HasCustomModifiers() && !e.Input.IsModifierKey();
        }
      }

      var doNotAddToCaptured = false;
      var isModifier = e.Input.IsModifierKey();

      // If there are standard modifiers virtually active and a non-modifier key down event is not captured, these modifiers need to be injected.
      var standardModifiers = Modifiers.ToStandardModifiers();
      if (e.Down && !isModifier && !e.Capture && standardModifiers != Modifiers.None)
      {
        // A mouse event is handled separately.
        if (e.Input.IsMouseInput())
        {
          // Inject the key down events of the modifiers. These injections occur during the hook procedure of the mouse event, and will therefore arrive in time.
          Env.CreateInjector().Add(standardModifiers, true).Run();


          ModifiersToRelease |= standardModifiers;

          // Specify when to release the modifiers.
          TimeOfRelease = e.Input == Input.WheelDown || e.Input == Input.WheelUp ? ReleaseTime.AtNextEvent : ReleaseTime.AfterMouseUpEvent;
        }
        else
        {
          e.Capture = true;
          doNotAddToCaptured = true;
          // Inject the modifier down events, the key down event, and the modifier up events.
          Env.CreateInjector().Add(standardModifiers, true).Add(e.Input, true).Add(standardModifiers, false).Run();
        }
      }

      // For a non-modifier key down, or a modifier key down that is captured, the stuck modifiers are reset and the almost stuck modifiers are cleared.
      if (e.Down && (!isModifier || e.Capture))
      {
        DeactivateStuckModifiers();
        AlmostStuckModifiers = Modifiers.None;
      }

      // Update time of release.
      if (ModifiersToRelease != Modifiers.None && TimeOfRelease == ReleaseTime.AfterMouseUpEvent && e.Up && e.Input.IsMouseInput())
      {
        TimeOfRelease = ReleaseTime.AtNextEvent;
      }

      // A modifier key that is not captured affects the virtual modifier state.
      if (isModifier && !e.Capture)
      {
        var modifier = e.Input.ToModifier();
        if (e.Down)
        {
          // A modifier key down event has two cases:
          //   1) If this key was in the stuck state, it is no longer stuck, but instead it is again almost stuck.
          //   2) If this modifier was not yet active, then make it active and set it in the almost stuck state.
          if (StuckModifiers.HasFlag(modifier))
          {
            StuckModifiers &= ~modifier;
            AlmostStuckModifiers |= modifier;
          }
          else if (!Modifiers.HasFlag(modifier))
          {
            Modifiers |= modifier;
            AlmostStuckModifiers |= modifier;
          }
        }
        else
        {
          // For a modifier key up event, add the modifier to the stuck keys when it was almost stuck, but make it inactive otherwise.
          if (AlmostStuckModifiers.HasFlag(modifier))
          {
            StuckModifiers |= modifier;
            AlmostStuckModifiers &= ~modifier;
          }
          else
          {
            Modifiers &= ~modifier;
          }
        }
      }

      // Capture all modifiers and some non-modifier key up events.
      if (isModifier)
      {
        // Capture all modifiers. Only injected modifiers will get through.
        e.Capture = true;
      }
      else
      {
        // For a non-modifier key down event, add the key to the captured keys.
        if (e.Capture && e.Down && !doNotAddToCaptured)
        {
          Captured.Add(e.Input);
        }
        // For a non-modifier key up event for which the key down was captured, also capture the key up.
        else if (e.Up && Captured.Contains(e.Input))
        {
          e.Capture = true;
          Captured.Remove(e.Input);
        }
      }
    }

    private void DeactivateStuckModifiers()
    {
      Modifiers &= ~StuckModifiers;
      StuckModifiers = Modifiers.None;
    }

    public void Reset()
    {
      Modifiers = Modifiers.None;
      StuckModifiers = Modifiers.None;
      AlmostStuckModifiers = Modifiers.None;
      ModifiersToRelease = Modifiers.None;
      Captured.Clear();
      ResetStandardModifierKeys();
      TargetHook.Reset();
    }

    public string GetStateInfo()
    {
      var s = TargetHook.GetTestStateInfo();
      if ((Modifiers | StuckModifiers | AlmostStuckModifiers) != Modifiers.None || Captured.Count > 0)
      {
        s += nameof(InputHook) + Helper.GetBindingsSuffix(Modifiers, nameof(Modifiers), StuckModifiers, nameof(StuckModifiers), AlmostStuckModifiers, nameof(AlmostStuckModifiers), Captured.Count, nameof(Captured)) + '\n';
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

    private void Replace(ExecuteAtParseTimeData data, [AllowSpaces] LocatedString argument, bool surroundWithSpaces)
    {
      var chord = data.Chord;
      int backspaceCount, deleteCount;
      if (data.Section.IsMode)
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
        BackspaceAction = backspaceAction;
        replaceAction();
      }
      data.ParserOutput.AddHotkey(data.Section, chord, Action, description);
    }

    enum ReleaseTime
    {
      AfterMouseUpEvent, AtNextEvent
    }
  }
}

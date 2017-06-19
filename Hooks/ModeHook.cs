using System;
using System.Collections.Generic;
using System.Linq;

namespace InputMaster.Hooks
{
  internal class ModeHook : Actor, IComboHook
  {
    /// <summary>
    /// Collection of all available modes.
    /// </summary>
    private readonly Dictionary<string, Mode> Modes = new Dictionary<string, Mode>();
    private readonly ModeViewer ModeViewer;
    /// <summary>
    /// Currently active mode, or null when no mode is active.
    /// </summary>
    private Mode ActiveMode;
    /// <summary>
    /// The hotkeys that match the input buffer (for multi-key modes).
    /// </summary>
    private IEnumerable<ModeHotkey> ModeHotkeys;
    /// <summary>
    /// The number of inputs already in the buffer (for multi-key modes).
    /// </summary>
    private int InputCount;
    private bool Hidden;

    public ModeHook()
    {
      ModeViewer = new ModeViewer();
      Env.Parser.NewParserOutput += parserOutput =>
      {
        Modes.Clear();
        foreach (var mode in parserOutput.Modes)
        {
          Modes[mode.Name] = mode;
        }
      };
    }

    /// <summary>
    /// Returns whether a mode is active.
    /// </summary>
    public bool Active => ActiveMode != null;

    [Command]
    public void EnterMode(string name, [ValidFlags("h")]string flags = "")
    {
      EnterMode(name, Input.None);
      Hidden = flags.Contains('h');
    }

    [Command]
    public void EnterModeHot(HotkeyTrigger trigger, string name)
    {
      EnterMode(name, trigger.Combo.Input);
    }

    [Command(CommandTypes.ModeOnly)]
    public void LeaveMode()
    {
      ModeViewer.Hide();
      ClearActiveMode();
    }

    private void EnterMode(string name, Input input = Input.None)
    {
      if (Modes.TryGetValue(name, out var mode))
      {
        EnterMode(mode, input);
      }
      else
      {
        Env.Notifier.WriteError($"Mode '{name}' not found.");
      }
    }

    public void EnterMode(Mode mode, Input input = Input.None)
    {
      ClearActiveMode();
      ActiveMode = mode;
      ClearInput();
      if (input != Input.None)
      {
        if (!mode.IsComposeMode)
        {
          Handle(new ComboArgs(new Combo(input)));
        }
        else
        {
          Env.Notifier.WriteError($"Cannot use '{nameof(EnterModeHot)}' on a {Constants.ComposeModeSectionIdentifier} '{mode.Name}'.");
        }
      }
    }

    public void Reset()
    {
      LeaveMode();
    }

    public string GetTestStateInfo()
    {
      if (Active)
      {
        return nameof(ModeHook) + Helper.GetBindingsSuffix(Active, nameof(Active));
      }
      return "";
    }

    public void Handle(ComboArgs e)
    {
      if (ActiveMode.IsComposeMode)
      {
        HandleComposeMode(e);
      }
      else
      {
        foreach (var modeHotkey in ModeHotkeys)
        {
          if (modeHotkey.Chord.TestPosition(0, e.Combo))
          {
            e.Capture = true;
            try
            {
              modeHotkey.Action(e.Combo);
            }
            catch (Exception ex) when (!Helper.IsFatalException(ex))
            {
              Env.Notifier.WriteError(ex);
            }
            return;
          }
        }
        if (e.Combo == Env.Config.ShowModeCombo)
        {
          e.Capture = true;
          ToggleViewerVisibility();
        }
        else if (!e.Combo.Input.IsMouseInput())
        {
          e.Capture = true;
          LeaveMode();
        }
      }
    }

    private void ClearActiveMode()
    {
      if (ActiveMode != null)
      {
        ActiveMode = null;
        Hidden = false;
      }
    }

    private void ClearInput()
    {
      InputCount = 0;
      ModeHotkeys = ActiveMode.GetHotkeys();
    }

    private void HandleComposeMode(ComboArgs e)
    {
      if (e.Combo.Input.IsMouseInput())
      {
        return;
      }

      // Capture any non-modifier key. Modifier keys should not be captured as that would prevent the key from modifying the virtual modifier state.
      if (!e.Combo.Input.IsModifierKey())
      {
        e.Capture = true;
      }

      // Check for hit, update hotkeys and input count.
      ModeHotkey hit = null;
      var oldModeHotkeys = ModeHotkeys;
      var oldCount = InputCount;
      if (!e.Combo.Input.IsModifierKey())
      {
        var newModeHotkeys = new List<ModeHotkey>();
        foreach (var modeHotkey in ModeHotkeys)
        {
          if (modeHotkey.Chord.TestPosition(InputCount, e.Combo))
          {
            if (modeHotkey.Chord.Length - 1 == InputCount)
            {
              if (hit == null || modeHotkey.Chord.IsMoreSpecificThan(hit.Chord))
              {
                hit = modeHotkey;
              }
            }
            else
            {
              newModeHotkeys.Add(modeHotkey);
            }
          }
        }
        InputCount++;
        ModeHotkeys = newModeHotkeys;
      }
      else if (InputCount == 0)
      {
        hit = ModeHotkeys.FirstOrDefault(z => z.Chord.Length == 1 && z.Chord.TestPosition(0, e.Combo));
      }

      if (hit == null)
      {
        if (e.Combo == Env.Config.ClearModeCombo)
        {
          ClearInput();
        }
        else if (e.Combo == Env.Config.ShowModeCombo)
        {
          ModeHotkeys = oldModeHotkeys;
          InputCount = oldCount;
          ToggleViewerVisibility();
        }
        else if (Env.Config.ClearModeCombos.Contains(e.Combo))
        {
          LeaveMode();
        }
      }
      else
      {
        LeaveMode();
        try
        {
          hit.Action(e.Combo);
        }
        catch (Exception ex) when (!Helper.IsFatalException(ex))
        {
          Env.Notifier.WriteError(ex);
        }
      }
      ModeViewer.UpdateText(GetDisplayText());
    }

    private void ToggleViewerVisibility()
    {
      if (!Hidden)
      {
        ModeViewer.ToggleVisibility(GetDisplayText());
      }
    }

    private string GetDisplayText()
    {
      var modeHotkeys = new List<ModeHotkey>(ModeHotkeys).Where(z => z.Description != null && !z.Description.Contains(Constants.HiddenTag)).ToList();
      modeHotkeys.Sort((a, b) => string.CompareOrdinal(a.Description, b.Description));
      return string.Join("\n", modeHotkeys.Select(z => z.Description));
    }
  }
}

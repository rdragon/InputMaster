using System;
using System.Collections.Generic;
using System.Linq;

namespace InputMaster.Hooks
{
  class ModeHook : IComboHook
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

    public ModeHook(IParserOutputProvider parserOutputProvider)
    {
      Helper.ForbidNull(parserOutputProvider, nameof(parserOutputProvider));
      ModeViewer = new ModeViewer();

      parserOutputProvider.NewParserOutput += (parserOutput) =>
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

    public event Action LeavingMode = delegate { };

    [CommandTypes(CommandTypes.Visible)]
    public void EnterMode(string name, [ValidFlags("h")]string flags = "")
    {
      EnterMode(name, Input.None);
      Hidden = flags.Contains('h');
    }

    [CommandTypes(CommandTypes.Visible)]
    public void EnterModeHot(HotkeyTrigger trigger, string name)
    {
      EnterMode(name, trigger.Combo.Input);
    }

    [CommandTypes(CommandTypes.Visible | CommandTypes.ModeOnly)]
    public void LeaveMode()
    {
      ModeViewer.Hide();
      ClearActiveMode();
    }

    public void EnterMode(string name, Input input = Input.None)
    {
      Mode mode;
      if (Modes.TryGetValue(name, out mode))
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
          Handle(new ComboArgs(new Combo(input, Modifiers.None)));
        }
        else
        {
          Env.Notifier.WriteError($"Cannot use '{nameof(EnterModeHot)}' on a {Config.ComposeModeSectionIdentifier} '{mode.Name}'.");
        }
      }
    }

    public void Reset()
    {
      LeaveMode();
    }

    public void ClearModes()
    {
      Modes.Clear();
    }

    public string GetTestStateInfo()
    {
      if (Active)
      {
        return nameof(ModeHook) + Helper.GetBindingsSuffix(Active, nameof(Active));
      }
      else
      {
        return "";
      }
    }

    public void Handle(ComboArgs e)
    {
      if (e.Combo.Input.IsMouseInput())
      {
        return;
      }

      if (ActiveMode.IsComposeMode)
      {
        HandleComposeMode(e);
      }
      else
      {
        e.Capture = true;
        foreach (var modeHotkey in ModeHotkeys)
        {
          if (modeHotkey.Chord.TestPosition(0, e.Combo))
          {
            modeHotkey.Action(e.Combo);
            return;
          }
        }
        if (e.Combo == Config.ShowModeCombo)
        {
          ToggleViewerVisibility();
        }
        else if (Config.ClearModeCombos.Contains(e.Combo))
        {
          LeaveMode();
        }
      }
    }

    private void ClearActiveMode()
    {
      if (ActiveMode != null)
      {
        LeavingMode();
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
              if (hit == null || (modeHotkey.Chord.IsMoreSpecificThan(hit.Chord)))
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
        if (e.Combo == Config.ClearModeCombo)
        {
          ClearInput();
        }
        else if (e.Combo == Config.ShowModeCombo)
        {
          ModeHotkeys = oldModeHotkeys;
          InputCount = oldCount;
          ToggleViewerVisibility();
        }
        else if (Config.ClearModeCombos.Contains(e.Combo))
        {
          LeaveMode();
        }
      }
      else
      {
        LeaveMode();
        hit.Action(e.Combo);
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
      var modeHotkeys = new List<ModeHotkey>(ModeHotkeys).Where(z => z.Description != null && !z.Description.Contains("[hidden]")).ToList();
      modeHotkeys.Sort((a, b) => { return a.Description.CompareTo(b.Description); });
      return string.Join("\n", modeHotkeys.Select(z => z.Description));
    }
  }
}

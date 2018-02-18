using System;
using System.Collections.Generic;
using System.Linq;

namespace InputMaster.Hooks
{
  public class ModeHook : Actor, IComboHook
  {
    /// <summary>
    /// Returns whether a mode is active.
    /// </summary>
    public bool Active => _activeMode != null;
    /// <summary>
    /// Collection of all available modes.
    /// </summary>
    private readonly Dictionary<string, Mode> _modes = new Dictionary<string, Mode>();
    private readonly ModeViewer _modeViewer = new ModeViewer();
    /// <summary>
    /// Currently active mode, or null when no mode is active.
    /// </summary>
    private Mode _activeMode;
    /// <summary>
    /// The hotkeys that match the input buffer (for multi-key modes).
    /// </summary>
    private IEnumerable<ModeHotkey> _modeHotkeys;
    /// <summary>
    /// The number of inputs already in the buffer (for multi-key modes).
    /// </summary>
    private int _inputCount;
    private bool _hidden;

    public ModeHook()
    {
      Env.Parser.NewParserOutput += parserOutput =>
      {
        _modes.Clear();
        foreach (var mode in parserOutput.Modes)
          _modes[mode.Name] = mode;
      };
    }

    [Command]
    public void EnterMode(string name, [ValidFlags("h")]string flags = "")
    {
      EnterMode(name, Input.None);
      _hidden = flags.Contains('h');
    }

    [Command]
    public void EnterModeHot(HotkeyTrigger trigger, string name)
    {
      EnterMode(name, trigger.Combo.Input);
    }

    [Command(CommandTypes.ModeOnly)]
    public void LeaveMode()
    {
      _modeViewer.Hide();
      ClearActiveMode();
    }

    private void EnterMode(string name, Input input = Input.None)
    {
      if (_modes.TryGetValue(name, out var mode))
        EnterMode(mode, input);
      else
        Env.Notifier.Error($"Mode '{name}' not found.");
    }

    public void EnterMode(Mode mode, Input input = Input.None)
    {
      ClearActiveMode();
      _activeMode = mode;
      ClearInput();
      if (input != Input.None)
      {
        if (!mode.IsComposeMode)
          Handle(new ComboArgs(new Combo(input)));
        else
          Env.Notifier.Error($"Cannot use '{nameof(EnterModeHot)}' on a {Constants.ComposeModeSectionIdentifier} '{mode.Name}'.");
      }
    }

    public void Reset()
    {
      LeaveMode();
    }

    public string GetTestStateInfo()
    {
      if (Active)
        return nameof(ModeHook) + Helper.GetBindingsSuffix(Active, nameof(Active));
      return "";
    }

    public void Handle(ComboArgs e)
    {
      if (_activeMode.IsComposeMode)
        HandleComposeMode(e);
      else
      {
        foreach (var modeHotkey in _modeHotkeys)
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
      if (_activeMode != null)
      {
        _activeMode = null;
        _hidden = false;
      }
    }

    private void ClearInput()
    {
      _inputCount = 0;
      _modeHotkeys = _activeMode.GetHotkeys();
    }

    private void HandleComposeMode(ComboArgs e)
    {
      if (e.Combo.Input.IsMouseInput())
        return;

      // Capture any non-modifier key. Modifier keys should not be captured as that would prevent the key from modifying the virtual
      // modifier state.
      if (!e.Combo.Input.IsModifierKey())
        e.Capture = true;

      // Check for hit, update hotkeys and input count.
      ModeHotkey hit = null;
      var oldModeHotkeys = _modeHotkeys;
      var oldCount = _inputCount;
      if (!e.Combo.Input.IsModifierKey())
      {
        var newModeHotkeys = new List<ModeHotkey>();
        foreach (var modeHotkey in _modeHotkeys)
        {
          if (modeHotkey.Chord.TestPosition(_inputCount, e.Combo))
          {
            if (modeHotkey.Chord.Length - 1 == _inputCount)
            {
              if (hit == null || modeHotkey.Chord.IsMoreSpecificThan(hit.Chord))
                hit = modeHotkey;
            }
            else
              newModeHotkeys.Add(modeHotkey);
          }
        }
        _inputCount++;
        _modeHotkeys = newModeHotkeys;
      }
      else if (_inputCount == 0)
        hit = _modeHotkeys.FirstOrDefault(z => z.Chord.Length == 1 && z.Chord.TestPosition(0, e.Combo));

      if (hit == null)
      {
        if (e.Combo == Env.Config.ClearModeCombo)
          ClearInput();
        else if (e.Combo == Env.Config.ShowModeCombo)
        {
          _modeHotkeys = oldModeHotkeys;
          _inputCount = oldCount;
          ToggleViewerVisibility();
        }
        else if (e.Combo == Env.Config.PrintModeCombo)
        {
          _modeHotkeys = oldModeHotkeys;
          _inputCount = oldCount;
          Helper.ShowSelectableText(GetDisplayText());
          LeaveMode();
        }
        else if (Env.Config.ClearModeCombos.Contains(e.Combo))
          LeaveMode();
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
      _modeViewer.UpdateText(GetDisplayText());
    }

    private void ToggleViewerVisibility()
    {
      if (!_hidden)
        _modeViewer.ToggleVisibility(GetDisplayText());
    }

    private string GetDisplayText()
    {
      var modeHotkeys = new List<ModeHotkey>(_modeHotkeys).Where(z => z.Description != null &&
        !z.Description.Contains(Env.Config.HiddenTag)).ToList();
      modeHotkeys.Sort((a, b) => string.CompareOrdinal(a.Description, b.Description));
      return string.Join("\n", modeHotkeys.Select(z => z.Description));
    }
  }
}

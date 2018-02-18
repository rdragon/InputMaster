using System;

namespace InputMaster.Hooks
{
  public class ComboHook : IComboHook
  {
    public bool Active => _hotkeyCollection != null;
    private readonly Combo[] _buffer = new Combo[Env.Config.MaxChordLength];
    private readonly Chord _chord = new Chord(Env.Config.MaxChordLength);
    private int _length;
    private HotkeyCollection _hotkeyCollection = new HotkeyCollection();

    public ComboHook()
    {
      Env.Parser.NewParserOutput += parserOutput =>
      {
        _hotkeyCollection = parserOutput.HotkeyCollection;
      };
    }

    public void Reset()
    {
      _length = 0;
    }

    public string GetTestStateInfo()
    {
      // Do not write any state information, as it is okay if there are some combos left after a test.
      return "";
    }

    public void Handle(ComboArgs e)
    {
      Env.ForegroundListener.Update();
      Action<Combo> action = null;
      _chord.Clear();
      var combo = e.Combo;
      if (!combo.Input.IsModifierKey())
      {
        _buffer[_length++ % _buffer.Length] = combo;
        var i = _length;
        var k = Math.Min(_hotkeyCollection.MaxChordLength, _length);
        for (var j = 0; j < k; j++)
        {
          _chord.InsertAtStart(_buffer[--i % _buffer.Length]);
          if (_hotkeyCollection.TryGetAction(_chord, out var action1))
            action = action1;
        }
      }
      else
      {
        _chord.InsertAtStart(combo);
        if (_hotkeyCollection.TryGetAction(_chord, out var action1))
          action = action1;
      }
      if (action != null)
      {
        Reset();
        try
        {
          action(e.Combo);
        }
        catch (Exception ex) when (!Helper.IsFatalException(ex))
        {
          Env.Notifier.WriteError(ex);
        }
        e.Capture = true;
      }
      else if (Env.Config.InsertSpaceAfterComma && e.Combo == new Combo(Input.Comma))
      {
        e.Capture = true;
        Env.CreateInjector().Add(Input.Comma).Add(Input.Space).Run();
      }
    }
  }
}

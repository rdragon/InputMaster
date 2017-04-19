using System;

namespace InputMaster.Hooks
{
  class ComboHook : IComboHook
  {
    private readonly Combo[] Buffer = new Combo[Config.MaxChordLength];
    private readonly Chord Chord = new Chord(Config.MaxChordLength);
    private int Length;
    private HotkeyCollection HotkeyCollection = new HotkeyCollection();

    public bool Active => HotkeyCollection != null;

    public ComboHook(IParserOutputProvider parserOutputProvider)
    {
      Helper.ForbidNull(parserOutputProvider, nameof(parserOutputProvider)).NewParserOutput += (parserOutput) =>
      {
        HotkeyCollection = parserOutput.HotkeyCollection;
      };
    }

    public void Reset()
    {
      Length = 0;
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
      Chord.Clear();
      var combo = e.Combo;
      if (!combo.Input.IsModifierKey())
      {
        Buffer[(Length++) % Buffer.Length] = combo;
        var i = Length;
        var k = Math.Min(HotkeyCollection.MaxChordLength, Length);
        for (int j = 0; j < k; j++)
        {
          Chord.InsertAtStart(Buffer[(--i) % Buffer.Length]);
          action = HotkeyCollection.TryGetAction(Chord) ?? action;
        }
      }
      else
      {
        Chord.InsertAtStart(combo);
        action = HotkeyCollection.TryGetAction(Chord);
      }
      if (action != null)
      {
        Reset();
        try
        {
          action(e.Combo);
        }
        catch (Exception ex) when (!Helper.IsCriticalException(ex))
        {
          Env.Notifier.WriteError(ex);
        }
        e.Capture = true;
      }
      else if (Config.InsertSpaceAfterComma && e.Combo == new Combo(Input.Comma))
      {
        e.Capture = true;
        Env.CreateInjector().Add(Input.Comma).Add(Input.Space).Run();
      }
    }
  }
}

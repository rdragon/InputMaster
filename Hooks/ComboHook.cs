using System;

namespace InputMaster.Hooks
{
  public class ComboHook : IComboHook
  {
    private readonly Combo[] Buffer = new Combo[Env.Config.MaxChordLength];
    private readonly Chord Chord = new Chord(Env.Config.MaxChordLength);
    private int Length;
    private HotkeyCollection HotkeyCollection = new HotkeyCollection();

    public bool Active => HotkeyCollection != null;

    public ComboHook()
    {
      Env.Parser.NewParserOutput += parserOutput =>
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
        Buffer[Length++ % Buffer.Length] = combo;
        var i = Length;
        var k = Math.Min(HotkeyCollection.MaxChordLength, Length);
        for (var j = 0; j < k; j++)
        {
          Chord.InsertAtStart(Buffer[--i % Buffer.Length]);
          if (HotkeyCollection.TryGetAction(Chord, out var action1))
            action = action1;
        }
      }
      else
      {
        Chord.InsertAtStart(combo);
        if (HotkeyCollection.TryGetAction(Chord, out var action1))
        {
          action = action1;
        }
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

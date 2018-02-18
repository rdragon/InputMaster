using System.Text;
using InputMaster;

namespace UnitTests
{
  public class TestOutputHandler : IInputHook
  {
    private readonly StringBuilder _output = new StringBuilder();
    private Modifiers _modifiers;

    public void Handle(InputArgs e)
    {
      if (e.Input.IsModifierKey())
      {
        if (e.Down)
          _modifiers |= e.Input.ToModifier();
        else
          _modifiers &= ~e.Input.ToModifier();
      }
      else if (e.Down)
        _output.Append(new Combo(e.Input, _modifiers));
    }

    public void Reset()
    {
      _output.Clear();
    }

    public string GetStateInfo()
    {
      return _output.ToString();
    }
  }
}

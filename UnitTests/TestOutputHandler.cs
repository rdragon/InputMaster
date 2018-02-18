using System.Text;
using InputMaster;

namespace UnitTests
{
  public class TestOutputHandler : IInputHook
  {
    private Modifiers Modifiers;

    private readonly StringBuilder Output = new StringBuilder();

    public void Handle(InputArgs e)
    {
      if (e.Input.IsModifierKey())
      {
        if (e.Down)
        {
          Modifiers |= e.Input.ToModifier();
        }
        else
        {
          Modifiers &= ~e.Input.ToModifier();
        }
      }
      else if (e.Down)
      {
        Output.Append(new Combo(e.Input, Modifiers));
      }
    }

    public void Reset()
    {
      Output.Clear();
    }

    public string GetStateInfo()
    {
      return Output.ToString();
    }
  }
}

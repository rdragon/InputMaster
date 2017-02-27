using InputMaster;
using System.Text;

namespace UnitTests
{
  class OutputHandler
  {
    private readonly StringBuilder Output;
    private Modifiers Modifiers;

    public OutputHandler(StringBuilder output)
    {
      Output = output;
    }

    public void Handle(Input input, bool down)
    {
      if (input.IsModifierKey())
      {
        if (down)
        {
          Modifiers |= input.ToModifier();
        }
        else
        {
          Modifiers &= ~input.ToModifier();
        }
      }
      else if (down)
      {
        Output.Append(new Combo(input, Modifiers).ToString());
      }
    }
  }
}

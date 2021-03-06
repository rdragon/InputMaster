﻿namespace InputMaster.Hooks
{
  public class InputCounter : IInjectorStream<InputCounter>
  {
    public int LeftCount { get; private set; }
    public int RightCount { get; private set; }
    private readonly bool _isInjectedInput;

    public InputCounter(bool isInjectedInput)
    {
      _isInjectedInput = isInjectedInput;
    }

    public InputCounter Add(char c)
    {
      LeftCount++;
      return this;
    }

    public InputCounter Add(Input input, bool down)
    {
      if (!down)
        return this;
      if (input == Input.Left)
      {
        LeftCount--;
        RightCount++;
      }
      else if (input == Input.Right)
      {
        LeftCount++;
        RightCount--;
      }
      else if (input == Input.Bs)
        LeftCount--;
      else if (input == Input.Del)
        RightCount--;
      else if (Env.Config.InsertSpaceAfterComma && input == Input.Comma && !_isInjectedInput)
        LeftCount += 2;
      else if (input.IsCharacterKey() || input == Input.Space)
        LeftCount++;
      return this;
    }
  }
}

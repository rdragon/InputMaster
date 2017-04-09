namespace InputMaster
{
  class InputCounter : IInjectorStream<InputCounter>
  {
    private bool IsInjectedInput;

    public InputCounter(bool isInjectedInput)
    {
      IsInjectedInput = isInjectedInput;
    }

    public int LeftCount { get; private set; }
    public int RightCount { get; private set; }

    public InputCounter Add(char c)
    {
      LeftCount++;
      return this;
    }

    public InputCounter Add(Input input, bool down)
    {
      if (down)
      {
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
        {
          LeftCount--;
        }
        else if (input == Input.Del)
        {
          RightCount--;
        }
        else if (Config.InsertSpaceAfterComma && input == Input.Comma && !IsInjectedInput)
        {
          LeftCount += 2;
        }
        else if (input.IsCharacterKey())
        {
          LeftCount++;
        }
      }
      return this;
    }
  }
}

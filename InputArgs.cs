namespace InputMaster
{
  internal class InputArgs
  {
    public InputArgs(Input input, bool down)
    {
      Input = input;
      Down = down;
    }

    public Input Input { get; }
    public bool Down { get; }
    public bool Capture { get; set; }
    public bool Up => !Down;

    public override string ToString()
    {
      return Input.ToTokenString() + " (" + (Down ? "down" : "up") + ")";
    }
  }
}

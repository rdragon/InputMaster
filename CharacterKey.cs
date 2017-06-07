namespace InputMaster
{
  internal struct CharacterKey
  {
    public Input Input { get; }
    public char Char { get; }
    public char ShiftedChar { get; }

    public CharacterKey(Input input, char chr, char shiftedChar)
    {
      Input = input;
      Char = chr;
      ShiftedChar = shiftedChar;
    }
  }
}

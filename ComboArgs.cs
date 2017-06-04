namespace InputMaster
{
  internal class ComboArgs
  {
    public ComboArgs(Combo combo)
    {
      Combo = combo;
    }

    public Combo Combo { get; }
    public bool Capture { get; set; }

    public override string ToString()
    {
      return Combo.ToString();
    }
  }
}

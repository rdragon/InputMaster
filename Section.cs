namespace InputMaster
{
  abstract class Section
  {
    public Section()
    {
      Column = -1;
    }

    public int Column { get; set; }

    public bool IsStandardSection => this is StandardSection;
    public bool IsMode => this is Mode;
    public StandardSection AsStandardSection => this as StandardSection;
    public Mode AsMode => this as Mode;
  }
}

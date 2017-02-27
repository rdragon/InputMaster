namespace InputMaster
{
  abstract class Section
  {
    public Section()
    {
      Column = -1;
    }

    public int Column { get; set; }

    public bool IsStandardSection { get { return this is StandardSection; } }
    public bool IsMode { get { return this is Mode; } }
    public StandardSection AsStandardSection { get { return this as StandardSection; } }
    public Mode AsMode { get { return this as Mode; } }
  }
}

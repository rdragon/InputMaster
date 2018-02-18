namespace InputMaster.Forms
{
  public class RtbPosition
  {
    public int SelectionStart { get; set; }
    public int ScrollPosition { get; set; }

    public RtbPosition(int selectionStart, int scrollPosition)
    {
      SelectionStart = selectionStart;
      ScrollPosition = scrollPosition;
    }
  }
}

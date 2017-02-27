using System.IO;

namespace InputMaster.Forms
{
  class RtbPosition
  {
    public int SelectionStart { get; }
    public int ScrollPosition { get; }

    public RtbPosition(int selectionStart, int scrollPosition)
    {
      SelectionStart = selectionStart;
      ScrollPosition = scrollPosition;
    }

    public RtbPosition(BinaryReader reader)
    {
      SelectionStart = reader.ReadInt32();
      ScrollPosition = reader.ReadInt32();
    }

    public void Write(BinaryWriter writer)
    {
      writer.Write(SelectionStart);
      writer.Write(ScrollPosition);
    }

    public bool Equals(RtbPosition other)
    {
      if (other == null)
      {
        return false;
      }
      return SelectionStart == other.SelectionStart && ScrollPosition == other.ScrollPosition;
    }
  }
}

using System.IO;

namespace InputMaster.Forms
{
  internal struct RtbPosition
  {
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

    public int SelectionStart { get; }
    public int ScrollPosition { get; }

    public void Write(BinaryWriter writer)
    {
      writer.Write(SelectionStart);
      writer.Write(ScrollPosition);
    }

    public static bool operator ==(RtbPosition pos1, RtbPosition pos2)
    {
      return pos1.ScrollPosition == pos2.ScrollPosition && pos1.SelectionStart == pos2.SelectionStart;
    }

    public static bool operator !=(RtbPosition pos1, RtbPosition pos2)
    {
      return !(pos1 == pos2);
    }

    public override bool Equals(object obj)
    {
      return obj != null && obj.GetType() == typeof(RtbPosition) && ((RtbPosition)obj) == this;
    }

    public override int GetHashCode()
    {
      return SelectionStart + 1000000007 * ScrollPosition;
    }
  }
}

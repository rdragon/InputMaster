using System;

namespace InputMaster.Parsers
{
  internal struct Location : IEquatable<Location>
  {
    public static readonly Location Unknown = new Location();

    private readonly int Line;

    public Location(int line, int column)
    {
      Line = line >= 1 ? line : throw new ArgumentOutOfRangeException(nameof(line));
      Column = column >= 1 ? column : throw new ArgumentOutOfRangeException(nameof(column));
    }

    public int Column { get; }
    public Location NextColumn => HasLocation ? new Location(Line, Column + 1) : this;
    public Location NextLine => HasLocation ? new Location(Line + 1, 1) : this;
    private bool HasLocation => Line > 0;

    public static bool operator ==(Location a, Location b)
    {
      return a.Equals(b);
    }

    public static bool operator !=(Location a, Location b)
    {
      return !a.Equals(b);
    }

    public Location AddColumns(int count)
    {
      if (count < 0)
      {
        throw new ArgumentOutOfRangeException(nameof(count));
      }
      return HasLocation ? new Location(Line, Column + count) : this;
    }

    public override string ToString()
    {
      return HasLocation ? $"line {Line}, column {Column}" : "(unknown location)";
    }

    public override int GetHashCode()
    {
      return Line * 1000000007 + Column;
    }

    public override bool Equals(object obj)
    {
      return obj is Location && Equals((Location)obj);
    }

    public bool Equals(Location other)
    {
      return Line == other.Line && Column == other.Column;
    }
  }
}

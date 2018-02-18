using System;

namespace InputMaster.Parsers
{
  public struct Location : IEquatable<Location>
  {
    public static readonly Location Unknown = new Location();
    private readonly int _line;

    public Location(int line, int column)
    {
      _line = line >= 1 ? line : throw new ArgumentOutOfRangeException(nameof(line));
      Column = column >= 1 ? column : throw new ArgumentOutOfRangeException(nameof(column));
    }

    public int Column { get; }
    public Location NextColumn => HasLocation ? new Location(_line, Column + 1) : this;
    public Location NextLine => HasLocation ? new Location(_line + 1, 1) : this;
    private bool HasLocation => _line > 0;

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
        throw new ArgumentOutOfRangeException(nameof(count));
      return HasLocation ? new Location(_line, Column + count) : this;
    }

    public override string ToString()
    {
      return HasLocation ? $"line {_line}, column {Column}" : "(unknown location)";
    }

    public override int GetHashCode()
    {
      return _line * 1000000007 + Column;
    }

    public override bool Equals(object obj)
    {
      return obj is Location && Equals((Location)obj);
    }

    public bool Equals(Location other)
    {
      return _line == other._line && Column == other.Column;
    }
  }
}

using System;

namespace InputMaster.Parsers
{
  internal struct Location : IEquatable<Location>
  {
    public static readonly Location Unknown = new Location();

    private readonly int LineField;
    private readonly int ColumnField;

    public Location(int line)
    {
      if (line < 1)
      {
        throw new ArgumentOutOfRangeException(nameof(line));
      }
      LineField = line;
      ColumnField = 0;
    }

    public Location(int line, int column)
    {
      if (line < 1)
      {
        throw new ArgumentOutOfRangeException(nameof(line));
      }
      if (column < 1)
      {
        throw new ArgumentOutOfRangeException(nameof(column));
      }
      LineField = line;
      ColumnField = column;
    }

    public bool HasLine => LineField > 0;

    public bool HasColumn => HasLine && ColumnField > 0;

    public int Line
    {
      get
      {
        if (HasLine)
        {
          return LineField;
        }
        throw new InvalidOperationException();
      }
    }

    public int Column
    {
      get
      {
        if (HasColumn)
        {
          return ColumnField;
        }
        throw new InvalidOperationException();
      }
    }

    public Location NextColumn => HasColumn ? new Location(LineField, ColumnField + 1) : this;

    public Location NextLine => HasLine ? new Location(LineField + 1, 1) : this;

    public Location WithoutColumnInfo => HasColumn ? new Location(LineField) : this;

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
      return HasColumn ? new Location(LineField, ColumnField + count) : this;
    }

    public override string ToString()
    {
      if (HasColumn)
      {
        return $"line {Line}, column {Column}";
      }
      return HasLine ? $"line {Line}" : "(no position information)";
    }

    public override int GetHashCode()
    {
      return LineField * 1000000007 + ColumnField;
    }

    public override bool Equals(object obj)
    {
      return obj is Location && Equals((Location)obj);
    }

    public bool Equals(Location other)
    {
      return LineField == other.LineField && ColumnField == other.ColumnField;
    }
  }
}

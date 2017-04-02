using System;

namespace InputMaster.Parsers
{
  struct Location : IEquatable<Location>
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
      else
      {
        LineField = line;
        ColumnField = 0;
      }
    }

    public Location(int line, int column)
    {
      if (line < 1)
      {
        throw new ArgumentOutOfRangeException(nameof(line));
      }
      else if (column < 1)
      {
        throw new ArgumentOutOfRangeException(nameof(column));
      }
      else
      {
        LineField = line;
        ColumnField = column;
      }
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
        else
        {
          throw new InvalidOperationException();
        }
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
        else
        {
          throw new InvalidOperationException();
        }
      }
    }

    public Location NextColumn
    {
      get
      {
        if (HasColumn)
        {
          return new Location(LineField, ColumnField + 1);
        }
        else
        {
          return this;
        }
      }
    }

    public Location NextLine
    {
      get
      {
        if (HasLine)
        {
          return new Location(LineField + 1, 1);
        }
        else
        {
          return this;
        }
      }
    }

    public Location WithoutColumnInfo
    {
      get
      {
        if (HasColumn)
        {
          return new Location(LineField);
        }
        else
        {
          return this;
        }
      }
    }

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
      else if (HasColumn)
      {
        return new Location(LineField, ColumnField + count);
      }
      else
      {
        return this;
      }
    }

    public override string ToString()
    {
      if (HasColumn)
      {
        return $"line {Line}, column {Column}";
      }
      else if (HasLine)
      {
        return $"line {Line}";
      }
      else
      {
        return "(no position information)";
      }
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

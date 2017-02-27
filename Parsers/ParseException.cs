using System;

namespace InputMaster.Parsers
{
  public class ParseException : Exception
  {
    public bool HasLocation { get; }

    public ParseException() { }

    public ParseException(string message, Exception innerException = null) : base(message, innerException) { }

    internal ParseException(Location location, Exception innerException = null) : base(location.ToString(), innerException)
    {
      HasLocation = true;
    }

    internal ParseException(LocatedString locatedString, Exception innerException = null) : base(locatedString.ToString(), innerException)
    {
      HasLocation = true;
    }

    internal ParseException(Location location, string message, Exception innerException = null) : base(location + ": " + message, innerException)
    {
      HasLocation = true;
    }

    internal ParseException(LocatedString locatedString, string message, Exception innerException = null) : base(locatedString + ": " + message, innerException)
    {
      HasLocation = true;
    }
  }
}

using System;

namespace InputMaster.Parsers
{
  internal class ParseException : Exception
  {
    public bool HasLocation { get; }

    public ParseException(string message, Exception innerException = null) : base(message, innerException) { }

    public ParseException(Location location, Exception innerException = null) : base(location.ToString(), innerException)
    {
      HasLocation = true;
    }

    public ParseException(LocatedString locatedString, Exception innerException = null) : base(locatedString.ToString(), innerException)
    {
      HasLocation = true;
    }

    public ParseException(Location location, string message, Exception innerException = null) : base(location + ": " + message, innerException)
    {
      HasLocation = true;
    }

    public ParseException(LocatedString locatedString, string message, Exception innerException = null) : base(locatedString + ": " + message, innerException)
    {
      HasLocation = true;
    }
  }
}

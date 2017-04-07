using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace InputMaster.Parsers
{
  /// <summary>
  /// Functionality for reading a string one character at a time. Keeps track of position in the string.
  /// </summary>
  abstract class CharReader
  {
    private readonly string Text;
    private readonly Location StartLocation;
    private int Index;

    public CharReader(LocatedString locatedString)
    {
      var text = Helper.ForbidNull(locatedString.Value, nameof(locatedString) + "." + nameof(locatedString.Value));
      if (text.Contains('\t'))
      {
        throw new ParseException($"Tab character(s) found in input. Please use spaces only.");
      }
      Debug.Assert(!text.Contains("\r\n"));
      Text = text;
      StartLocation = locatedString.Location;
      Location = StartLocation;
    }

    protected bool EndOfStream => Index == Text.Length;
    protected bool EndOfLine => Current == '\n';
    protected char Current
    {
      get
      {
        if (EndOfStream)
        {
          return '\n';
        }
        else
        {
          return Text[Index];
        }
      }
    }
    protected Location Location { get; private set; }

    private static bool IsIdentifierCharacter(char c)
    {
      return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';
    }

    protected void Reset()
    {
      Index = 0;
      Location = StartLocation;
    }

    protected char Read()
    {
      var c = Current;
      if (!EndOfStream)
      {
        if (EndOfLine)
        {
          Location = Location.NextLine;
        }
        else
        {
          Location = Location.NextColumn;
        }
        Index++;
      }
      return c;
    }

    protected void Read(char c)
    {
      Require(c);
      Read();
    }

    protected void ReadMany(char c)
    {
      while (!EndOfStream && Current == c)
      {
        Read();
      }
    }

    protected void ReadSome(char c)
    {
      Require(c);
      ReadMany(c);
    }

    protected LocatedString ReadWhile(Func<char, bool> func)
    {
      Helper.ForbidNull(func, nameof(func));
      var location = Location;
      var startIndex = Index;
      while (!EndOfStream && func(Current))
      {
        Read();
      }
      return new LocatedString(Text.Substring(startIndex, Index - startIndex), location);
    }

    protected LocatedString ReadUntil(params char[] chars)
    {
      return ReadWhile(c => !chars.Contains(c));
    }

    protected void Read(string text)
    {
      foreach (var c in Helper.ForbidNull(text, nameof(text)))
      {
        Read(c);
      }
    }

    protected bool TryRead(string text)
    {
      if (At(text))
      {
        Read(text);
        return true;
      }
      else
      {
        return false;
      }
    }

    protected bool At(string text)
    {
      Helper.ForbidNull(text, nameof(text));
      return (!EndOfStream || text.Length == 0) && Text.Substring(Index).StartsWith(text);
    }

    protected bool At(Regex regex)
    {
      return Helper.ForbidNull(regex, nameof(regex)).IsMatch(Text.Substring(Index));
    }

    protected void ForbidEndOfStream()
    {
      if (EndOfStream)
      {
        throw CreateException("Unexpected end of stream.");
      }
    }

    protected ParseException CreateException(string message)
    {
      return new ParseException(Location, message);
    }

    protected void Require(char c)
    {
      if (c != Current)
      {
        if (EndOfStream)
        {
          throw CreateException($"Unexpected end of stream, expecting '{c}'.");
        }
        else if (EndOfLine)
        {
          throw CreateException($"Unexpected end of line, expecting '{c}'.");
        }
        else
        {
          throw CreateException($"Unexpected character '{Current}', expecting '{c}'.");
        }
      }
    }

    protected void Forbid(char c)
    {
      if (Current == c)
      {
        throw CreateException($"Unexpected character '{c}'.");
      }
    }

    protected LocatedString ReadToken(out string text)
    {
      var location = Location;
      Read(Config.TokenStart);
      text = ReadUntil(Config.TokenEnd).Value;
      Read(Config.TokenEnd);
      return new LocatedString(Helper.CreateTokenString(text), location);
    }

    protected LocatedString ReadUnicodeToken(out string text)
    {
      var location = Location;
      var s = "UNICODE[";
      Read(s);
      text = ReadUntil(']').Value;
      Read(']');
      return new LocatedString(s + text + ']', location);
    }

    protected LocatedString ReadIdentifier()
    {
      return ReadIdentifier(true);
    }

    protected LocatedString TryReadIdentifier()
    {
      return ReadIdentifier(false);
    }

    private LocatedString ReadIdentifier(bool throwException)
    {
      if (throwException)
      {
        ForbidEndOfStream();
      }
      else if (EndOfStream)
      {
        return LocatedString.None;
      }
      if (Current < 'A' || Current > 'Z')
      {
        if (throwException)
        {
          throw CreateException($"Unexpected character '{Current}'. An identifier should start with an upper case letter.");
        }
        else
        {
          return LocatedString.None;
        }
      }
      else
      {
        return ReadWhile(IsIdentifierCharacter);
      }
    }
  }
}

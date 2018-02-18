﻿using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace InputMaster.Parsers
{
  /// <summary>
  /// Functionality for reading a string one character at a time. Keeps track of position in the string.
  /// </summary>
  public abstract class CharReader
  {
    private readonly string Text;
    private int Index;

    protected CharReader(LocatedString locatedString)
    {
      var text = locatedString.Value;
      if (text.Contains('\t'))
      {
        throw new ParseException("Tab character(s) found in input. Please use spaces only.");
      }
      Helper.ForbidCarriageReturn(ref text);
      Text = text;
      Location = locatedString.Location;
    }

    protected bool EndOfStream => Index == Text.Length;
    protected bool EndOfLine => Current == '\n';
    protected char Current => EndOfStream ? '\n' : Text[Index];
    protected Location Location { get; private set; }

    protected char Read()
    {
      if (EndOfStream)
      {
        return Current;
      }
      Location = EndOfLine ? Location.NextLine : Location.NextColumn;
      return Text[Index++];
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

    protected LocatedString ReadUntil(params char[] chars)
    {
      return ReadWhile(c => !chars.Contains(c));
    }

    protected void Read(string text)
    {
      foreach (var c in text)
      {
        Read(c);
      }
    }

    protected bool TryRead(string text)
    {
      if (!At(text))
      {
        return false;
      }
      Read(text);
      return true;
    }

    protected bool At(Regex regex)
    {
      return regex.IsMatch(Text.Substring(Index));
    }

    protected void ForbidEndOfStream()
    {
      if (EndOfStream)
      {
        throw CreateException("Unexpected end of stream.");
      }
    }

    protected void Require(char c)
    {
      if (c == Current)
      {
        return;
      }
      if (EndOfStream)
      {
        throw CreateException($"Unexpected end of stream, expecting '{c}'.");
      }
      if (EndOfLine)
      {
        throw CreateException($"Unexpected end of line, expecting '{c}'.");
      }
      throw CreateException($"Unexpected character '{Current}', expecting '{c}'.");
    }

    protected LocatedString ReadToken(out string text)
    {
      var location = Location;
      Read(Constants.TokenStart);
      text = ReadUntil(Constants.TokenEnd).Value;
      Read(Constants.TokenEnd);
      return new LocatedString(Helper.CreateTokenString(text), location);
    }

    protected LocatedString ReadIdentifier()
    {
      ForbidEndOfStream();
      if (Current < 'A' || Current > 'Z')
      {
        throw CreateException($"Unexpected character '{Current}'. An identifier should start with an upper case letter.");
      }
      return ReadWhile(Constants.IsIdentifierCharacter);
    }

    private ParseException CreateException(string message)
    {
      return new ParseException(Location, message);
    }

    private bool At(string text)
    {
      return (!EndOfStream || text.Length == 0) && Text.Substring(Index).StartsWith(text);
    }

    private LocatedString ReadWhile(Func<char, bool> func)
    {
      var location = Location;
      var startIndex = Index;
      while (!EndOfStream && func(Current))
      {
        Read();
      }
      return new LocatedString(Text.Substring(startIndex, Index - startIndex), location);
    }
  }
}

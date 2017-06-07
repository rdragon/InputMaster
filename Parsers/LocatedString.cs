using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace InputMaster.Parsers
{
  internal struct LocatedString : IEquatable<LocatedString>
  {
    public static readonly LocatedString None = new LocatedString();
    public static readonly LocatedString Empty = new LocatedString("");
    private static readonly string DelimiterPattern = $" *{Regex.Escape(ParserConfig.ArgumentDelimiter)} *";
    private static readonly Regex DelimiterRegex = new Regex($"{DelimiterPattern}| +");
    private static readonly Regex DelimiterRegexAllowSpace = new Regex(DelimiterPattern);

    public LocatedString(string text) : this(text, Location.Unknown) { }

    public LocatedString(string text, Location location)
    {
      Helper.ForbidNull(text, nameof(text));
      Value = text;
      Location = location;
    }

    public string Value { get; }
    public Location Location { get; }
    public int Length => Value.Length;

    public static bool operator ==(LocatedString a, LocatedString b)
    {
      return a.Equals(b);
    }

    public static bool operator !=(LocatedString a, LocatedString b)
    {
      return !a.Equals(b);
    }

    public LocatedString TrimStart()
    {
      var s = Value.TrimStart();
      return new LocatedString(s, Location.AddColumns(Value.Length - s.Length));
    }

    public LocatedString TrimEnd()
    {
      return new LocatedString(Value.TrimEnd(), Location);
    }

    public LocatedString Trim()
    {
      return TrimStart().TrimEnd();
    }

    public LocatedString Require(string delimiter, int targetCount = -1)
    {
      Helper.ForbidNullOrEmpty(delimiter, nameof(delimiter));
      var count = Split(delimiter).Length;
      if (targetCount != -1 && count != targetCount)
      {
        throw GetException(targetCount);
      }
      return this;
    }

    private Exception GetException(int targetCount = -1, int minCount = -1, int maxCount = -1)
    {
      if (targetCount != -1)
      {
        throw CreateException($"Wrong number of arguments given, expecting {targetCount} arguments.");
      }
      if (minCount != -1)
      {
        throw CreateException($"Not enough arguments given, expecting at least {minCount}.");
      }
      if (maxCount != -1)
      {
        throw CreateException($"Too many arguments given, expecting at most {maxCount}.");
      }
      throw new ArgumentException();
    }

    public LocatedString[] Split(string s)
    {
      Helper.ForbidNullOrEmpty(s, nameof(s));
      var parts = Value.Length == 0 ? new string[0] : Value.Split(new[] { s }, StringSplitOptions.None);
      var column = 0;
      var ar = new LocatedString[parts.Length];
      for (var i = 0; i < parts.Length; i++)
      {
        ar[i] = new LocatedString(parts[i], Location.AddColumns(column)).Trim();
        column += parts[i].Length + s.Length;
      }
      return ar;
    }

    public LocatedString Substring(int startIndex)
    {
      Helper.RequireInInterval(startIndex, nameof(startIndex), 0, Value.Length);
      return new LocatedString(Value.Substring(startIndex), Location.AddColumns(startIndex));
    }

    public LocatedString Substring(int startIndex, int length)
    {
      Helper.RequireAtLeast(startIndex, nameof(startIndex), 0);
      Helper.RequireAtLeast(length, nameof(length), 0);
      Helper.RequireAtMost(startIndex + length, nameof(startIndex) + "+" + nameof(length), Value.Length);
      return new LocatedString(Value.Substring(startIndex, length), Location.AddColumns(startIndex));
    }

    public LocatedString Replace(string str, string replacement)
    {
      Helper.ForbidNullOrEmpty(str, nameof(str));
      Helper.ForbidNull(replacement, nameof(replacement));
      var newValue = Value.Replace(str, replacement);
      return new LocatedString(newValue, newValue.Length != Value.Length ? Location.WithoutColumnInfo : Location);
    }

    public override string ToString()
    {
      return Location == Location.Unknown ? $"\"{Value}\"" : $"\"{Value}\", {Location}";
    }

    public override bool Equals(object obj)
    {
      return obj is LocatedString && Equals((LocatedString)obj);
    }

    public bool Equals(LocatedString other)
    {
      return Value == other.Value && Location == other.Location;
    }

    public override int GetHashCode()
    {
      return Value.GetHashCode() * 1000000007 + Location.GetHashCode();
    }

    public IEnumerable<object> ReadArguments(IEnumerable<ParameterInfo> parameterInfos)
    {
      var myParameterInfos = parameterInfos.ToList();
      var arguments = new List<object>();
      var current = Trim();
      foreach (var parameterInfo in myParameterInfos)
      {
        Helper.ForbidNull(parameterInfo, nameof(parameterInfo));
        if (current.Length == 0)
        {
          if (parameterInfo.IsOptional)
          {
            break;
          }
          throw GetException(minCount: myParameterInfos.TakeWhile(z => !z.IsOptional).Count());
        }
        var regex = parameterInfo.IsDefined(typeof(AllowSpacesAttribute)) ? DelimiterRegexAllowSpace : DelimiterRegex;
        var match = regex.Match(current.Value);
        LocatedString argument;
        if (match.Success)
        {
          argument = current.Substring(0, match.Index);
          current = current.Substring(match.Index + match.Length);
        }
        else
        {
          argument = current;
          current = Empty;
        }
        arguments.Add(argument.ReadArgument(parameterInfo));
      }
      if (current.Length > 0)
      {
        throw GetException(maxCount: myParameterInfos.Count);
      }
      return arguments.Concat(Enumerable.Repeat(Type.Missing, myParameterInfos.Count - arguments.Count));
    }

    private object ReadArgument(ParameterInfo parameterInfo)
    {
      var type = parameterInfo.ParameterType;
      if (type == typeof(bool) || type == typeof(bool?))
      {
        switch (Value)
        {
          case "true": return true;
          case "false": return false;
          default: throw CreateException("Failed to parse as bool.");
        }
      }
      if (type == typeof(int) || type == typeof(int?))
      {
        if (!int.TryParse(Value, out var x))
        {
          throw CreateException("Failed to parse as int.");
        }
        if (!parameterInfo.IsDefined(typeof(ValidRangeAttribute)))
        {
          return x;
        }
        var range = parameterInfo.GetCustomAttribute<ValidRangeAttribute>();
        if (x < range.Minimum || x > range.Maximum)
        {
          throw CreateException("Argument out of range" + Helper.GetBindingsSuffix(x, nameof(x), range.Minimum, "min", range.Maximum, "max"));
        }
        return x;
      }
      if (type == typeof(TimeSpan) || type == typeof(TimeSpan?))
      {
        if (TimeSpan.TryParse(Value, out var x))
        {
          return x;
        }
        throw CreateException("Failed to parse as TimeSpan.");
      }
      if (type == typeof(DirectoryInfo))
      {
        return new DirectoryInfo(Value);
      }
      if (type == typeof(FileInfo))
      {
        return new FileInfo(Value);
      }
      if (type == typeof(Action))
      {
        return Env.CreateInjector().Add(this, Env.Config.DefaultInputReader).Compile();
      }
      if (type == typeof(IInjector))
      {
        return Env.CreateInjector().Add(this, Env.Config.DefaultInputReader);
      }
      if (type == typeof(string))
      {
        if (!parameterInfo.IsDefined(typeof(ValidFlagsAttribute)))
        {
          return Value;
        }
        var flagsString = parameterInfo.GetCustomAttribute<ValidFlagsAttribute>().FlagsString;
        foreach (var c in Value.Where(z => !char.IsWhiteSpace(z)))
        {
          if (!flagsString.Contains(c))
          {
            throw CreateException($"Invalid flag '{c}', expecting one of \"{flagsString}\".");
          }
        }
        return Value;
      }
      if (type == typeof(LocatedString) || type == typeof(LocatedString?))
      {
        return this;
      }
      if (type == typeof(Chord))
      {
        return Env.Config.DefaultChordReader.CreateChord(this);
      }
      if (type == typeof(Input) || type == typeof(Input?))
      {
        var chord = Env.Config.DefaultChordReader.CreateChord(this);
        if (chord.Length > 1 || chord.Length == 0 || chord.First().Modifiers != Modifiers.None)
        {
          throw CreateException("Failed to parse as a single Input.");
        }
        return chord.First().Input;
      }
      throw CreateException($"Argument type '{type.Name}' not supported.");
    }

    private ParseException CreateException(string text)
    {
      return new ParseException(this, text);
    }
  }
}

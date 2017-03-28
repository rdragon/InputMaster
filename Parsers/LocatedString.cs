using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace InputMaster.Parsers
{
  struct LocatedString
  {
    public static LocatedString None = new LocatedString();
    public static LocatedString Empty = new LocatedString("");

    public LocatedString(string text) : this(text, Location.Unknown) { }

    public LocatedString(string text, Location location)
    {
      Helper.ForbidNull(text, nameof(text));
      Value = text;
      Location = location;
    }

    public string Value { get; }
    public Location Location { get; }
    public int Length { get { return Value.Length; } }

    public static bool operator ==(LocatedString a, LocatedString b)
    {
      return a.Equals(b);
    }

    public static bool operator !=(LocatedString a, LocatedString b)
    {
      return !a.Equals(b);
    }

    public override string ToString()
    {
      if (Location == Location.Unknown)
      {
        return $"\"{Value}\"";
      }
      else
      {
        return $"\"{Value}\", {Location}";
      }
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

    public LocatedString Require(char delimiter, int targetCount = -1, int minCount = -1, int maxCount = -1)
    {
      var count = Split(delimiter).Length;
      if (targetCount != -1 && count != targetCount)
      {
        throw GetException(targetCount: targetCount);
      }
      else if (minCount != -1 && count < minCount)
      {
        throw GetException(targetCount: targetCount);
      }
      else if (maxCount != -1 && count > maxCount)
      {
        throw GetException(targetCount: targetCount);
      }
      return this;
    }

    public Exception GetException(int targetCount = -1, int minCount = -1, int maxCount = -1)
    {
      if (targetCount != -1)
      {
        throw CreateException($"Wrong number of arguments given, expecting {targetCount} arguments.");
      }
      else if (minCount != -1)
      {
        throw CreateException($"Not enough arguments given, expecting at least {minCount}.");
      }
      else if (maxCount != -1)
      {
        throw CreateException($"Too many arguments given, expecting at most {maxCount}.");
      }
      else
      {
        throw new ArgumentException();
      }
    }

    public LocatedString[] Split(char c = ' ')
    {
      var parts = Value.Length == 0 ? new string[0] : Value.Split(c);
      var column = 0;
      var ar = new LocatedString[parts.Length];
      for (int i = 0; i < parts.Length; i++)
      {
        ar[i] = new LocatedString(parts[i], Location.AddColumns(column)).Trim();
        column += parts[i].Length + 1;
      }
      return ar;
    }

    public LocatedString Substring(int startIndex)
    {
      return new LocatedString(Value.Substring(startIndex), Location.AddColumns(startIndex));
    }

    public LocatedString Substring(int startIndex, int length)
    {
      return new LocatedString(Value.Substring(startIndex, length), Location.AddColumns(startIndex));
    }

    public LocatedString Replace(string str, string replacement)
    {
      var newValue = Value.Replace(str, replacement);
      return new LocatedString(newValue, newValue.Length != Value.Length ? Location.WithoutColumnInfo : Location);
    }

    public IEnumerable<object> ReadArguments(IEnumerable<ParameterInfo> parameterInfos)
    {
      var delimiter = Value.Contains(Config.ArgumentDelimiter) ? Config.ArgumentDelimiter : ' ';
      return ReadArguments(parameterInfos.ToArray(), delimiter);
    }

    private IEnumerable<object> ReadArguments(ParameterInfo[] parameterInfos, char delimiter)
    {
      var arguments = new List<object>();
      var current = Trim();
      foreach (var parameterInfo in parameterInfos)
      {
        if (current.Length == 0)
        {
          if (parameterInfo.IsOptional)
          {
            break;
          }
          else
          {
            throw GetException(minCount: parameterInfos.TakeWhile(z => !z.IsOptional).Count());
          }
        }
        else
        {
          var c = (parameterInfo.IsDefined(typeof(AllowSpacesAttribute)) && delimiter == ' ') ? Config.ArgumentDelimiter : delimiter;
          var j = current.Value.IndexOf(c);
          LocatedString argument;
          if (j == -1)
          {
            argument = current;
            current = Empty;
          }
          else
          {
            argument = current.Substring(0, j).TrimEnd();
            current = current.Substring(j + 1).TrimStart();
          }
          arguments.Add(argument.ReadArgument(parameterInfo));
        }
      }
      if (current.Length > 0)
      {
        throw GetException(maxCount: parameterInfos.Length);
      }
      else
      {
        return arguments.Concat(Enumerable.Repeat(Type.Missing, parameterInfos.Length - arguments.Count));
      }
    }

    private object ReadArgument(ParameterInfo parameterInfo)
    {
      var type = parameterInfo.ParameterType;
      if (type == typeof(bool) || type == typeof(bool?))
      {
        if (Value == "true")
        {
          return true;
        }
        else if (Value == "false")
        {
          return false;
        }
        else
        {
          throw CreateException("Failed to parse as bool.");
        }
      }
      else if (type == typeof(int) || type == typeof(int?))
      {
        int x;
        if (int.TryParse(Value, out x))
        {
          if (parameterInfo != null && parameterInfo.IsDefined(typeof(ValidRangeAttribute)))
          {
            var range = parameterInfo.GetCustomAttribute<ValidRangeAttribute>();
            if (x < range.Minimum || x > range.Maximum)
            {
              throw CreateException($"Argument out of range" + Helper.GetBindingsSuffix(x, nameof(x), range.Minimum, "min", range.Maximum, "max"));
            }
          }
          return x;
        }
        else
        {
          throw CreateException($"Failed to parse as int.");
        }
      }
      else if (type == typeof(TimeSpan) || type == typeof(TimeSpan?))
      {
        TimeSpan x;
        if (TimeSpan.TryParse(Value, out x))
        {
          return x;
        }
        else
        {
          throw CreateException($"Failed to parse as TimeSpan.");
        }
      }
      else if (type == typeof(Action))
      {
        return Env.CreateInjector().Add(this, Config.DefaultInputReader).Compile();
      }
      else if (type == typeof(IInjector))
      {
        return Env.CreateInjector().Add(this, Config.DefaultInputReader);
      }
      else if (type == typeof(string))
      {
        if (parameterInfo != null && parameterInfo.IsDefined(typeof(ValidFlagsAttribute)))
        {
          var flagsString = parameterInfo.GetCustomAttribute<ValidFlagsAttribute>().FlagsString;
          foreach (var c in Value.Where(z => !char.IsWhiteSpace(z)))
          {
            if (!flagsString.Contains(c))
            {
              throw CreateException($"Invalid flag '{c}', expecting one of \"{flagsString}\".");
            }
          }
        }
        return Value;
      }
      else if (type == typeof(LocatedString) || type == typeof(LocatedString?))
      {
        return this;
      }
      else if (type == typeof(Chord))
      {
        return Config.DefaultChordReader.CreateChord(this);
      }
      else if (type == typeof(Input) || type == typeof(Input?))
      {
        var chord = Config.DefaultChordReader.CreateChord(this);
        if (chord.Length > 1 || chord.Length == 0 || chord.First().Modifiers != Modifiers.None)
        {
          throw CreateException("Failed to parse as a single Input.");
        }
        else
        {
          return chord.First().Input;
        }
      }
      else
      {
        throw CreateException($"Argument type '{type.Name}' not supported.");
      }
    }

    private ParseException CreateException(string text)
    {
      return new ParseException(this, text);
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
  }
}

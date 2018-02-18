using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using InputMaster.Parsers;
using InputMaster.Win32;
using Microsoft.Win32.SafeHandles;
using Newtonsoft.Json;

namespace InputMaster
{
  public class HookHandle : SafeHandleZeroOrMinusOneIsInvalid
  {
    public HookHandle() : base(true) { }

    protected override bool ReleaseHandle()
    {
      return NativeMethods.UnhookWindowsHookEx(handle);
    }
  }

  public class HotkeyTrigger
  {
    public HotkeyTrigger(Combo combo)
    {
      Combo = combo;
    }

    public Combo Combo { get; }
  }

  public class HotkeyFile
  {
    public HotkeyFile(string name, string text)
    {
      Name = name;
      Text = text;
    }

    public string Name { get; }
    public string Text { get; }
  }

  public abstract class Section
  {
    protected Section()
    {
      Column = -1;
    }

    public int Column { get; set; }
  }

  public class FlagSection : StandardSection
  {
    private readonly string _flag;

    public FlagSection(StandardSection parent, string text) : base(parent)
    {
      _flag = text;
    }

    protected override bool ComputeEnabled()
    {
      return Env.FlagManager.HasFlag(_flag);
    }
  }

  public class RegexSection : StandardSection
  {
    private readonly Regex _regex;
    private readonly RegexSectionType _type;

    public RegexSection(StandardSection parent, Regex regex, RegexSectionType type) : base(parent)
    {
      _regex = regex;
      if (!Enum.IsDefined(typeof(RegexSectionType), type))
        throw new ArgumentOutOfRangeException(nameof(type));
      _type = type;
    }

    protected override bool ComputeEnabled()
    {
      string s;
      switch (_type)
      {
        case RegexSectionType.Window: s = Env.ForegroundListener.ForegroundWindowTitle; break;
        default: s = Env.ForegroundListener.ForegroundProcessName; break;
      }
      return _regex.IsMatch(s);
    }
  }

  public class ModeHotkey
  {
    public ModeHotkey(Chord chord, Action<Combo> action, string description)
    {
      Chord = chord;
      Action = action;
      Description = description;
    }

    public Chord Chord { get; }
    public Action<Combo> Action { get; }
    public string Description { get; }
  }

  public class ComboArgs
  {
    public ComboArgs(Combo combo)
    {
      Combo = combo;
    }

    public Combo Combo { get; }
    public bool Capture { get; set; }

    public override string ToString()
    {
      return Combo.ToString();
    }
  }

  public abstract class Actor
  {
    protected Actor()
    {
      Env.CommandCollection.AddActor(this);
    }
  }

  public class AmbiguousHotkeyException : Exception
  {
    public AmbiguousHotkeyException(string message, Exception innerException = null) : base(message, innerException) { }
  }

  public class WrappedException : Exception
  {
    public WrappedException(string message, Exception innerException = null) : base(message, innerException) { }
  }

  public class FatalException : Exception
  {
    public FatalException(string message, Exception innerException = null) : base(message, innerException) { }
  }

  public class DecryptionFailedException : Exception
  {
    public DecryptionFailedException(string message, Exception innerException = null) : base(message, innerException) { }
  }

  public class GitException : Exception
  {
    public GitException(string message, Exception innerException = null) : base(message, innerException) { }
  }

  public class MatrixPassword
  {
    public string Value { get; set; }
    public PasswordBlueprint Blueprint { get; set; }

    public MatrixPassword(string value, PasswordBlueprint blueprint)
    {
      Value = value;
      Blueprint = blueprint;
    }
  }

  public class PasswordBlueprint
  {
    public int Length { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public string Direction { get; set; }
    public BlueprintShape Shape { get; set; }

    public override string ToString()
    {
      string shape;
      switch (Shape)
      {
        case BlueprintShape.Straight: shape = ""; break;
        case BlueprintShape.Stairs: shape = "/"; break;
        case BlueprintShape.Spiral: shape = "@"; break;
        case BlueprintShape.L: shape = "¬"; break;
        default: throw new ArgumentException();
      }
      return $"{X},{Y}.{Direction}{shape}{Length}";
    }
  }

  public class PasswordDecomposition
  {
    public string Prefix { get; }
    public IEnumerable<PasswordBlueprint> BluePrints { get { return _blueprints.AsReadOnly(); } }
    private List<PasswordBlueprint> _blueprints = new List<PasswordBlueprint>();

    public PasswordDecomposition(string password)
    {
      Prefix = password;
    }

    public PasswordDecomposition(string password, IEnumerable<PasswordBlueprint> blueprints) : this(password)
    {
      _blueprints = blueprints.ToList();
    }
  }

  public class StateFile
  {
    public string File { get; }
    public StateHandlerFlags Flags { get; }

    public StateFile(string file, StateHandlerFlags flags)
    {
      File = file;
      Flags = flags;
    }
  }

  public class AccountModel
  {
    public string Title { get; }
    public string LoginName { get; }
    public string Password { get; }
    public string Extra { get; }
    public string MatrixDecomposition { get; }

    public AccountModel(string title, string loginName, string password, string extra, string matrixDecomposition)
    {
      Title = title;
      LoginName = loginName;
      Password = password;
      Extra = extra;
      MatrixDecomposition = matrixDecomposition;
    }
  }

  public class ChordJsonConverter : JsonConverter
  {
    public override bool CanConvert(Type objectType)
    {
      return objectType == typeof(Chord);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      return Env.Config.DefaultChordReader.CreateChord(new LocatedString(reader.Value.ToString()));
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
      writer.WriteValue(value.ToString());
    }
  }

  [JsonConverter(typeof(TitleFilterJsonConverter))]
  public class TitleFilter
  {
    public string Value { get; }
    private readonly Regex _regex;

    public TitleFilter(string value)
    {
      Value = value;
      _regex = Helper.GetRegex(Value, RegexOptions.IgnoreCase);
    }

    public bool IsEnabled()
    {
      return _regex.IsMatch(Env.ForegroundListener.ForegroundWindowTitle);
    }
  }

  public class TitleFilterJsonConverter : JsonConverter
  {
    public override bool CanConvert(Type objectType)
    {
      return objectType == typeof(TitleFilter);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      return new TitleFilter(reader.Value.ToString());
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
      writer.WriteValue(((TitleFilter)value).Value);
    }
  }
}

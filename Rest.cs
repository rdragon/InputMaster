using System;
using System.Collections.Generic;
using System.Linq;
using InputMaster.Win32;
using JetBrains.Annotations;
using Microsoft.Win32.SafeHandles;

namespace InputMaster
{
  [UsedImplicitly]
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
}

using System;
using System.IO;
using System.Drawing;
using InputMaster.Extensions;
using InputMaster.Parsers;
using System.Linq;
using System.Text.RegularExpressions;
using InputMaster.Win32;

namespace InputMaster
{
  static class Config
  {
    // Paths
    public static readonly DirectoryInfo DataDir = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "InputMaster"));
    public static readonly DirectoryInfo CacheDir = new DirectoryInfo(Path.Combine(DataDir.FullName, "Cache"));
    public static readonly FileInfo HotkeyFile = new FileInfo(Path.Combine(DataDir.FullName, "Hotkeys.txt"));
    public static readonly FileInfo WindowHandleFile = new FileInfo(Path.Combine(CacheDir.FullName, "WindowHandle"));
    public static readonly FileInfo ErrorLogFile = new FileInfo(Path.Combine(CacheDir.FullName, "ErrorLog.txt"));
    public static DirectoryInfo TextEditorDir = new DirectoryInfo(Path.Combine(DataDir.FullName, "TextEditor"));

    // Parsing
    public const char TokenStart = '{';
    public const char TokenEnd = '}';
    public const string SectionIdentifier = "√>";
    public const string SpecialCommandIdentifier = "√:";
    public const string MultipleCommandsIdentifier = "√+";
    public const string CommentIdentifier = "√#";
    public const string ArgumentDelimiter = "√,";
    public const string FlagSectionIdentifier = "Flag";
    public const string InputModeSectionIdentifier = "InputMode";
    public const string ComposeModeSectionIdentifier = "ComposeMode";
    private const string InnerIdentifierTokenPattern = "[A-Z][a-zA-Z0-9_]+";
    public static string TokenPattern = CreateTokenPattern($@"({InnerIdentifierTokenPattern}|(0|[1-9][0-9]*)x)");
    public static string IdentifierTokenPattern = CreateTokenPattern(InnerIdentifierTokenPattern);
    public static InputReader DefaultInputReader = new InputReader(InputReaderFlags.AllowCustomCharacter | InputReaderFlags.AllowHoldRelease | InputReaderFlags.AllowMultiplier);
    public static InputReader DefaultChordReader = new InputReader(InputReaderFlags.AllowCustomModifier);
    public static InputReader DefaultModeChordReader = new InputReader(InputReaderFlags.AllowCustomModifier | InputReaderFlags.AllowKeywordAny);
    public static InputReader LiteralInputReader = new InputReader(InputReaderFlags.ParseLiteral);

    // English US keyboard layout
    public const string Keyboard = "`-=[];'\\,./0123456789abcdefghijklmnopqrstuvwxyz";
    public const string ShiftedKeyboard = "~_+{}:\"|<>?)!@#$%^&*(ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public static readonly Input[] KeyboardInputs = new Input[] { Input.Grave, Input.Dash, Input.Is, Input.LBracket, Input.RBracket, Input.Semicolon, Input.Quote, Input.Backslash, Input.Comma, Input.Period, Input.Slash, Input.D0, Input.D1, Input.D2, Input.D3, Input.D4, Input.D5, Input.D6, Input.D7, Input.D8, Input.D9, Input.A, Input.B, Input.C, Input.D, Input.E, Input.F, Input.G, Input.H, Input.I, Input.J, Input.K, Input.L, Input.M, Input.N, Input.O, Input.P, Input.Q, Input.R, Input.S, Input.T, Input.U, Input.V, Input.W, Input.X, Input.Y, Input.Z };

    // Rest
    public const Input CloseKey = Input.Esc;
    public const Input ToggleHookKey = Input.NumLock;
    public static readonly Combo ClearModeCombo = new Combo(Input.Bs);
    public static readonly Combo ShowModeCombo = new Combo(Input.Space);
    public static readonly Combo[] ClearModeCombos = new Combo[] { new Combo(CloseKey), new Combo(Input.Comma) };
    public static readonly TimeSpan ExitRunningInputMasterTimeout = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan NotifierTextLifetime = TimeSpan.FromSeconds(1.5);
    public static readonly TimeSpan SchedulerInterval = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan ProcessManagerInterval = TimeSpan.FromMinutes(1);
    public static readonly Color ForegroundColor = Color.Black;
    public static readonly Color BackgroundColor = Color.White;
    public static readonly Font Font = new Font("Consolas", 11);
    public const int ClipboardTries = 10;
    public const int ClipboardDelay = 100;
    public const string LogDateTimeFormat = "s";
    public static readonly bool InsertSpaceAfterComma = true;
    public const int MaxChordLength = 10;
    public const string NotifierWindowTitle = "Notifier - InputMaster";
    public const bool CaptureLmb = false;

    // Text Editor
    public static bool EnableTextEditor = false;
    public const string TextEditorSectionIdentifier = "¶";
    public static readonly TimeSpan SaveTimerInterval = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan UpdatePanelDelay = TimeSpan.FromSeconds(1);
    public const int SplitterDistance = 300;
    public const int MaxTextEditorTabs = 3;
    public const string TextEditorWindowTitle = "Text Editor - InputMaster";
    public const string TextEditorModeName = "TextEditor";
    public static readonly Combo TextEditorDesktopHotkey = Combo.None;
    public static readonly bool UseCipher = false;
    public const int CipherDerivationIterations = 100;
    public static readonly FileInfo KeyFile = null;
    public static readonly bool AskForPassword = false;

    public static Modifiers ToModifier(this Input input)
    {
      switch (input)
      {
        case Input.LShift:
        case Input.RShift:
          return Modifiers.Shift;
        case Input.LCtrl:
        case Input.RCtrl:
          return Modifiers.Ctrl;
        case Input.LAlt:
        case Input.RAlt:
          return Modifiers.Alt;
        case Input.LWin:
        case Input.RWin:
          return Modifiers.Win;
        default:
          return Modifiers.None;
      }
    }

    public static bool IsCharacterKey(this Input input)
    {
      return input == Input.Space || KeyboardInputs.Contains(input);
    }

    public static readonly Input[] LeftModifierKeys = new Input[] { Input.LShift, Input.LCtrl, Input.LAlt, Input.LWin };

    public static void Initialize(InstanceCollection instances)
    {
      var colorTracker = new ColorTracker(instances.FlagManager);
      instances.Brain.AddDisposable(colorTracker);
      var processManager = new HiddenProcessManager(instances.Brain);
      instances.Brain.AddDisposable(processManager);
      var scheduler = new Scheduler(instances.Brain, processManager);
      instances.Brain.AddDisposable(scheduler);
      instances.CommandCollection.AddActors(new SecondClipboard(instances.ForegroundInteractor), new CustomActor(), colorTracker);

    }

    public static void Start(InstanceCollection instances)
    {
      if ((NativeMethods.GetKeyState(Input.NumLock) & 1) == 1)
      {
        Env.CreateInjector().Add(Input.NumLock).Run();
      }
    }

    private static string CreateTokenPattern(string text)
    {
      return Regex.Escape(TokenStart.ToString()) + text + Regex.Escape(TokenEnd.ToString());
    }
  }

  [Flags]
  enum Modifiers
  {
    None = 0,
    Shift = 1,
    Ctrl = 2,
    Alt = 4,
    Win = 8,
    StandardModifiers = 15,
  }

  enum DynamicHotkeyEnum
  {
    None, Cut, Copy, Paste, LineUp, LineDown, RemoveLine, SaveAll, CloseTab,
  }
}


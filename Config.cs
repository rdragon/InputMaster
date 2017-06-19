using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using InputMaster.Extensions;
using InputMaster.Parsers;
using InputMaster.Win32;
using InputMaster.KeyboardLayouts;
// ReSharper disable All

namespace InputMaster
{
  internal class Config
  {
    protected readonly Dictionary<string, string> PreprocessorReplaces = new Dictionary<string, string>();
    protected readonly Dictionary<string, Input> CustomInputs = new Dictionary<string, Input>();
    protected readonly Dictionary<string, Combo> CustomCombos = new Dictionary<string, Combo>();
    private readonly Dictionary<Input, Modifiers> ModifierDict = new Dictionary<Input, Modifiers>();
    private readonly Dictionary<Modifiers, Input> ModifierKeyDict = new Dictionary<Modifiers, Input>();

    public Config()
    {
      var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "InputMaster");
      DataDir = Path.Combine(path, "Data");
      CacheDir = Path.Combine(path, "Cache");
      SharedDir = Path.Combine(path, "Shared");
      TextEditorDir = Path.Combine(DataDir, "TextEditor");
      HotkeyFile = Path.Combine(DataDir, "Hotkeys.im");
      WindowHandleFile = Path.Combine(CacheDir, "WindowHandle");
      ErrorLogFile = Path.Combine(CacheDir, "ErrorLog.txt");
      AccountsOutputFile = Path.Combine(CacheDir, "Accounts.txt");
      SharedFilesDirName = "SharedFiles";
      DefaultTextEditor = Path.Combine(Environment.SystemDirectory, "notepad.exe");
      Notepadpp = @"C:\Program Files (x86)\Notepad++\notepad++.exe";
      ScreenshotsDir = Path.Combine(DataDir, "Screenshots");
      DefaultWebBrowser = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";
      DefaultInputReader = new InputReader(InputReaderFlags.AllowCustomCharacter | InputReaderFlags.AllowHoldRelease | InputReaderFlags.AllowMultiplier);
      DefaultChordReader = new InputReader(InputReaderFlags.AllowCustomModifier);
      DefaultModeChordReader = new InputReader(InputReaderFlags.AllowCustomModifier | InputReaderFlags.AllowKeywordAny);
      LiteralInputReader = new InputReader(InputReaderFlags.ParseLiteral);
      KeyboardLayout = new LayoutEnglishUnitedStates();
      CloseKey = Input.Esc;
      ToggleHookKey = Input.NumLock;
      ClearModeCombo = new Combo(Input.Bs);
      ShowModeCombo = new Combo(Input.Space);
      ClearModeCombos = new[] { new Combo(CloseKey), new Combo(Input.Comma) };
      ExitOtherInputMasterTimeout = TimeSpan.FromSeconds(1);
      NotifierTextLifetime = TimeSpan.FromSeconds(1.5);
      SchedulerInterval = TimeSpan.FromSeconds(1);
      ProcessManagerInterval = TimeSpan.FromMinutes(1);
      ForegroundColor = Color.Black;
      BackgroundColor = Color.White;
      Font = new Font("Consolas", 11);
      ClipboardTries = 10;
      ClipboardDelay = 100;
      LogDateTimeFormat = "s";
      InsertSpaceAfterComma = true;
      MaxChordLength = 10;
      NotifierWindowTitle = "Notifier - InputMaster";
      EmailSuffix = "@example.com";
      DefaultPasswordLength = 12;
      OpenAccountModeName = "OpenAccount";
      ModifyAccountModeName = "ModifyAccount";
      AccountModeName = "Account";
      TextEditorWindowTitle = "Text Editor - InputMaster";
      SaveTimerInterval = TimeSpan.FromSeconds(30);
      UpdatePanelInterval = TimeSpan.FromSeconds(1);
      SplitterDistance = 300;
      MaxTextEditorTabs = 3;
      TextEditorModeName = "TextEditor";
      CipherDerivationIterations = 100;
      foreach (var pair in ConfigHelper.ModifierKeys)
      {
        var input = pair.Item1;
        var modifier = pair.Item2;
        if (!ModifierKeyDict.ContainsKey(modifier))
        {
          ModifierKeyDict.Add(modifier, input);
        }
        ModifierDict.Add(input, modifier);
      }
      var modifierWithoutKey = Helper.Modifiers.FirstOrDefault(z => !ModifierKeyDict.ContainsKey(z));
      if (modifierWithoutKey != Modifiers.None)
      {
        throw new FatalException($"No modifier key found for modifier {modifierWithoutKey}.");
      }
    }

    public string DataDir { get; protected set; }
    public string CacheDir { get; protected set; }
    public string SharedDir { get; protected set; }
    public string TextEditorDir { get; protected set; }
    public string HotkeyFile { get; protected set; }
    public string WindowHandleFile { get; protected set; }
    public string ErrorLogFile { get; protected set; }
    public string AccountsOutputFile { get; protected set; }
    public string SharedFilesDirName { get; protected set; }
    public string DefaultTextEditor { get; protected set; }
    public string DefaultWebBrowser { get; protected set; }
    public string Notepadpp { get; protected set; }
    public string ScreenshotsDir { get; protected set; }
    public InputReader DefaultInputReader { get; protected set; }
    public InputReader DefaultChordReader { get; protected set; }
    public InputReader DefaultModeChordReader { get; protected set; }
    public InputReader LiteralInputReader { get; protected set; }
    public IKeyboardLayout KeyboardLayout { get; protected set; }
    public Input CloseKey { get; protected set; }
    public Input ToggleHookKey { get; protected set; }
    public Combo ClearModeCombo { get; protected set; }
    public Combo ShowModeCombo { get; protected set; }
    public Combo[] ClearModeCombos { get; protected set; }
    public TimeSpan ExitOtherInputMasterTimeout { get; protected set; }
    public TimeSpan NotifierTextLifetime { get; protected set; }
    public TimeSpan SchedulerInterval { get; protected set; }
    public TimeSpan ProcessManagerInterval { get; protected set; }
    public Color ForegroundColor { get; protected set; }
    public Color BackgroundColor { get; protected set; }
    public Font Font { get; protected set; }
    public int ClipboardTries { get; protected set; }
    public int ClipboardDelay { get; protected set; }
    public string LogDateTimeFormat { get; protected set; }
    public bool InsertSpaceAfterComma { get; protected set; }
    public int MaxChordLength { get; protected set; }
    public string NotifierWindowTitle { get; protected set; }
    public bool CaptureLmb { get; protected set; }
    public string EmailSuffix { get; protected set; }
    public int DefaultPasswordLength { get; protected set; }
    public string OpenAccountModeName { get; protected set; }
    public string ModifyAccountModeName { get; protected set; }
    public string AccountModeName { get; protected set; }
    public string LocalAccountId { get; protected set; }
    public bool EnableTextEditor { get; protected set; }
    public string TextEditorWindowTitle { get; protected set; }
    public TimeSpan SaveTimerInterval { get; protected set; }
    public TimeSpan UpdatePanelInterval { get; protected set; }
    public int SplitterDistance { get; protected set; }
    public int MaxTextEditorTabs { get; protected set; }
    public string TextEditorModeName { get; protected set; }
    public Combo TextEditorDesktopHotkey { get; protected set; }
    public int CipherDerivationIterations { get; protected set; }
    public FileInfo KeyFile { get; protected set; }
    public bool AskForPassword { get; protected set; }
    public Warnings Warnings { get; protected set; }

    public virtual void Run()
    {
      PreprocessorReplaces.Add(nameof(DataDir), DataDir);
      PreprocessorReplaces.Add(nameof(CacheDir), CacheDir);
      PreprocessorReplaces.Add(nameof(SharedDir), SharedDir);
      PreprocessorReplaces.Add(nameof(HotkeyFile), HotkeyFile);
      PreprocessorReplaces.Add(nameof(WindowHandleFile), WindowHandleFile);
      PreprocessorReplaces.Add(nameof(ErrorLogFile), ErrorLogFile);
      PreprocessorReplaces.Add(nameof(TextEditorDir), TextEditorDir);
      PreprocessorReplaces.Add(nameof(AccountsOutputFile), AccountsOutputFile);
      PreprocessorReplaces.Add(nameof(DefaultTextEditor), DefaultTextEditor);
      PreprocessorReplaces.Add(nameof(Notepadpp), Notepadpp);

      Env.AddActor(new MiscActor());
      Env.AddActor(new ForegroundInteractor());
      Env.AddActor(new SecondClipboard());
      Env.AddActor(new ColorTracker());
      Env.AddActor(new VarActor());

      if ((NativeMethods.GetKeyState(Input.NumLock) & 1) == 1)
      {
        Env.CreateInjector().Add(Input.NumLock).Run();
      }
    }

    public bool TryGetModifierKey(Modifiers modifier, out Input input) => ModifierKeyDict.TryGetValue(modifier, out input);
    public bool TryGetModifier(Input input, out Modifiers modifier) => ModifierDict.TryGetValue(input, out modifier);
    public bool TryGetCustomCombo(string name, out Combo combo) => CustomCombos.TryGetValue(name, out combo);
    public bool TryGetCustomInput(string name, out Input input) => CustomInputs.TryGetValue(name, out input);
    public bool TryGetPreprocessorReplace(string key, out string value) => PreprocessorReplaces.TryGetValue(key, out value);
    public virtual string GetChordText(string title) => Helper.GetChordText(title);
  }
}


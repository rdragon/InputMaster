using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using InputMaster.Parsers;
using InputMaster.Win32;
using InputMaster.KeyboardLayouts;
using System.Windows.Forms;
using InputMaster.Instances;
using System.Threading.Tasks;

namespace InputMaster
{
  /// <summary>
  /// All properties are thread-safe.
  /// </summary>
  public class Config
  {
    protected readonly Dictionary<string, string> PreprocessorReplaces = new Dictionary<string, string>();
    protected readonly Dictionary<string, Input> CustomInputs = new Dictionary<string, Input>();
    protected readonly Dictionary<string, Combo> CustomCombos = new Dictionary<string, Combo>();
    private readonly Dictionary<Input, Modifiers> ModifierDict = new Dictionary<Input, Modifiers>();
    private readonly Dictionary<Modifiers, Input> ModifierKeyDict = new Dictionary<Modifiers, Input>();

    public Config()
    {
      foreach (var pair in ConfigHelper.ModifierKeys)
      {
        var input = pair.Item1;
        var modifier = pair.Item2;
        if (!ModifierKeyDict.ContainsKey(modifier))
          ModifierKeyDict.Add(modifier, input);
        ModifierDict.Add(input, modifier);
      }
      var modifierWithoutKey = Helper.Modifiers.FirstOrDefault(z => !ModifierKeyDict.ContainsKey(z));
      if (modifierWithoutKey != Modifiers.None)
        throw new FatalException($"No modifier key found for modifier {modifierWithoutKey}.");
    }

    public virtual string DataDir { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
      "InputMaster");
    public virtual string DefaultWebBrowser { get; } = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";
    public virtual string Notepadpp { get; } = @"C:\Program Files (x86)\Notepad++\notepad++.exe";
    public virtual string InputMasterPublishDir { get; } = @"C:\bin\InputMaster";
    public virtual string InputMasterReadOnlyPublishDir { get; } = @"C:\bin\InputMasterReadOnly";
    public virtual string InputMasterFileName { get; } = "InputMaster.exe";
    public virtual string CacheDir => DataDir;
    public virtual string TextEditorDir => Path.Combine(DataDir, "TextEditor");
    public virtual string HotkeyFile => Path.Combine(DataDir, "Hotkeys.im");
    public virtual string WindowHandleFile => Path.Combine(CacheDir, "WindowHandle");
    public virtual string ErrorLogFile => Path.Combine(CacheDir, "ErrorLog.txt");
    public virtual string DefaultTextEditor => Path.Combine(Environment.SystemDirectory, "notepad.exe");
    public virtual string ScreenshotsDir => Path.Combine(DataDir, "Screenshots");
    public virtual string TextEditorHotkeyFile => Path.Combine(TextEditorDir, "hotkeys");
    public virtual string IndexFileName { get; } = "index";
    public virtual string LogDateTimeFormat { get; } = "s";
    public virtual string NotifierWindowTitle { get; } = "Notifier - InputMaster";
    public virtual string TextEditorWindowTitle { get; } = "Text Editor - InputMaster";
    public virtual string DefaultEmail { get; } = "example@example.com";
    public virtual string OpenAccountModeName { get; } = "OpenAccount";
    public virtual string ModifyAccountModeName { get; } = "ModifyAccount";
    public virtual string AccountModeName { get; } = "Account";
    public virtual string TextEditorModeName { get; } = "TextEditor";
    public virtual string TextEditorSharedModeName { get; } = "SharedTextEditor";
    public virtual string HiddenTag { get; } = "[hidden]";
    public virtual string SharedTag { get; } = "[shared]";
    public virtual string AccountUploadUrl { get; } = null;
    public virtual string ResetCommandLineArgument { get; } = "reset";
    public virtual string CyptroDirectory { get; } = null;
    public virtual string CyptroDataDirectory { get; } = null;
    public virtual string CyptroAccountsName { get; } = null;
    public virtual string CyptroSaltSuffix { get; } = "96tNpQYEsaE427PRNlOc7N3WK";
    public virtual int LocalAccountId { get; } = 0;
    public virtual int ClipboardTries { get; } = 10;
    public virtual int ClipboardDelay { get; } = 100;
    public virtual int MaxChordLength { get; } = 10;
    public virtual int SplitterDistance { get; } = 300;
    public virtual int MaxTextEditorTabs { get; } = 3;
    public virtual int CyptroDerivationCount { get; } = 10000;
    public virtual int KeySize { get; } = 256 / 8; // AES-256
    public virtual int KeyDerivationBlockSize { get; } = 512 / 8; // SHA-1 block size
    public virtual int TextEditorFileNameLength { get; } = 8;
    public virtual int MatrixPasswordLength { get; } = 5;
    public virtual int PasswordMiddleLength { get; } = 3;
    /// <summary>
    /// Accounts below this id are not uploaded.
    /// </summary>
    public virtual int MinAccountUploadId { get; } = 10;
    public virtual int AccountUploaderMaxAccounts { get; } = 50;
    public virtual bool InsertSpaceAfterComma { get; } = true;
    public virtual bool CaptureLmb { get; } = false;
    public virtual bool EnableTextEditor { get; } = false;
    public virtual InputReader DefaultInputReader { get; } = new InputReader(InputReaderFlags.AllowCustomCharacter |
      InputReaderFlags.AllowHoldRelease | InputReaderFlags.AllowMultiplier);
    public virtual InputReader DefaultChordReader { get; } = new InputReader(InputReaderFlags.AllowCustomModifier);
    public virtual InputReader DefaultModeChordReader { get; } = new InputReader(InputReaderFlags.AllowCustomModifier |
      InputReaderFlags.AllowKeywordAny);
    public virtual InputReader LiteralInputReader { get; } = new InputReader(InputReaderFlags.ParseLiteral);
    public virtual IKeyboardLayout KeyboardLayout { get; } = new LayoutEnglishUnitedStates();
    public virtual Input CloseKey { get; } = Input.Esc;
    public virtual Input ToggleHookKey { get; } = Input.NumLock;
    public virtual Combo ClearModeCombo { get; } = new Combo(Input.Bs);
    public virtual Combo ShowModeCombo { get; } = new Combo(Input.Space);
    public virtual Combo PrintModeCombo { get; } = new Combo(Input.Space, Modifiers.Shift);
    public virtual Combo TextEditorDesktopHotkey { get; } = Combo.None;
    public virtual Combo[] ClearModeCombos { get; } = new[] { new Combo(Input.Esc), new Combo(Input.Comma) };
    public virtual TimeSpan ExitOtherInputMasterTimeout { get; } = TimeSpan.FromSeconds(1);
    public virtual TimeSpan NotifierTextLifetime { get; } = TimeSpan.FromSeconds(1.5);
    public virtual TimeSpan SchedulerInterval { get; } = TimeSpan.FromSeconds(1);
    public virtual TimeSpan ProcessManagerInterval { get; } = TimeSpan.FromMinutes(1);
    public virtual TimeSpan SaveTimerInterval { get; } = TimeSpan.FromSeconds(30);
    public virtual TimeSpan UpdatePanelInterval { get; } = TimeSpan.FromSeconds(1);
    public virtual TimeSpan AccountUploadTimeout { get; } = TimeSpan.FromSeconds(10);
    public virtual TimeSpan GitTimeout { get; } = TimeSpan.FromSeconds(30);
    public virtual Color ForegroundColor { get; } = Color.Black;
    public virtual Color BackgroundColor { get; } = Color.White;
    public virtual Font Font { get; } = new Font("Consolas", 11);

    public virtual Form CreateMainForm()
    {
      return new NotifyForm();
    }

    public virtual void Initialize()
    {
      PreprocessorReplaces.Add(nameof(DataDir), DataDir);
      PreprocessorReplaces.Add(nameof(CacheDir), CacheDir);
      PreprocessorReplaces.Add(nameof(HotkeyFile), HotkeyFile);
      PreprocessorReplaces.Add(nameof(WindowHandleFile), WindowHandleFile);
      PreprocessorReplaces.Add(nameof(ErrorLogFile), ErrorLogFile);
      PreprocessorReplaces.Add(nameof(TextEditorDir), TextEditorDir);
      PreprocessorReplaces.Add(nameof(DefaultTextEditor), DefaultTextEditor);
      PreprocessorReplaces.Add(nameof(Notepadpp), Notepadpp);
    }

    public virtual Task Run()
    {
      if ((NativeMethods.GetKeyState(Input.NumLock) & 1) == 1)
        Env.CreateInjector().Add(Input.NumLock).Run();
      return Task.CompletedTask;
    }

    public virtual Task<byte[]> GetKeyAsync()
    {
      return Task.FromResult(new byte[KeySize]);
    }

    public bool TryGetModifierKey(Modifiers modifier, out Input input) => ModifierKeyDict.TryGetValue(modifier, out input);
    public bool TryGetModifier(Input input, out Modifiers modifier) => ModifierDict.TryGetValue(input, out modifier);
    public bool TryGetCustomCombo(string name, out Combo combo) => CustomCombos.TryGetValue(name, out combo);
    public bool TryGetCustomInput(string name, out Input input) => CustomInputs.TryGetValue(name, out input);
    public bool TryGetPreprocessorReplace(string key, out string value) => PreprocessorReplaces.TryGetValue(key, out value);
  }
}


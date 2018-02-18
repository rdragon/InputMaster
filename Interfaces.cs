using System;
using System.Collections.Generic;
using System.ComponentModel;
using InputMaster.Parsers;
using InputMaster.Win32;
using System.Threading.Tasks;

namespace InputMaster
{
  public interface INotifier
  {
    void Info(string message);
    void Warning(string message);
    void Error(string message);
    void LogError(string message);
    void SetPersistentText(string text);
    string GetLog();
    void CaptureForeground();
    ISynchronizeInvoke SynchronizingObject { get; }
  }

  public interface IInputHook
  {
    void Handle(InputArgs e);
    void Reset();
    string GetStateInfo();
  }

  public interface IComboHook
  {
    void Handle(ComboArgs e);
    void Reset();
    string GetTestStateInfo();
    bool Active { get; }
  }

  public interface IInjectorStream<out T>
  {
    T Add(Input input, bool down);
    T Add(char c);
  }

  public interface IInjector<out T> : IInjectorStream<T>
  {
    void Run();
    Action Compile();
    T CreateInjector();
  }

  public interface IInjector : IInjector<IInjector> { }

  public interface IForegroundListener
  {
    string ForegroundWindowTitle { get; }
    string ForegroundProcessName { get; }
    void Update();
  }

  public interface IFlagManager
  {
    bool HasFlag(string flag);
    void ToggleFlag(string flag);
    void SetFlag(string flag);
    void ClearFlag(string flag);
    void ClearFlags();
    event Action FlagsChanged;
    IEnumerable<string> GetFlags();
  }

  public interface IParser
  {
    void UpdateHotkeyFile(HotkeyFile hotkeyFile);
    void UpdateParseAction(string name, ParseAction action);
    void Run();
    void GetAction(string name, out Action<IInjectorStream<object>> action);
    bool IsDynamicHotkey(string name);
    void FireNewParserOutput(ParserOutput parserOutput); // For unit tests.
    event Action<ParserOutput> NewParserOutput;
  }

  public interface IScheduler
  {
    void AddJob(string name, Action action, TimeSpan delay, TimeSpan? retryDelay = null);
    void AddJob(string name, Func<Task> action, TimeSpan delay, TimeSpan? retryDelay = null);
    void AddFileJob(string file, string arguments, TimeSpan delay);
  }

  public interface IProcessManager
  {
    void StartHiddenProcess(string file, string arguments = "", TimeSpan? timeout = null);
  }

  public interface ICommandCollection
  {
    void AddActor(object actor);
    Command GetCommand(LocatedString locatedName);
  }

  public interface IApp
  {
    event Action Run;
    event Action Save;
    event Action Unhook;
    event Action Exiting;
    void AddSaveAction(Func<Task> action);
    void AddExitAction(Func<Task> action);
    void TriggerRun();
    /// <summary>
    /// For debugging purposes.
    /// </summary>
    void TriggerUnhook();
    Task TriggerSaveAsync();
    Task TriggerExitAsync();
  }

  public interface IKeyboardLayout
  {
    bool TryReadKeyboardMessage(WindowMessage message, IntPtr data, out InputArgs inputArgs);
    Combo GetCombo(char c);
    string ConvertComboToString(Combo combo);
    bool IsCharacterKey(Input input);
  }

  /// <summary>
  /// Thread-safety is required for all classes implementing this interface.
  /// </summary>
  public interface ICipher
  {
    byte[] Encrypt(byte[] data);
    byte[] Decrypt(byte[] data);
  }

  public interface IStateHandler<T> : IStateHandler
  {
    Task<T> LoadAsync();
  }

  public interface IStateHandler
  {
    Task SaveAsync();
  }

  public interface IState
  {
    (bool, string message) Fix();
  }

  public interface IStateHandlerFactory
  {
    IStateHandler<T> Create<T>(T state, string file, StateHandlerFlags flags) where T : IState;
    IEnumerable<StateFile> GetExportableStateFiles();
  }
}

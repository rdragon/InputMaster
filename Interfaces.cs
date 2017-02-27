using InputMaster.Parsers;
using System;

namespace InputMaster
{
  interface INotifier
  {
    void Write(string text);
    void WriteWarning(string text);
    void WriteError(string text);
    void SetPersistentText(string text);
    void ShowLog();
    void CaptureForeground();
    void RequestExit();
    void Disable();
  }

  interface IInputHook
  {
    void Handle(InputArgs e);
    void Reset();
    string GetStateInfo();
  }

  interface IComboHook
  {
    void Handle(ComboArgs e);
    void Reset();
    string GetTestStateInfo();
    bool Active { get; }
  }

  interface IInjectorStream<out T>
  {
    T Add(Input input, bool down);
    T Add(char c);
  }

  interface IInjectorStream : IInjectorStream<IInjectorStream> { }

  interface IInjector<out T> : IInjectorStream<T>
  {
    void Run();
    Action Compile();
    T CreateInjector();
  }

  interface IInjector : IInjector<IInjector> { }

  interface IForegroundListener : IFlagViewer
  {
    string ForegroundWindowTitle { get; }
    string ForegroundProcessName { get; }
    DynamicHotkeyCollection DynamicHotkeyCollection { get; }
    int Counter { get; }
    void Update();
    string GetForegroundInfoSuffix();
  }

  interface IFlagViewer
  {
    bool IsFlagSet(string flag);
    event Action FlagsChanged;
  }

  interface IParserOutputProvider
  {
    event Action<ParserOutput> NewParserOutput;
  }
}

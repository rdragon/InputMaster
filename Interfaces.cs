using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using InputMaster.Parsers;

namespace InputMaster
{
  internal interface IFactory
  {
    T Create<T>() where T : class;
  }

  internal interface INotifier
  {
    void Write(string message);
    void WriteWarning(string message);
    void WriteError(string message);
    void SetPersistentText(string text);
    string GetLog();
    void CaptureForeground();
    ISynchronizeInvoke SynchronizingObject { get; }
  }

  internal interface IInputHook
  {
    void Handle(InputArgs e);
    void Reset();
    string GetStateInfo();
  }

  internal interface IComboHook
  {
    void Handle(ComboArgs e);
    void Reset();
    string GetTestStateInfo();
    bool Active { get; }
  }

  internal interface IInjectorStream<out T>
  {
    T Add(Input input, bool down);
    T Add(char c);
  }

  internal interface IInjectorStream : IInjectorStream<IInjectorStream> { }

  internal interface IInjector<out T> : IInjectorStream<T>
  {
    void Run();
    Action Compile();
    T CreateInjector();
  }

  internal interface IInjector : IInjector<IInjector> { }

  internal interface IForegroundListener
  {
    string ForegroundWindowTitle { get; }
    string ForegroundProcessName { get; }
    void Update();
  }

  internal interface IFlagManager
  {
    bool IsSet(string flag);
    void ToggleFlag(string flag);
    void ClearFlags();
    event Action FlagsChanged;
    IEnumerable<string> GetFlags();
  }

  internal interface IParser
  {
    void UpdateHotkeyFile(HotkeyFile hotkeyFile);
    void UpdateParseAction(string name, ParseAction action);
    void Run();
    bool TryGetAction(string name, bool complainIfNotFound, out Action<IInjectorStream<object>> action);
    void FireNewParserOutput(ParserOutput parserOutput); // For unit tests.
    bool Enabled { get; set; }
    event Action<ParserOutput> NewParserOutput;
  }

  internal interface IScheduler
  {
    void AddJob(string name, Action action, TimeSpan delay);
    void AddFileJob(FileInfo file, string arguments, TimeSpan delay);
  }

  internal interface IProcessManager
  {
    void StartHiddenProcess(FileInfo file, string arguments = "", TimeSpan? timeout = null);
  }

  internal interface ICommandCollection
  {
    void AddActor(object actor);
    Command GetCommand(LocatedString locatedName);
  }

  internal interface IApp
  {
    event Action Exiting;
  }
}

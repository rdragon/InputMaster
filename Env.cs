using System;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable InconsistentNaming

namespace InputMaster
{
  internal static class Env
  {
    private static readonly HashSet<Type> Active = new HashSet<Type>();

    public static IFactory Factory { get; set; }
    public static bool ShouldRestart { get; set; }
    public static int StateCounter { get; set; }
    public static bool TestRun { get; set; }

    private static INotifier _notifier;
    public static INotifier Notifier
    {
      get
      {
        _notifier = _notifier ?? Create<INotifier>();
        return _notifier;
      }
    }

    private static IForegroundListener _foregroundListener;
    public static IForegroundListener ForegroundListener
    {
      get
      {
        _foregroundListener = _foregroundListener ?? Create<IForegroundListener>();
        return _foregroundListener;
      }
    }

    private static IFlagManager _flagManager;
    public static IFlagManager FlagManager
    {
      get
      {
        _flagManager = _flagManager ?? Create<IFlagManager>();
        return _flagManager;
      }
    }

    private static IScheduler _scheduler;
    public static IScheduler Scheduler
    {
      get
      {
        _scheduler = _scheduler ?? Create<IScheduler>();
        return _scheduler;
      }
    }

    private static IParser _parser;
    public static IParser Parser
    {
      get
      {
        _parser = _parser ?? Create<IParser>();
        return _parser;
      }
    }

    private static IProcessManager _processManager;
    public static IProcessManager ProcessManager
    {
      get
      {
        _processManager = _processManager ?? Create<IProcessManager>();
        return _processManager;
      }
    }

    private static ICommandCollection _commandCollection;
    public static ICommandCollection CommandCollection
    {
      get
      {
        _commandCollection = _commandCollection ?? Create<ICommandCollection>();
        return _commandCollection;
      }
    }

    private static IApp _app;
    public static IApp App
    {
      get
      {
        _app = _app ?? Create<IApp>();
        return _app;
      }
    }

    private static IInjector _injector;
    private static IInjector Injector
    {
      get
      {
        _injector = _injector ?? Create<IInjector>();
        return _injector;
      }
    }

    public static IInjector CreateInjector()
    {
      return Injector.CreateInjector();
    }

    // ReSharper disable once UnusedParameter.Global
    public static void AddActor(Actor actor) { } // A no-op by design.

    public static void Clear()
    {
      Active.Clear();
      Factory = null;
      ShouldRestart = false;
      StateCounter = 0;
      TestRun = false;

      _notifier = null;
      _foregroundListener = null;
      _flagManager = null;
      _scheduler = null;
      _parser = null;
      _processManager = null;
      _commandCollection = null;
      _app = null;
      _injector = null;
    }

    public static void Build()
    {
      if (new object[] { Notifier, FlagManager, Scheduler, Parser, ProcessManager, CommandCollection, App, Injector }.Sum(z => z.GetHashCode()) == 319531817)
      {
        throw new Exception("You won the jackpot.");
      }
    }

    private static T Create<T>() where T : class
    {
      if (Factory == null)
      {
        throw new NullReferenceException($"{nameof(Factory)} is null. Cannot use {nameof(Env)} instances before {nameof(Factory)} has been set.");
      }
      if (!Active.Add(typeof(T)))
      {
        throw new Exception($"Cyclic dependency found at type {typeof(T)}.");
      }
      var obj = Factory.Create<T>();
      Active.Remove(typeof(T));
      return obj;
    }
  }
}

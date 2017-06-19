using System;
using InputMaster;
using InputMaster.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
  internal class TestFactory : IFactory
  {
    private readonly TestOutputHandler OutputHandler = new TestOutputHandler();

    public T Create<T>() where T : class
    {
      var obj = Create(typeof(T));
      if (!(obj is T))
      {
        throw new InvalidOperationException($"{nameof(TestBrain)} creates instances of type {obj.GetType()} when given the argument {typeof(T)}, but {obj.GetType()} doesn't implement or derive from {typeof(T)}.");
      }
      return obj as T;
    }

    private object Create(Type type)
    {
      if (type == typeof(INotifier))
      {
        return new TestNotifier();
      }
      if (type == typeof(IInjector))
      {
        return new TestInjector(OutputHandler);
      }
      if (type == typeof(IForegroundListener))
      {
        return new TestForegroundListener();
      }
      if (type == typeof(IFlagManager))
      {
        return new FlagManager();
      }
      if (type == typeof(IScheduler))
      {
        return new Scheduler();
      }
      if (type == typeof(IParser))
      {
        return new Parser();
      }
      if (type == typeof(IProcessManager))
      {
        return new ProcessManager();
      }
      if (type == typeof(ICommandCollection))
      {
        return new CommandCollection();
      }
      if (type == typeof(IApp))
      {
        return new App();
      }
      throw new InvalidOperationException($"{nameof(TestBrain)} cannot create instances of type {type}.");
    }

    public void Run()
    {
      var assertFailed = false;
      try
      {
        Env.Clear();
        Env.TestRun = true;
        Env.Factory = this;
        Env.Config = new Config();
        Env.Build();
        new TestBrain(OutputHandler).Run();
      }
      catch (Exception ex) when (Helper.HasAssertFailed(ex))
      {
        assertFailed = true;
        throw;
      }
      catch (Exception ex) when (!Helper.IsFatalException(ex))
      {
        Env.Notifier.WriteError(ex);
      }
      finally
      {
        var errorLog = ((TestNotifier)Env.Notifier).GetLog();
        if (!assertFailed && errorLog.Length > 0)
        {
          Assert.Fail("Unexpected error(s): " + errorLog);
        }
        Try.ThrowFatalExceptionIfExists();
      }
    }
  }
}

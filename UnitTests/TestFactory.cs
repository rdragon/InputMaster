using System;
using InputMaster;
using InputMaster.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using InputMaster.Instances;
using Moq;
using InputMaster.Hooks;
using System.Security.Cryptography;

namespace UnitTests
{
  public class TestFactory
  {
    private readonly TestOutputHandler _outputHandler = new TestOutputHandler();

    public void Run()
    {
      var assertFailed = false;
      try
      {
        Env.Config = new Config();
        Env.Clear();
        Env.RunningUnitTests = true;
        Env.Notifier = new TestNotifier();
        Env.App = new App();
        Env.RandomNumberGenerator = new Mock<RandomNumberGenerator>().Object;
        Env.CommandCollection = new CommandCollection();
        Env.Cipher = new Mock<ICipher>().Object;
        Env.StateHandlerFactory = new TestStateHandlerFactory();
        Env.Settings = new Settings();
        Env.Parser = new Parser();
        Env.ModeHook = new ModeHook();
        Env.ForegroundListener = new TestForegroundListener();
        Env.FlagManager = FlagManager.GetFlagManagerAsync().Result;
        Env.Scheduler = Scheduler.GetSchedulerAsync().Result;
        Env.ProcessManager = new ProcessManager();
        Env.Injector = new TestInjector(_outputHandler);
        Env.PasswordMatrix = new PasswordMatrix(InputMaster.Properties.Resources.PasswordMatrix6x5);
        Env.AccountManager = new Mock<AccountManager>().Object;
        new TestBrain(_outputHandler).Run();
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
          Assert.Fail("Unexpected error(s): " + errorLog);
      }
    }
  }
}

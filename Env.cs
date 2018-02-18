using InputMaster.Hooks;
using System.Security.Cryptography;

namespace InputMaster
{
  public static class Env
  {
    public static bool ShouldRestart { get; set; }
    public static string RestartArguments { get; set; }
    public static int StateCounter { get; set; }
    public static bool RunningUnitTests { get; set; }
    /// <summary>
    /// Thread-safety is required for the type of this property.
    /// </summary>
    public static Config Config { get; set; }
    public static INotifier Notifier { get; set; }
    /// <summary>
    /// Thread-safety is required for the type of this property.
    /// </summary>
    public static ICipher Cipher { get; set; }
    public static Settings Settings { get; set; }
    public static IApp App { get; set; }
    public static ModeHook ModeHook { get; set; }
    public static IForegroundListener ForegroundListener { get; set; }
    public static IStateHandlerFactory StateHandlerFactory { get; set; }
    public static IFlagManager FlagManager { get; set; }
    public static IScheduler Scheduler { get; set; }
    public static IParser Parser { get; set; }
    public static IProcessManager ProcessManager { get; set; }
    public static ICommandCollection CommandCollection { get; set; }
    public static IInjector Injector { get; set; }
    /// <summary>
    /// Thread-safety is required for the type of this property.
    /// </summary>
    public static RandomNumberGenerator RandomNumberGenerator { get; set; }
    public static PasswordMatrix PasswordMatrix { get; set; }
    public static AccountManager AccountManager { get; set; }

    public static IInjector CreateInjector()
    {
      return Injector.CreateInjector();
    }

    public static void AddActor(Actor actor) { } // A no-op by design.

    /// <summary>
    /// Resets everyting except Config.
    /// </summary>
    public static void Clear()
    {
      ShouldRestart = false;
      StateCounter = 0;
      RunningUnitTests = false;
      Notifier = null;
      ForegroundListener = null;
      StateHandlerFactory = null;
      FlagManager = null;
      Scheduler = null;
      Parser = null;
      ProcessManager = null;
      CommandCollection = null;
      App = null;
      Cipher = null;
      Injector = null;
      RandomNumberGenerator = null;
      PasswordMatrix = null;
      Settings = null;
      AccountManager = null;
      ModeHook = null;
    }
  }
}

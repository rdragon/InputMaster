using System.IO;
using InputMaster.Hooks;
using InputMaster.Parsers;
using InputMaster.TextEditor;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;

namespace InputMaster.Instances
{
  public class Factory
  {
    public TextEditorForm TextEditorForm { get; private set; }
    private readonly INotifier _notifier;

    public Factory(INotifier notifier)
    {
      _notifier = notifier;
    }

    public async Task Run()
    {
      Env.Clear();
      Env.Notifier = _notifier;
      Env.App = new App();
      Env.RandomNumberGenerator = GetRandomNumberGenerator();
      Env.CommandCollection = new CommandCollection();
      Env.Cipher = new Cipher(await Env.Config.GetKeyAsync());
      Env.StateHandlerFactory = new JsonStateHandlerFactory();
      Env.Settings = await GetSettingsAsync();
      Env.Parser = new Parser();
      Env.ModeHook = new ModeHook();
      Env.ForegroundListener = new ForegroundListener();
      Env.FlagManager = await FlagManager.GetFlagManagerAsync();
      Env.Scheduler = await Scheduler.GetSchedulerAsync();
      Env.ProcessManager = new ProcessManager();
      Env.Injector = new Injector();
      Env.PasswordMatrix = await PasswordMatrix.GetPasswordMatrixAsync();
      Env.AccountManager = await AccountManager.GetAccountManagerAsync();
      var comboHook = new ComboHook();
      var comboRelay = new ComboRelay(Env.ModeHook, comboHook);
      var inputHook = new InputHook(comboRelay);
      var inputRelay = new InputRelay(inputHook);
      var primaryHook = new PrimaryHook(inputRelay);
      var fileManager = await FileManager.GetFileManagerAsync();
      TextEditorForm = new TextEditorForm(fileManager);
      await Env.Config.Run();
      Env.App.TriggerRun();
    }

    private static async Task<Settings> GetSettingsAsync()
    {
      var stateHandler = Env.StateHandlerFactory.Create(new Settings(), nameof(Settings),
        StateHandlerFlags.UseCipher | StateHandlerFlags.UserEditable | StateHandlerFlags.Exportable);
      return await stateHandler.LoadAndSaveAsync();
    }

    private static RandomNumberGenerator GetRandomNumberGenerator()
    {
      var randomNumberGenerator = new RNGCryptoServiceProvider();
      Env.App.Exiting += randomNumberGenerator.Dispose;
      return randomNumberGenerator;
    }
  }
}

using System;
using System.IO;
using System.Windows.Forms;
using InputMaster.Forms;
using InputMaster.Hooks;
using InputMaster.Parsers;
using InputMaster.Properties;
using InputMaster.TextEditor;

namespace InputMaster
{
  internal class Factory : IFactory
  {
    private readonly NotifyForm NotifyForm;

    public Factory(NotifyForm notifyForm)
    {
      Helper.ForbidNull(notifyForm, nameof(notifyForm));
      NotifyForm = notifyForm;
    }

    public TextEditorForm TextEditorForm { get; private set; }
    public AccountManager AccountManager { get; private set; }
    public SharedFileManager SharedFileManager { get; private set; }
    public FileManager FileManager { get; private set; }

    public T Create<T>() where T : class
    {
      var obj = Create(typeof(T));
      if (!(obj is T))
      {
        throw new InvalidOperationException($"{nameof(Factory)} creates instances of type {obj.GetType()} when given the argument {typeof(T)}, but {obj.GetType()} doesn't implement or derive from {typeof(T)}.");
      }
      return obj as T;
    }

    private object Create(Type type)
    {
      if (type == typeof(INotifier))
      {
        return NotifyForm;
      }
      if (type == typeof(IInjector))
      {
        return new Injector();
      }
      if (type == typeof(IForegroundListener))
      {
        return new ForegroundListener();
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
      if (type == typeof(ICipher))
      {
        return new Cipher();
      }
      throw new InvalidOperationException($"{nameof(Factory)} cannot create instances of type {type}.");
    }

    public void Run()
    {
      Directory.CreateDirectory(Env.Config.DataDir);
      Directory.CreateDirectory(Env.Config.CacheDir);
      Directory.CreateDirectory(Env.Config.SharedDir);
      if (!File.Exists(Env.Config.HotkeyFile))
      {
        File.WriteAllText(Env.Config.HotkeyFile, "");
      }
      Env.Clear();
      Env.Factory = this;
      Env.Build();
      var modeHook = new ModeHook();
      var comboHook = new ComboHook();
      var comboRelay = new ComboRelay(modeHook, comboHook);
      var inputHook = new InputHook(comboRelay);
      var inputRelay = new InputRelay(inputHook);
      var primaryHook = new PrimaryHook(inputRelay);
      FileManager = new FileManager(out var accountFileProvider, out var sharedPasswordProvider, out var sharedFileProvider);
      AccountManager = new AccountManager(modeHook, accountFileProvider);
      SharedFileManager = new SharedFileManager(sharedFileProvider, sharedPasswordProvider);
      TextEditorForm = new TextEditorForm(modeHook, FileManager);
      TextEditorForm.StartAsync();
      CreateNotifyIcon();
      Env.Config.Run();
      Env.Parser.EnableOnce();
      primaryHook.Register();
    }

    private static void CreateNotifyIcon()
    {
      var notifyIcon = new NotifyIcon
      {
        Icon = Resources.NotifyIcon,
        Text = "InputMaster",
        Visible = true
      };
      notifyIcon.MouseClick += (s, e) => Application.Exit();
      Env.App.Exiting += notifyIcon.Dispose;
    }
  }
}

using InputMaster.Forms;
using InputMaster.Hooks;
using InputMaster.Parsers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace InputMaster
{
  class Brain
  {
    private readonly List<IDisposable> Disposables = new List<IDisposable>();
    private bool StartCalled;
    private bool ExitCalled;

    public event Action Exiting = delegate { };

    public void Start()
    {
      if (StartCalled)
      {
        throw new InvalidOperationException();
      }
      else
      {
        StartCalled = true;
      }

      Config.DataDir.Create();
      Config.CacheDir.Create();

      var notifier = new Notifier(this);
      Env.Notifier = notifier;
      Env.SynchronizingObject = notifier.SynchronizingObject;
      Env.InjectorPrototype = new Injector();
      notifier.RequestingExit += Exit;
      Disposables.Add(notifier);
      File.WriteAllText(Config.WindowHandleFile.FullName, notifier.WindowHandle.ToString());
      if (!Config.HotkeyFile.Exists)
      {
        File.WriteAllText(Config.HotkeyFile.FullName, "");
      }

      var commandCollection = new CommandCollection();
      var hotkeyFileWatcher = new FileChangedWatcher(Config.HotkeyFile);
      Disposables.Add(hotkeyFileWatcher);
      var flagManager = new FlagManager(this);
      var parser = new Parser(commandCollection, hotkeyFileWatcher);
      var foregroundListener = new ForegroundListener(flagManager, parser);
      Env.ForegroundListener = foregroundListener;
      var modeHook = new ModeHook(parser);
      var comboHook = new ComboHook(parser);
      var comboRelay = new ComboRelay(modeHook, comboHook);
      var inputHook = new InputHook(comboRelay);
      var inputRelay = new InputRelay(inputHook);
      var primaryHook = new PrimaryHook(this, inputRelay);
      Disposables.Add(primaryHook);
      var foregroundInteractor = new ForegroundInteractor();
      var actor = new Actor();

      TextEditorForm textEditorForm = null;
      if (Config.EnableTextEditor)
      {
        textEditorForm = new TextEditorForm(this, modeHook, parser);
        Disposables.Add(textEditorForm);
        commandCollection.AddActors(textEditorForm);
      }

      commandCollection.AddActors(this, notifier, commandCollection, foregroundInteractor, flagManager, foregroundListener, parser, primaryHook, inputRelay, inputHook, comboRelay, comboHook, modeHook, actor);
      var instances = new InstanceCollection(this, notifier, commandCollection, foregroundInteractor, flagManager, foregroundListener, parser, textEditorForm, primaryHook, inputRelay, inputHook, comboRelay, comboHook, modeHook, actor);

      Config.Initialize(instances);
      CreateNotifyIcon();

      // Initialization finished. Run some final commands before starting main loop.
      Config.Start(instances);
      hotkeyFileWatcher.Enable(notifier.SynchronizingObject);
      hotkeyFileWatcher.RaiseChangedEvent();
      Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
      textEditorForm?.Start();
      primaryHook.Register();
      Application.Run();
    }

    public void AddDisposable(IDisposable disposable)
    {
      Disposables.Add(disposable);
    }

    private void CreateNotifyIcon()
    {
      var notifyIcon = new NotifyIcon
      {
        Icon = Properties.Resources.NotifyIcon,
        Text = "InputMaster",
        Visible = true
      };
      notifyIcon.MouseClick += (s, e) => { Exit(); };
      Disposables.Add(notifyIcon);
    }

    public void Exit()
    {
      if (!ExitCalled)
      {
        ExitCalled = true;
        Exiting();
        foreach (var disposable in Disposables)
        {
          disposable.Dispose();
        }
        Application.Exit();
      }
    }
  }
}

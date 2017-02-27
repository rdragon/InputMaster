using InputMaster.Forms;
using InputMaster.Hooks;
using InputMaster.Parsers;

namespace InputMaster
{
  class InstanceCollection
  {
    public Brain Brain { get; }
    public Notifier Notifier { get; }
    public CommandCollection CommandCollection { get; }
    public ForegroundInteractor ForegroundInteractor { get; }
    public FlagManager FlagManager { get; }
    public ForegroundListener ForegroundListener { get; }
    public Parser Parser { get; }
    public TextEditorForm TextEditorForm { get; }
    public PrimaryHook PrimaryHook { get; }
    public InputRelay InputRelay { get; }
    public InputHook InputHook { get; }
    public ComboRelay ComboRelay { get; }
    public ComboHook ComboHook { get; }
    public ModeHook ModeHook { get; }
    public Actor Actor { get; }

    public InstanceCollection(Brain brain, Notifier notifier, CommandCollection commandCollection, ForegroundInteractor foregroundInteractor, FlagManager flagManager, ForegroundListener foregroundListener, Parser parser, TextEditorForm textEditorForm, PrimaryHook primaryHook, InputRelay inputRelay, InputHook inputHook, ComboRelay comboRelay, ComboHook comboHook, ModeHook modeHook, Actor actor)
    {
      Brain = brain;
      Notifier = notifier;
      CommandCollection = commandCollection;
      ForegroundInteractor = foregroundInteractor;
      FlagManager = flagManager;
      ForegroundListener = foregroundListener;
      Parser = parser;
      TextEditorForm = textEditorForm;
      PrimaryHook = primaryHook;
      InputRelay = inputRelay;
      InputHook = inputHook;
      ComboRelay = comboRelay;
      ComboHook = comboHook;
      ModeHook = modeHook;
      Actor = actor;
    }
  }
}

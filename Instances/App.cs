using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;

namespace InputMaster.Instances
{
  public class App : IApp
  {
    private readonly List<Func<Task>> _saveActions = new List<Func<Task>>();
    private readonly List<Func<Task>> _exitActions = new List<Func<Task>>();
    private bool _saving;
    private bool _saveAgain;

    public App()
    {
      var saveTimer = new Timer
      {
        Interval = (int)Env.Config.SaveTimerInterval.TotalMilliseconds
      };
      Run += () => saveTimer.Enabled = true;
      saveTimer.Tick += async (s, e) => await TriggerSaveAsync();
      Exiting += saveTimer.Dispose;
    }

    public event Action Run = delegate { };
    public event Action Save = delegate { };
    public event Action Unhook = delegate { };
    public event Action Exiting = delegate { };

    public void AddSaveAction(Func<Task> action)
    {
      _saveActions.Add(Try.Wrap(action));
    }

    public bool RemoveSaveAction(Func<Task> action)
    {
      return _saveActions.Remove(action);
    }

    public void AddExitAction(Func<Task> action)
    {
      _exitActions.Add(Try.Wrap(action));
    }

    public void TriggerRun()
    {
      Run();
    }

    public void TriggerUnhook()
    {
      Unhook();
    }

    public async Task TriggerSaveAsync()
    {
      if (_saving)
      {
        _saveAgain = true;
        return;
      }
      try
      {
        _saving = true;
        while (true)
        {
          _saveAgain = false;
          Save();
          await Task.WhenAll(_saveActions.Select(z => z()));
          if (!_saveAgain)
            break;
        }
      }
      finally
      {
        _saving = false;
      }
    }

    public Task TriggerExitAsync()
    {
      Unhook();
      Exiting();
      return Task.WhenAll(_exitActions.Select(z => z()));
    }
  }
}

using System;
using System.Windows.Forms;

namespace InputMaster
{
  internal class App : IApp
  {
    public App()
    {
      Application.ApplicationExit += OnExit;
      var saveTimer = new Timer
      {
        Interval = (int)Env.Config.SaveTimerInterval.TotalMilliseconds,
        Enabled = true
      };
      saveTimer.Tick += (s, e) => SaveTick();
      Exiting += saveTimer.Dispose;
    }

    public event Action Exiting = delegate { };
    public event Action SaveTick = delegate { };

    private void OnExit(object sender, EventArgs e)
    {
      Exiting();
      Application.ApplicationExit -= OnExit;
    }
  }
}

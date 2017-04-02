using System;
using System.Windows.Forms;

namespace InputMaster
{
  /// <summary>
  /// A <see cref="System.Windows.Forms.Timer"/> that only fires once.
  /// </summary>
  public class Timeout : IDisposable
  {
    private readonly Timer Timer = new Timer();
    /// <summary>
    /// To make sure <see cref="Elapsed"/> is only fired once.
    /// </summary>
    private bool Fired;

    public Timeout()
    {
      Timer.Tick += (s, e) =>
      {
        // Perhaps this line is enough to prevent the elapsed event from firing multiple times, but how can we be sure?
        Timer.Stop();

        if (!Fired)
        {
          Fired = true;
          Elapsed();
        }
      };
    }

    public bool IsRunning => Timer.Enabled;

    public event Action Elapsed = delegate { };

    public void Start(TimeSpan delay)
    {
      Timer.Stop();
      Timer.Interval = Math.Max(1, (int)Math.Round(delay.TotalMilliseconds));
      Fired = false;
      Timer.Start();
    }

    public void Stop()
    {
      Timer.Stop();
    }

    public void Shortcut()
    {
      if (IsRunning)
      {
        Timer.Stop();
        if (!Fired)
        {
          Fired = true;
          Elapsed();
        }
      }
    }

    public void Dispose()
    {
      Timer.Dispose();
    }
  }
}

using InputMaster.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InputMaster.Actors
{
  public class CustomNotifyIcon : Actor
  {
    private readonly NotifyIcon _notifyIcon;
    private readonly Timer _timer = new Timer { Interval = (int)Env.Config.SchedulerInterval.TotalMilliseconds };
    private readonly Icon _icon;
    private bool _alertIsShown;
    private MyState _state;

    private CustomNotifyIcon()
    {
      _icon = Program.ReadOnly ? Resources.AlertIcon : Resources.NotifyIcon;
      _notifyIcon = new NotifyIcon
      {
        Icon = _icon,
        Text = "InputMaster",
        Visible = true
      };
      _notifyIcon.MouseClick += (s, e) =>
      {
        if (_alertIsShown)
          HideAlert();
        else
          Application.Exit();
      };
      Env.App.Exiting += _notifyIcon.Dispose;
      Env.App.Exiting += _timer.Dispose;
      if (!Program.ReadOnly)
        Env.App.Run += () => _timer.Enabled = true;
      _timer.Tick += (s, e) =>
      {
        while (_state.Dict.Any() && _state.Dict.Keys.First() < DateTime.Now)
        {
          var key = _state.Dict.Keys.First();
          var value = _state.Dict[key];
          _state.Dict.Remove(key);
          ShowAlert(value);
        }
      };
    }

    private async Task<CustomNotifyIcon> Initialize()
    {
      var stateHandler = Env.StateHandlerFactory.Create(new MyState(), nameof(CustomNotifyIcon), StateHandlerFlags.SavePeriodically);
      _state = await stateHandler.LoadAsync();
      return this;
    }

    public static Task<CustomNotifyIcon> GetCustomNotifyIcon()
    {
      return new CustomNotifyIcon().Initialize();
    }

    [Command]
    private async Task TimerAsync(int defaultValue, [ValidFlags("mp")]string flags)
    {
      if (Program.ReadOnly)
        throw new ArgumentException("Not possible when read only is true.");
      var isMinutes = flags.Contains('m');
      var showPopup = flags.Contains('p');
      var units = isMinutes ? "minutes" : "seconds";
      var number = defaultValue;
      var input = defaultValue.ToString();
      if (showPopup)
      {
        input = await Helper.TryGetStringAsync($"Number of {units}", defaultValue.ToString());
        if (input == null)
          return;
        number = int.Parse(input);
      }
      var seconds = isMinutes ? number * 60 : number;
      AddToDict(DateTime.Now.AddSeconds(seconds), $"{input} {units} are over!!!".ToUpperInvariant());
    }

    private void AddToDict(DateTime date, string text)
    {
      while (_state.Dict.ContainsKey(date))
        date = date.AddTicks(1);
      _state.Dict.Add(date, text);
    }

    [Command]
    private void StopAllTimers()
    {
      _state.Dict.Clear();
      Env.Notifier.Info("Stopped all timers.");
    }

    private void ShowAlert(string text)
    {
      _notifyIcon.Icon = Resources.AlertIcon;
      _alertIsShown = true;
      var len = 500;
      var sb = new StringBuilder(len + text.Length);
      while (sb.Length < len)
        sb.Append(text + " ");
      var str = sb.ToString() + "\n" + sb.ToString() + "\n" + sb.ToString();
      Env.Notifier.Info(str);
    }

    private void HideAlert()
    {
      _notifyIcon.Icon = _icon;
      _alertIsShown = false;
    }

    public class MyState : IState
    {
      public SortedDictionary<DateTime, string> Dict { get; set; }

      public (bool, string message) Fix()
      {
        Dict = Dict ?? new SortedDictionary<DateTime, string>();
        return (true, "");
      }
    }
  }
}

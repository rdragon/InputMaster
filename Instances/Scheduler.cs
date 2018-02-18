using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InputMaster.Instances
{
  public class Scheduler : IScheduler
  {
    private readonly SortedDictionary<DateTime, Job> _jobs = new SortedDictionary<DateTime, Job>();
    private readonly Timer _timer = new Timer { Interval = (int)Env.Config.SchedulerInterval.TotalMilliseconds };
    private MyState _state;

    private Scheduler()
    {
      Env.App.Run += () => _timer.Enabled = true;
      Env.App.Exiting += _timer.Dispose;
      _timer.Tick += async (s, e) =>
      {
        while (_jobs.Count > 0 && _jobs.Keys.First() < DateTime.Now)
        {
          var job = _jobs.Values.First();
          _jobs.Remove(_jobs.Keys.First());
          var success = await job.Run();
          var virtualRunDate = job.RetryDelay.HasValue && !success ? DateTime.Now.Subtract(job.Delay).Add(job.RetryDelay.Value) :
            DateTime.Now;
          AddToJobs(virtualRunDate.Add(job.Delay), job);
          _state.LastRuns[job.Name] = virtualRunDate;
        }
      };
    }

    private async Task<Scheduler> Initialize()
    {
      var stateHandler = Env.StateHandlerFactory.Create(new MyState(), Path.Combine(Env.Config.CacheDir, nameof(Scheduler)),
        StateHandlerFlags.SavePeriodically);
      _state = await stateHandler.LoadAsync();
      return this;
    }

    public static Task<Scheduler> GetSchedulerAsync()
    {
      return new Scheduler().Initialize();
    }

    public void AddJob(string name, Action action, TimeSpan delay, TimeSpan? retryDelay = null)
    {
      AddJob(name, action, null, delay, retryDelay);
    }

    public void AddJob(string name, Func<Task> action, TimeSpan delay, TimeSpan? retryDelay = null)
    {
      AddJob(name, null, action, delay, retryDelay);
    }

    private void AddJob(string name, Action action, Func<Task> function, TimeSpan delay, TimeSpan? retryDelay)
    {
      Helper.RequireInInterval(delay, nameof(delay), Env.Config.SchedulerInterval, TimeSpan.MaxValue);
      if (retryDelay.HasValue)
        Helper.RequireInInterval(retryDelay.Value, nameof(retryDelay), Env.Config.SchedulerInterval, TimeSpan.MaxValue);
      if (_jobs.Any(z => z.Value.Name == name))
        throw new ArgumentException($"A job with name '{name}' already exists.", nameof(name));
      if (!_state.LastRuns.TryGetValue(name, out var date))
      {
        date = DateTime.Now;
        _state.LastRuns[name] = date;
      }
      AddToJobs(date.Add(delay), new Job(name, action, function, delay, retryDelay));
    }

    public void AddFileJob(string file, string arguments, TimeSpan delay)
    {
      var taskName = file;
      if (arguments.Length > 0)
        taskName = $"\"{taskName}\" {arguments}";
      AddJob(taskName, () =>
      {
        Env.ProcessManager.StartHiddenProcess(file, arguments);
      }, delay);
    }

    private void AddToJobs(DateTime date, Job job)
    {
      while (_jobs.ContainsKey(date))
        date = date.AddTicks(1);
      _jobs.Add(date, job);
    }

    private class Job
    {
      private readonly Action Action;
      private readonly Func<Task> Function;

      public string Name { get; }
      public TimeSpan Delay { get; }
      public TimeSpan? RetryDelay { get; }

      public Job(string name, Action action, Func<Task> function, TimeSpan delay, TimeSpan? retryDelay)
      {
        Name = name;
        Action = action;
        Function = function;
        Delay = delay;
        RetryDelay = retryDelay;
      }

      public async Task<bool> Run()
      {
        try
        {
          if (Function != null)
            await Function();
          else
            Action();
          return true;
        }
        catch (Exception ex) when (!Helper.IsFatalException(ex))
        {
          Env.Notifier.WriteError(ex, "Failed to complete a scheduled job" + Helper.GetBindingsSuffix(nameof(Name), Name));
          return false;
        }
      }
    }

    public class MyState : IState
    {
      public Dictionary<string, DateTime> LastRuns { get; set; }

      public (bool, string message) Fix()
      {
        LastRuns = LastRuns ?? new Dictionary<string, DateTime>();
        return (true, "");
      }
    }
  }
}

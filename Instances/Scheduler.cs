using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace InputMaster.Instances
{
  internal class Scheduler : IScheduler
  {
    private readonly Dictionary<string, DateTime> LastRuns = new Dictionary<string, DateTime>();
    private readonly SortedDictionary<DateTime, Job> Jobs = new SortedDictionary<DateTime, Job>();
    private readonly Timer Timer = new Timer { Interval = (int)Env.Config.SchedulerInterval.TotalMilliseconds, Enabled = true };
    private MyState State;

    public Scheduler()
    {
      Env.App.Exiting += Timer.Dispose;
      var stateLoaded = false;
      Timer.Tick += (s, e) =>
      {
        if (!stateLoaded)
        {
          stateLoaded = true;
          // We cannot load the state inside the constructor as that would create a circular dependency.
          State = new MyState(this);
          State.Load();
        }
        while (Jobs.Count > 0 && Jobs.Keys.First() < DateTime.Now)
        {
          var job = Jobs.Values.First();
          Jobs.Remove(Jobs.Keys.First());
          AddToJobs(DateTime.Now.Add(job.Delay), job);
          LastRuns[job.Name] = DateTime.Now;
          if (State != null)
          {
            State.Changed = true;
          }
          job.Run();
        }
      };
    }

    public void AddJob(string name, Action action, TimeSpan delay)
    {
      Helper.RequireInInterval(delay, nameof(delay), Env.Config.SchedulerInterval, TimeSpan.MaxValue);
      if (Jobs.Any(z => z.Value.Name == name))
      {
        throw new ArgumentException($"A job with name '{name}' already exists.", nameof(name));
      }
      if (!LastRuns.TryGetValue(name, out var date))
      {
        date = DateTime.Now;
        LastRuns[name] = date;
        if (State != null)
        {
          State.Changed = true;
        }
      }
      AddToJobs(date.Add(delay), new Job(name, action, delay));
    }

    public void AddFileJob(string file, string arguments, TimeSpan delay)
    {
      var taskName = file;
      if (arguments.Length > 0)
      {
        taskName = $"\"{taskName}\" {arguments}";
      }
      AddJob(taskName, () =>
      {
        Env.ProcessManager.StartHiddenProcess(file, arguments);
      }, delay);
    }

    private void AddToJobs(DateTime date, Job job)
    {
      while (Jobs.ContainsKey(date))
      {
        date = date.AddTicks(1);
      }
      Jobs.Add(date, job);
    }

    private class Job
    {
      private readonly Action Action;

      public string Name { get; }
      public TimeSpan Delay { get; }

      public Job(string name, Action action, TimeSpan delay)
      {
        Name = name;
        Action = action;
        Delay = delay;
      }

      public void Run()
      {
        try
        {
          Action();
        }
        catch (Exception ex) when (!Helper.IsFatalException(ex))
        {
          Env.Notifier.WriteError(ex, "Failed to complete a scheduled job" + Helper.GetBindingsSuffix(nameof(Name), Name));
        }
      }
    }

    private class MyState : State<Scheduler>
    {
      public MyState(Scheduler scheduler) : base(nameof(Scheduler), scheduler) { }

      protected override void Save(BinaryWriter writer)
      {
        writer.Write(Parent.Jobs.Count);
        foreach (var job in Parent.Jobs.Values)
        {
          writer.Write(job.Name);
          writer.Write(Parent.LastRuns[job.Name].Ticks);
        }
      }

      protected override void Load(BinaryReader reader)
      {
        var count = reader.ReadInt32();
        for (var i = 0; i < count; i++)
        {
          var name = reader.ReadString();
          var ticks = reader.ReadInt64();
          if (Parent.LastRuns.ContainsKey(name))
          {
            Parent.LastRuns[name] = new DateTime(ticks);
          }
          else
          {
            Parent.LastRuns.Add(name, new DateTime(ticks));
          }
        }
      }
    }
  }
}

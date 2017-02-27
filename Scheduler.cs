using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace InputMaster
{
  class Scheduler : IDisposable
  {
    private readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>();
    private readonly MyState State;
    private readonly Timer Timer = new Timer { Interval = (int)Config.SchedulerInterval.TotalMilliseconds, Enabled = true };
    private readonly HiddenProcessManager ProcessManager;

    public Scheduler(Brain brain, HiddenProcessManager processManager)
    {
      ProcessManager = processManager;
      State = new MyState(this);
      State.Load();
      brain.Exiting += Try.Wrap(State.Save);
    }

    public void AddTask(string name, Action action, TimeSpan delay)
    {
      Entry entry;
      if (!Entries.TryGetValue(name, out entry))
      {
        entry = new Entry();
        Entries[name] = entry;
      }
      if (entry.Loaded)
      {
        throw new ArgumentException($"A task with name '{name}' already exists.", nameof(name));
      }
      else
      {
        entry.Loaded = true;
      }
      Timer.Tick += (s, e) =>
      {
        if (entry.LastSuccess.Add(delay) < DateTime.Now)
        {
          State.Changed = true;
          try
          {
            action();
            entry.LastSuccess = DateTime.Now;
          }
          catch (Exception ex) when (!Helper.IsCriticalException(ex))
          {
            Env.Notifier.WriteError(ex, Helper.GetUnhandledExceptionWarningMessage(suffix: "during execution of a task") + Helper.GetBindingsSuffix(nameof(name), name));
          }
        }
      };
    }

    public void AddFileTask(FileInfo file, string arguments, TimeSpan delay)
    {
      var taskName = file.FullName;
      if (arguments.Length > 0)
      {
        taskName = $"\"{taskName}\" {arguments}";
      }
      AddTask(taskName, () =>
      {
        ProcessManager.StartHiddenProcess(file, arguments);
      }, delay);
    }

    public void Dispose()
    {
      Timer.Dispose();
    }

    private class MyState : State
    {
      private Scheduler Parent;

      public MyState(Scheduler scheduler) : base(nameof(Scheduler))
      {
        Parent = scheduler;
      }

      protected override void Save(BinaryWriter writer)
      {
        writer.Write(Parent.Entries.Count);
        foreach (var pair in Parent.Entries)
        {
          writer.Write(pair.Key);
          pair.Value.Write(writer);
        }
      }

      protected override void Load(BinaryReader reader)
      {
        var n = reader.ReadInt32();
        for (int i = 0; i < n; i++)
        {
          var key = reader.ReadString();
          var entry = new Entry(reader);
          Parent.Entries.Add(key, entry);
        }
      }
    }

    private class Entry
    {
      public DateTime LastSuccess;

      public Entry()
      {
        LastSuccess = DateTime.MinValue;
      }

      public Entry(BinaryReader reader)
      {
        LastSuccess = new DateTime(reader.ReadInt64());
      }

      public bool Loaded { get; set; }

      public void Write(BinaryWriter writer)
      {
        writer.Write(LastSuccess.Ticks);
      }
    }
  }
}

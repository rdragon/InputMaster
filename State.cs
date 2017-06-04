using System;
using System.IO;

namespace InputMaster
{
  internal abstract class State<T>
  {
    protected readonly T Parent;
    private readonly FileInfo DataFile;

    protected State(string name, T parent) : this(name, parent, Config.CacheDir) { }

    protected State(string name, T parent, DirectoryInfo dir)
    {
      Helper.RequireValidFileName(name);
      Helper.ForbidNull(parent, nameof(parent));
      Parent = parent;
      if (Env.TestRun)
      {
        return;
      }
      DataFile = new FileInfo(Path.Combine(dir.FullName, name));
      Env.Scheduler.AddJob($"State<{name}>", Save, Config.SaveTimerInterval);
      Env.App.Exiting += Try.Wrap(Save);
    }

    public bool Changed { get; set; }

    public void Load()
    {
      if (Env.TestRun || !DataFile.Exists)
      {
        return;
      }
      using (var stream = DataFile.OpenRead())
      using (var reader = new BinaryReader(stream))
      {
        try
        {
          Load(reader);
        }
        catch (Exception ex) when (!Helper.IsCriticalException(ex))
        {
          Env.Notifier.WriteError(ex, $"Failed to load state data from '{DataFile}'.");
        }
      }
    }

    public void Save()
    {
      if (!Changed || Env.TestRun)
      {
        return;
      }
      using (var stream = File.Open(DataFile.FullName, FileMode.Create))
      using (var writer = new BinaryWriter(stream))
      {
        Save(writer);
        Changed = false;
      }
    }

    protected abstract void Load(BinaryReader reader);

    protected abstract void Save(BinaryWriter writer);
  }
}

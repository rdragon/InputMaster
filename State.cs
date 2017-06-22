using System;
using System.IO;

namespace InputMaster
{
  internal abstract class State<T>
  {
    protected readonly T Parent;
    private readonly string DataFile;

    protected State(string name, T parent) : this(name, parent, Env.Config.CacheDir) { }

    protected State(string name, T parent, string dir)
    {
      var filename = Helper.GetValidFileName(name, '_');
      Parent = parent;
      if (Env.TestRun)
      {
        return;
      }
      DataFile = Path.Combine(dir, filename);
      Env.App.SaveTick += Save;
      Env.App.Exiting += Try.Wrap(Save);
    }

    public bool Changed { private get; set; }

    public void Load()
    {
      if (Env.TestRun || !File.Exists(DataFile))
      {
        return;
      }
      using (var stream = File.OpenRead(DataFile))
      using (var reader = new BinaryReader(stream))
      {
        try
        {
          Load(reader);
        }
        catch (Exception ex) when (!Helper.IsFatalException(ex))
        {
          Env.Notifier.WriteError(ex, $"Failed to load state data from '{DataFile}'.");
        }
      }
    }

    private void Save()
    {
      if (!Changed || Env.TestRun)
      {
        return;
      }
      using (var stream = File.Open(DataFile, FileMode.Create))
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

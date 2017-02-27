using System;
using System.IO;

namespace InputMaster
{
  abstract class State
  {
    private readonly FileInfo File;
    private bool IsLoaded;

    public State(string name)
    {
      File = new FileInfo(Path.Combine(Config.CacheDir.FullName, name));
    }

    public bool Changed { get; set; }

    public void Load()
    {
      if (File.Exists)
      {
        using (var stream = File.OpenRead())
        {
          using (var reader = new BinaryReader(stream))
          {
            try
            {
              Load(reader);
            }
            catch (Exception ex) when (!Helper.IsCriticalException(ex))
            {
              Env.Notifier.WriteError(ex, $"Failed to load state data from '{File}'.");
            }
          }
        }
      }
      Fix();
      Changed = false;
      IsLoaded = true;
    }

    public void Save()
    {
      if (IsLoaded && Changed)
      {
        using (var stream = System.IO.File.Open(File.FullName, FileMode.Create))
        {
          using (var writer = new BinaryWriter(stream))
          {
            Save(writer);
            Changed = false;
          }
        }
      }
    }

    protected abstract void Load(BinaryReader reader);

    protected abstract void Save(BinaryWriter writer);

    protected virtual void Fix() { }
  }
}

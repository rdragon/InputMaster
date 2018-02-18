using System;
using System.IO;
using System.Threading.Tasks;

namespace InputMaster
{
  public class FileChangedWatcher : IDisposable
  {
    private readonly FileSystemWatcher _fileSystemWatcher;
    private readonly string _file;
    private string _previousContents;

    public FileChangedWatcher(string file)
    {
      _file = file;
      _fileSystemWatcher = new FileSystemWatcher(Path.GetDirectoryName(_file), Path.GetFileName(_file))
      {
        SynchronizingObject = Env.Notifier.SynchronizingObject,
        EnableRaisingEvents = true
      };
      _fileSystemWatcher.Changed += async (s, e) => await ChangedAsync();
    }

    public event Action<string> TextChanged = delegate { };

    public void Dispose()
    {
      _fileSystemWatcher.Dispose();
    }

    public Task RaiseChangedEventAsync()
    {
      return ChangedAsync();
    }

    private async Task ChangedAsync()
    {
      var text = await Helper.ReadAllTextAsync(_file);
      if (text != _previousContents)
      {
        _previousContents = text;
        TextChanged(text);
      }
    }
  }
}

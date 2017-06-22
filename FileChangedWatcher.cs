using System;
using System.IO;
using System.Threading.Tasks;

namespace InputMaster
{
  internal class FileChangedWatcher : IDisposable
  {
    private readonly FileSystemWatcher FileSystemWatcher;
    private readonly string File;
    private string PreviousContents;

    public FileChangedWatcher(string file)
    {
      File = file;
      FileSystemWatcher = new FileSystemWatcher(Path.GetDirectoryName(File), Path.GetFileName(File))
      {
        SynchronizingObject = Env.Notifier.SynchronizingObject,
        EnableRaisingEvents = true
      };
      FileSystemWatcher.Changed += async (s, e) => await ChangedAsync();
    }

    public event Action<string> TextChanged = delegate { };

    public void Dispose()
    {
      FileSystemWatcher.Dispose();
    }

    public async void RaiseChangedEventAsync()
    {
      await ChangedAsync();
    }

    private async Task ChangedAsync()
    {
      var text = await Helper.ReadAllTextAsync(File);
      if (text != PreviousContents)
      {
        PreviousContents = text;
        TextChanged(text);
      }
    }
  }
}

using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace InputMaster
{
  class FileChangedWatcher : IDisposable
  {
    private readonly FileSystemWatcher FileSystemWatcher;
    private readonly FileInfo File;
    private string PreviousContents;

    public FileChangedWatcher(FileInfo file)
    {
      File = Helper.ForbidNull(file, nameof(file));
      FileSystemWatcher = new FileSystemWatcher(file.DirectoryName, file.Name);

      FileSystemWatcher.Changed += Changed;
    }

    public event Action<string> TextChanged = delegate { };

    public void Enable(ISynchronizeInvoke synchronizingObject = null)
    {
      if (synchronizingObject != null)
      {
        FileSystemWatcher.SynchronizingObject = synchronizingObject;
      }
      FileSystemWatcher.EnableRaisingEvents = true;
    }

    public void Dispose()
    {
      FileSystemWatcher.Dispose();
    }

    public void RaiseChangedEvent()
    {
      Changed(null, null);
    }

    private async void Changed(object sender, EventArgs e)
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

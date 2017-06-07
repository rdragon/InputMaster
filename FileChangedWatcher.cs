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
      File = Helper.ForbidNull(file, nameof(file));
      FileSystemWatcher = new FileSystemWatcher(Path.GetDirectoryName(File), Path.GetFileName(File))
      {
        SynchronizingObject = Env.Notifier.SynchronizingObject,
        EnableRaisingEvents = true
      };
      FileSystemWatcher.Changed += Changed;
    }

    public event Action<string> TextChanged = delegate { };

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

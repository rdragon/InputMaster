using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InputMaster
{
  public class StateExporter
  {
    private readonly string _directory;
    private readonly StringBuilder _importLog = new StringBuilder();

    public StateExporter(string directory)
    {
      _directory = directory;
    }

    public async Task ExportAsync()
    {
      await Env.App.TriggerSaveAsync();
      await Task.WhenAll(Env.StateHandlerFactory.GetExportableStateFiles().Select(ExportAsync));
    }

    public async Task ImportAsync()
    {
      await Env.App.TriggerSaveAsync();
      try
      {
        await Task.WhenAll(Env.StateHandlerFactory.GetExportableStateFiles().Select(ImportAsync));
      }
      finally
      {
        if (0 < _importLog.Length)
          await Helper.ShowSelectableTextAsync("Warning", _importLog);
        Env.ShouldRestart = true;
        Application.Exit();
      }
    }

    private async Task ExportAsync(StateFile stateFile)
    {
      if (Path.IsPathRooted(stateFile.File))
      {
        Env.Notifier.Warning($"Cannot export '{stateFile.File}', only relative paths are allowed.");
        return;
      }
      var sourceFile = Path.Combine(Env.Config.DataDir, stateFile.File);
      if (!File.Exists(sourceFile))
      {
        Env.Notifier.Warning($"Cannot export '{sourceFile}', file not found.");
        return;
      }
      string text;
      if (stateFile.Flags.HasFlag(StateHandlerFlags.UseCipher))
      {
        text = await Env.Cipher.TryDecryptFileAsync(sourceFile);
        if (text == null)
        {
          Env.Notifier.Warning($"Cannot export '{sourceFile}', decryption failed.");
          return;
        }
      }
      else
      {
        text = File.ReadAllText(sourceFile);
      }
      var targetFile = Path.Combine(_directory, stateFile.File);
      Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
      File.WriteAllText(targetFile, text);
      Env.Notifier.Info($"Created file '{targetFile}'.");
    }

    private async Task ImportAsync(StateFile stateFile)
    {
      if (Path.IsPathRooted(stateFile.File))
      {
        _importLog.AppendLine($"Cannot import to '{stateFile.File}', only relative paths are allowed.");
        return;
      }
      var sourceFile = Path.Combine(_directory, stateFile.File);
      if (!File.Exists(sourceFile))
      {
        _importLog.AppendLine($"Cannot import '{sourceFile}', file not found.");
        return;
      }
      var text = File.ReadAllText(sourceFile);
      var targetFile = Path.Combine(Env.Config.DataDir, stateFile.File);
      Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
      if (stateFile.Flags.HasFlag(StateHandlerFlags.UseCipher))
        await Env.Cipher.EncryptToFileAsync(targetFile, text);
      else
        File.WriteAllText(targetFile, text);
    }
  }
}

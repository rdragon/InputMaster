using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace InputMaster.TextEditor
{
  internal class SharedFileManager
  {
    private List<SharedFile> SharedFiles = new List<SharedFile>();
    private readonly string TimestampSuffix = "_timestamp";
    private string SharedPassword;

    public SharedFileManager(IValueProvider<IEnumerable<SharedFile>> sharedFileProvider, IValueProvider<string> sharedPasswordProvider)
    {
      sharedFileProvider.ExecuteMany(sharedFiles => SharedFiles = sharedFiles.ToList());
      sharedPasswordProvider.ExecuteOnce(sharedPassword => SharedPassword = sharedPassword);
    }

    public async Task ExportAsync()
    {
      var targetDir = Path.Combine(Env.Config.SharedDir, Env.Config.SharedFilesDirName);
      var tempDir = Path.Combine(Env.Config.CacheDir, Env.Config.SharedFilesDirName);
      Helper.ForceDeleteDir(tempDir);
      if (Directory.Exists(targetDir))
      {
        Directory.Move(targetDir, tempDir);
        Directory.CreateDirectory(targetDir);
      }
      foreach (var sharedFile in SharedFiles)
      {
        await ExportAsync(sharedFile, targetDir, tempDir);
      }
      Directory.Delete(tempDir, true);
    }

    public async Task<IEnumerable<TitleTextPair>> ImportAsync(string dir)
    {
      Helper.RequireExistsDir(dir);
      foreach (var sharedFile in SharedFiles)
      {
        Helper.ForceDeleteFile(sharedFile.NameFile);
        Helper.ForceDeleteFile(sharedFile.DataFile);
      }
      SharedFiles.Clear();
      var pairs = new List<TitleTextPair>();
      foreach (var file in Directory.EnumerateFiles(dir))
      {
        if (file.EndsWith(TimestampSuffix, StringComparison.OrdinalIgnoreCase))
        {
          continue;
        }
        RequireSharedPassword();
        var s = await new Cipher(SharedPassword).DecryptAsync(file);
        pairs.Add(JsonConvert.DeserializeObject<TitleTextPair>(s));
      }
      return pairs;
    }

    private void RequireSharedPassword()
    {
      if (SharedPassword == null)
      {
        throw new InvalidOperationException("No shared password loaded.");
      }
    }

    private async Task ExportAsync(SharedFile sharedFile, string targetDir, string tempDir)
    {
      var tempFile = Path.Combine(tempDir, sharedFile.Id);
      var targetFile = Path.Combine(targetDir, sharedFile.Id);
      var tempTimestampFile = Path.Combine(tempDir, sharedFile.Id + TimestampSuffix);
      var targetTimestampFile = Path.Combine(targetDir, sharedFile.Id + TimestampSuffix);
      if (File.Exists(tempFile) && Helper.TryReadJson(tempTimestampFile, out SharedFileTimestamp tempTimestamp))
      {
        if (
          File.GetLastWriteTimeUtc(sharedFile.NameFile) == tempTimestamp.NameFileTimestamp &&
          File.GetLastWriteTimeUtc(sharedFile.DataFile) == tempTimestamp.DataFileTimestamp)
        {
          File.Move(tempFile, targetFile);
          File.Move(tempTimestampFile, targetTimestampFile);
          return;
        }
      }

      RequireSharedPassword();
      var text = await Env.Cipher.DecryptAsync(sharedFile.DataFile);
      Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
      await new Cipher(SharedPassword).EncryptAsync(targetFile, JsonConvert.SerializeObject(new TitleTextPair(sharedFile.Title, text)));
      var timestamp = new SharedFileTimestamp(sharedFile.NameFile, sharedFile.DataFile);
      File.WriteAllText(targetTimestampFile, JsonConvert.SerializeObject(timestamp));
    }
  }
}

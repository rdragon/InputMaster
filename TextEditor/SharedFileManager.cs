using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System;

namespace InputMaster.TextEditor
{
  internal class SharedFileManager
  {
    private readonly TextEditorForm TextEditorForm;
    private readonly List<SharedFile> SharedFiles = new List<SharedFile>();
    private readonly string TimestampSuffix = "_timestamp";

    public SharedFileManager(TextEditorForm textEditorForm)
    {
      TextEditorForm = textEditorForm;
    }

    public void Clear()
    {
      SharedFiles.Clear();
    }

    public void Add(SharedFile sharedFile)
    {
      SharedFiles.Add(sharedFile);
    }

    public void Export()
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
        Export(sharedFile, targetDir, tempDir);
      }
      Directory.Delete(tempDir, true);
    }

    public void Import(string dir)
    {
      Helper.RequireExistsDir(dir);
      TextEditorForm.CloseAll();
      foreach (var sharedFile in SharedFiles)
      {
        Helper.ForceDeleteFile(sharedFile.NameFile);
        Helper.ForceDeleteFile(sharedFile.DataFile);
      }
      SharedFiles.Clear();
      foreach (var file in Directory.EnumerateFiles(dir))
      {
        if (file.EndsWith(TimestampSuffix, StringComparison.OrdinalIgnoreCase))
        {
          continue;
        }
        var s = Cipher.Decrypt(file, TextEditorForm.SafePassword);
        var pair = JsonConvert.DeserializeObject<TitleTextPair>(s);
        TextEditorForm.CreateNewFile(pair.Title, pair.Text);
      }
    }

    private void Export(SharedFile sharedFile, string targetDir, string tempDir)
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

      Helper.ForbidNull(TextEditorForm.SafePassword, nameof(TextEditorForm.SafePassword));
      var text = TextEditorForm.Decrypt(sharedFile.DataFile);
      Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
      Cipher.Encrypt(targetFile, JsonConvert.SerializeObject(new TitleTextPair(sharedFile.Title, text)), TextEditorForm.SafePassword);
      var timestamp = new SharedFileTimestamp(sharedFile.NameFile, sharedFile.DataFile);
      File.WriteAllText(targetTimestampFile, JsonConvert.SerializeObject(timestamp));
    }
  }
}

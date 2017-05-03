using InputMaster.Forms;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace InputMaster
{
  class SharedFileManager
  {
    private TextEditorForm TextEditorForm;
    private readonly List<SharedFile> SharedFiles = new List<SharedFile>();
    private const string TimestampSuffix = "_timestamp";

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
      var target = new DirectoryInfo(Path.Combine(Config.DataDir.FullName, "SharedFiles"));
      var temp = new DirectoryInfo(Path.Combine(Config.CacheDir.FullName, "SharedFiles"));
      Helper.Delete(temp);
      if (target.Exists)
      {
        Directory.Move(target.FullName, temp.FullName);
        temp.Refresh();
        target.Create();
      }
      foreach (var sharedFile in SharedFiles)
      {
        Export(sharedFile, target, temp);
      }
      Helper.Delete(temp);
    }

    public void Import(DirectoryInfo dir)
    {
      Helper.RequireExists(dir);
      TextEditorForm.CloseAll();
      foreach (var sharedFile in SharedFiles)
      {
        sharedFile.NameFile.Delete();
        sharedFile.DataFile.Delete();
      }
      SharedFiles.Clear();
      foreach (var file in dir.GetFiles())
      {
        if (file.Name.EndsWith(TimestampSuffix))
        {
          continue;
        }
        var s = Cipher.Decrypt(file, TextEditorForm.SafePassword);
        var pair = JsonConvert.DeserializeObject<TitleTextPair>(s);
        TextEditorForm.CreateNewFile(pair.Title, pair.Text);
      }
    }

    private void Export(SharedFile sharedFile, DirectoryInfo target, DirectoryInfo temp)
    {
      sharedFile.NameFile.Refresh();
      sharedFile.DataFile.Refresh();

      var tempFile = new FileInfo(Path.Combine(temp.FullName, sharedFile.Id));
      var targetFile = new FileInfo(Path.Combine(target.FullName, sharedFile.Id));
      var tempTimestampFile = new FileInfo(Path.Combine(temp.FullName, sharedFile.Id + TimestampSuffix));
      var targetTimestampFile = new FileInfo(Path.Combine(target.FullName, sharedFile.Id + TimestampSuffix));
      if (tempFile.Exists && Helper.TryReadJson(tempTimestampFile, out SharedFileTimestamp tempTimestamp))
      {
        if (sharedFile.NameFile.LastWriteTimeUtc == tempTimestamp.NameFileTimestamp && sharedFile.DataFile.LastWriteTimeUtc == tempTimestamp.DataFileTimestamp)
        {
          tempFile.MoveTo(targetFile.FullName);
          tempTimestampFile.MoveTo(targetTimestampFile.FullName);
          return;
        }
      }

      Helper.ForbidNull(TextEditorForm.SafePassword, nameof(TextEditorForm.SafePassword));
      var text = TextEditorForm.Decrypt(sharedFile.DataFile);
      targetFile.Directory.Create();
      Cipher.Encrypt(targetFile, JsonConvert.SerializeObject(new TitleTextPair(sharedFile.Title, text)), TextEditorForm.SafePassword);
      var timestamp = new SharedFileTimestamp(sharedFile.NameFile.LastWriteTimeUtc, sharedFile.DataFile.LastWriteTimeUtc);
      File.WriteAllText(targetTimestampFile.FullName, JsonConvert.SerializeObject(timestamp));
    }
  }
}

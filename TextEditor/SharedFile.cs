using System;
using System.IO;
using Newtonsoft.Json;

namespace InputMaster.TextEditor
{
  internal class SharedFile
  {
    public SharedFile(string title, string nameFile, string dataFile)
    {
      Title = title;
      var match = Constants.SharedFileRegex.Match(title);
      Id = match.Groups["id"].Value;
      NameFile = nameFile;
      DataFile = dataFile;
    }

    public string Id { get; }
    public string Title { get; }
    public string NameFile { get; }
    public string DataFile { get; }
  }

  internal class SharedFileTimestamp
  {
    public SharedFileTimestamp() { }

    public SharedFileTimestamp(string nameFile, string dataFile)
    {
      NameFileTimestamp = File.GetLastWriteTimeUtc(nameFile);
      DataFileTimestamp = File.GetLastWriteTimeUtc(dataFile);
    }

    [JsonProperty]
    public DateTime NameFileTimestamp { get; private set; }
    [JsonProperty]
    public DateTime DataFileTimestamp { get; private set; }
  }

  internal class TitleTextPair
  {
    public TitleTextPair() { }

    public TitleTextPair(string title, string text)
    {
      Title = title;
      Text = text;
    }

    [JsonProperty]
    public string Title { get; private set; }
    [JsonProperty]
    public string Text { get; private set; }
  }
}

using Newtonsoft.Json;
using System;
using System.IO;

namespace InputMaster
{
  class SharedFile
  {
    public SharedFile(string title, FileInfo nameFile, FileInfo dataFile)
    {
      Title = title;
      var match = Config.SharedFileRegex.Match(title);
      Id = match.Groups["id"].Value;
      NameFile = nameFile;
      DataFile = dataFile;
    }

    public string Id { get; }
    public string Title { get; }
    public FileInfo NameFile { get; }
    public FileInfo DataFile { get; }
  }

  class SharedFileTimestamp
  {
    public SharedFileTimestamp() { }

    public SharedFileTimestamp(DateTime nameFileTimestamp, DateTime dataFileTimestamp)
    {
      NameFileTimestamp = nameFileTimestamp;
      DataFileTimestamp = dataFileTimestamp;
    }

    [JsonProperty]
    public DateTime NameFileTimestamp { get; private set; }
    [JsonProperty]
    public DateTime DataFileTimestamp { get; private set; }
  }

  class TitleTextPair
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

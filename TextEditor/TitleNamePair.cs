using Newtonsoft.Json;

namespace InputMaster.TextEditor
{
  public class TitleNamePair
  {
    [JsonProperty]
    public string Title { get; private set; }
    [JsonProperty]
    public string Name { get; private set; }

    public TitleNamePair() { }

    public TitleNamePair(string title, string name)
    {
      Title = title;
      Name = name;
    }
  }
}

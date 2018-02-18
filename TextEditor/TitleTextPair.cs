using Newtonsoft.Json;

namespace InputMaster.TextEditor
{
  public class TitleTextPair
  {
    [JsonProperty]
    public string Title { get; private set; }
    [JsonProperty]
    public string Text { get; private set; }

    public TitleTextPair() { }

    public TitleTextPair(string str)
    {
      var i = str.IndexOf('\n');
      if (i < 0)
        return;
      Title = str.Substring(0, i);
      Text = str.Substring(i + 1);
    }

    public TitleTextPair(string title, string text)
    {
      Title = title;
      Text = text;
    }

    public override string ToString()
    {
      return Title + "\n" + Text;
    }
  }
}

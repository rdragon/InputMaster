namespace InputMaster
{
  internal class HotkeyFile
  {
    public HotkeyFile(string name, string text)
    {
      Name = name;
      Text = text;
    }

    public string Name { get; }
    public string Text { get; }
  }
}

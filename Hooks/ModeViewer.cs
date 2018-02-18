namespace InputMaster.Hooks
{
  public class ModeViewer
  {
    private string Text;

    public void Hide()
    {
      SetText("");
    }

    public void ToggleVisibility(string text)
    {
      SetText(string.IsNullOrEmpty(Text) ? text : "");
    }

    public void SetText(string text)
    {
      if (text == Text)
      {
        return;
      }
      Text = text;
      Env.Notifier.SetPersistentText(Text);
    }

    public void UpdateText(string text)
    {
      if (string.IsNullOrEmpty(Text))
      {
        return;
      }
      SetText(text);
    }
  }
}

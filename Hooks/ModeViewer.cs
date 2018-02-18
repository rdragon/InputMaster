namespace InputMaster.Hooks
{
  public class ModeViewer
  {
    private string _text;

    public void Hide()
    {
      SetText("");
    }

    public void ToggleVisibility(string text)
    {
      SetText(string.IsNullOrEmpty(_text) ? text : "");
    }

    public void SetText(string text)
    {
      if (text == _text)
        return;
      _text = text;
      Env.Notifier.SetPersistentText(_text);
    }

    public void UpdateText(string text)
    {
      if (string.IsNullOrEmpty(_text))
        return;
      SetText(text);
    }
  }
}

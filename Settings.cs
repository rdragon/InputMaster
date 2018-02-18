namespace InputMaster
{
  public class Settings : IState
  {
    public string PasswordPrefix { get; set; } = "";

    public (bool, string message) Fix()
    {
      PasswordPrefix = PasswordPrefix ?? "";
      return (true, "");
    }
  }
}

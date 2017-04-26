using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace InputMaster
{
  class Account
  {
    [JsonProperty]
    private List<TitleFilter> TitleFilters = new List<TitleFilter>();

    public Account() { }

    /// <param name="sensitiveDataAccount">An Account instance which supplies the username, password and email when these are missing from the given account.</param>
    public Account(Account account, bool exludePassword = false, bool excludeUsernameAndEmail = false, Account sensitiveDataAccount = null)
    {
      Id = account.Id;
      Title = account.Title;
      Chord = account.Chord;
      if (!excludeUsernameAndEmail)
      {
        Username = account.Username;
        Email = account.Email;
      }
      if (!exludePassword)
      {
        Password = account.Password;
      }
      UseEmailAsUsername = account.UseEmailAsUsername;
      Description = account.Description;
      Hidden = account.Hidden;
      Order = account.Order;
      TitleFilters = account.TitleFilters;
      if (sensitiveDataAccount != null)
      {
        if (string.IsNullOrWhiteSpace(Username))
        {
          Username = sensitiveDataAccount.Username;
        }
        if (string.IsNullOrWhiteSpace(Password))
        {
          Password = sensitiveDataAccount.Password;
        }
        if (string.IsNullOrWhiteSpace(Email))
        {
          Email = sensitiveDataAccount.Email;
        }
      }
    }

    public Account(int id, string username, string password, string email)
    {
      Id = id;
      Username = username;
      Password = password;
      Email = email;
    }

    [JsonProperty]
    public int Id { get; private set; }
    [JsonProperty]
    public string Title { get; private set; } = "";
    [JsonProperty]
    public Chord Chord { get; private set; } = new Chord(0);
    [JsonProperty]
    public string Username { get; private set; } = "";
    [JsonProperty]
    public string Password { get; private set; } = "";
    [JsonProperty]
    public string Email { get; private set; } = "";
    [JsonProperty]
    public bool UseEmailAsUsername { get; private set; }
    [JsonProperty]
    public string Description { get; private set; } = "";
    [JsonProperty]
    public bool Hidden { get; private set; }
    [JsonProperty]
    public int Order { get; private set; }

    public string GetUsername()
    {
      return UseEmailAsUsername ? Email : Username;
    }

    public bool IsEnabled()
    {
      return TitleFilters.Any(z => z.IsEnabled());
    }
  }
}

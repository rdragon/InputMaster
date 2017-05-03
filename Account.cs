using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace InputMaster
{
  class Account
  {
    [JsonProperty]
    private List<TitleFilter> TitleFilters = new List<TitleFilter>();
    [JsonProperty]
    private Dictionary<string, string> LinkedAccounts = new Dictionary<string, string>();
    [JsonProperty]
    private string Username = "";
    [JsonProperty]
    private string Password = "";
    [JsonProperty]
    private string Email = "";

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
      LinkedAccounts = account.LinkedAccounts;
      AccountManager = account.AccountManager;
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

    public Account(string id, string username, string password, string email)
    {
      Id = id;
      Username = username;
      Password = password;
      Email = email;
    }

    [JsonProperty]
    public string Id { get; private set; }
    [JsonProperty]
    public string Title { get; private set; } = "";
    [JsonProperty]
    public Chord Chord { get; private set; } = new Chord(0);
    [JsonProperty]
    public bool UseEmailAsUsername { get; private set; }
    [JsonProperty]
    public string Description { get; private set; } = "";
    [JsonProperty]
    public bool Hidden { get; private set; }
    [JsonProperty]
    public int Order { get; private set; }
    [JsonIgnore]
    public AccountManager AccountManager { get; set; }

    public string GetUsername()
    {
      var account = GetLinkedAccount();
      return account.UseEmailAsUsername ? account.Email : account.Username;
    }

    public string GetPassword()
    {
      return GetLinkedAccount().Password;
    }

    public string GetEmail()
    {
      return GetLinkedAccount().Email;
    }

    public bool HasForegroundWindowMatch()
    {
      return TitleFilters.Any(z => z.IsEnabled());
    }

    private Account GetLinkedAccount()
    {
      Account account = null;
      if (LinkedAccounts != null)
      {
        foreach (var pair in LinkedAccounts)
        {
          if (Env.ForegroundListener.IsFlagSet(pair.Key) && account == null)
          {
            AccountManager.TryGetAccount(pair.Value, out account);
          }
        }
      }
      return account ?? this;
    }
  }
}

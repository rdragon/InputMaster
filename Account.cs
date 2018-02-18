using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;

namespace InputMaster
{
  public class Account
  {
    /// <summary>
    /// A new account always gets an id that is one larger then the largest id that is in use.
    /// </summary>
    public int Id { get; set; } = 0;
    public Chord Chord { get; set; } = new Chord(0);
    public string Title { get; set; } = "";
    /// <summary>
    /// If not set then the email field is used as login name, otherwise this field is used.
    /// </summary>
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string OldPassword { get; set; } = "";
    /// <summary>
    /// An extra field that you can easily paste. For example, use it for the URL for a VPN account.
    /// </summary>
    public string Extra { get; set; } = "";
    /// <summary>
    /// Multiple accounts that trigger for the same foreground windows can be given distinct slots, so that you as user can control multiple
    /// accounts for the same application.
    /// </summary>
    public int Slot { get; set; } = 0;
    /// <summary>
    /// If there are multiple accounts with a foreground window match, the one with the lowest precedence is chosen. 
    /// </summary>
    public int Precedence { get; set; } = 0;
    public string Description { get; set; } = "";
    /// <summary>
    /// Determines the outcome of HasForegroundWindowMatch.
    /// </summary>
    public List<TitleFilter> TitleFilters { get; set; } = new List<TitleFilter>();
    /// <summary>
    /// Maps flag manager flags to account ids. The first linked account with a flag that is set is used instead in the GetX() functions.
    /// </summary>
    public Dictionary<string, int> LinkedAccounts { get; set; } = new Dictionary<string, int>();
    public List<int> MatrixDecomposition { get; set; } = new List<int>();

    public Account() { }

    public Account(Account account)
    {
      Id = account.Id;
      Chord = account.Chord;
      Title = account.Title;
      Username = account.Username;
      Email = account.Email;
      Password = account.Password;
      OldPassword = account.OldPassword;
      Extra = account.Extra;
      Slot = account.Slot;
      Precedence = account.Precedence;
      Description = account.Description;
      TitleFilters = account.TitleFilters;
      LinkedAccounts = account.LinkedAccounts;
    }

    public string GetLoginName()
    {
      var account = GetLinkedAccountOrSelf();
      return string.IsNullOrWhiteSpace(account.Username) ? account.Email : account.Username;
    }

    public string GetPassword()
    {
      return GetLinkedAccountOrSelf().Password;
    }

    public string GetOldPassword()
    {
      return GetLinkedAccountOrSelf().OldPassword;
    }

    public string GetEmail()
    {
      return GetLinkedAccountOrSelf().Email;
    }

    public string GetExtra()
    {
      return GetLinkedAccountOrSelf().Extra;
    }

    public List<int> GetMatrixDecomposition()
    {
      return GetLinkedAccountOrSelf().MatrixDecomposition;
    }

    public bool HasForegroundWindowMatch()
    {
      return TitleFilters.Any(z => z.IsEnabled());
    }

    public async Task<string> GetPasswordInfoAsync(bool partial)
    {
      if (!partial)
        return "a[" + JsonConvert.SerializeObject(new AccountModel(null, GetLoginName(), GetPassword(), Helper.GetTextOrNull(GetExtra()),
          null), Helper.JsonSerializerSettings) + "]";
      var decomposition = await Helper.GetPasswordDecompositionAsync(GetPassword(), GetMatrixDecomposition());
      if (!decomposition.BluePrints.Any())
        return "";
      return "a[" + JsonConvert.SerializeObject(new AccountModel(null, GetLoginName(), decomposition.Prefix,
        Helper.GetTextOrNull(GetExtra()), string.Join(" + ", decomposition.BluePrints)), Helper.JsonSerializerSettings) + "]";
    }

    private Account GetLinkedAccountOrSelf()
    {
      Account account = null;
      if (LinkedAccounts != null)
        foreach (var pair in LinkedAccounts)
          if (Env.FlagManager.HasFlag(pair.Key) && account == null)
            Env.AccountManager.TryGetAccount(pair.Value, out account);
      return account ?? this;
    }
  }
}

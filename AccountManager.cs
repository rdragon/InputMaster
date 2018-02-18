using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;

namespace InputMaster
{
  public class AccountManager : Actor
  {
    private Account _currentAccount;
    private MyState _state;

    public AccountManager() { } // For Mocking.

    private AccountManager(bool dummy)
    {
      Env.Parser.UpdateParseAction(nameof(AccountManager), parserOutput =>
      {
        var mode = parserOutput.AddMode(new Mode(Env.Config.OpenAccountModeName, true));
        var modifyMode = parserOutput.AddMode(new Mode(Env.Config.ModifyAccountModeName, true));
        foreach (var account in GetAccounts())
        {
          if (account.Chord.Length == 0)
            continue;
          mode.AddHotkey(new ModeHotkey(account.Chord, combo =>
          {
            Env.ModeHook.EnterMode(Env.Config.AccountModeName);
            _currentAccount = account;
          }, account.Chord + " " + account.Title + " " + account.Description));
          modifyMode.AddHotkey(new ModeHotkey(account.Chord, async combo =>
          {
            await ModifyAccountAsync(account.Id);
          }, account.Chord + " " + account.Title + " " + account.Description), true);
        }
      });
    }

    private async Task<AccountManager> Initialize()
    {
      var stateHandler = Env.StateHandlerFactory.Create(new MyState(), nameof(AccountManager),
        StateHandlerFlags.UseCipher | StateHandlerFlags.Exportable | StateHandlerFlags.SavePeriodically);
      _state = await stateHandler.LoadAsync();
      return this;
    }

    public static Task<AccountManager> GetAccountManagerAsync()
    {
      return new AccountManager(false).Initialize();
    }

    public IEnumerable<Account> GetAllAccounts()
    {
      return _state.Accounts.Values;
    }

    private static async Task<Account> ShowAndEditAccountAsync(Account account)
    {
      var text = Helper.JsonSerialize(account, Formatting.Indented);
      while (true)
      {
        text = await Helper.TryGetStringAsync("Account", text, selectAll: false, containsJson: true);
        if (text == null)
          return account;
        try
        {
          var newAccount = JsonConvert.DeserializeObject<Account>(text) ?? account;
          if (newAccount.Id != account.Id)
          {
            newAccount.Id = account.Id;
            Env.Notifier.Warning("Ids can only be changed by modifying the complete json. Id change is ignored.");
          }
          return newAccount;
        }
        catch (Exception ex) when (!Helper.IsFatalException(ex))
        {
          Env.Notifier.Info(ex.ToString());
        }
      }
    }

    [Command]
    private async Task CreateRandomAccountAsync(string passwordPrefix = "")
    {
      var account = new Account()
      {
        Id = GetNewId(),
        Email = Helper.GetRandomEmail(),
        Password = Helper.GetRandomPassword()
      };
      account = await ShowAndEditAccountAsync(account);
      _state.Accounts[account.Id] = account;
      UpdateHook();
      _currentAccount = account;
    }

    [Command]
    public Task ModifyCurrentAccountAsync()
    {
      return ModifyAccountAsync(_currentAccount?.Id);
    }

    [Command]
    public async Task ModifyAccountAsync(int? id = null)
    {
      if (!id.HasValue)
      {
        var s = await Helper.TryGetLineAsync("Id");
        if (s == null)
          return;
        if (!int.TryParse(s, out var x))
          throw new ArgumentException("Cannot parse value as int.");
        id = x;
      }
      if (!TryGetAccount(id.Value, out var account))
      {
        Env.Notifier.Info($"No account with id '{id}' found.");
        return;
      }
      var newAccount = await ShowAndEditAccountAsync(account);
      _state.Accounts[newAccount.Id] = newAccount;
      UpdateHook();
      _currentAccount = newAccount;
    }

    [Command]
    public void SetCurrentAccount(int id)
    {
      TryGetAccount(id, out _currentAccount);
    }

    [Command]
    public Task ModifyAllAccountsAsync()
    {
      return ModifyAllAccounts(false);
    }

    public async Task ModifyAllAccounts(bool showPassword)
    {
      string text;
      {
        var strippedAccounts = GetSortedAccounts().Select(z => new Account(z)).ToList();
        if (!showPassword)
          strippedAccounts.ForEach(z => { z.Password = ""; z.OldPassword = ""; });
        text = Helper.JsonSerialize(strippedAccounts, Formatting.Indented);
      }
      List<Account> newAccounts;
      var first = true;
      while (true)
      {
        text = await Helper.TryGetStringAsync("Accounts", text, selectAll: false, startWithFindDialog: first, containsJson: true);
        if (text == null)
          return;
        first = false;
        try
        {
          newAccounts = JsonConvert.DeserializeObject<List<Account>>(text) ?? throw new ArgumentException();
          break;
        }
        catch (Exception ex) when (!Helper.IsFatalException(ex))
        {
          Env.Notifier.Info(ex.ToString());
        }
      }
      if (!showPassword)
      {
        foreach (var newAccount in newAccounts)
        {
          if (!TryGetAccount(newAccount.Id, out var oldAccount))
            continue;
          newAccount.Password = string.IsNullOrWhiteSpace(newAccount.Password) ? oldAccount.Password : newAccount.Password;
          newAccount.OldPassword = string.IsNullOrWhiteSpace(newAccount.OldPassword) ? oldAccount.OldPassword : newAccount.OldPassword;
        }
      }
      _state.Accounts.Clear();
      foreach (var newAccount in newAccounts)
        _state.Accounts[newAccount.Id] = newAccount;
      UpdateHook();
    }

    /// <summary>
    /// c: current account
    /// a: start with {Ctrl}a
    /// l: login name
    /// e: email
    /// x: extra
    /// o: old password
    /// p: password
    /// z: end with enter
    /// </summary>
    [Command]
    public void PasteAccount(int slot, [ValidFlags("calexopz")]string flags = "")
    {
      var account = flags.Contains('c') ? _currentAccount : GetAccounts().Where(z => z.Slot == slot &&
        z.HasForegroundWindowMatch()).OrderBy(z => z.Precedence).FirstOrDefault();
      if (account == null)
        return;
      _currentAccount = account;
      var injector = Env.CreateInjector();
      if (flags.Contains('a'))
        injector.Add(Input.A, Modifiers.Ctrl);
      {
        var first = true;
        var str = "lexop";
        var values = new string[] { account.GetLoginName(), account.GetEmail(), account.GetExtra(), account.GetOldPassword(),
          account.GetPassword() };
        Debug.Assert(str.Length == values.Length);
        for (int i = 0; i < str.Length; i++)
        {
          var c = str[i];
          if (!flags.Contains(c))
            continue;
          var value = values[i];
          if (!first)
            injector.Add(Input.Tab);
          first = false;
          injector.Add(value ?? "", Env.Config.LiteralInputReader);
        }
      }
      if (flags.Contains('z'))
        injector.Add(Input.Enter);
      injector.Run();
    }

    [Command]
    public async Task RunAsync([AllowSpaces] string filePath, [AllowSpaces]string arguments = "")
    {
      if (!TryGetAccount(Env.Config.LocalAccountId, out var account))
        await Helper.StartProcessAsync(filePath, arguments);
      var s = new SecureString();
      foreach (var c in account.GetPassword())
        s.AppendChar(c);
      await Helper.StartProcessAsync(filePath, account.GetLoginName(), s, Environment.UserDomainName, arguments);
    }

    [Command]
    public Task EditAsync([AllowSpaces]string filePath)
    {
      return RunAsync(Env.Config.DefaultTextEditor, $"\"{filePath}\"");
    }

    [Command]
    public Task SurfAsync([AllowSpaces]string url)
    {
      return RunAsync(Env.Config.DefaultWebBrowser, $"\"{url}\"");
    }

    public IEnumerable<Account> GetAccounts()
    {
      return _state.Accounts.Values.AsEnumerable();
    }

    public bool TryGetAccount(int id, out Account account)
    {
      return _state.Accounts.TryGetValue(id, out account);
    }

    private void UpdateHook()
    {
      Env.Parser.Run();
    }

    private int GetNewId()
    {
      return GetAccounts().Any() ? GetAccounts().Max(z => z.Id) + 1 : Env.Config.MinAccountUploadId;
    }

    private IEnumerable<Account> GetSortedAccounts()
    {
      var list = GetAccounts().ToList();
      list.Sort((x, y) => x.Id.CompareTo(y.Id));
      return list;
    }

    private class MyState : IState
    {
      public Dictionary<int, Account> Accounts { get; set; }

      public (bool, string message) Fix()
      {
        Accounts = Accounts ?? new Dictionary<int, Account>();
        return (true, "");
      }
    }
  }
}

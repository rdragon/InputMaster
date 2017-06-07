using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using InputMaster.Hooks;
using Newtonsoft.Json;
using InputMaster.TextEditor;

namespace InputMaster
{
  internal class AccountManager : Actor
  {
    private readonly Dictionary<string, Account> Accounts = new Dictionary<string, Account>();
    private readonly ModeHook ModeHook;
    private readonly TextEditorForm TextEditorForm;
    private bool Changed;
    private Account CurrentAccount;
    private bool Loaded;

    public AccountManager(TextEditorForm textEditorForm, ModeHook modeHook)
    {
      Helper.ForbidNull(textEditorForm, nameof(textEditorForm));
      Helper.ForbidNull(modeHook, nameof(modeHook));
      TextEditorForm = textEditorForm;
      ModeHook = modeHook;
      TextEditorForm.Saving += () =>
      {
        if (!Changed)
        {
          return;
        }
        if (Loaded)
        {
          TextEditorForm.Encrypt(TextEditorForm.AccountFile, JsonConvert.SerializeObject(Accounts.Values, Formatting.Indented));
          Changed = false;
        }
        else
        {
          Env.Notifier.WriteError("Could not save account data. A program restart is required.");
        }
      };
      if (TextEditorForm.HasStarted)
      {
        Initialize();
      }
      else
      {
        TextEditorForm.Started += Initialize;
      }
    }

    public static string CreateRandomIdentifier(int length)
    {
      using (var r = new RNGCryptoServiceProvider())
      {
        var sb = new StringBuilder();
        var c = GetNextRandomChar(r);
        while (c >= '0' && c <= '9')
        {
          c = GetNextRandomChar(r);
        }
        sb.Append(c);
        for (var i = 1; i < length; i++)
        {
          sb.Append(GetNextRandomChar(r));
        }
        return sb.ToString();
      }
    }

    public static string CreateRandomPassword(int length)
    {
      return CreateRandomIdentifier(length + 1).Substring(1);
    }

    private static char GetNextRandomChar(RandomNumberGenerator randomNumberGenerator)
    {
      var buffer = new byte[8];
      randomNumberGenerator.GetBytes(buffer);
      var i = BitConverter.ToUInt64(buffer, 0) % 36;
      if (i < 10)
      {
        return (char)('0' + i);
      }
      return (char)('a' + i - 10);
    }

    private static Account GetModifiedAccount(Account account)
    {
      var strippedAccount = new Account(account, excludePassword: true, excludeUsernameAndEmail: true);
      var s = Helper.GetString("Account", JsonConvert.SerializeObject(strippedAccount, Formatting.Indented), selectAll: false);
      if (string.IsNullOrWhiteSpace(s))
      {
        return account;
      }
      strippedAccount = JsonConvert.DeserializeObject<Account>(s);
      return new Account(strippedAccount, sensitiveDataAccount: account);
    }

    private static string AskId()
    {
      return Helper.GetString("id");
    }

    private static string Increment(StringBuilder sb, int i = 0)
    {
      if (i == sb.Length)
      {
        sb.Append('1');
      }
      else
      {
        switch (sb[i])
        {
          case 'z':
            sb[i] = '0';
            Increment(sb, i + 1);
            break;
          case '9':
            sb[i] = 'a';
            break;
          default:
            sb[i]++;
            break;
        }
      }
      return new string(sb.ToString().Reverse().ToArray());
    }

    [CommandTypes(CommandTypes.Visible)]
    public async Task CreateRandomAccount(string passwordPrefix = "")
    {
      await Task.Yield();
      var account = new Account(GetNewId(), CreateRandomIdentifier(6), passwordPrefix + CreateRandomPassword(Env.Config.DefaultPasswordLength), CreateRandomIdentifier(6) + Env.Config.EmailSuffix);
      account = GetModifiedAccount(account);
      AddAccount(account);
      Parse();
    }

    [CommandTypes(CommandTypes.Visible)]
    public async Task ModifyAccount(string id = null)
    {
      await Task.Yield();
      id = id ?? AskId();
      if (!string.IsNullOrEmpty(id) && Accounts.TryGetValue(id, out var account))
      {
        var newAccount = GetModifiedAccount(account);
        Accounts.Remove(account.Id);
        AddAccount(newAccount);
        Parse();
      }
    }

    [CommandTypes(CommandTypes.Visible)]
    public void PrintAccountUsername()
    {
      if (CurrentAccount == null) return;
      Env.CreateInjector().Add(CurrentAccount.GetUsername(), Env.Config.LiteralInputReader).Run();
    }

    [CommandTypes(CommandTypes.Visible)]
    public void PrintAccountPassword()
    {
      if (CurrentAccount == null) return;
      Env.CreateInjector().Add(CurrentAccount.GetPassword(), Env.Config.LiteralInputReader).Run();
    }

    [CommandTypes(CommandTypes.Visible)]
    public void PrintAccountEmail()
    {
      if (CurrentAccount == null) return;
      Env.CreateInjector().Add(CurrentAccount.GetEmail(), Env.Config.LiteralInputReader).Run();
    }

    [CommandTypes(CommandTypes.Visible)]
    public void SetCurrentAccount(string id)
    {
      TryGetAccount(id, out CurrentAccount);
    }

    [CommandTypes(CommandTypes.Visible)]
    public async Task ModifyAllAccounts(bool showUsernameAndEmail = false)
    {
      await Task.Yield();
      var strippedAccounts = GetSortedAccounts().Select(a => new Account(a, excludeUsernameAndEmail: !showUsernameAndEmail, excludePassword: true)).ToList();
      var s = Helper.GetString("Accounts", JsonConvert.SerializeObject(strippedAccounts, Formatting.Indented), selectAll: false);
      if (!string.IsNullOrWhiteSpace(s))
      {
        var submittedAccounts = JsonConvert.DeserializeObject<List<Account>>(s);
        var newAccounts = submittedAccounts.Select(a => new Account(a, sensitiveDataAccount: Accounts.ContainsKey(a.Id) ? Accounts[a.Id] : null)).ToList();
        Accounts.Clear();
        foreach (var account in newAccounts)
        {
          AddAccount(account);
        }
        Parse();
      }
    }

    [CommandTypes(CommandTypes.Visible)]
    public void PasteAccount(int order, [ValidFlags("u")]string flags = "")
    {
      foreach (var account in Accounts.Values)
      {
        if (account.Order == order && account.HasForegroundWindowMatch())
        {
          var injector = Env.CreateInjector();
          if (flags.Contains('u'))
          {
            injector.Add(account.GetUsername(), Env.Config.LiteralInputReader);
            injector.Add(Input.Tab);
          }
          injector.Add(account.GetPassword(), Env.Config.LiteralInputReader);
          injector.Add(Input.Enter);
          injector.Run();
          break;
        }
      }
    }

    [CommandTypes(CommandTypes.Visible)]
    public async Task EnterAccount(string id = null)
    {
      await Task.Yield();
      id = id ?? AskId();
      if (!string.IsNullOrEmpty(id) && Accounts.TryGetValue(id, out var account))
      {
        ModeHook.EnterMode(Env.Config.AccountModeName);
        CurrentAccount = account;
      }
    }

    [CommandTypes(CommandTypes.Visible)]
    public void Run([AllowSpaces] string filePath, [AllowSpaces]string arguments = "")
    {
      if (TryGetAccount(Env.Config.LocalAccountId, out var account))
      {
        var s = new SecureString();
        foreach (var c in account.GetPassword())
        {
          s.AppendChar(c);
        }
        Helper.StartProcess(filePath, arguments, account.GetUsername(), s, Environment.UserDomainName);
      }
      else
      {
        Helper.StartProcess(filePath, arguments);
      }
    }

    [CommandTypes(CommandTypes.Visible)]
    public void Edit([AllowSpaces]string filePath)
    {
      Run(Env.Config.DefaultTextEditor, $"\"{filePath}\"");
    }

    [CommandTypes(CommandTypes.Visible)]
    public void Surf([AllowSpaces]string url)
    {
      Run(Env.Config.DefaultWebBrowser, $"\"{url}\"");
    }

    public IEnumerable<Account> GetAccounts()
    {
      return Accounts.Values.ToList();
    }

    public bool TryGetAccount(string id, out Account account)
    {
      return Accounts.TryGetValue(id, out account);
    }

    private void Initialize()
    {
      if (TextEditorForm.AccountFile == null)
      {
        return;
      }
      try
      {
        var text = TextEditorForm.Decrypt(TextEditorForm.AccountFile);
        var accounts = JsonConvert.DeserializeObject<List<Account>>(text);
        foreach (var account in accounts)
        {
          AddAccount(account);
        }
      }
      catch (Exception ex) when (!Helper.IsCriticalException(ex))
      {
        Env.Notifier.WriteError(ex, "Failed to load account data.");
        return;
      }
      Parse();
      Changed = false;
      Loaded = true;
    }

    private void AddAccount(Account account)
    {
      if (Accounts.ContainsKey(account.Id))
      {
        Env.Notifier.WriteError($"Multiple accounts with id '{account.Id}' found.");
      }
      Accounts[account.Id] = account;
      account.SetAccountManager(this);
      Changed = true;
    }

    private void Parse()
    {
      Env.Parser.UpdateParseAction(nameof(AccountManager), parserOutput =>
      {
        var mode = parserOutput.AddMode(new Mode(Env.Config.OpenAccountModeName, true));
        var modifyMode = parserOutput.AddMode(new Mode(Env.Config.ModifyAccountModeName, true));
        foreach (var account in Accounts.Values)
        {
          if (account.Chord.Length == 0)
          {
            continue;
          }
          mode.AddHotkey(new ModeHotkey(account.Chord, combo =>
          {
            ModeHook.EnterMode(Env.Config.AccountModeName);
            CurrentAccount = account;
          }, account.Chord + " " + account.Title + " " + account.Description));
          modifyMode.AddHotkey(new ModeHotkey(account.Chord, async combo =>
          {
            await ModifyAccount(account.Id);
          }, account.Chord + " " + account.Title + " " + account.Description), true);
        }
        File.WriteAllLines(Env.Config.AccountsOutputFile,
          Accounts.Values.Select(z => z.Id + " " + z.Title + " " + z.Description + (z.Chord.Length == 0 ? "" : $" ({z.Chord})")).ToArray());
      });
      Env.Parser.Run();
    }

    private string GetNewId()
    {
      var sb = new StringBuilder("0");
      var id = "0";
      while (Accounts.ContainsKey(id))
      {
        id = Increment(sb);
      }
      return id;
    }

    private IEnumerable<Account> GetSortedAccounts()
    {
      var list = Accounts.Values.ToList();
      list.Sort((x, y) => string.CompareOrdinal(x.Id, y.Id));
      return list;
    }
  }
}

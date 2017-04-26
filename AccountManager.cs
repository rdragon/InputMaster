using InputMaster.Forms;
using InputMaster.Hooks;
using InputMaster.Parsers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace InputMaster
{
  class AccountManager
  {
    private Dictionary<int, Account> Accounts = new Dictionary<int, Account>();
    private bool Changed;
    private Parser Parser;
    private Account CurrentAccount;
    private ModeHook ModeHook;
    private bool Loaded;

    public AccountManager(TextEditorForm textEditorForm, Parser parser, ModeHook modeHook)
    {
      Parser = parser;
      ModeHook = modeHook;
      textEditorForm.Started += () =>
      {
        if (textEditorForm.AccountFile == null)
        {
          return;
        }
        try
        {
          var text = textEditorForm.Decrypt(textEditorForm.AccountFile);
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
      };

      textEditorForm.Saving += () =>
      {
        if (Changed)
        {
          if (Loaded)
          {
            textEditorForm.Encrypt(textEditorForm.AccountFile, JsonConvert.SerializeObject(Accounts.Values, Formatting.Indented));
            Changed = false;
          }
          else
          {
            Env.Notifier.WriteError("Could not save account data. A program restart is required.");
          }
        }
      };

      ModeHook.LeavingMode += () => CurrentAccount = null;
    }

    public static string CreateRandomIdentifier(int length)
    {
      using (var r = new RNGCryptoServiceProvider())
      {
        Func<char> f = () =>
        {
          byte[] buffer = new byte[8];
          r.GetBytes(buffer);
          var i = BitConverter.ToUInt64(buffer, 0) % 36;
          if (i < 10)
          {
            return (char)('0' + i);
          }
          else
          {
            return (char)('a' + i - 10);
          }
        };
        var sb = new StringBuilder();
        var c = f();
        while (c >= '0' && c <= '9')
        {
          c = f();
        }
        sb.Append(c);
        for (int i = 1; i < length; i++)
        {
          sb.Append(f());
        }
        return sb.ToString();
      }
    }

    public static string CreateRandomPassword(int length)
    {
      return CreateRandomIdentifier(length + 1).Substring(1);
    }

    private static Account GetModifiedAccount(Account account)
    {
      var strippedAccount = new Account(account, exludePassword: true, excludeUsernameAndEmail: true);
      var s = Helper.GetString("Account", JsonConvert.SerializeObject(strippedAccount, Formatting.Indented), selectAll: false);
      if (!string.IsNullOrWhiteSpace(s))
      {
        strippedAccount = JsonConvert.DeserializeObject<Account>(s);
        return new Account(strippedAccount, sensitiveDataAccount: account);
      }
      else
      {
        return account;
      }
    }

    [CommandTypes(CommandTypes.Visible)]
    public async Task CreateRandomAccount(string passwordPrefix = "")
    {
      await Task.Yield();
      var account = new Account(GetNewId(), CreateRandomIdentifier(6), passwordPrefix + CreateRandomPassword(Config.DefaultPasswordLength), CreateRandomIdentifier(6) + Config.EmailSuffix);
      account = GetModifiedAccount(account);
      AddAccount(account);
      Parse();
    }

    [CommandTypes(CommandTypes.Visible)]
    public async Task ModifyAccount(int? id = null)
    {
      await Task.Yield();
      if (!id.HasValue)
      {
        var s = Helper.GetString("id");
        if (s != null && int.TryParse(s, out int x))
        {
          id = x;
        }
        else
        {
          return;
        }
      }
      if (Accounts.TryGetValue(id.Value, out Account account))
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
      Env.CreateInjector().Add(CurrentAccount.GetUsername(), Config.LiteralInputReader).Run();
    }

    [CommandTypes(CommandTypes.Visible)]
    public void PrintAccountPassword()
    {
      if (CurrentAccount == null) return;
      Env.CreateInjector().Add(CurrentAccount.GetPassword(), Config.LiteralInputReader).Run();
    }

    [CommandTypes(CommandTypes.Visible)]
    public void PrintAccountEmail()
    {
      if (CurrentAccount == null) return;
      Env.CreateInjector().Add(CurrentAccount.GetEmail(), Config.LiteralInputReader).Run();
    }

    [CommandTypes(CommandTypes.Visible)]
    public async Task ModifyAllAccounts(bool showUsernameAndEmail = false)
    {
      await Task.Yield();
      var strippedAccounts = GetSortedAccounts().Select(a => new Account(a, excludeUsernameAndEmail: !showUsernameAndEmail, exludePassword: true)).ToList();
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
            injector.Add(account.GetUsername(), Config.LiteralInputReader);
            injector.Add(Input.Tab);
          }
          injector.Add(account.GetPassword(), Config.LiteralInputReader);
          injector.Add(Input.Enter);
          injector.Run();
          break;
        }
      }
    }

    [CommandTypes(CommandTypes.Visible)]
    public void EnterAccount(int id)
    {
      if (Accounts.TryGetValue(id, out Account account))
      {
        ModeHook.EnterMode(Config.AccountModeName, "");
        CurrentAccount = account;
      }
    }

    public IEnumerable<Account> GetAccounts()
    {
      return Accounts.Values.ToList();
    }

    public Account TryGetAccount(int id)
    {
      if (Accounts.TryGetValue(id, out Account account))
      {
        return account;
      }
      else
      {
        return null;
      }
    }

    private void AddAccount(Account account)
    {
      if (Accounts.ContainsKey(account.Id))
      {
        Env.Notifier.WriteError($"Multiple accounts with id '{account.Id}' found.");
      }
      Accounts[account.Id] = account;
      account.AccountManager = this;
      Changed = true;
    }

    private void Parse()
    {
      Parser.UpdateParseAction(nameof(AccountManager), (parserOutput) =>
      {
        var mode = parserOutput.AddMode(new Mode(Config.OpenAccountModeName, true));
        var modifyMode = parserOutput.AddMode(new Mode(Config.ModifyAccountModeName, true));
        foreach (var account in Accounts.Values)
        {
          if (account.Chord.Length == 0)
          {
            continue;
          }
          mode.AddHotkey(new ModeHotkey(account.Chord, (combo) =>
          {
            ModeHook.EnterMode(Config.AccountModeName, "");
            CurrentAccount = account;
          }, account.Chord + " " + account.Description));
          modifyMode.AddHotkey(new ModeHotkey(account.Chord, async (combo) =>
          {
            await ModifyAccount(account.Id);
          }, account.Chord + " " + account.Description), true);
        }
      });
      Parser.Parse();
    }

    private int GetNewId()
    {
      int id = 0;
      while (Accounts.ContainsKey(id))
      {
        id++;
      }
      return id;
    }

    private IEnumerable<Account> GetSortedAccounts()
    {
      var list = Accounts.Values.ToList();
      list.Sort((x, y) => x.Id.CompareTo(y.Id));
      return list;
    }
  }
}

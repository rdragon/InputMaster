using InputMaster.Instances;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace InputMaster.Actors
{
  public class AccountUploader : Actor
  {
    private MyState _state;
    private ICipher _fullAccountCipher;
    private ICipher _partialAccountCipher;
    private CyptroUpdater _cyptroUpdater;
    private bool _initialized;

    private AccountUploader()
    {
      Env.App.AddSaveAction(() => RunAsync(Env.AccountManager.GetAccounts()));
    }

    private async Task<AccountUploader> InitializeAsync()
    {
      var stateHandler = Env.StateHandlerFactory.Create(new MyState(), nameof(AccountUploader),
        StateHandlerFlags.UseCipher | StateHandlerFlags.UserEditable | StateHandlerFlags.Exportable | StateHandlerFlags.SavePeriodically);
      _state = await stateHandler.LoadAsync();
      _fullAccountCipher = new Cipher(await Helper.GetKeyAsync(_state.FullAccountPassword, Helper.GetCyptroSalt("0"),
        Env.Config.CyptroDerivationCount));
      _partialAccountCipher = new Cipher(await Helper.GetKeyAsync(_state.PartialAccountPassword, Helper.GetCyptroSalt("1"),
        Env.Config.CyptroDerivationCount));
      _cyptroUpdater = await CyptroUpdater.GetCyptroUpdaterAsync();
      _initialized = true;
      if (string.IsNullOrWhiteSpace(Env.Config.AccountUploadUrl))
        Env.Notifier.Info("No account upload url set.");
      return this;
    }

    public static Task<AccountUploader> GetAccountUploaderAsync()
    {
      return new AccountUploader().InitializeAsync();
    }

    private async Task RunAsync(IEnumerable<Account> allAccountsIn)
    {
      if (!_initialized)
        return;
      var allAccounts = allAccountsIn.ToList();
      var accounts = allAccounts.Where(z => !ShouldSkip(z)).Take(Env.Config.AccountUploaderMaxAccounts).ToList();
      if (accounts.Count == 0 && allAccounts.Count == _state.AccountCount)
        return;
      _state.CyptroNeedsUpdate = true;
      if (!string.IsNullOrWhiteSpace(Env.Config.AccountUploadUrl))
        if (!await UploadAccounts(accounts))
          return;
      foreach (var account in accounts)
        _state.HashDictionary[account.Id] = GetHash(account);
      _state.AccountCount = allAccounts.Count;
    }

    private async Task<bool> UploadAccounts(IEnumerable<Account> accounts)
    {
      var tasks = accounts.Select(z => GetEntryAsync(z, false)).Concat(accounts.Select(z => GetEntryAsync(z, true)));
      var entries = (await Task.WhenAll(tasks)).WhereNotNull().ToList();
      var entriesDict = new Dictionary<string, Entry>(entries.Count);
      foreach (var entry in entries)
      {
        entriesDict[entry.Id] = entry;
        entry.Id = null;
      }
      var dict = new Dictionary<string, string> {
          { "entries", JsonConvert.SerializeObject(entriesDict, Helper.JsonSerializerSettings) },
          { "allIds", JsonConvert.SerializeObject(Env.AccountManager.GetAccounts().SelectMany(z => new string[] { GetId(z, false),
              GetId(z, true)}).ToList()) }};
      var response = await RunRequest<UploadResponse>("updateEntries", (client, url) => client.PostAsync(url,
        new FormUrlEncodedContent(dict)));
      if (response == null)
        return false;
      var deletedMessage = 0 < response.DeletedCount ? $" ({response.DeletedCount} entries deleted)" : "";
      if (response.Count != entries.Count)
      {
        Env.Notifier.Warning(
          $"Could not upload all accounts, only {response.Count} / {entries.Count} have been uploaded{deletedMessage}.");
        return false;
      }
      Env.Notifier.Info($"Accounts uploaded ({response.Count} entries){deletedMessage}.");
      return true;
    }

    private async Task<T> RunRequest<T>(string action, Func<HttpClient, string, Task<HttpResponseMessage>> func = null)
      where T : ResponseBase
    {
      using (var httpClient = new HttpClient())
      {
        var url = Env.Config.AccountUploadUrl + $"?action={action}&password={_state.AccountUploadPassword}";
        httpClient.Timeout = Env.Config.AccountUploadTimeout;
        HttpResponseMessage httpResponse;
        try
        {
          httpResponse = await (func == null ? httpClient.GetAsync(url) : func(httpClient, url));
        }
        catch (TaskCanceledException)
        {
          Env.Notifier.Warning($"Action '{action}' failed. Could not connect to server.");
          return null;
        }
        if (!httpResponse.IsSuccessStatusCode)
        {
          Env.Notifier.Warning($"Action '{action}' failed. Response status code is {httpResponse.StatusCode}.");
          return null;
        }
        T response = null;
        try
        {
          response = JsonConvert.DeserializeObject<T>(await httpResponse.Content.ReadAsStringAsync(), Helper.JsonSerializerSettings);
        }
        catch (JsonException ex)
        {
          Env.Notifier.Warning($"Action '{action}' failed: {ex}");
          return null;
        }
        if (response.Status != "ok")
        {
          Env.Notifier.Warning($"Action '{action}' failed. Response from server: {response.Message}");
          return null;
        }
        return response;
      }
    }

    private bool ShouldSkip(Account account)
    {
      return account.Id < Env.Config.MinAccountUploadId || string.IsNullOrWhiteSpace(account.Title) ||
       _state.HashDictionary.ContainsKey(account.Id) && _state.HashDictionary[account.Id].SequenceEqual(GetHash(account));
    }

    private async Task<Entry> GetEntryAsync(Account account, bool partial)
    {
      var id = GetId(account, partial);
      var text = await account.GetPasswordInfoAsync(partial);
      if (text.Length == 0)
        return null;
      var data = await (partial ? _partialAccountCipher : _fullAccountCipher).EncryptToBase64Async(text);
      return new Entry(id, GetTitle(account, partial), data);
    }

    private static string GetId(Account account, bool partial)
    {
      return account.Id + (partial ? "_1" : "_0");
    }

    private static string GetTitle(Account account, bool partial)
    {
      return account.Title + (partial ? " (partial)" : "");
    }

    private static byte[] GetHash(Account account)
    {
      return Helper.GetSha256(account.Title + "\n" + account.GetLoginName() + "\n" + account.GetPassword() + "\n" + account.GetExtra());
    }

    [Command]
    public async Task CheckCyptroAlarm()
    {
      var response = await RunRequest<AlarmResponse>("checkAlarm");
      if (response == null || !response.Alarm)
        return;
      Env.Notifier.Info("Cyptro alarm has been set.");
    }

    [Command]
    public async Task ClearCyptroAlarm()
    {
      var response = await RunRequest<ResponseBase>("clearAlarm");
      if (response != null)
        Env.Notifier.Info("Cyptro alarm has been cleared.");
    }

    [Command]
    private void ResetAccountUploader()
    {
      _state.HashDictionary.Clear();
    }

    [Command]
    public async Task UpdateAndPushCyptro()
    {
      if (!_state.CyptroNeedsUpdate)
        return;
      var git = new GitExecutor(Env.Config.CyptroDirectory);
      var status = await git.GetStatus();
      if (status.HasFlag(GitStatusFlags.WorkingTreeDirty))
        throw new Exception($"Cannot update cyptro as working tree is dirty.");
      await _cyptroUpdater.Update();
      status = await git.GetStatus();
      if (!status.HasFlag(GitStatusFlags.WorkingTreeDirty))
        throw new Exception($"Cyptro update failed, working tree is still clean.");
      await git.AddAll();
      await git.Commit("Update data directory");
      status = await git.GetStatus();
      if (status.HasFlag(GitStatusFlags.WorkingTreeDirty) || status.HasFlag(GitStatusFlags.IndexDirty))
        throw new Exception($"Cyptro data directory updated but could not commit the changes.");
      status = await git.GetStatus();
      if (!status.HasFlag(GitStatusFlags.AwaitingPush))
        throw new Exception($"Cyptro data directory updated but cannot push.");
      await git.Push();
      status = await git.GetStatus();
      if (!status.HasFlag(GitStatusFlags.UpToDate))
        throw new Exception($"Cyptro data directory updated but the push has failed.");
      Env.Notifier.Info("Updated cyptro and pushed the changes.");
      _state.CyptroNeedsUpdate = false;
    }

    private class MyState : IState
    {
      public string AccountUploadPassword { get; set; }
      public string PartialAccountPassword { get; set; }
      public string FullAccountPassword { get; set; }
      public bool CyptroNeedsUpdate { get; set; }
      public Dictionary<int, byte[]> HashDictionary { get; set; }
      public int AccountCount { get; set; }

      public (bool, string message) Fix()
      {
        AccountUploadPassword = AccountUploadPassword ?? "";
        FullAccountPassword = FullAccountPassword ?? "";
        PartialAccountPassword = PartialAccountPassword ?? "";
        HashDictionary = HashDictionary ?? new Dictionary<int, byte[]>();
        return (true, "");
      }
    }

    public class Entry
    {
      public string Id { get; set; }
      public string Title { get; set; }
      public string Encrypted { get; set; }

      public Entry(string id, string title, string data)
      {
        Id = id;
        Title = title;
        Encrypted = data;
      }
    }

    private class ResponseBase
    {
      public string Message { get; set; }
      public string Status { get; set; }
    }

    private class UploadResponse : ResponseBase
    {
      public int Count { get; set; }
      public int DeletedCount { get; set; }
    }

    private class AlarmResponse : ResponseBase
    {
      public bool Alarm { get; set; }
    }
  }
}

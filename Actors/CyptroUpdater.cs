using InputMaster.Instances;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InputMaster.Actors
{
  public class CyptroUpdater : Actor
  {
    private readonly Dictionary<string, string> _passwords = new Dictionary<string, string>();
    private readonly Dictionary<string, Task<Cipher>> _tasks = new Dictionary<string, Task<Cipher>>();
    private readonly Dictionary<string, ICipher> _ciphers = new Dictionary<string, ICipher>();

    private CyptroUpdater(MyState state)
    {
      foreach (var pair in state.Passwords)
      {
        _passwords.Add(pair.Key, pair.Value);
      }
    }

    private async Task<ICipher> GetCipher(string name)
    {
      if (_ciphers.TryGetValue(name, out var cipher))
        return cipher;
      if (_tasks.TryGetValue(name, out var task))
        return await task;
      if (!_passwords.TryGetValue(name, out var password))
        throw new ArgumentException($"Cannot find password for '{name}'.");
      task = Helper.GetKeyAsync(password, Helper.GetCyptroSalt(name), Env.Config.CyptroDerivationCount)
        .ContinueWith(z => new Cipher(z.Result));
      _tasks.Add(name, task);
      cipher = await task;
      _ciphers.Add(name, cipher);
      return cipher;
    }

    public static async Task<CyptroUpdater> GetCyptroUpdaterAsync()
    {
      var stateHandler = Env.StateHandlerFactory.Create(new MyState(), nameof(CyptroUpdater),
        StateHandlerFlags.Exportable | StateHandlerFlags.UseCipher | StateHandlerFlags.UserEditable);
      return new CyptroUpdater(await stateHandler.LoadAndSaveAsync());
    }

    public async Task Update()
    {
      var accounts = Env.AccountManager.GetAllAccounts().Where(z => !string.IsNullOrWhiteSpace(z.Title));
      var list = accounts.OrderBy(z => z.Title.ToLowerInvariant()).Select(z => new AccountModel(z.Title, z.GetLoginName(), z.GetPassword(),
       Helper.GetTextOrNull(z.GetExtra()), null));
      await Update(Env.Config.CyptroAccountsName ?? "accounts", "a" + JsonConvert.SerializeObject(list,
        Helper.JsonSerializerSettings));
    }

    public async Task Update(string name, string plainText)
    {
      var dir = Path.Combine(Env.Config.CyptroDirectory, Env.Config.CyptroDataDirectory);
      Directory.CreateDirectory(dir);
      var entriesFile = Path.Combine(dir, "entries.json");
      if (!File.Exists(entriesFile))
        File.WriteAllText(entriesFile, "{}");
      var entriesText = File.ReadAllText(entriesFile);
      var entries = JsonConvert.DeserializeObject<Dictionary<string, string>>(entriesText);
      if (!entries.ContainsKey(name))
      {
        entries.Add(name, name);
        File.WriteAllText(entriesFile, JsonConvert.SerializeObject(entries));
      }
      await (await GetCipher(name)).EncryptToFileAsync(Path.Combine(dir, $"{name}.txt"), plainText, writeBase64: true);
    }

    [Command]
    private async Task CyptroUpdateTest1()
    {
      await Update("test", "a" + JsonConvert.SerializeObject(new List<AccountModel> {
        new AccountModel("gmail", "rik@gmail.com", "zmnumnxehvrk", null, null),
        new AccountModel("github", "rdragon", "vbmckxmmgpez", null, null)}, Helper.JsonSerializerSettings));
    }

    [Command]
    private async Task CyptroUpdateTest2()
    {
      await Update("test", "tSome text");
    }

    [Command]
    private async Task CyptroUpdateTest3()
    {
      await CyptroUpdateImage("test", @"C:\io\img.png");
    }

    [Command]
    private async Task CyptroUpdateImage(string name, string file)
    {
      Helper.RequireExistsFile(file);
      var plain = "i" + Convert.ToBase64String(File.ReadAllBytes(file));
      await Update(name, plain);
    }

    private class MyState : IState
    {
      public Dictionary<string, string> Passwords { get; set; }

      public (bool, string message) Fix()
      {
        Passwords = Passwords ?? new Dictionary<string, string>();
        return (true, "");
      }
    }
  }
}

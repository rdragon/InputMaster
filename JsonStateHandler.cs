using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InputMaster
{
  public class JsonStateHandler<T> : IStateHandler<T> where T : IState
  {
    private readonly string _file;
    private readonly StateHandlerFlags _flags;
    private bool _doNotSave;
    private bool _loaded;
    private T _state;
    private byte[] _hash = new byte[0]; // We use this to check for changes.

    public JsonStateHandler(T state, string file, StateHandlerFlags flags)
    {
      _state = state;
      _file = Path.IsPathRooted(file) ? file :
        Path.Combine(Env.Config.DataDir, file + (flags.HasFlag(StateHandlerFlags.UseCipher) ? "" : ".json"));
      _flags = flags;
      if (_flags.HasFlag(StateHandlerFlags.SavePeriodically))
        Env.App.AddSaveAction(SaveAsync);
    }

    public async Task<T> LoadAsync()
    {
      if (!File.Exists(_file))
        return await AskUserForStateIfUserEditableAsync();
      string text;
      if (_flags.HasFlag(StateHandlerFlags.UseCipher))
      {
        text = await Env.Cipher.TryDecryptFileAsync(_file);
        if (text == null)
        {
          await AskUserToDeleteAsync();
          text = "";
        }
      }
      else
        text = File.ReadAllText(_file);
      _hash = Helper.GetSha256(text);
      if ((Program.Reset || string.IsNullOrWhiteSpace(text)) && _flags.HasFlag(StateHandlerFlags.UserEditable))
        return await AskUserForStateAsync(text);
      if (string.IsNullOrWhiteSpace(text))
        return UseCurrentState();
      return await ParseTextAsync(text);
    }

    private async Task AskUserToDeleteAsync()
    {
      await Task.Yield(); // See Helper.TryGetStringAsync.
      var result = MessageBox.Show($"Could not decrypt '{_file}'. Do you want to delete this file?", "Warning",
        MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
      switch (result)
      {
        case DialogResult.Yes:
          File.Delete(_file);
          return;
        case DialogResult.No:
          _doNotSave = true;
          return;
        default:
          throw new ArgumentException("Aborted.");
      }
    }

    private Task<T> AskUserForStateIfUserEditableAsync()
    {
      if (!_flags.HasFlag(StateHandlerFlags.UserEditable))
        return Task.FromResult(UseCurrentState());
      return AskUserForStateAsync(null);
    }

    private async Task<T> AskUserForStateAsync(string text)
    {
      text = string.IsNullOrWhiteSpace(text) ? JsonConvert.SerializeObject(_state, Formatting.Indented) : text;
      text = await Helper.TryGetStringAsync(_file, text, selectAll: false, containsJson: true, throwOnCancel: true);
      return await ParseTextAsync(text);
    }

    private async Task<T> ParseTextAsync(string text)
    {
      T state;
      try
      {
        state = JsonConvert.DeserializeObject<T>(text);
      }
      catch (JsonException ex)
      {
        await Helper.ShowError("Cannot not parse input as json: " + ex);
        return await AskUserForStateAsync(text);
      }
      var (valid, message) = state.Fix();
      if (valid)
      {
        _state = state;
        return UseCurrentState();
      }
      await Helper.ShowError("Input is invalid:\n" + message);
      return await AskUserForStateAsync(text);
    }

    private T UseCurrentState()
    {
      _loaded = true;
      var (valid, message) = _state.Fix();
      if (!valid)
        throw new ArgumentException("State is invalid: " + message);
      return _state;
    }

    public Task SaveAsync()
    {
      if (!_loaded || _doNotSave || Program.ReadOnly)
        return Task.CompletedTask;
      var text = JsonConvert.SerializeObject(_state, Formatting.Indented);
      var hash = Helper.GetSha256(text);
      if (hash.SequenceEqual(_hash))
        return Task.CompletedTask;
      _hash = hash;
      Directory.CreateDirectory(Path.GetDirectoryName(_file));
      if (_flags.HasFlag(StateHandlerFlags.UseCipher))
        return Env.Cipher.EncryptToFileAsync(_file, text);
      File.WriteAllText(_file, text);
      return Task.CompletedTask;
    }
  }
}

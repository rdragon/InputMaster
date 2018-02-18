using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InputMaster.Actors
{
  public class SecondClipboard : Actor
  {
    private readonly List<string> _values = new List<string>();

    [Command]
    private async Task SecondClipboardCopyAsync()
    {
      var text = await ForegroundInteractor.GetSelectedTextAsync();
      _values.Add(text);
      Env.Notifier.Info($"'{Helper.Truncate(text, 20)}' copied");
    }

    [Command]
    private Task SecondClipboardPasteAsync()
    {
      var text = string.Join("  ", _values);
      if (text.Length > 0)
        return ForegroundInteractor.PasteAsync(text);
      return Task.CompletedTask;
    }

    [Command]
    private Task SecondClipboardPasteReverse()
    {
      var text = string.Join("  ", _values.Reverse<string>());
      return ForegroundInteractor.PasteAsync(text);
    }

    [Command]
    private void SecondClipboardClear()
    {
      _values.Clear();
    }
  }
}

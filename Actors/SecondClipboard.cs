using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InputMaster.Actors
{
  internal class SecondClipboard : Actor
  {
    private readonly List<string> Values = new List<string>();

    [Command]
    private async Task SecondClipboardCopyAsync()
    {
      var text = await ForegroundInteractor.GetSelectedTextAsync();
      Values.Add(text);
      Env.Notifier.Write($"'{Helper.Truncate(text, 20)}' copied");
    }

    [Command]
    private Task SecondClipboardPasteAsync()
    {
      var text = string.Join("  ", Values);
      if (text.Length > 0)
      {
        return ForegroundInteractor.PasteAsync(text);
      }
      return Task.CompletedTask;
    }

    [Command]
    private Task SecondClipboardPasteReverse()
    {
      var text = string.Join("  ", Values.Reverse<string>());
      return ForegroundInteractor.PasteAsync(text);
    }

    [Command]
    private void SecondClipboardClear()
    {
      Values.Clear();
    }
  }
}

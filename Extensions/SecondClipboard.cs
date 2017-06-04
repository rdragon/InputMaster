using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InputMaster.Extensions
{
  [CommandTypes(CommandTypes.Visible)]
  internal class SecondClipboard : Actor
  {
    private readonly List<string> Values = new List<string>();

    public async Task SecondClipboardCopy()
    {
      var text = await ForegroundInteractor.GetSelectedText();
      Values.Add(text);
      Env.Notifier.Write($"'{Helper.Truncate(text, 20)}' copied");
    }

    public async Task SecondClipboardPaste()
    {
      var text = string.Join("  ", Values);
      if (text.Length > 0)
      {
        await ForegroundInteractor.Paste(text);
      }
    }

    public async Task SecondClipboardPasteReverse()
    {
      var text = string.Join("  ", Values.Reverse<string>());
      await ForegroundInteractor.Paste(text);
    }

    public void SecondClipboardClear()
    {
      Values.Clear();
    }
  }
}

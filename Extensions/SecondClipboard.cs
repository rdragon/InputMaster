using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace InputMaster.Extensions
{
  [CommandTypes(CommandTypes.Visible)]
  class SecondClipboard
  {
    private readonly List<string> Values = new List<string>();
    private readonly ForegroundInteractor ForegroundInteractor;

    public SecondClipboard(ForegroundInteractor foregroundInteractor)
    {
      ForegroundInteractor = foregroundInteractor;
    }

    public async Task SecondClipboardCopy()
    {
      var s = await ForegroundInteractor.GetSelectedText();
      lock (Values)
      {
        Values.Add(s);
      }
      Env.Notifier.Write($"'{Helper.Truncate(s, 20)}' copied");
    }

    public async Task SecondClipboardPaste()
    {
      string s;
      lock (Values)
      {
        s = string.Join("  ", Values);
      }
      if (s.Length > 0)
      {
        await ForegroundInteractor.Paste(s);
      }
    }

    public async Task SecondClipboardPasteReverse()
    {
      string s;
      lock (Values)
      {
        s = string.Join("  ", Values.Reverse<string>());
      }
      await ForegroundInteractor.Paste(s);
    }

    public void SecondClipboardClear()
    {
      lock (Values)
      {
        Values.Clear();
      }
    }
  }
}

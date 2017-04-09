using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InputMaster
{
  class ForegroundInteractor
  {
    [CommandTypes(CommandTypes.Visible)]
    public async Task SumSelection()
    {
      await ModifySelectedText(s =>
      {
        double x = 0;
        foreach (var item in Regex.Split(s, @"\s+").Where(z => !string.IsNullOrWhiteSpace(z)))
        {
          x += double.Parse(item.Replace(",", "."), System.Globalization.NumberStyles.AllowDecimalPoint);
        }
        return x.ToString();
      });
    }

    [CommandTypes(CommandTypes.Visible)]
    public async Task SortSelectedLines()
    {
      await ModifySelectedLines(lines =>
      {
        lines.Sort();
        return lines;
      });
    }

    [CommandTypes(CommandTypes.Visible)]
    public async Task ReverseSelectedLines()
    {
      await ModifySelectedLines(lines =>
      {
        lines.Reverse();
        return lines;
      });
    }

    [CommandTypes(CommandTypes.Visible)]
    public async Task RandomlyPermuteSelectedLines()
    {
      await ModifySelectedLines(lines =>
      {
        var r = new Random();
        var n = lines.Count;
        var newLines = new List<string>();
        for (int i = 0; i < n; i++)
        {
          var j = r.Next(lines.Count);
          newLines.Add(lines[j]);
          lines.RemoveAt(j);
        }
        return newLines;
      });
    }

    [CommandTypes(CommandTypes.Visible)]
    public async Task ReplicateSelectedText()
    {
      await Task.Delay(1);
      var s = Helper.GetString("Count");
      var i = int.Parse(s);
      await ModifySelectedText(t => { return string.Concat(Enumerable.Repeat(t, i)); });
    }

    [CommandTypes(CommandTypes.Visible)]
    public async Task ListDirectorySizes()
    {
      var path = await GetSelectedText();
      var dir = new DirectoryInfo(path);
      Helper.RequireExists(dir);
      Helper.ShowSelectableText(Helper.AlignColumns(
        string.Join("\n",
        dir.GetDirectories()
        .Select(z => new { Dir = z, Size = Helper.GetLength(z) })
        .OrderBy(z => -z.Size)
        .Select(z => z.Dir.Name + "  " + Helper.ByteCountToString(z.Size)))));
    }

    [CommandTypes(CommandTypes.Visible)]
    public async Task PartitionSelectedLines()
    {
      var pattern = Helper.GetString("pattern");
      await ModifySelectedLines((lines) =>
      {
        var r = new Regex(pattern);
        var a = new List<string>();
        var b = new List<string>();
        foreach (var line in lines)
        {
          (r.IsMatch(line) ? a : b).Add(line);
        }
        return a.Concat(new string[] { "", "", "" }).Concat(b);
      });
    }

    [CommandTypes(CommandTypes.Visible)]
    public async Task Paste(string text)
    {
      await Helper.SetClipboardTextAsync(text.Replace("\n", Environment.NewLine));
      Env.CreateInjector().Add(Env.ForegroundListener.DynamicHotkeyCollection.GetAction(DynamicHotkeyEnum.Paste)).Run();
    }

    [CommandTypes(CommandTypes.Visible)]
    public static async Task ClearClipboard()
    {
      await Helper.ClearClipboardAsync();
    }

    public async Task ModifySelectedText(Func<string, string> func)
    {
      var s = await GetSelectedText();
      var t = func(s);
      await Paste(t);
    }

    private async Task ModifySelectedLines(Func<List<string>, IEnumerable<string>> func)
    {
      await ModifySelectedText(s =>
      {
        return string.Join("\n", func(s.Split('\n').ToList()));
      });
    }

    public static async Task ModifyClipboardText(Func<string, string> func)
    {
      var s = await Helper.GetClipboardTextAsync();
      var t = func(s);
      await Helper.SetClipboardTextAsync(t);
    }

    public async Task<string> GetWordAtCursor()
    {
      var injector = Env.CreateInjector();
      injector.Add(Input.Left, Modifiers.Ctrl);
      injector.Add(Input.Right, Modifiers.Ctrl | Modifiers.Shift);
      injector.Run();
      return await GetSelectedText();
    }

    public async Task<string> GetWordAtMouse()
    {
      Env.CreateInjector().Add(Input.Lmb, count: 2).Run();
      return await GetSelectedText();
    }

    public async Task<string> GetSelectedText()
    {
      await Helper.ClearClipboardAsync();
      Env.CreateInjector().Add(Env.ForegroundListener.DynamicHotkeyCollection.GetAction(DynamicHotkeyEnum.Copy)).Run();
      var s = await Helper.GetClipboardTextAsync();
      return s.Replace(Environment.NewLine, "\n");
    }
  }
}

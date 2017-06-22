using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InputMaster.Actors
{
  internal class ForegroundInteractor : Actor
  {
    [Command]
    public static Task SumSelection()
    {
      return ModifySelectedTextAsync(s =>
      {
        return Regex.Split(s, @"\s+")
          .Where(z => !string.IsNullOrWhiteSpace(z))
          .Sum(z => double.Parse(z.Replace(",", "."), NumberStyles.AllowDecimalPoint))
          .ToString();
      });
    }

    [Command]
    public static Task SortSelectedLines()
    {
      return ModifySelectedLinesAsync(lines =>
      {
        lines.Sort();
        return lines;
      });
    }

    [Command]
    public static Task ReverseSelectedLines()
    {
      return ModifySelectedLinesAsync(lines =>
      {
        lines.Reverse();
        return lines;
      });
    }

    [Command]
    public static Task RandomlyPermuteSelectedLines()
    {
      return ModifySelectedLinesAsync(lines =>
      {
        var r = new Random();
        var n = lines.Count;
        var newLines = new List<string>();
        for (var i = 0; i < n; i++)
        {
          var j = r.Next(lines.Count);
          newLines.Add(lines[j]);
          lines.RemoveAt(j);
        }
        return newLines;
      });
    }

    [Command]
    public static async Task ReplicateSelectedTextAsync()
    {
      await Task.Yield();
      if (!Helper.TryGetString("Count", out var s))
      {
        return;
      }
      var i = int.Parse(s);
      await ModifySelectedTextAsync(t => string.Concat(Enumerable.Repeat(t, i)));
    }

    [Command]
    public static async Task ListDirectorySizesAsync()
    {
      var dir = await GetSelectedTextAsync();
      Helper.RequireExistsDir(dir);
      Helper.ShowSelectableText(Helper.AlignColumns(
        string.Join("\n",
        Directory.EnumerateDirectories(dir)
        .Select(z => new { Dir = z, Size = Directory.EnumerateFiles(z, "*", SearchOption.AllDirectories).Aggregate(0L, (x, y) => x + y.Length) })
        .OrderBy(z => -z.Size)
        .Select(z => Path.GetFileName(z.Dir) + "  " + Helper.ByteCountToString(z.Size)))));
    }

    [Command]
    public static async Task PartitionSelectedLinesAsync()
    {
      await Task.Yield();
      if (!Helper.TryGetString("pattern", out var pattern))
      {
        return;
      }
      await ModifySelectedLinesAsync(lines =>
      {
        var r = new Regex(pattern);
        var a = new List<string>();
        var b = new List<string>();
        foreach (var line in lines)
        {
          (r.IsMatch(line) ? a : b).Add(line);
        }
        return a.Concat(new[] { "", "", "" }).Concat(b);
      });
    }

    [Command]
    public static async Task PasteAsync(string text)
    {
      await Helper.SetClipboardTextAsync(text);
      Env.Parser.GetAction(DynamicHotkeyEnum.Paste, out var action);
      Env.CreateInjector().Add(action).Run();
    }

    [Command]
    public static Task ClearClipboardAsync()
    {
      return Helper.ClearClipboardAsync();
    }

    private static async Task ModifySelectedTextAsync(Func<string, string> func)
    {
      var s = await GetSelectedTextAsync();
      var t = func(s);
      await PasteAsync(t);
    }

    private static Task ModifySelectedLinesAsync(Func<List<string>, IEnumerable<string>> func)
    {
      return ModifySelectedTextAsync(s => string.Join("\n", func(s.Split('\n').ToList())));
    }

    public static async Task ModifyClipboardTextAsync(Func<string, string> func)
    {
      var s = await Helper.GetClipboardTextAsync();
      var t = func(s);
      await Helper.SetClipboardTextAsync(t);
    }

    public static async Task<string> GetSelectedTextAsync()
    {
      await Helper.ClearClipboardAsync();
      Env.Parser.GetAction(DynamicHotkeyEnum.Copy, out var action);
      Env.CreateInjector().Add(action).Run();
      return await Helper.GetClipboardTextAsync();
    }
  }
}

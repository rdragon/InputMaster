using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InputMaster
{
  internal class ForegroundInteractor : Actor
  {
    [CommandTypes(CommandTypes.Visible)]
    public static async Task SumSelection()
    {
      await ModifySelectedText(s =>
      {
        return Regex.Split(s, @"\s+")
          .Where(z => !string.IsNullOrWhiteSpace(z))
          .Sum(z => double.Parse(z.Replace(",", "."), NumberStyles.AllowDecimalPoint))
          .ToString();
      });
    }

    [CommandTypes(CommandTypes.Visible)]
    public static async Task SortSelectedLines()
    {
      await ModifySelectedLines(lines =>
      {
        lines.Sort();
        return lines;
      });
    }

    [CommandTypes(CommandTypes.Visible)]
    public static async Task ReverseSelectedLines()
    {
      await ModifySelectedLines(lines =>
      {
        lines.Reverse();
        return lines;
      });
    }

    [CommandTypes(CommandTypes.Visible)]
    public static async Task RandomlyPermuteSelectedLines()
    {
      await ModifySelectedLines(lines =>
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

    [CommandTypes(CommandTypes.Visible)]
    public static async Task ReplicateSelectedText()
    {
      await Task.Yield();
      var s = Helper.GetString("Count");
      var i = int.Parse(s);
      await ModifySelectedText(t => string.Concat(Enumerable.Repeat(t, i)));
    }

    [CommandTypes(CommandTypes.Visible)]
    public static async Task ListDirectorySizes()
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
    public static async Task PartitionSelectedLines()
    {
      var pattern = Helper.GetString("pattern");
      await ModifySelectedLines(lines =>
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

    [CommandTypes(CommandTypes.Visible)]
    public static async Task Paste(string text)
    {
      await Helper.SetClipboardTextAsync(text.Replace("\n", Environment.NewLine));
      if (!Env.Parser.TryGetAction(DynamicHotkeyEnum.Paste, true, out var action))
      {
        return;
      }
      Env.CreateInjector().Add(action).Run();
    }

    [CommandTypes(CommandTypes.Visible)]
    public static async Task ClearClipboard()
    {
      await Helper.ClearClipboardAsync();
    }

    private static async Task ModifySelectedText(Func<string, string> func)
    {
      var s = await GetSelectedText();
      var t = func(s);
      await Paste(t);
    }

    private static async Task ModifySelectedLines(Func<List<string>, IEnumerable<string>> func)
    {
      await ModifySelectedText(s => string.Join("\n", func(s.Split('\n').ToList())));
    }

    public static async Task ModifyClipboardText(Func<string, string> func)
    {
      var s = await Helper.GetClipboardTextAsync();
      var t = func(s);
      await Helper.SetClipboardTextAsync(t);
    }

    public static async Task<string> GetSelectedText()
    {
      await Helper.ClearClipboardAsync();
      if (!Env.Parser.TryGetAction(DynamicHotkeyEnum.Copy, true, out var action))
      {
        return "";
      }
      Env.CreateInjector().Add(action).Run();
      var s = await Helper.GetClipboardTextAsync();
      return s.Replace(Environment.NewLine, "\n");
    }
  }
}

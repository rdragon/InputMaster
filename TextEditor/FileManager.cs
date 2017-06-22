using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using InputMaster.Parsers;
using System.Linq;

namespace InputMaster.TextEditor
{
  internal class FileManager
  {
    private readonly string DataDirName = "data";
    private readonly string NamesDirName = "names";
    private readonly ValueProvider<string> AccountFileProvider = new ValueProvider<string>();
    private readonly ValueProvider<string> SharedPasswordProvider = new ValueProvider<string>();
    private readonly ValueProvider<IEnumerable<SharedFile>> SharedFileProvider = new ValueProvider<IEnumerable<SharedFile>>();
    private bool Initialized;

    public FileManager(out IValueProvider<string> accountFileProvider, out IValueProvider<string> sharedPasswordProvider,
      out IValueProvider<IEnumerable<SharedFile>> sharedFileProvider)
    {
      accountFileProvider = AccountFileProvider;
      sharedPasswordProvider = SharedPasswordProvider;
      sharedFileProvider = SharedFileProvider;
      Directory.CreateDirectory(DataDir);
      Directory.CreateDirectory(NamesDir);
      InitializeAsync();
    }

    private async void InitializeAsync()
    {
      Env.Parser.DisableOnce();
      await CompileTextEditorModeAsync();
      Initialized = true;
      Env.Parser.EnableOnce();
    }

    public Action<string> OpenFile { private get; set; }
    private string DataDir => GetDataDir(Env.Config.TextEditorDir);
    private string NamesDir => GetNamesDir(Env.Config.TextEditorDir);

    public string GetDataFile(string nameOrDataFile)
    {
      return Path.Combine(GetDataDir(Path.GetDirectoryName(Path.GetDirectoryName(nameOrDataFile))), Path.GetFileName(nameOrDataFile));
    }

    private string GetNameFile(string nameOrDataFile)
    {
      return Path.Combine(GetNamesDir(Path.GetDirectoryName(Path.GetDirectoryName(nameOrDataFile))), Path.GetFileName(nameOrDataFile));
    }

    private string GetDataDir(string parentDir)
    {
      return Path.Combine(parentDir, DataDirName);
    }

    private string GetNamesDir(string parentDir)
    {
      return Path.Combine(parentDir, NamesDirName);
    }

    public async Task<int> ExportToDirectoryAsync(string sourceDir, string targetDir)
    {
      var fromNamesDir = GetNamesDir(sourceDir);
      var count = 0;
      Directory.CreateDirectory(targetDir);
      foreach (var file in Directory.EnumerateFiles(fromNamesDir))
      {
        var title = await Env.Cipher.DecryptAsync(file);
        var text = await Env.Cipher.DecryptAsync(GetDataFile(file));
        var name = Helper.GetValidFileName(title, '_');
        var targetFile = new FileInfo(Path.Combine(targetDir, name + ".txt"));
        File.WriteAllText(targetFile.FullName, title + "\n" + text);
        count++;
      }
      return count;
    }

    private void HandleAccountFile(List<FileLink> links)
    {
      var accountFiles = links.Where(z => z.Title.Contains(Constants.AccountFileTag)).Take(2).ToList();
      if (accountFiles.Count > 1)
      {
        Env.Notifier.WriteError("Multiple account files found.");
      }
      else if (accountFiles.Count == 0 && Env.Config.Warnings.HasFlag(Warnings.MissingAccountFile))
      {
        Env.Notifier.WriteWarning($"No accounts file found (required tag: {Constants.AccountFileTag}).");
      }
      AccountFileProvider.SetValue(accountFiles.Count == 0 ? null : GetDataFile(accountFiles[0].File));
    }

    private async Task HandleSharedPasswordFileAsync(List<FileLink> links)
    {
      var sharedPasswordFiles = links.Where(z => z.Title.Contains(Constants.SharedPasswordFileTag)).Take(2).ToList();
      if (sharedPasswordFiles.Count > 1)
      {
        Env.Notifier.WriteError("Multiple shared password files found.");
      }
      else if (sharedPasswordFiles.Count == 0 && Env.Config.Warnings.HasFlag(Warnings.MissingSharedPasswordFile))
      {
        Env.Notifier.WriteWarning($"No shared password file found (required tag: {Constants.SharedPasswordFileTag}).");
      }
      SharedPasswordProvider.SetValue(sharedPasswordFiles.Count == 0 ? null : await Env.Cipher.DecryptAsync(GetDataFile(sharedPasswordFiles[0].File)));
    }

    private async Task HandleHotkeyFileAsync(List<FileLink> links)
    {
      var hotkeyFiles = links.Where(z => z.Title.Contains(Constants.HotkeyFileTag)).Take(2).ToList();
      if (hotkeyFiles.Count == 0)
      {
        return;
      }
      if (hotkeyFiles.Count > 1)
      {
        Env.Notifier.WriteError("Multiple hotkey files found.");
      }
      var text = await Env.Cipher.DecryptAsync(GetDataFile(hotkeyFiles[0].File));
      Env.Parser.UpdateHotkeyFile(new HotkeyFile(nameof(TextEditorForm), text));
    }

    public async Task CompileTextEditorModeAsync()
    {
      var links = new List<FileLink>();
      var sharedFiles = new List<SharedFile>();
      foreach (var file in Directory.EnumerateFiles(NamesDir))
      {
        var title = await Env.Cipher.DecryptAsync(file);
        links.Add(new FileLink { Title = title, File = file });
        if (Constants.SharedFileRegex.IsMatch(title))
        {
          sharedFiles.Add(new SharedFile(title, file, GetDataFile(file)));
        }
      }
      SharedFileProvider.SetValue(sharedFiles);
      await HandleHotkeyFileAsync(links);
      if (!Initialized)
      {
        HandleAccountFile(links);
        await HandleSharedPasswordFileAsync(links);
      }
      Env.Parser.UpdateParseAction(nameof(TextEditorForm), parserOutput =>
      {
        var mode = parserOutput.AddMode(new Mode(Env.Config.TextEditorModeName, true));
        foreach (var link in links)
        {
          var file = link.File;
          var title = link.Title;
          if (!Env.Config.TryGetChordText(title, out var chordText))
          {
            continue;
          }
          var chord = Env.Config.DefaultChordReader.CreateChord(new LocatedString(chordText));

          void Action(Combo combo)
          {
            OpenFile(file);
            if (Env.Config.TextEditorDesktopHotkey != Combo.None)
            {
              Env.CreateInjector().Add(Env.Config.TextEditorDesktopHotkey).Run();
            }
          }

          var hotkey = new ModeHotkey(chord, Action, title);
          mode.AddHotkey(hotkey);
        }
      });
      Env.Parser.Run();
    }

    public async Task ImportFromDirectoryAsync()
    {
      if (!Helper.TryGetString("Please give a directory from which to import all text files.", out var dir))
      {
        return;
      }
      Helper.ForbidWhitespace(dir, nameof(dir));
      var count = 0;
      foreach (var file in new DirectoryInfo(dir).GetFiles("*.txt"))
      {
        var s = Helper.ReadAllText(file.FullName);
        var i = s.IndexOf('\n');
        var failed = false;
        if (i == -1)
        {
          failed = true;
        }
        else
        {
          var title = s.Substring(0, i).Trim();
          if (title.Length == 0)
          {
            failed = true;
          }
          else
          {
            var text = s.Substring(i + 1);
            await CreateNewFileAsync(title, text);
            count++;
          }
        }
        if (failed)
        {
          Env.Notifier.WriteError($"Cannot import '{file.FullName}', file is in incorrect format.");
        }
      }
      Env.Notifier.Write($"Imported {count} files from '{dir}'.");
      await CompileTextEditorModeAsync();
    }

    public async Task ExportToDirectoryAsync()
    {
      if (!Helper.TryGetString("Please give a directory to which to export all text files.", out var dir))
      {
        return;
      }
      Helper.ForbidWhitespace(dir, nameof(dir));
      var count = await Task.Run(() => ExportToDirectoryAsync(Env.Config.TextEditorDir, dir));
      Env.Notifier.Write($"Exported {count} files to '{dir}'.");
    }

    public async Task FindAllAsync()
    {
      if (!Helper.TryGetString("Find All", out var s))
      {
        return;
      }
      var r = Helper.GetRegex(s, RegexOptions.IgnoreCase);
      var log = await Task.Run(async () =>
      {
        var sb = new StringBuilder();
        foreach (var dataFile in Directory.GetFiles(DataDir))
        {
          var text = await Env.Cipher.DecryptAsync(dataFile);
          var count = r.Matches(text).Count;
          if (count > 0)
          {
            sb.Append($"{await Env.Cipher.DecryptAsync(GetNameFile(dataFile))}  ({count} match{(count != 1 ? "es" : "")})\n");
          }
        }
        return sb;
      });
      Helper.ShowSelectableText(log.Length == 0 ? "No matches found!" : log.ToString());
    }

    public async Task<string> CreateNewFileAsync(string title, string text)
    {
      var i = 0;
      while (true)
      {
        var file = Path.Combine(NamesDir, i.ToString());
        if (!File.Exists(file))
        {
          await Env.Cipher.EncryptAsync(file, title);
          await Env.Cipher.EncryptAsync(GetDataFile(file), text);
          return file;
        }
        i++;
      }
    }

    private class FileLink
    {
      public string File { get; set; }
      public string Title { get; set; }
    }
  }
}

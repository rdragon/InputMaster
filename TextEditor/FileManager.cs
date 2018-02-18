using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using InputMaster.Parsers;
using System.Linq;
using System.Windows.Forms;

namespace InputMaster.TextEditor
{
  /// <summary>
  /// Keeps track of the index, a dictionary from file names to file titles.
  /// Updates text file titles.
  /// Creates new text files.
  /// Compiles the two text editor modes.
  /// </summary>
  public class FileManager
  {
    private MyState _state;

    private FileManager()
    {
      Directory.CreateDirectory(Env.Config.TextEditorDir);
      Env.Parser.UpdateParseAction(nameof(TextEditorForm), parserOutput =>
      {
        var mode = parserOutput.AddMode(new Mode(Env.Config.TextEditorModeName, true));
        var sharedMode = parserOutput.AddMode(new Mode(Env.Config.TextEditorSharedModeName, true));
        foreach (var pair in _state.Index)
        {
          var name = pair.Key;
          var title = pair.Value;
          if (!Helper.TryGetChordText(title, out var chordText))
            continue;
          var chord = Env.Config.DefaultChordReader.CreateChord(new LocatedString(chordText));

          void Action(Combo combo)
          {
            OpenFile(name);
            if (Env.Config.TextEditorDesktopHotkey != Combo.None)
              Env.CreateInjector().Add(Env.Config.TextEditorDesktopHotkey).Run();
          }

          mode.AddHotkey(new ModeHotkey(chord, Action, title));
          if (title.IndexOf(Env.Config.SharedTag, StringComparison.OrdinalIgnoreCase) >= 0)
            sharedMode.AddHotkey(new ModeHotkey(chord, Action, Helper.StripTags(title)));
        }
      });
    }

    private async Task<FileManager> Initialize()
    {
      var stateHandler = Env.StateHandlerFactory.Create(new MyState(), Path.Combine(Env.Config.TextEditorDir, Env.Config.IndexFileName),
         StateHandlerFlags.UseCipher | StateHandlerFlags.SavePeriodically);
      _state = await stateHandler.LoadAsync();
      if (_state.Index.Count == 0)
        await CreateIndex();
      if (File.Exists(Env.Config.TextEditorHotkeyFile))
        await LoadTextEditorHotkeyFile();
      return this;
    }

    public static Task<FileManager> GetFileManagerAsync()
    {
      return new FileManager().Initialize();
    }

    public string GetTitle(string name)
    {
      return _state.Index[name];
    }

    public void SetTitle(string name, string title)
    {
      _state.Index[name] = title;
    }

    private async Task LoadTextEditorHotkeyFile()
    {
      var text = new TitleTextPair(await Env.Cipher.DecryptFileAsync(Env.Config.TextEditorHotkeyFile)).Text;
      Env.Parser.UpdateHotkeyFile(new HotkeyFile(nameof(TextEditorForm), text));
    }

    private async Task CreateIndex()
    {
      _state.Index.Clear();
      foreach (var file in Directory.GetFiles(Env.Config.TextEditorDir).Where(z => Path.GetFileName(z) != Env.Config.IndexFileName))
      {
        var name = Path.GetFileName(file);
        var pair = new TitleTextPair(await Env.Cipher.DecryptFileAsync(file));
        _state.Index[name] = pair.Title;
      }
    }

    public static string GetFile(string name)
    {
      return Path.Combine(Env.Config.TextEditorDir, name);
    }

    /// <summary>
    /// This event is fired by the compiled text editor mode.
    /// </summary>
    public Action<string> OpenFile { private get; set; }

    /// <summary>
    /// Thread-safe.
    /// </summary>
    public static Task<int> ExportToDirectoryAsync(string sourceDir, string targetDir)
    {
      return Task.Run(() => ExportToDirectoryInternalAsync(sourceDir, targetDir));
    }

    /// <summary>
    /// Thread-safe.
    /// </summary>
    private static async Task<int> ExportToDirectoryInternalAsync(string sourceDir, string targetDir)
    {
      var count = 0;
      Directory.CreateDirectory(targetDir);
      foreach (var file in Directory.EnumerateFiles(sourceDir))
      {
        var name = Path.GetFileName(file);
        if (name == Env.Config.IndexFileName)
          continue;
        var contents = await Env.Cipher.DecryptFileAsync(file);
        var targetFile = new FileInfo(Path.Combine(targetDir, name + ".txt"));
        Helper.WriteAllText(targetFile.FullName, contents);
        count++;
      }
      return count;
    }

    public void CompileTextEditorMode()
    {
      if (!Env.Config.EnableTextEditor)
        return;
      Env.Parser.Run();
    }

    public async Task ImportFromDirectoryAsync()
    {
      var dir = await Helper.TryGetStringAsync("Please give a directory from which to import all text files.");
      if (dir == null)
        return;
      var generateNames = MessageBox.Show("Generate random file names?", "Generate random file names?", MessageBoxButtons.YesNo) ==
        DialogResult.Yes;
      var count = 0;
      foreach (var file in Directory.GetFiles(dir, "*.txt"))
      {
        var s = await Helper.ReadAllTextAsync(file);
        var i = s.IndexOf('\n');
        bool failed;
        if (i == -1)
          failed = true;
        else
        {
          var title = s.Substring(0, i).Trim();
          if (title.Length == 0)
            failed = true;
          else
          {
            var text = s.Substring(i + 1);
            await CreateNewFileAsync(title, text, generateNames ? null : Path.GetFileNameWithoutExtension(file));
            count++;
            failed = false;
          }
        }
        if (failed)
          Env.Notifier.Error($"Cannot import '{file}', file is in incorrect format.");
      }
      Env.Notifier.Info($"Imported {count} file(s) from '{dir}'.");
    }

    public static async Task ExportToDirectoryAsync()
    {
      var dir = await Helper.TryGetStringAsync("Please give a directory to which to export all text files.");
      if (dir == null)
        return;
      Helper.ForbidWhitespace(dir, nameof(dir));
      var count = await ExportToDirectoryAsync(Env.Config.TextEditorDir, dir);
      Env.Notifier.Info($"Exported {count} files to '{dir}'.");
    }

    public async Task FindAllAsync()
    {
      var s = await Helper.TryGetStringAsync("Find All");
      if (s == null)
        return;
      var r = Helper.GetRegex(s, RegexOptions.IgnoreCase);
      // Thread-safe.
      var (log, errorLog) = await Task.Run(async () =>
      {
        var sbLog = new StringBuilder();
        var sbErrorLog = new StringBuilder();
        foreach (var file in Directory.GetFiles(Env.Config.TextEditorDir))
        {
          if (Path.GetFileName(file).Equals(Env.Config.IndexFileName, StringComparison.OrdinalIgnoreCase))
            continue;
          var text = await Env.Cipher.TryDecryptFileAsync(file);
          if (text == null)
          {
            sbErrorLog.Append($"File '{file}' could not be decrypted.\n");
            continue;
          }
          var pair = new TitleTextPair(text);
          if (pair.Text == null)
          {
            sbErrorLog.Append($"File '{file}' is not in correct format.\n");
            continue;
          }
          var count = r.Matches(pair.Text).Count;
          if (count > 0)
            sbLog.Append($"{pair.Title}  ({count} match{(count != 1 ? "es" : "")})\n");
        }
        return (sbLog, sbErrorLog);
      });
      if (0 < errorLog.Length)
        Env.Notifier.Error(errorLog.ToString());
      Helper.ShowSelectableText(log.Length == 0 ? "No matches found!" : log.ToString());
    }

    public async Task<string> CreateNewFileAsync(string title, string text, string name = null)
    {
      name = name ?? Helper.GetRandomName(Env.Config.TextEditorFileNameLength);
      var file = GetFile(name);
      await Env.Cipher.EncryptToFileAsync(file, title + "\n" + text);
      _state.Index[name] = title;
      return name;
    }

    public void DeleteFile(string name)
    {
      _state.Index.Remove(name);
      File.Delete(GetFile(name));
    }

    public class MyState : IState
    {
      public Dictionary<string, string> Index { get; set; }

      public (bool, string message) Fix()
      {
        Index = Index ?? new Dictionary<string, string>();
        return (true, "");
      }
    }
  }
}

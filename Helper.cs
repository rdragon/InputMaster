using InputMaster.Forms;
using InputMaster.Hooks;
using InputMaster.Win32;
using InputMaster.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InputMaster
{
  static class Helper
  {
    public static string CreateTokenString(string text)
    {
      return Config.TokenStart + text + Config.TokenEnd;
    }

    public static string ReadIdentifierTokenString(LocatedString locatedString)
    {
      if (Regex.IsMatch(locatedString.Value, "^" + Config.IdentifierTokenPattern + "$"))
      {
        return locatedString.Value.Substring(1, locatedString.Length - 2);
      }
      else
      {
        throw new ParseException(locatedString, "Not in correct format. Expecting an identifier token.");
      }
    }

    public static void SetForegroundWindowForce(IntPtr window)
    {
      NativeMethods.ShowWindow(window, ShowWindowArgument.Minimize);
      NativeMethods.ShowWindow(window, ShowWindowArgument.Restore);
      NativeMethods.SetForegroundWindow(window);
    }

    public static void CloseWindow(IntPtr window)
    {
      NativeMethods.SendNotifyMessage(window, WindowMessage.Close, IntPtr.Zero, IntPtr.Zero);
    }

    public static int GetMouseWheelDelta(Message m)
    {
      return (m.WParam.ToInt32() >> 16);
    }

    public static void PushRange<T>(Stack<T> stack, IEnumerable<T> values)
    {
      foreach (var value in values)
      {
        stack.Push(value);
      }
    }

    public static void StartProcess(string fileName, string arguments = "", bool captureForeground = false)
    {
      if (captureForeground)
      {
        Env.Notifier.CaptureForeground();
      }
      try
      {
        var p = Process.Start(fileName, arguments);
        if (p != null)
        {
          p.Dispose();
        }
      }
      catch (Exception ex)
      {
        throw new WrappedException("Failed to start process" + GetBindingsSuffix(fileName, nameof(fileName), arguments, nameof(arguments)), ex);
      }
    }

    public static void HandleFatalException(Exception exception, string suffix = "")
    {
      Try.SetException(new WrappedException(GetUnhandledExceptionFatalErrorMessage(suffix), exception));
      Env.Notifier.RequestExit();
    }

    public static string GetUnhandledExceptionWarningMessage(string suffix = "")
    {
      if (suffix != "")
      {
        suffix = " " + suffix;
      }
      return $"Unhandled exception thrown{suffix}.";
    }

    public static string GetUnhandledExceptionFatalErrorMessage(string suffix = "")
    {
      if (suffix != "")
      {
        suffix = " " + suffix;
      }
      return $"Unhandled exception thrown{suffix}. Exiting program.";
    }

    public static void Unhook()
    {
      PrimaryHook.Unhook();
    }

    public static void ShowSelectableText(object value, bool scrollToBottom = false)
    {
      var str = value.ToString();
      if (str.Length > 0)
      {
        new ShowStringForm(str, scrollToBottom).Show();
      }
    }

    public static T Exchange<T>(ref T value, T newValue)
    {
      var oldValue = value;
      value = newValue;
      return oldValue;
    }

    public static string GetString(string title, string defaultValue = null, bool selectAll = true)
    {
      using (var form = new GetStringForm(ForbidNull(title, nameof(title)), defaultValue, selectAll))
      {
        form.ShowDialog();
        return form.GetValue();
      }
    }

    public static string GetStringLine(string title, string defaultValue = null, bool isPassword = false)
    {
      using (var form = new GetStringLineForm(ForbidNull(title, nameof(title)), defaultValue, isPassword))
      {
        form.ShowDialog();
        return form.GetValue();
      }
    }

    public static async Task<string> GetClipboardTextAsync()
    {
      for (int i = Config.ClipboardTries; i > 0; i--)
      {
        try
        {
          var s = Clipboard.GetText();
          if (!string.IsNullOrEmpty(s))
          {
            return s;
          }
        }
        catch (ExternalException) { }
        if (i > 1)
        {
          await Task.Delay(Config.ClipboardDelay);
        }
      }
      throw new IOException("Failed to retrieve text data from the clipboard.");
    }

    public static async Task SetClipboardTextAsync(string text)
    {
      for (int i = Config.ClipboardTries; i > 0; i--)
      {
        try
        {
          Clipboard.SetText(text);
          return;
        }
        catch (ExternalException) { }
        if (i > 1)
        {
          await Task.Delay(Config.ClipboardDelay);
        }
      }
      throw new IOException("Failed to write text data to the clipboard.");
    }

    public static async Task ClearClipboardAsync()
    {
      for (int i = Config.ClipboardTries; i > 0; i--)
      {
        try
        {
          Clipboard.Clear();
          return;
        }
        catch (ExternalException) { }
        if (i > 1)
        {
          await Task.Delay(Config.ClipboardDelay);
        }
      }
      throw new IOException("Failed to clear the clipboard.");
    }

    public static void TryCatchLog(Action action)
    {
      try
      {
        action();
      }
      catch (Exception ex) when (!IsCriticalException(ex))
      {
        Env.Notifier.WriteError(ex);
      }
    }

    public static void Run(Func<Task> action)
    {
      Task.Factory.StartNew(async () =>
      {
        try
        {
          await action();
        }
        catch (Exception ex)
        {
          HandleFatalException(ex);
        }
      }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public static void Run(Action action)
    {
      Task.Factory.StartNew(() =>
      {
        try
        {
          action();
        }
        catch (Exception ex)
        {
          HandleFatalException(ex);
        }
      }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.FromCurrentSynchronizationContext());
    }



    public static void Swap<T>(ref T val1, ref T val2)
    {
      T swap = val1;
      val1 = val2;
      val2 = swap;
    }

    public static async Task<T> TryAsync<T>(Func<T> action, int count = 10, int pauseInterval = 100)
    {
      for (int i = 0; i < count - 1; i++)
      {
        try
        {
          return action();
        }
        catch (Exception ex) when (!IsCriticalException(ex))
        {
          await Task.Delay(pauseInterval);
        }
      }
      return action();
    }

    public static async Task TryAsync(Action action, int count = 10, int pauseInterval = 100)
    {
      await TryAsync((Func<object>)(() => { action(); return null; }), count, pauseInterval);
    }

    public static void Beep()
    {
      Task.Run(() => { Console.Beep(); });
    }

    public static bool IsPowerOfTwo(int n)
    {
      if ((n & 1) != 0)
      {
        return n == 1;
      }
      else if (n != 0)
      {
        return IsPowerOfTwo(n >> 1);
      }
      else
      {
        return false;
      }
    }

    public static int CountOnes(int i)
    {
      i = i - ((i >> 1) & 0x55555555);
      i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
      return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
    }

    public static T ForbidNull<T>(T value, string name)
    {
      if (value == null)
      {
        throw new ArgumentNullException(name);
      }
      else
      {
        return value;
      }
    }

    public static string ForbidNullOrEmpty(string str, string name)
    {
      ForbidNull(str, name);
      if (str.Length == 0)
      {
        throw new ArgumentException("Unexpected empty string.", name);
      }
      else
      {
        return str;
      }
    }

    public static string ForbidNullOrWhitespace(string str, string name)
    {
      ForbidNull(str, name);
      if (string.IsNullOrWhiteSpace(str))
      {
        throw new ArgumentException("String consists of whitespace only.", name);
      }
      else
      {
        return str;
      }
    }

    public static bool IsCriticalException(Exception ex)
    {
      return ex is StackOverflowException || ex is OutOfMemoryException || ex is ThreadAbortException || ex is AssertFailedException || ex is NullReferenceException || ex is IndexOutOfRangeException || ex is AccessViolationException || (ex.InnerException != null && IsCriticalException(ex.InnerException));
    }

    public static bool HasAssertFailed(Exception ex)
    {
      return ex is AssertFailedException || (ex.InnerException != null && HasAssertFailed(ex.InnerException));
    }

    public static string ByteCountToString(long byteCount)
    {
      string[] labels = { "B", "KB", "MB", "GB", "TB" };
      double length = byteCount;
      int i = 0;
      while (length >= 1024 && i < labels.Length - 1)
      {
        i++;
        length = length / 1024;
      }
      return string.Format("{0:0.##} {1}", length, labels[i]);
    }

    public static void RequireExists(FileSystemInfo fsi)
    {
      if (!fsi.Exists)
      {
        if (fsi is FileInfo)
        {
          throw new FileNotFoundException($"File '{fsi.FullName}' not found.");
        }
        else
        {
          throw new DirectoryNotFoundException($"Directory '{fsi.FullName}' not found.");
        }
      }
    }

    public static void ForbidExists(FileSystemInfo fsi)
    {
      if (fsi.Exists)
      {
        if (fsi is FileInfo)
        {
          throw new FileNotFoundException($"File '{fsi.FullName}' already exists.");
        }
        else
        {
          throw new DirectoryNotFoundException($"Directory '{fsi.FullName}' already exists.");
        }
      }
    }

    /// <summary>
    /// Removes or replaces all invalid file name characters.
    /// </summary>
    public static string GetValidFileName(string name, char? replacement = null)
    {
      ForbidNullOrWhitespace(name, nameof(name));
      var invalidChars = Path.GetInvalidFileNameChars();
      if (replacement.HasValue)
      {
        if (invalidChars.Contains(replacement.Value))
        {
          throw new ArgumentException($"Invalid file name character '{replacement.Value}' used as replacement character.", nameof(replacement));
        }
        return new string(name.Select(c =>
        {
          if (invalidChars.Contains(c))
          {
            return replacement.Value;
          }
          else
          {
            return c;
          }
        }).ToArray());
      }
      else
      {
        var s = new string(name.Where(c => !invalidChars.Contains(c)).ToArray());
        if (string.IsNullOrWhiteSpace(s))
        {
          return "New File";
        }
        else
        {
          return s;
        }
      }
    }

    public static string RequireValidPath(string path)
    {
      ForbidNull(path, nameof(path));
      Path.GetFullPath(path); // throws exception on non-valid path
      return path;
    }

    public static string RequireValidFilePath(string path)
    {
      RequireValidPath(path);
      if (string.IsNullOrWhiteSpace(Path.GetFileName(path)))
      {
        throw new ArgumentException($"The path '{path}' does not point to a file.");
      }
      else
      {
        return path;
      }
    }

    public static string RequireValidFileName(string name)
    {
      if (!IsValidFileName(name))
      {
        throw new ArgumentException($"The name '{name}' is not a valid file name.");
      }
      else
      {
        return name;
      }
    }

    public static bool IsValidPath(string path)
    {
      try
      {
        RequireValidPath(path);
        return true;
      }
      catch (Exception ex) when (!IsCriticalException(ex))
      {
        return false;
      }
    }

    public static bool IsValidFileName(string name)
    {
      ForbidNullOrWhitespace(name, nameof(name));
      return GetValidFileName(name) == name;
    }

    /// <summary>
    /// Returns the sum of the lengths of all files in the directory.
    /// </summary>
    public static long GetLength(DirectoryInfo dir)
    {
      ForbidNull(dir, nameof(dir));
      long size = 0;
      foreach (var item in dir.EnumerateFiles("*", SearchOption.AllDirectories))
      {
        size += item.Length;
      }
      return size;
    }

    public static void Delete(DirectoryInfo dir)
    {
      foreach (var file in dir.GetFiles("*", SearchOption.AllDirectories))
      {
        file.Attributes = FileAttributes.Normal;
      }
      dir.Delete(true);
    }

    public static void Delete(FileInfo file)
    {
      file.Attributes = FileAttributes.Normal;
      file.Delete();
    }

    /// <summary>
    /// Copies a complete directory.
    /// Assumption: <paramref name="targetDir"/>  is not a subdirectory of <paramref name="sourceDir"/>.
    /// </summary>
    public static void Copy(DirectoryInfo sourceDir, DirectoryInfo targetDir, bool overwrite = false)
    {
      RequireExists(sourceDir);
      if (sourceDir.FullName.ToLower() == targetDir.FullName.ToLower())
      {
        return;
      }
      targetDir.Create();
      var targetPath = targetDir.FullName;
      foreach (FileInfo file in sourceDir.GetFiles())
      {
        Copy(file, new FileInfo(Path.Combine(targetPath, file.Name)), overwrite);
      }
      foreach (DirectoryInfo dir in sourceDir.GetDirectories())
      {
        Copy(dir, new DirectoryInfo(Path.Combine(targetPath, dir.Name)), overwrite);
      }
    }

    public static void Copy(FileInfo sourceFile, FileInfo targetFile, bool overwrite = false)
    {
      if (!overwrite)
      {
        ForbidExists(targetFile);
      }
      if (targetFile.Exists)
      {
        targetFile.Attributes = FileAttributes.Normal;
      }
      File.Copy(sourceFile.FullName, targetFile.FullName, overwrite);
      targetFile.Refresh();
    }

    /// <summary>
    /// Asynchronously reads the contents of a file. Throws an exception after a certain number of tries.
    /// </summary>
    public static async Task<string> ReadAllTextAsync(FileInfo file)
    {
      ForbidNull(file, nameof(file));
      return await TryAsync(() => { return File.ReadAllText(file.FullName).Replace("\r\n", "\n"); });
    }

    /// <summary>
    /// Returns a path based on <paramref name="path"/> that doesn't point to any file or directory.
    /// </summary>
    public static string GetNewPath(string path)
    {
      RequireValidPath(path);
      var file = new FileInfo(path);
      var dir = new DirectoryInfo(path);
      if (!file.Exists && !dir.Exists)
      {
        return path;
      }
      else
      {
        string basePath;
        string extension;
        if (file.Exists)
        {
          basePath = Path.Combine(file.DirectoryName, Path.GetFileNameWithoutExtension(file.Name));
          extension = file.Extension;
        }
        else
        {
          basePath = dir.FullName;
          extension = "";
        }
        int i = 0;
        while (true)
        {
          i++;
          var newPath = $"{basePath} ({i}){extension}";
          if (!File.Exists(newPath) && !Directory.Exists(newPath))
          {
            return newPath;
          }
        }
      }
    }

    /// <summary>
    /// Returns the index of the first character of the line containing the given index (a line has the form '[^\n]*\n').
    /// </summary>
    public static int GetLineStart(string text, int index)
    {
      ForbidNull(text, nameof(text));
      RequireInInterval(index, nameof(index), 0, text.Length);
      if (index == 0)
      {
        return 0;
      }
      else
      {
        return text.LastIndexOf('\n', index - 1) + 1;
      }
    }

    /// <summary>
    /// Returns the index of the last character of the line containing the given index.
    /// </summary>
    public static int GetLineEnd(string text, int index)
    {
      ForbidNull(text, nameof(text));
      RequireInInterval(index, nameof(index), 0, text.Length);
      for (; index < text.Length; index++)
      {
        if (text[index] == '\n')
        {
          break;
        }
      }
      return index;
    }

    /// <summary>
    /// Returns a string of a maximum specified length, with trailing ellipses when the given string needs to be truncated.
    /// </summary>
    public static string Truncate(string text, int length)
    {
      if (text == null)
      {
        return null;
      }
      else if (text.Length <= length)
      {
        return text;
      }
      else if (length < 3)
      {
        throw new ArgumentOutOfRangeException(nameof(length), length, "Length should be at least 3.");
      }
      else
      {
        int i = length - 3;
        if (char.IsLowSurrogate(text[i]))
        {
          i--;
        }
        return text.Substring(0, i) + "...";
      }
    }

    /// <summary>
    /// If given string is of the form '/{0}/', then a regex with pattern {0} is returned.
    /// Otherwise the string is parsed as a literal string, and a regex matching the given string is returned.
    /// </summary>
    public static Regex GetRegex(string text, RegexOptions options = RegexOptions.None, bool fullMatchIfLiteral = false)
    {
      ForbidNull(text, nameof(text));
      text = Parser.RunPreprocessor(text);
      if (text.Length > 1 && text[0] == '/' && text[text.Length - 1] == '/')
      {
        text = text.Substring(1, text.Length - 2);
      }
      else
      {
        text = Regex.Escape(text);
        if (fullMatchIfLiteral)
        {
          text = "^" + text + "$";
        }
      }
      return new Regex(text, options);
    }

    /// <summary>
    /// Example: "some string (ss)" -> "ss".
    /// Example: "some other string" -> null.
    /// </summary>
    public static string GetChordText(string text)
    {
      ForbidNull(text, nameof(text));
      var m = Regex.Match(text, @"\((.+)\) *$");
      if (m.Success)
      {
        return m.Groups[1].Value;
      }
      else
      {
        return null;
      }
    }

    public static byte[] GetSecureHash(string text)
    {
      return new SHA256Managed().ComputeHash(Encoding.UTF8.GetBytes(text));
    }

    /// <summary>
    /// For each two adjacent lines with the same indentation and that both contain a column boundary, the column boundaries are aligned. A column boundary is a string of at least <paramref name="minSpaceCount"/> spaces (outside the indentation).
    /// </summary>
    public static string AlignColumns(string text, int minSpaceCount = 2)
    {
      RequireAtLeast(minSpaceCount, nameof(minSpaceCount), 1);
      if (text == null)
      {
        return null;
      }
      var lines = text.Split('\n');
      AlignColumns(lines);
      return string.Join("\n", lines);
    }

    /// <summary>
    /// For each two adjacent lines with the same indentation and that both contain a column boundary, the column boundaries are aligned. A column boundary is a string of at least <paramref name="minSpaceCount"/> spaces (outside the indentation).
    /// </summary>
    public static void AlignColumns(string[] lines, int minSpaceCount = 2)
    {
      RequireAtLeast(minSpaceCount, nameof(minSpaceCount), 1);
      if (lines == null)
      {
        return;
      }
      var indents = new int[lines.Length];
      var columns = new int[lines.Length];
      var spaces = new string(' ', minSpaceCount);
      int i;
      for (i = 0; i < lines.Length; i++)
      {
        ForbidNull(lines[i], $"lines[{i}]");
        indents[i] = lines[i].Length - lines[i].TrimStart(' ').Length;
        columns[i] = lines[i].IndexOf(spaces, indents[i]);
      }
      i = 0;
      while (i < lines.Length)
      {
        var maxColumn = 0;
        var j = i;
        while (j < lines.Length && columns[j] >= 0 && indents[j] == indents[i])
        {
          maxColumn = Math.Max(maxColumn, columns[j]);
          j++;
        }
        if (j > i)
        {
          for (int k = i; k < j; k++)
          {
            var suffix = lines[k].Substring(columns[k]).TrimStart(' ');
            lines[k] = lines[k].Substring(0, columns[k]) + new string(' ', maxColumn + minSpaceCount - columns[k]) + suffix;
          }
          i = j;
        }
        else
        {
          i++;
        }
      }
    }

    /// <summary>
    /// Returns a string containing a list of argument names and their values, enclosed in parenthesis and followed by a period.
    /// For example: ' (min = 0, max = 9).'.
    /// </summary>
    /// <param name="arguments"></param>
    /// <returns></returns>
    public static string GetBindingsSuffix(params object[] arguments)
    {
      if (arguments.Length % 2 == 1)
      {
        throw new ArgumentException("Expecting an even number of arguments.");
      }
      var n = arguments.Length / 2;
      var bindings = new string[n];
      for (int i = 0; i < n; i++)
      {
        bindings[i] = $"{arguments[2 * i + 1]} = {arguments[2 * i]}";
      }
      var sb = new StringBuilder();
      sb.Append(" (");
      sb.Append(string.Join(", ", bindings));
      sb.Append(").");
      return sb.ToString();
    }

    public static void RequireInInterval<T>(T value, string name, T min, T max) where T : IComparable<T>
    {
      if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
      {
        throw new ArgumentOutOfRangeException(name, "Argument out of range" + GetBindingsSuffix(value, name, min, nameof(min), max, nameof(max)));
      }
    }

    public static void RequireEqual<T>(T value, string name, T target) where T : IEquatable<T>
    {
      if (!value.Equals(target))
      {
        throw new ArgumentOutOfRangeException(name, "Arguments are not equal" + GetBindingsSuffix(value, name, target, nameof(target)));
      }
    }

    public static void RequireAtLeast<T>(T value, string name, T min) where T : IComparable<T>
    {
      if (value.CompareTo(min) < 0)
      {
        throw new ArgumentOutOfRangeException(name, "Argument out of range" + GetBindingsSuffix(value, name, min, nameof(min)));
      }
    }

    public static void RequireAtMost<T>(T value, string name, T max) where T : IComparable<T>
    {
      if (value.CompareTo(max) > 0)
      {
        throw new ArgumentOutOfRangeException(name, "Argument out of range" + GetBindingsSuffix(value, name, max, nameof(max)));
      }
    }
  }
}

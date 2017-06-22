using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using InputMaster.Forms;
using InputMaster.Parsers;
using InputMaster.Win32;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace InputMaster
{
  internal static class Helper
  {
    public static string CreateTokenString(string text)
    {
      return Constants.TokenStart + text + Constants.TokenEnd;
    }

    public static string ReadIdentifierTokenString(LocatedString locatedString)
    {
      if (Regex.IsMatch(locatedString.Value, "^" + Constants.IdentifierTokenPattern + "$"))
      {
        return locatedString.Value.Substring(1, locatedString.Length - 2);
      }
      throw new ParseException(locatedString, "Not in correct format. Expecting an identifier token.");
    }

    public static void SetForegroundWindowForce(IntPtr window)
    {
      NativeMethods.ShowWindow(window, ShowWindowArgument.Minimize);
      NativeMethods.ShowWindow(window, ShowWindowArgument.Restore);
      NativeMethods.SetForegroundWindow(window);
    }

    public static int GetMouseWheelDelta(Message m)
    {
      return m.WParam.ToInt32() >> 16;
    }

    public static void StartProcess(string fileName, string arguments = "", bool captureForeground = false)
    {
      if (captureForeground)
      {
        Env.Notifier.CaptureForeground();
      }
      try
      {
        Process.Start(fileName, arguments)?.Dispose();
      }
      catch (Exception ex)
      {
        throw new WrappedException("Failed to start process" + GetBindingsSuffix(fileName, nameof(fileName), arguments, nameof(arguments)), ex);
      }
    }

    public static void StartProcess(string fileName, string userName, SecureString password, string domain, string arguments = "", bool captureForeground = false)
    {
      if (captureForeground)
      {
        Env.Notifier.CaptureForeground();
      }
      try
      {
        ForbidNull(password);
        ForbidNull(domain);
        Process.Start(fileName, arguments, userName, password, domain)?.Dispose();
      }
      catch (Exception ex)
      {
        throw new WrappedException("Failed to start process" + GetBindingsSuffix(
          fileName, nameof(fileName),
          arguments, nameof(arguments),
          userName, nameof(userName),
          password.Length, $"{nameof(password)}.{nameof(password.Length)}",
          domain, nameof(domain)), ex);
      }
    }

    /// <summary>
    /// Log the exception, and exit the application if it is a fatal exception.
    /// </summary>
    /// <param name="exception"></param>
    public static void HandleException(Exception exception)
    {
      if (IsFatalException(exception))
      {
        HandleFatalException(exception);
        return;
      }
      try
      {
        Env.Notifier.WriteError(exception);
      }
      catch (Exception ex)
      {
        HandleFatalException(ex);
      }
    }

    private static void HandleFatalException(Exception exception)
    {
      Try.HandleFatalException(exception);
      Application.Exit();
    }

    public static void ShowSelectableText(object value, bool scrollToBottom = false)
    {
      var str = value.ToString();
      if (str.Length > 0)
      {
        new ShowStringForm(str, scrollToBottom).Show();
      }
    }

    public static bool TryGetString(string title, out string value, string defaultValue = "", bool selectAll = true)
    {
      using (var form = new GetStringForm(title, defaultValue, selectAll))
      {
        form.ShowDialog();
        return form.TryGetValue(out value);
      }
    }

    public static bool TryGetLine(string title, out string value, string defaultValue = "", bool isPassword = false)
    {
      using (var form = new GetStringLineForm(title, defaultValue, isPassword))
      {
        form.ShowDialog();
        return form.TryGetValue(out value);
      }
    }

    public static async Task<string> GetClipboardTextAsync()
    {
      for (var i = Env.Config.ClipboardTries; i > 0; i--)
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
          await Task.Delay(Env.Config.ClipboardDelay);
        }
      }
      throw new IOException("Failed to retrieve text data from the clipboard.");
    }

    public static async Task SetClipboardTextAsync(string text)
    {
      for (var i = Env.Config.ClipboardTries; i > 0; i--)
      {
        try
        {
          Clipboard.SetText(text);
          return;
        }
        catch (ExternalException) { }
        if (i > 1)
        {
          await Task.Delay(Env.Config.ClipboardDelay);
        }
      }
      throw new IOException("Failed to write text data to the clipboard.");
    }

    public static async Task ClearClipboardAsync()
    {
      for (var i = Env.Config.ClipboardTries; i > 0; i--)
      {
        try
        {
          Clipboard.Clear();
          return;
        }
        catch (ExternalException) { }
        if (i > 1)
        {
          await Task.Delay(Env.Config.ClipboardDelay);
        }
      }
      throw new IOException("Failed to clear the clipboard.");
    }

    private static async Task<T> TryAsync<T>(Func<T> action, int count = 10, int pauseInterval = 100)
    {
      for (var i = 0; i < count - 1; i++)
      {
        try
        {
          return action();
        }
        catch (Exception ex) when (!IsFatalException(ex))
        {
          await Task.Delay(pauseInterval);
        }
      }
      return action();
    }

    public static void Beep()
    {
      Task.Run(() => { Console.Beep(); });
    }

    public static IReadOnlyCollection<Modifiers> Modifiers { get; } =
      Enum.GetValues(typeof(Modifiers)).Cast<Modifiers>().Where(z => IsPowerOfTwo((int)z)).ToList().AsReadOnly();

    private static bool IsPowerOfTwo(int n)
    {
      if ((n & 1) != 0)
      {
        return n == 1;
      }
      if (n != 0)
      {
        return IsPowerOfTwo(n >> 1);
      }
      return false;
    }

    public static int CountOnes(int i)
    {
      i = i - ((i >> 1) & 0x55555555);
      i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
      return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
    }

    public static void ForbidNull(params object[] values)
    {
      for (int i = 0; i < values.Length; i++)
      {
        if (values[i] == null)
        {
          throw new ArgumentNullException($"Argument {i + 1} is null.");
        }
      }
    }

    public static void ForbidEmpty(string str, string name = "(unspecified)")
    {
      ForbidNull(str);
      if (str.Length == 0)
      {
        throw new ArgumentException("String is empty.", name);
      }
    }

    public static void ForbidWhitespace(string str, string name = "(unspecified)")
    {
      ForbidNull(str);
      ForbidEmpty(str, name);
      if (string.IsNullOrWhiteSpace(str))
      {
        throw new ArgumentException("String consists of whitespace only.", name);
      }
    }

    /// <summary>
    /// Returns true for exceptions that we cannot / should not recover from.
    /// </summary>
    public static bool IsFatalException(Exception ex)
    {
      return ex is FatalException || ex is StackOverflowException || ex is OutOfMemoryException || ex is ThreadAbortException || ex is AssertFailedException && Env.TestRun || ex is AccessViolationException || ex.InnerException != null && IsFatalException(ex.InnerException);
    }

    public static bool HasAssertFailed(Exception ex)
    {
      return ex is AssertFailedException || ex.InnerException != null && HasAssertFailed(ex.InnerException);
    }

    public static string ByteCountToString(long byteCount)
    {
      string[] labels = { "B", "KB", "MB", "GB", "TB" };
      double length = byteCount;
      var i = 0;
      while (length >= 1024 && i < labels.Length - 1)
      {
        i++;
        length = length / 1024;
      }
      return $"{length:0.##} {labels[i]}";
    }

    public static void RequireExistsDir(string dir)
    {
      if (!Directory.Exists(dir))
      {
        throw new DirectoryNotFoundException($"Directory '{dir}' not found.");
      }
    }

    public static void RequireExistsFile(string file)
    {
      if (!File.Exists(file))
      {
        throw new FileNotFoundException($"File '{file}' not found.");
      }
    }

    /// <summary>
    /// Replaces all invalid file name characters.
    /// </summary>
    public static string GetValidFileName(string name, char replacement)
    {
      ForbidWhitespace(name);
      var invalidChars = Path.GetInvalidFileNameChars();
      if (invalidChars.Contains(replacement))
      {
        throw new ArgumentException($"Invalid file name character '{replacement}' used as replacement character.", nameof(replacement));
      }
      return new string(name.Select(c => invalidChars.Contains(c) ? replacement : c).ToArray());
    }

    public static void ForceDeleteDir(string dir)
    {
      var info = new DirectoryInfo(dir);
      if (info.Exists)
      {
        foreach (var file in info.GetFiles("*", SearchOption.AllDirectories))
        {
          file.Attributes = FileAttributes.Normal;
        }
        info.Delete(true);
      }
    }

    public static void ForceDeleteFile(string file)
    {
      var info = new FileInfo(file);
      if (info.Exists)
      {
        info.Attributes = FileAttributes.Normal;
        info.Delete();
      }
    }

    public static string ReadAllText(string file) => RemoveCarriageReturns(File.ReadAllText(file));

    /// <summary>
    /// Asynchronously reads the contents of a file. Throws an exception after a certain number of tries.
    /// </summary>
    public static async Task<string> ReadAllTextAsync(string file)
    {
      return await TryAsync(() => ReadAllText(file));
    }

    public static bool TryReadJson<T>(string file, out T val)
    {
      try
      {
        val = JsonConvert.DeserializeObject<T>(ReadAllText(file));
        return true;
      }
      catch (Exception ex) when (!IsFatalException(ex)) { }
      val = default(T);
      return false;
    }

    /// <summary>
    /// Returns the index of the first character of the line containing the given index (a line has the form '[^\n]*\n').
    /// </summary>
    public static int GetLineStart(string text, int index)
    {
      RequireInInterval(index, nameof(index), 0, text.Length);
      if (index == 0)
      {
        return 0;
      }
      return text.LastIndexOf('\n', index - 1) + 1;
    }

    /// <summary>
    /// Returns the index of the last character of the line containing the given index.
    /// </summary>
    public static int GetLineEnd(string text, int index)
    {
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
      if (text.Length <= length)
      {
        return text;
      }
      if (length < 3)
      {
        throw new ArgumentOutOfRangeException(nameof(length), length, "Length should be at least 3.");
      }
      var i = length - 3;
      if (char.IsLowSurrogate(text[i]))
      {
        i--;
      }
      return text.Substring(0, i) + "...";
    }

    /// <summary>
    /// If given string is of the form '/{0}/', then a regex with pattern {0} is returned.
    /// Otherwise the string is parsed as a literal string, and a regex matching the given string is returned.
    /// </summary>
    public static Regex GetRegex(string text, RegexOptions options = RegexOptions.None, bool fullMatchIfLiteral = false)
    {
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
    /// Example: "some string (ab)" -> "ab".
    /// Example: "some other string" -> null.
    /// </summary>
    public static bool TryGetChordText(string text, out string chordText)
    {
      var m = Regex.Match(text, @"\(([^)]+)\) *$");
      if (m.Success)
      {
        chordText = m.Groups[1].Value;
        return true;
      }
      chordText = null;
      return false;
    }

    /// <summary>
    /// For each two adjacent lines with the same indentation and that both contain a column boundary, the column boundaries are aligned. A column boundary is a string of at least <paramref name="minSpaceCount"/> spaces (outside the indentation).
    /// </summary>
    public static string AlignColumns(string text, int minSpaceCount = 2)
    {
      if (text == null)
      {
        return null;
      }
      RequireAtLeast(minSpaceCount, nameof(minSpaceCount), 1);
      var lines = text.Split('\n');
      AlignColumns(lines);
      return string.Join("\n", lines);
    }

    /// <summary>
    /// For each two adjacent lines with the same indentation and that both contain a column boundary, the column boundaries are aligned. A column boundary is a string of at least <paramref name="minSpaceCount"/> spaces (outside the indentation).
    /// </summary>
    private static void AlignColumns(string[] lines, int minSpaceCount = 2)
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
        indents[i] = lines[i].Length - lines[i].TrimStart(' ').Length;
        columns[i] = lines[i].IndexOf(spaces, indents[i], StringComparison.Ordinal);
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
          for (var k = i; k < j; k++)
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
      for (var i = 0; i < n; i++)
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

    /// <summary>
    /// Inclusive.
    /// </summary>
    public static void RequireAtMost<T>(T value, string name, T max) where T : IComparable<T>
    {
      if (value.CompareTo(max) > 0)
      {
        throw new ArgumentOutOfRangeException(name, "Argument out of range" + GetBindingsSuffix(value, name, max, nameof(max)));
      }
    }

    public static IEnumerable<(T1, T2, T3)> Zip3<T1, T2, T3>(
      IEnumerable<T1> first,
      IEnumerable<T2> second,
      IEnumerable<T3> third)
    {
      using (var e1 = first.GetEnumerator())
      using (var e2 = second.GetEnumerator())
      using (var e3 = third.GetEnumerator())
      {
        while (e1.MoveNext() && e2.MoveNext() && e3.MoveNext())
        {
          yield return (e1.Current, e2.Current, e3.Current);
        }
      }
    }

    public static void RequireTrue(bool condition)
    {
      if (!condition)
      {
        throw new ArgumentException("Condition is false.");
      }
    }

    public static string RemoveCarriageReturns(string text)
    {
      return text.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    public static void ForbidCarriageReturn(ref string text)
    {
      if (!text.Contains('\r'))
      {
        return;
      }
      Env.Notifier.WriteError("String contains carriage return(s)." + GetBindingsSuffix(Truncate(text, 50), nameof(text)));
      text = RemoveCarriageReturns(text);
    }

    public static string JsonSerialize(object value, Formatting formatting)
    {
      return RemoveCarriageReturns(JsonConvert.SerializeObject(value, formatting));
    }
  }
}

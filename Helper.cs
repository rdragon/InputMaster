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
using System.Security.Cryptography;
using Newtonsoft.Json.Serialization;

namespace InputMaster
{
  public static class Helper
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

    public static async Task StartProcessAsync(string fileName, string arguments = "", bool captureForeground = false)
    {
      await Task.Yield(); // See TryGetStringAsync.
      if (captureForeground)
        Env.Notifier.CaptureForeground();
      try
      {
        Process.Start(fileName, arguments)?.Dispose();
      }
      catch (Exception ex)
      {
        throw new WrappedException("Failed to start process" +
          GetBindingsSuffix(fileName, nameof(fileName), arguments, nameof(arguments)), ex);
      }
    }

    public static async Task StartProcessAsync(string fileName, string userName, SecureString password, string domain, string arguments = "",
      bool captureForeground = false)
    {
      await Task.Yield(); // See TryGetStringAsync.
      if (captureForeground)
        Env.Notifier.CaptureForeground();
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

    public static Task ShowError(string message)
    {
      Env.Notifier.LogError(message);
      return ShowSelectableTextAsync("Error", message);
    }

    /// <summary>
    /// Log the exception, and exit the application if it is a fatal exception.
    /// </summary>
    /// <param name="exception"></param>
    public static async Task HandleExceptionAsync(Exception exception)
    {
      if (IsFatalException(exception))
      {
        await Try.HandleFatalException(exception);
        return;
      }
      try
      {
        Env.Notifier.WriteError(exception);
      }
      catch (Exception ex)
      {
        await Try.HandleFatalException(ex);
      }
    }

    public static void ShowSelectableText(object value, bool scrollToBottom = false)
    {
      var str = value.ToString();
      new ShowStringForm("Message", str, scrollToBottom).Show();
    }

    public static async Task ShowSelectableTextAsync(string title, object value)
    {
      var str = value.ToString();
      await Task.Yield(); // See TryGetStringAsync.
      new ShowStringForm(title, str, false).ShowDialog();
    }

    public static async Task<string> TryGetStringAsync(string title, string defaultValue = "", bool selectAll = true,
      bool startWithFindDialog = false, bool forceForeground = true, bool containsJson = false, bool throwOnCancel = false)
    {
      await Task.Yield(); // This makes sure we are not blocking the hook procedure. 
      using (var form = new GetStringForm(title, defaultValue, selectAll, startWithFindDialog, forceForeground, containsJson))
      {
        form.ShowDialog();
        if (!form.TryGetValue(out var value) && throwOnCancel)
          throw new ArgumentException("Aborted.");
        return value;
      }
    }

    public static async Task<string> TryGetLineAsync(string title, string defaultValue = "", bool isPassword = false,
      bool throwOnCancel = false)
    {
      await Task.Yield(); // See TryGetStringAsync.
      using (var form = new GetStringLineForm(title, defaultValue, isPassword))
      {
        form.ShowDialog();
        if (!form.TryGetValue(out var value) && throwOnCancel)
          throw new ArgumentException("Aborted.");
        return value;
      }
    }

    public static async Task<int> GetIntAsync(string title, string defaultValue, int minValue)
    {
      var text = await TryGetLineAsync(title, defaultValue, throwOnCancel: true);
      if (!int.TryParse(text, out var value))
      {
        Env.Notifier.Warning("Cannot parse as int.");
        return await GetIntAsync(title, text, minValue);
      }
      if (value < minValue)
      {
        Env.Notifier.Warning($"Value '{value}' is too low (minimum = {minValue}).");
        return await GetIntAsync(title, text, minValue);
      }
      return value;
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
            return RemoveCarriageReturns(s);
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
          Clipboard.SetText(InsertCarriageReturns(text));
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

    /// <summary>
    /// Thread-safe.
    /// </summary>
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
      return ex is FatalException || ex is StackOverflowException || ex is OutOfMemoryException || ex is ThreadAbortException ||
        ex is AssertFailedException && Env.RunningUnitTests || ex is AccessViolationException ||
        ex.InnerException != null && IsFatalException(ex.InnerException);
    }

    public static bool HasAssertFailed(Exception ex)
    {
      return ex is AssertFailedException || ex.InnerException != null && HasAssertFailed(ex.InnerException);
    }

    public static string ByteCountToString(long byteCount)
    {
      if (byteCount < 0)
        return byteCount.ToString();
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
        throw new DirectoryNotFoundException($"Directory '{dir}' not found.");
    }

    public static void RequireExistsFile(params string[] files)
    {
      foreach (var file in files)
      {
        ForbidWhitespace(file);
        if (!File.Exists(file))
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

    /// <summary>
    /// Thread-safe.
    /// </summary>
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

    /// <summary>
    /// Thread-safe.
    /// </summary>
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
      Env.Notifier.Error("String contains carriage return(s)." + GetBindingsSuffix(Truncate(text, 50), nameof(text)));
      text = RemoveCarriageReturns(text);
    }

    public static string JsonSerialize(object value, Formatting formatting)
    {
      return RemoveCarriageReturns(JsonConvert.SerializeObject(value, formatting));
    }

    /// <summary>
    /// Thread-safe.
    /// </summary>
    public static string InsertCarriageReturns(string text)
    {
      return RemoveCarriageReturns(text).Replace("\n", "\r\n");
    }

    /// <summary>
    /// Thread-safe.
    /// </summary>
    public static void WriteAllText(string file, string text)
    {
      File.WriteAllText(file, InsertCarriageReturns(text));
    }

    public static string StripTags(string title)
    {
      return title
        .Replace(Env.Config.SharedTag + " ", "")
        .Replace(Env.Config.SharedTag, "")
        .Replace(Env.Config.HiddenTag + " ", "")
        .Replace(Env.Config.HiddenTag, "");
    }

    /// <summary>
    /// Thread-safe.
    /// </summary>
    private static ulong GetRandomLong()
    {
      var buffer = new byte[8];
      Env.RandomNumberGenerator.GetBytes(buffer);
      return BitConverter.ToUInt64(buffer, 0);
    }

    /// <summary>
    /// Thread-safe.
    /// </summary>
    public static int GetRandomInt(int max)
    {
      if (max == 0)
        return 0;
      Debug.Assert(0 < max);
      return (int)(GetRandomLong() % (ulong)max);
    }

    /// <summary>
    /// Thread-safe.
    /// </summary>
    public static byte[] GetRandomBytes(int count)
    {
      var bytes = new byte[count];
      Env.RandomNumberGenerator.GetBytes(bytes);
      return bytes;
    }

    public static char GetRandomNameChar()
    {
      return (char)('a' + GetRandomInt(26));
    }

    public static char GetRandomNumberChar()
    {
      return (char)('1' + GetRandomInt(9));
    }

    public static string GetRandomName(int length)
    {
      var sb = new StringBuilder();
      for (var i = 0; i < length; i++)
      {
        sb.Append(GetRandomNameChar());
      }
      return sb.ToString();
    }

    public static string GetRandomNumber(int length)
    {
      var sb = new StringBuilder();
      for (var i = 0; i < length; i++)
      {
        sb.Append(GetRandomNumberChar());
      }
      return sb.ToString();
    }

    public static string GetRandomPassword(string prefix, int midSize, int suffixSize)
    {
      return prefix + GetRandomName(midSize) + Env.PasswordMatrix.CreateRandomMatrixPassword(suffixSize).Value;
    }

    public static string GetRandomPassword()
    {
      return GetRandomPassword(Env.Settings.PasswordPrefix, 3, 5);
    }

    public static string GetRandomEmail()
    {
      var sb = new StringBuilder();
      foreach (var c in Env.Config.DefaultEmail)
      {
        sb.Append(c == 'X' ? GetRandomNameChar() : c);
      }
      return sb.ToString();
    }

    public static (string, string) SplitAt(string str, int i)
    {
      if (i < 0)
        throw new ArgumentOutOfRangeException(nameof(i));
      if (str == null)
        return (null, null);
      var j = Math.Min(i, str.Length);
      return (str.Substring(0, j), str.Substring(j));
    }

    public static string GetTextOrNull(string str)
    {
      return string.IsNullOrWhiteSpace(str) ? null : str;
    }

    public static async Task<PasswordDecomposition> GetPasswordDecompositionAsync(string password, List<int> matrixDecompositionIn)
    {
      if (matrixDecompositionIn?.Any(z => z <= 0) == true)
        throw new ArgumentOutOfRangeException(nameof(matrixDecompositionIn));
      var matrixDecomposition = 0 < matrixDecompositionIn?.Count ? new List<int>(matrixDecompositionIn) :
        new List<int> { Env.Config.MatrixPasswordLength };
      var matrixLength = matrixDecomposition.Sum();
      var prefixLength = password.Length - matrixLength;
      if (prefixLength < 0)
        return new PasswordDecomposition(password);
      var blueprints = new List<PasswordBlueprint>();
      var (prefix, rest) = SplitAt(password, prefixLength);
      foreach (var n in matrixDecomposition)
      {
        string part;
        (part, rest) = SplitAt(rest, n);
        var blueprint = await Env.PasswordMatrix.GetBlueprintAsync(part);
        if (blueprint == null)
          return new PasswordDecomposition(password);
        blueprints.Add(blueprint);
      }
      return new PasswordDecomposition(prefix, blueprints);
    }

    public static T[] SubArray<T>(T[] data, int index, int length)
    {
      var result = new T[length];
      Array.Copy(data, index, result, 0, length);
      return result;
    }

    public static byte[] GetSha256(string text)
    {
      using (var sha256 = new SHA256Managed())
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
    }

    public static string GetSha1(string text)
    {
      using (var sha1 = new SHA1Managed())
      {
        var bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(text));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
          sb.Append(b.ToString("x2"));
        return sb.ToString();
      }
    }

    public static async void AwaitTask(Task task)
    {
      await task;
    }

    public static JsonSerializerSettings JsonSerializerSettings { get; } = new JsonSerializerSettings
    {
      ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
      NullValueHandling = NullValueHandling.Ignore
    };

    public static Task<byte[]> GetKeyAsync(string password, byte[] salt, int derivationCount)
    {
      return Task.Run(() => GetKey(password, salt, derivationCount));
    }

    /// <summary>
    /// Thread-safe.
    /// </summary>
    public static byte[] GetKey(string password, byte[] salt, int derivationCount)
    {
      using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, derivationCount))
        return deriveBytes.GetBytes(Env.Config.KeySize);
    }

    public static byte[] GetCyptroSalt(string name)
    {
      return Encoding.UTF8.GetBytes(name + Env.Config.CyptroSaltSuffix);
    }

    public static async Task StartReadOnlyAsync()
    {
      // Thread-safe.
      await Task.Run(() =>
      {
        var info = new ProcessStartInfo("olrsl",
          $"clone \"{Env.Config.InputMasterPublishDir}\" \"{Env.Config.InputMasterReadOnlyPublishDir}\"")
        {
          WindowStyle = ProcessWindowStyle.Hidden
        };
        using (var process = Process.Start(info))
        {
          if (process == null)
            throw new Exception("Could not start process.");
          process.WaitForExit();
          if (process.ExitCode != 0)
            throw new Exception("Clone failed.");
        }
      });
      var file = Path.Combine(Env.Config.InputMasterReadOnlyPublishDir, Env.Config.InputMasterFileName);
      if (!File.Exists(file))
        throw new Exception("Could not find executable.");
      Process.Start(file, "readonly")?.Dispose();
    }
  }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using InputMaster.Win32;

namespace InputMaster.Instances
{
  public class ForegroundListener : IForegroundListener
  {
    public string ForegroundWindowTitle { get; private set; } = "";
    public string ForegroundProcessName { get; private set; } = "";
    private readonly int _maxNameCacheCount = 500;
    private readonly int _maxCaptionLength = 1000;
    private readonly Dictionary<int, string> _nameCache = new Dictionary<int, string>();
    private readonly StringBuilder _captionBuffer;
    private int _foregroundProcessId;

    public ForegroundListener()
    {
      _captionBuffer = new StringBuilder(_maxCaptionLength);
    }

    private static int GetProcessId(IntPtr window)
    {
      NativeMethods.GetWindowThreadProcessId(window, out var id);
      return id;
    }

    public void Update()
    {
      var window = NativeMethods.GetForegroundWindow();
      var title = GetWindowTitle(window);
      var id = GetProcessId(window);
      if (id == _foregroundProcessId && title == ForegroundWindowTitle)
        return;
      Env.StateCounter++;
      ForegroundWindowTitle = title;
      if (id == _foregroundProcessId)
        return;
      _foregroundProcessId = id;
      if (_nameCache.TryGetValue(id, out var name))
      {
        ForegroundProcessName = name;
        return;
      }
      if (_nameCache.Count == _maxNameCacheCount)
        _nameCache.Clear();
      using (var process = Process.GetProcessById(id))
        ForegroundProcessName = process.ProcessName;
      _nameCache[id] = ForegroundProcessName;
    }

    private string GetWindowTitle(IntPtr window)
    {
      var length = NativeMethods.GetWindowText(window, _captionBuffer, _maxCaptionLength);
      return length > 0 ? _captionBuffer.ToString() : "";
    }
  }
}

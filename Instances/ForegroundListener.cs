using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using InputMaster.Win32;

namespace InputMaster.Instances
{
  public class ForegroundListener : IForegroundListener
  {
    private readonly int MaxNameCacheCount = 500;
    private readonly int MaxCaptionLength = 1000;
    private readonly Dictionary<int, string> NameCache = new Dictionary<int, string>();
    private readonly StringBuilder CaptionBuffer;
    private int ForegroundProcessId;

    public ForegroundListener()
    {
      CaptionBuffer = new StringBuilder(MaxCaptionLength);
    }

    public string ForegroundWindowTitle { get; private set; } = "";
    public string ForegroundProcessName { get; private set; } = "";

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
      if (id == ForegroundProcessId && title == ForegroundWindowTitle)
      {
        return;
      }
      Env.StateCounter++;
      ForegroundWindowTitle = title;
      if (id == ForegroundProcessId)
      {
        return;
      }
      ForegroundProcessId = id;
      if (NameCache.TryGetValue(id, out var name))
      {
        ForegroundProcessName = name;
        return;
      }
      if (NameCache.Count == MaxNameCacheCount)
      {
        NameCache.Clear();
      }
      using (var process = Process.GetProcessById(id))
      {
        ForegroundProcessName = process.ProcessName;
      }
      NameCache[id] = ForegroundProcessName;
    }

    private string GetWindowTitle(IntPtr window)
    {
      var length = NativeMethods.GetWindowText(window, CaptionBuffer, MaxCaptionLength);
      return length > 0 ? CaptionBuffer.ToString() : "";
    }
  }
}

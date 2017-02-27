using InputMaster.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace InputMaster
{
  class ForegroundListener : ForegroundListenerBase
  {
    private const int MaxNameCacheCount = 100;
    private const int MaxCaptionLength = 1000;

    private readonly Dictionary<int, string> NameCache = new Dictionary<int, string>();
    private readonly StringBuilder CaptionBuffer = new StringBuilder(MaxCaptionLength);
    private IntPtr ForegroundWindow;
    private int ForegroundProcessId;

    public ForegroundListener(IFlagViewer flagViewer, IParserOutputProvider parserOutputProvider) : base(flagViewer, parserOutputProvider) { }

    private static int GetProcessId(IntPtr window)
    {
      int id;
      NativeMethods.GetWindowThreadProcessId(window, out id);
      return id;
    }

    public override void Update()
    {
      var window = NativeMethods.GetForegroundWindow();
      if (window != ForegroundWindow)
      {
        ForegroundWindow = window;
        var id = GetProcessId(window);
        if (id != ForegroundProcessId)
        {
          ForegroundProcessId = id;
          string name;
          if (!NameCache.TryGetValue(id, out name))
          {
            if (NameCache.Count == MaxNameCacheCount)
            {
              NameCache.Clear();
            }
            using (var p = Process.GetProcessById(id))
            {
              name = p.ProcessName;
              NameCache[id] = name;
            }
          }
          ForegroundProcessName = name;
          Counter++;
        }
      }
      var title = GetWindowTitle(window);
      if (title != ForegroundWindowTitle)
      {
        ForegroundWindowTitle = title;
        Counter++;
      }
    }

    private string GetWindowTitle(IntPtr window)
    {
      var length = NativeMethods.GetWindowText(window, CaptionBuffer, MaxCaptionLength);
      return length > 0 ? CaptionBuffer.ToString() : "";
    }
  }
}

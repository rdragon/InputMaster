using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace InputMaster.Extensions
{
  class ColorTracker : IDisposable
  {
    private Timer Timer = new Timer { Interval = 100 };

    public ColorTracker(IFlagViewer flagViewer)
    {
      Timer.Tick += (s, e) =>
      {
        Env.Notifier.SetPersistentText(GetColor(Cursor.Position).ToString());
      };

      flagViewer.FlagsChanged += () =>
      {
        if (flagViewer.IsFlagSet(nameof(ColorTracker)))
        {
          Timer.Start();
        }
        else
        {
          Timer.Stop();
          Env.Notifier.SetPersistentText("");
        }
      };
    }

    private static Color GetColor(Point p)
    {
      var deviceContext = NativeMethods.GetDC(IntPtr.Zero);
      var pixel = NativeMethods.GetPixel(deviceContext, p.X, p.Y);
      NativeMethods.ReleaseDC(IntPtr.Zero, deviceContext);
      Color color = Color.FromArgb(pixel & 0x000000FF, (pixel & 0x0000FF00) >> 8, (pixel & 0x00FF0000) >> 16);
      return color;
    }

    [CommandTypes(CommandTypes.Visible)]
    public static void WriteColor()
    {
      Env.Notifier.Write(GetColor(Cursor.Position).ToString());
    }

    public void Dispose()
    {
      Timer.Dispose();
    }

    private static class NativeMethods
    {
      [DllImport("user32.dll")]
      public static extern IntPtr GetDC(IntPtr window);

      [DllImport("user32.dll")]
      public static extern int ReleaseDC(IntPtr window, IntPtr deviceContext);

      [DllImport("gdi32.dll")]
      public static extern int GetPixel(IntPtr deviceContext, int x, int y);
    }
  }
}

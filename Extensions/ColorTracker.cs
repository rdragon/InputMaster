using System;
using System.Drawing;
using System.Windows.Forms;
using InputMaster.Win32;

namespace InputMaster.Extensions
{
  internal class ColorTracker : Actor
  {
    private readonly Timer Timer = new Timer { Interval = 100 };

    public ColorTracker()
    {
      Timer.Tick += (s, e) =>
      {
        Env.Notifier.SetPersistentText(GetColor(Cursor.Position).ToString());
      };

      Env.FlagManager.FlagsChanged += () =>
      {
        if (Env.FlagManager.IsSet(nameof(ColorTracker)))
        {
          Timer.Start();
        }
        else
        {
          Timer.Stop();
          Env.Notifier.SetPersistentText("");
        }
      };

      Env.App.Exiting += Timer.Dispose;
    }

    [Command]
    public static void WriteColor()
    {
      Env.Notifier.Write(GetColor(Cursor.Position).ToString());
    }

    private static Color GetColor(Point p)
    {
      var deviceContext = NativeMethods.GetDC(IntPtr.Zero);
      var pixel = NativeMethods.GetPixel(deviceContext, p.X, p.Y);
      NativeMethods.ReleaseDC(IntPtr.Zero, deviceContext);
      var color = Color.FromArgb(pixel & 0x000000FF, (pixel & 0x0000FF00) >> 8, (pixel & 0x00FF0000) >> 16);
      return color;
    }
  }
}

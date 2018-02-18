using System;
using System.Drawing;
using System.Windows.Forms;
using InputMaster.Win32;

namespace InputMaster.Actors
{
  public class ColorTracker : Actor
  {
    private readonly Timer _timer = new Timer { Interval = 100 };

    public ColorTracker()
    {
      _timer.Tick += (s, e) =>
      {
        Env.Notifier.SetPersistentText(GetColor(Cursor.Position).ToString());
      };

      Env.FlagManager.FlagsChanged += () =>
      {
        if (Env.FlagManager.HasFlag(nameof(ColorTracker)))
          _timer.Start();
        else
        {
          _timer.Stop();
          Env.Notifier.SetPersistentText("");
        }
      };

      Env.App.Exiting += _timer.Dispose;
    }

    [Command]
    public static void WriteColor()
    {
      var color = GetColor(Cursor.Position);
      Env.Notifier.Info(color.ToString() + " " + GetHex(color));
    }

    private static string GetHex(Color color)
    {
      return "#" + color.R.ToString("x2") + color.G.ToString("x2") + color.B.ToString("x2");
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

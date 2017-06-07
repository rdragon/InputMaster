using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace InputMaster.Extensions
{
  [CommandTypes(CommandTypes.Visible)]
  internal class CustomActor : Actor
  {
    public static void SaveScreen()
    {
      var bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
      var graphics = Graphics.FromImage(bitmap);
      graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
      var name = Helper.GetString("Name", Helper.GetValidFileName(DateTime.Now.ToString(), '-'));
      if (string.IsNullOrWhiteSpace(name))
      {
        return;
      }
      var dir = @"C:\temp\screenshots";
      Directory.CreateDirectory(dir);
      bitmap.Save(Path.Combine(dir, name + ".jpg"), ImageFormat.Jpeg);
    }
  }
}

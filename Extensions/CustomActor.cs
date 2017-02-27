using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace InputMaster.Extensions
{
  [CommandTypes(CommandTypes.Visible)]
  class CustomActor
  {
    public static void SaveScreen()
    {
      Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
      Graphics graphics = Graphics.FromImage(bitmap as Image);
      graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
      var name = Helper.GetString("Name", Helper.GetValidFileName(DateTime.Now.ToString(), '-'));
      if (!string.IsNullOrWhiteSpace(name))
      {
        var dir = @"C:\temp\screenshots";
        Directory.CreateDirectory(dir);
        bitmap.Save(Path.Combine(dir, name + ".jpg"), ImageFormat.Jpeg);
      }
    }
  }
}

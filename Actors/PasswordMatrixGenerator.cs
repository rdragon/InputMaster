using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace InputMaster.Actors
{
  public class PasswordMatrixGenerator : Actor
  {
    [Command]
    private static async Task PasteRandomPrettyMatrixAsync()
    {
      var w = await Helper.GetIntAsync("Width", "167", 1);
      var h = await Helper.GetIntAsync("Height", "52", 1);
      var boundary = 3;
      var batch = 5;
      var sb = new StringBuilder();
      GenerateHorizontalNumberLine(sb, w, boundary, batch);
      var matrix = Helper.GetRandomName(w * h);
      var reader = new StringReader(matrix);
      for (int i = 0; i < h - 2; i++)
      {
        AddBoundary(sb, i, boundary, false);
        for (int j = 0; j < w - boundary * 2; j++)
          sb.Append((j + 1) % (batch + 1) == 0 ? ' ' : (char)reader.Read());
        AddBoundary(sb, i, boundary, true);
        sb.AppendLine();
      }
      GenerateHorizontalNumberLine(sb, w, boundary, batch);
      await ForegroundInteractor.PasteAsync(sb.ToString());
    }

    private static void AddBoundary(StringBuilder sb, int i, int boundary, bool right)
    {
      if (99 < i)
        throw new NotImplementedException();
      var space = new string(' ', boundary - 2);
      if (right)
        sb.Append(space);
      if (i < 10)
        sb.Append(" ");
      sb.Append(i);
      if (!right)
        sb.Append(space);
    }

    private static void GenerateHorizontalNumberLine(StringBuilder sb, int w, int boundary, int batch)
    {
      sb.Append(new string(' ', boundary));
      var i = 2 * boundary;
      var j = 0;
      while (i + j.ToString().Length <= w)
      {
        sb.Append(j);
        i += j.ToString().Length;
        var extra = batch + 1 - j.ToString().Length;
        Debug.Assert(0 <= extra);
        extra = Math.Min(w - i, extra);
        sb.Append(new string(' ', extra));
        i += extra;
        j += 5;
      }
      sb.Append(new string(' ', w - i));
      sb.AppendLine();
    }
  }
}

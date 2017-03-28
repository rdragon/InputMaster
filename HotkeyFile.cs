using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InputMaster
{
  class HotkeyFile
  {
    public HotkeyFile(string name, string text)
    {
      Name = name;
      Text = text;
    }

    public string Name { get; }
    public string Text { get; }
  }
}

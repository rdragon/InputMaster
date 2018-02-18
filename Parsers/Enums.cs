using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InputMaster.Parsers
{
  [Flags]
  public enum InputReaderFlags
  {
    None = 0,
    AllowKeywordAny = 1,
    AllowCustomModifier = 2,
    AllowHoldRelease = 4,
    AllowCustomCharacter = 8,
    ParseLiteral = 16,
    AllowMultiplier = 32,
    AllowDynamicHotkey = 64
  }
}

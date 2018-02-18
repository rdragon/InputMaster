using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InputMaster.Actors
{
  [Flags]
  public enum GitStatusFlags
  {
    None = 0,
    WorkingTreeDirty = 1,
    IndexDirty = 2,
    AwaitingPush = 4,
    UpToDate = 8
  }
}

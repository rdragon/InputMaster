using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InputMaster.Actors
{
  public class GitResponse
  {
    public string StandardOutput { get; }
    public string StandardError { get; }

    public GitResponse(string standardOutput, string standardError)
    {
      StandardOutput = standardOutput;
      StandardError = standardError;
    }
  }
}

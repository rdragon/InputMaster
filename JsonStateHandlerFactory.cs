using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InputMaster
{
  public class JsonStateHandlerFactory : IStateHandlerFactory
  {
    private readonly List<StateFile> _exportableStateFiles = new List<StateFile>();

    public IStateHandler<T> Create<T>(T state, string file, StateHandlerFlags flags) where T : IState
    {
      if (flags.HasFlag(StateHandlerFlags.Exportable))
        _exportableStateFiles.Add(new StateFile(file, flags));
      return new JsonStateHandler<T>(state, file, flags);
    }

    public IEnumerable<StateFile> GetExportableStateFiles()
    {
      return _exportableStateFiles;
    }
  }
}

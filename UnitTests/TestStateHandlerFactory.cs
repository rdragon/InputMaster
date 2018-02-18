using InputMaster;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnitTests
{
  public class TestStateHandlerFactory : IStateHandlerFactory
  {
    public IStateHandler<T> Create<T>(T state, string file, StateHandlerFlags flags) where T : IState
    {
      return new Mock<IStateHandler<T>>().Object;
    }

    public IEnumerable<StateFile> GetExportableStateFiles()
    {
      return new StateFile[0];
    }
  }
}

using InputMaster;

namespace UnitTests
{
  public class TestPrimaryHook : IInputHook
  {
    private readonly IInputHook _targetHook;

    public TestPrimaryHook(IInputHook targetHook)
    {
      _targetHook = targetHook;
    }

    public string GetStateInfo()
    {
      return _targetHook.GetStateInfo();
    }

    public void Handle(InputArgs e)
    {
      _targetHook.Handle(e);
      if (!e.Capture)
        Env.CreateInjector().Add(e.Input, e.Down).Run();
    }

    public void Reset()
    {
      _targetHook.Reset();
    }
  }
}

using InputMaster;

namespace UnitTests
{
  internal class TestPrimaryHook : IInputHook
  {
    private readonly IInputHook TargetHook;

    public TestPrimaryHook(IInputHook targetHook)
    {
      TargetHook = targetHook;
    }

    public string GetStateInfo()
    {
      return TargetHook.GetStateInfo();
    }

    public void Handle(InputArgs e)
    {
      TargetHook.Handle(e);
      if (!e.Capture)
      {
        Env.CreateInjector().Add(e.Input, e.Down).Run();
      }
    }

    public void Reset()
    {
      TargetHook.Reset();
    }
  }
}

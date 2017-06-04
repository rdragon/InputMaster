using System;
using System.Collections.Generic;
using InputMaster;

namespace UnitTests
{
  internal class TestInjector : IInjector
  {
    private readonly List<Action> Actions = new List<Action>();
    private readonly IInputHook TargetHook;

    public TestInjector(IInputHook targetHook)
    {
      TargetHook = targetHook;
    }

    public IInjector Add(char c)
    {
      throw new InvalidOperationException();
    }

    public IInjector Add(Input input, bool down)
    {
      Actions.Add(() => { TargetHook.Handle(new InputArgs(input, down)); });
      return this;
    }

    public Action Compile()
    {
      var actions = Actions.ToArray();
      return () =>
      {
        foreach (var action in actions)
        {
          action();
        }
      };
    }

    public void Run()
    {
      foreach (var action in Actions)
      {
        action();
      }
      Actions.Clear();
    }

    public IInjector CreateInjector()
    {
      return new TestInjector(TargetHook);
    }
  }
}

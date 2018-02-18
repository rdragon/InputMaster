using System;
using System.Collections.Generic;
using InputMaster;

namespace UnitTests
{
  public class TestInjector : IInjector
  {
    private readonly List<Action> _actions = new List<Action>();
    private readonly IInputHook _targetHook;

    public TestInjector(IInputHook targetHook)
    {
      _targetHook = targetHook;
    }

    public IInjector Add(char c)
    {
      throw new InvalidOperationException();
    }

    public IInjector Add(Input input, bool down)
    {
      _actions.Add(() => { _targetHook.Handle(new InputArgs(input, down)); });
      return this;
    }

    public Action Compile()
    {
      var actions = _actions.ToArray();
      return () =>
      {
        foreach (var action in actions)
          action();
      };
    }

    public void Run()
    {
      foreach (var action in _actions)
        action();
      _actions.Clear();
    }

    public IInjector CreateInjector()
    {
      return new TestInjector(_targetHook);
    }
  }
}

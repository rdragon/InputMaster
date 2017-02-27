using System;
using System.Collections.Generic;
using InputMaster;

namespace UnitTests
{
  class FakeInjectorFactory
  {
    public event Action<Input, bool> Injecting = delegate { };

    public FakeInjector CreateInjector()
    {
      return new FakeInjector(this);
    }

    public class FakeInjector : IInjector
    {
      private readonly List<Action> Actions = new List<Action>();
      private readonly FakeInjectorFactory Factory;

      public FakeInjector(FakeInjectorFactory factory)
      {
        Factory = factory;
      }

      public IInjector Add(char c)
      {
        throw new InvalidOperationException();
      }

      public IInjector Add(Input input, bool down)
      {
        Actions.Add(() => { Factory.Injecting(input, down); });
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
        return new FakeInjector(Factory);
      }
    }
  }
}

using System;

namespace InputMaster
{
  public class ValueProvider<T> : IValueProvider<T>
  {
    private T Value;
    private bool ValueSet;
    private Action<T> Action;
    private bool Executed;
    private bool Once;

    public void ExecuteOnce(Action<T> action)
    {
      Once = true;
      ExecuteMany(action);
    }

    public void ExecuteMany(Action<T> action)
    {
      if (Action != null)
      {
        throw new InvalidOperationException();
      }
      Action = action;
      Execute();
    }

    public void SetValue(T value)
    {
      if (ValueSet && Once)
      {
        return;
      }
      Value = value;
      ValueSet = true;
      Execute();
    }

    private void Execute()
    {
      if (!(Executed && Once) && ValueSet && Action != null)
      {
        Executed = true;
        Action(Value);
      }
    }
  }
}

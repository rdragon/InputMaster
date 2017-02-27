using System;
using System.ComponentModel;

namespace InputMaster
{
  /// <summary>
  /// Warning: this static class is not thread safe! Make sure to use the provided synchronizing object.
  /// </summary>
  static class Env
  {
    private static IInjector PrivateInjectorPrototype;
    private static IForegroundListener PrivateForegroundListener;

    public static INotifier Notifier { get; set; }
    public static ISynchronizeInvoke SynchronizingObject { get; set; }
    public static IForegroundListener ForegroundListener
    {
      get
      {
        if (PrivateForegroundListener == null)
        {
          throw new InvalidOperationException($"Trying to access {nameof(Env)}.{nameof(ForegroundListener)} before it has been set.");
        }
        else
        {
          return PrivateForegroundListener;
        }
      }
      set
      {
        PrivateForegroundListener = value;
      }
    }
    public static IInjector InjectorPrototype
    {
      get
      {
        if (PrivateInjectorPrototype == null)
        {
          throw new InvalidOperationException($"Trying to access {nameof(Env)}.{nameof(InjectorPrototype)} before it has been set.");
        }
        else
        {
          return PrivateInjectorPrototype;
        }
      }
      set
      {
        PrivateInjectorPrototype = value;
      }
    }

    public static IInjector CreateInjector()
    {
      return InjectorPrototype.CreateInjector();
    }
  }
}

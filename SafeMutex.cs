using System;
using System.Threading;

namespace InputMaster
{
  class SafeMutex : IDisposable
  {
    private readonly Mutex Mutex;

    public SafeMutex(string name)
    {
      Mutex = new Mutex(false, name);
    }

    public bool Acquired { get; private set; }

    public bool Acquire(TimeSpan? timeout = null)
    {
      if (!Acquired)
      {
        try
        {
          Acquired = Mutex.WaitOne(timeout.GetValueOrDefault(TimeSpan.Zero));
        }
        catch (AbandonedMutexException)
        {
          Acquired = true;
        }
      }
      return Acquired;
    }

    public void Release()
    {
      if (Acquired)
      {
        Mutex.ReleaseMutex();
        Acquired = false;
      }
    }

    public void Dispose()
    {
      Mutex.Dispose();
    }
  }
}

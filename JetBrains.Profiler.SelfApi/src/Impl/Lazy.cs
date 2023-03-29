using System;
using System.Threading;
using JetBrains.Annotations;

namespace JetBrains.Profiler.SelfApi.Impl
{
  // Note: Because net20 doesn't contain Lazy<T> implementation in mscorlib!
  internal sealed class Lazy<TValue>
  {
    #region Delegates

    public delegate TValue FuncDelegate();

    #endregion

    private readonly FuncDelegate myFunc;
    private readonly object myFuncLock = new();
    private volatile int myHasValue;
    private TValue myValue;

    public Lazy([NotNull] FuncDelegate func)
    {
      myFunc = func ?? throw new ArgumentNullException(nameof(func));
    }

    public TValue Value
    {
      get
      {
        if (myHasValue == 0)
          lock (myFuncLock)
            if (myHasValue == 0)
            {
              myValue = myFunc();
              Interlocked.Increment(ref myHasValue);
            }

        return myValue;
      }
    }
  }
}
using System;
using System.Threading;

namespace JetBrains.Profiler.SelfApi.Impl
{
  // Note: Because net20 doesn't contain Lazy<T> implementation in mscorlib!
  internal sealed class Lazy<TValue>
  {
    #region Delegates

    public delegate TValue FuncDelegate();

    #endregion

    private readonly FuncDelegate _func;
    private readonly object _funcLock = new object();
    private volatile int _hasValue;
    private TValue _value;

    public Lazy(FuncDelegate func)
    {
      _func = func ?? throw new ArgumentNullException(nameof(func));
    }

    public TValue Value
    {
      get
      {
        if (_hasValue == 0)
          lock (_funcLock)
            if (_hasValue == 0)
            {
              _value = _func();
              Interlocked.Increment(ref _hasValue);
            }

        return _value;
      }
    }
  }
}
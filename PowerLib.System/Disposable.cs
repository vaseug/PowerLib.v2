using PowerLib.System.Validation;
using System;

namespace PowerLib.System;

public delegate void OutFactory<T>(out T v);

public static class Disposable
{
  #region Methods

  public static TDisposable Create<TDisposable>(OutFactory<TDisposable> factory)
    where TDisposable : IDisposable
  {
    Argument.That.NotNull(factory);

    var disposable = default(TDisposable);
    try
    {
      factory(out disposable);
      return disposable;
    }
    catch
    {
      disposable?.Dispose();
      throw;
    }
  }

  public static void Dispose<TDisposable>(ref TDisposable? disposable)
    where TDisposable : IDisposable
    => Variable.Take(ref disposable)?.Dispose();

  public static void BlindDispose<TSource>(ref TSource? source)
    => (Variable.Take(ref source) as IDisposable)?.Dispose();

  #endregion
}

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace PowerLib.System.Collections.Disposables;

public interface IDisposablesDictionary<TKey> : IReadOnlyDictionary<TKey, IDisposable>, IDisposable
  where TKey : notnull
{
  #region Methods

  public TDisposable Add<TDisposable>(TKey key, Factory<TDisposable> factory)
    where TDisposable : class, IDisposable;

  public TDisposable Add<TDisposable>(TKey key, OutFactory<TDisposable> factory)
    where TDisposable : class, IDisposable;

  public TDisposable Add<TDisposable>(Factory<TDisposable> factory, Func<TDisposable, TKey> keySelector)
    where TDisposable : class, IDisposable;

  public TDisposable Add<TDisposable>(OutFactory<TDisposable> factory, Func<TDisposable, TKey> keySelector)
    where TDisposable : class, IDisposable;

  public bool TryTake(TKey key, [NotNullWhen(true)] out IDisposable? disposable);

  public IDisposable Take(TKey key);

  public KeyValuePair<TKey, IDisposable>[] Take();

  public bool Dispose(TKey key);

  #endregion
}

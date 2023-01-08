using System;
using System.Collections.Generic;

namespace PowerLib.System.Collections.Disposables;

public interface IDisposablesCollection : IReadOnlyCollection<IDisposable>, IDisposable
{
  #region Methods

  public TDisposable Add<TDisposable>(Factory<TDisposable> factory)
    where TDisposable : class, IDisposable;

  public TDisposable Add<TDisposable>(OutFactory<TDisposable> factory)
    where TDisposable : class, IDisposable;

  public IDisposable? Take<TDisposable>(TDisposable disposable)
    where TDisposable : class, IDisposable;

  public IDisposable[] Take();

  public bool Dispose<TDisposable>(TDisposable disposable)
    where TDisposable : class, IDisposable;

  #endregion
}

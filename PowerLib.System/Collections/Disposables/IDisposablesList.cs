using System;
using System.Collections.Generic;

namespace PowerLib.System.Collections.Disposables;

public interface IDisposablesList : IDisposablesCollection, IReadOnlyList<IDisposable>
{
  #region Methods

  public TDisposable Insert<TDisposable>(int index, Factory<TDisposable> factory)
    where TDisposable : class, IDisposable;

  public TDisposable Insert<TDisposable>(int index, OutFactory<TDisposable> factory)
    where TDisposable : class, IDisposable;

  IDisposable TakeAt(int index);

  void DisposeAt(int index);

  #endregion
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Disposables;

public abstract class DisposablesCollectionBase : IDisposablesCollection
{
  protected DisposablesCollectionBase()
  { }

  #region Internal properties

  protected virtual int ItemsCount { get; }

  #endregion
  #region Internal methods

  protected abstract bool ContainsItem(IDisposable disposable);

  protected abstract void AddItem(IDisposable disposable);

  protected abstract bool RemoveItem(IDisposable disposable);

  protected abstract IEnumerable<IDisposable> RemoveItems();

  protected abstract IEnumerator<IDisposable> GetItemsEnumerator();

  protected virtual void Dispose(bool disposing)
  {
    if (!disposing)
      return;
    foreach (var disposable in RemoveItems())
      Safe.Invoke(() => DisposeItem(disposable));
  }

  protected virtual void DisposeItem(IDisposable disposable)
  {
    disposable?.Dispose();
  }

  #endregion
  #region Public properties

  public int Count => ItemsCount;

  #endregion
  #region Public methods

  public bool Contains<TDisposable>(TDisposable disposable)
    where TDisposable : class, IDisposable
    => ContainsItem(disposable);

  public TDisposable Add<TDisposable>(Factory<TDisposable> factory)
    where TDisposable : class, IDisposable
  {
    Argument.That.NotNull(factory);

    return System.Disposable.Create((out TDisposable disposable) =>
    {
      disposable = factory();
      AddItem(disposable);
    });
  }

  public TDisposable Add<TDisposable>(OutFactory<TDisposable> factory)
    where TDisposable : class, IDisposable
  {
    Argument.That.NotNull(factory);

    return System.Disposable.Create((out TDisposable disposable) =>
    {
      factory(out disposable);
      AddItem(disposable);
    });
  }

  public IDisposable? Take<TDisposable>(TDisposable disposable)
    where TDisposable : class, IDisposable
  {
    Argument.That.NotNull(disposable);

    return RemoveItem(disposable) ? disposable : default;
  }

  public IDisposable[] Take()
    => RemoveItems().ToArray();

  public bool Dispose<T>(T disposable)
    where T : class, IDisposable
  {
    Argument.That.NotNull(disposable);

    if (!RemoveItem(disposable))
      return false;
    disposable.Dispose();
    return true;
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  public IEnumerator<IDisposable> GetEnumerator()
    => GetItemsEnumerator();

  #endregion
  #region Interfaces implementations

  IEnumerator IEnumerable.GetEnumerator()
    => GetItemsEnumerator();

  #endregion
}

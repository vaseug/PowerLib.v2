using System;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Disposables;

public abstract class DisposablesListBase : DisposablesCollectionBase, IDisposablesList
{
  protected DisposablesListBase()
  { }

  #region Internal methods

  protected abstract IDisposable GetItemAt(int index);

  protected abstract void InsertItem(int index, IDisposable disposable);

  protected abstract IDisposable RemoveItemAt(int index);

  #endregion
  #region Public properties

  public IDisposable this[int index]
    => GetItemAt(index);

  #endregion
  #region Public methods

  public TDisposable Insert<TDisposable>(int index, Factory<TDisposable> factory)
    where TDisposable : class, IDisposable
  {
    Argument.That.NotNull(factory);

    return System.Disposable.Create((out TDisposable disposable) =>
    {
      disposable = factory();
      InsertItem(index, disposable);
    });
  }

  public TDisposable Insert<TDisposable>(int index, OutFactory<TDisposable> factory)
    where TDisposable : class, IDisposable
  {
    Argument.That.NotNull(factory);

    return System.Disposable.Create((out TDisposable disposable) =>
    {
      factory(out disposable);
      InsertItem(index, disposable);
    });
  }

  public IDisposable TakeAt(int index)
    => RemoveItemAt(index);

  public void DisposeAt(int index)
    => RemoveItemAt(index).Dispose();

  #endregion
}

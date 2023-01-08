using System;
using System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Disposables;

public abstract class DisposablesList : DisposablesListBase
{
  private IList<IDisposable>? _innerBase;

  #region Constructors

  protected DisposablesList(IList<IDisposable> innerBase)
  {
    Argument.That.NotNull(innerBase);

    _innerBase = innerBase;
  }

  #endregion
  #region Internal properties

  protected IList<IDisposable> InnerBase
    => _innerBase ?? throw new ObjectDisposedException(null);

  protected override int ItemsCount => InnerBase.Count;

  #endregion
  #region Internal methods

  protected override bool ContainsItem(IDisposable disposable)
  {
    Argument.That.NotNull(disposable);

    return InnerBase.Contains(disposable);
  }

  protected override IDisposable GetItemAt(int index)
  {
    Argument.That.InRangeIn(InnerBase.Count, index);

    return InnerBase[index];
  }

  protected override void AddItem(IDisposable disposable)
  {
    Argument.That.NotNull(disposable);

    InnerBase.Add(disposable);
  }

  protected override void InsertItem(int index, IDisposable disposable)
  {
    Argument.That.NotNull(disposable);
    Argument.That.InRangeOut(InnerBase.Count, index);

    InnerBase.Insert(index, disposable);
  }

  protected override bool RemoveItem(IDisposable disposable)
  {
    Argument.That.NotNull(disposable);

    return InnerBase.Remove(disposable);
  }

  protected override IDisposable RemoveItemAt(int index)
  {
    Argument.That.InRangeIn(InnerBase.Count, index);

    return InnerBase.TakeAt(index);
  }

  protected override IEnumerable<IDisposable> RemoveItems()
  {
    while (InnerBase.Count > 0)
      yield return InnerBase.TakeLast();
  }

  protected override IEnumerator<IDisposable> GetItemsEnumerator()
  {
    foreach (var item in InnerBase)
      yield return item;
  }

  protected override void Dispose(bool disposing)
  {
    if (!disposing)
      return;
    base.Dispose(disposing);
    _innerBase = null;
  }

  #endregion
}

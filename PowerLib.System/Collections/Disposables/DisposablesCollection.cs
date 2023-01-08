using System;
using System.Collections.Generic;
using System.Linq;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Disposables;

public abstract class DisposablesCollection : DisposablesCollectionBase
{
  private ICollection<IDisposable>? _innerBase;

  #region Constructors

  protected DisposablesCollection(ICollection<IDisposable> innerBase)
  {
    Argument.That.NotNull(innerBase);

    _innerBase = innerBase;
  }

  #endregion
  #region Internal properties

  protected ICollection<IDisposable> InnerBase
    => _innerBase ?? throw new ObjectDisposedException(null);

  protected override int ItemsCount => InnerBase.Count;

  #endregion
  #region Internal methods

  protected override bool ContainsItem(IDisposable disposable)
  {
    Argument.That.NotNull(disposable);

    return InnerBase.Contains(disposable);
  }

  protected override void AddItem(IDisposable disposable)
  {
    Argument.That.NotNull(disposable);

    InnerBase.Add(disposable);
  }

  protected override bool RemoveItem(IDisposable disposable)
  {
    Argument.That.NotNull(disposable);

    return InnerBase.Remove(disposable);
  }

  protected override IEnumerable<IDisposable> RemoveItems()
  {
    var disposables = InnerBase.ToList();
    while (disposables.Count > 0)
    {
      var disposable = disposables.TakeLast<IDisposable>();
      if (InnerBase.Remove(disposable))
        yield return disposable;
    }
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

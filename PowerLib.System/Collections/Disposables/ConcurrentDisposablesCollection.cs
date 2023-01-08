using System;
using System.Collections.Generic;

namespace PowerLib.System.Collections.Disposables;

public abstract class ConcurrentDisposablesCollection : DisposablesCollection
{
  #region Constructors

  protected ConcurrentDisposablesCollection(ICollection<IDisposable> innerBase)
    : base(innerBase)
  { }

  #endregion
  #region Internal properties

  protected override int ItemsCount
  {
    get
    {
      EnterReadLock();
      try
      {
        return base.ItemsCount;
      }
      finally
      {
        ExitReadLock();
      }
    }
  }

  #endregion
  #region Internal methods

  protected abstract void EnterReadLock();

  protected abstract void EnterWriteLock();

  protected abstract void ExitReadLock();

  protected abstract void ExitWriteLock();

  protected override bool ContainsItem(IDisposable disposable)
  {
    EnterReadLock();
    try
    {
      return base.ContainsItem(disposable);
    }
    finally
    {
      ExitReadLock();
    }
  }

  protected override void AddItem(IDisposable disposable)
  {
    EnterWriteLock();
    try
    {
      base.AddItem(disposable);
    }
    finally
    {
      ExitWriteLock();
    }
  }

  protected override bool RemoveItem(IDisposable disposable)
  {
    EnterWriteLock();
    try
    {
      return base.RemoveItem(disposable);
    }
    finally
    {
      ExitWriteLock();
    }
  }

  protected override IEnumerable<IDisposable> RemoveItems()
  {
    EnterWriteLock();
    try
    {
      foreach (var item in base.RemoveItems())
        yield return item;
    }
    finally
    {
      ExitWriteLock();
    }
  }

  protected override IEnumerator<IDisposable> GetItemsEnumerator()
  {
    EnterReadLock();
    try
    {
      using var enumerator = base.GetItemsEnumerator();
      while (enumerator.MoveNext())
        yield return enumerator.Current;
    }
    finally
    {
      ExitReadLock();
    }
  }

  #endregion
}

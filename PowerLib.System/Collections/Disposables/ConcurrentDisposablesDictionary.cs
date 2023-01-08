using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace PowerLib.System.Collections.Disposables;

public abstract class ConcurrentDisposablesDictionary<TKey> : DisposablesDictionary<TKey>
  where TKey : notnull
{
  #region Constructors

  protected ConcurrentDisposablesDictionary(IDictionary<TKey, IDisposable> innerBase)
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

  protected override IEnumerable<TKey> KeyItems
  {
    get
    {
      EnterReadLock();
      try
      {
        foreach (var keyItem in base.KeyItems)
          yield return keyItem;
      }
      finally
      {
        ExitReadLock();
      }
    }
  }

  protected override IEnumerable<IDisposable> ValueItems
  {
    get
    {
      EnterReadLock();
      try
      {
        foreach (var valueItem in base.ValueItems)
          yield return valueItem;
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

  protected override bool ContainsKeyItem(TKey key)
  {
    EnterReadLock();
    try
    {
      return base.ContainsKeyItem(key);
    }
    finally
    {
      ExitReadLock();
    }
  }

  protected override bool GetValueItem(TKey key, [NotNullWhen(true)] out IDisposable? disposable)
  {
    EnterReadLock();
    try
    {
      return base.GetValueItem(key, out disposable);
    }
    finally
    {
      ExitReadLock();
    }
  }

  protected override void AddItem(TKey key, IDisposable disposable)
  {
    EnterWriteLock();
    try
    {
      base.AddItem(key, disposable);
    }
    finally
    {
      ExitWriteLock();
    }
  }

  protected override bool RemoveItem(TKey key, [NotNullWhen(true)] out IDisposable? disposable)
  {
    EnterWriteLock();
    try
    {
      return base.RemoveItem(key, out disposable);
    }
    finally
    {
      ExitWriteLock();
    }
  }

  protected override IEnumerable<KeyValuePair<TKey, IDisposable>> RemoveItems()
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

  protected override IEnumerator<KeyValuePair<TKey, IDisposable>> GetItemsEnumerator()
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

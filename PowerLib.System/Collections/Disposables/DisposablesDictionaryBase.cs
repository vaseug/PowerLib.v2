using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Disposables;

public abstract class DisposablesDictionaryBase<TKey> : IDisposablesDictionary<TKey>
  where TKey : notnull
{
  protected DisposablesDictionaryBase()
  { }

  #region Internal properties

  protected abstract int ItemsCount { get; }

  protected abstract IEnumerable<TKey> KeyItems { get; }

  protected abstract IEnumerable<IDisposable> ValueItems { get; }

  #endregion
  #region Internal methods

  protected abstract bool ContainsKeyItem(TKey key);

  protected abstract bool GetValueItem(TKey key, [NotNullWhen(true)] out IDisposable? disposable);

  protected abstract IDisposable GetValueItem(TKey key);

  protected abstract void AddItem(TKey key, IDisposable disposable);

  protected abstract IDisposable RemoveItem(TKey key);

  protected abstract bool RemoveItem(TKey key, [NotNullWhen(true)] out IDisposable? disposable);

  protected abstract IEnumerable<KeyValuePair<TKey, IDisposable>> RemoveItems();

  protected abstract IEnumerator<KeyValuePair<TKey, IDisposable>> GetItemsEnumerator();

  protected virtual void Dispose(bool disposing)
  {
    if (!disposing)
      return;
    foreach (var item in RemoveItems())
      Safe.Invoke(() => DisposeItem(item.Value));
  }

  protected virtual void DisposeItem(IDisposable disposable)
  {
    disposable?.Dispose();
  }

  #endregion
  #region Public properties

  public int Count
    => ItemsCount;

  public IDisposable this[TKey key]
    => GetValueItem(key);

  public IEnumerable<TKey> Keys
    => KeyItems;

  public IEnumerable<IDisposable> Values
    => ValueItems;

  #endregion
  #region Public methods

  public bool ContainsKey(TKey key)
    => ContainsKeyItem(key);

  public bool TryGetValue(TKey key, [NotNullWhen(true)] out IDisposable? value)
    => GetValueItem(key, out value);

  public TDisposable Add<TDisposable>(TKey key, Factory<TDisposable> factory)
    where TDisposable : class, IDisposable
  {
    Argument.That.NotNull(factory);

    return System.Disposable.Create((out TDisposable disposable) =>
    {
      disposable = factory();
      AddItem(key, disposable);
    });
  }

  public TDisposable Add<TDisposable>(TKey key, OutFactory<TDisposable> factory)
    where TDisposable : class, IDisposable
  {
    Argument.That.NotNull(factory);

    return System.Disposable.Create((out TDisposable disposable) =>
    {
      factory(out disposable);
      AddItem(key, disposable);
    });
  }

  public TDisposable Add<TDisposable>(Factory<TDisposable> factory, Func<TDisposable, TKey> keySelector)
    where TDisposable : class, IDisposable
  {
    Argument.That.NotNull(factory);
    Argument.That.NotNull(keySelector);

    return System.Disposable.Create((out TDisposable disposable) =>
    {
      disposable = factory();
      AddItem(keySelector(disposable), disposable);
    });
  }

  public TDisposable Add<TDisposable>(OutFactory<TDisposable> factory, Func<TDisposable, TKey> keySelector)
    where TDisposable : class, IDisposable
  {
    Argument.That.NotNull(factory);
    Argument.That.NotNull(keySelector);

    return System.Disposable.Create((out TDisposable disposable) =>
    {
      factory(out disposable);
      AddItem(keySelector(disposable), disposable);
    });
  }

  public IDisposable Take(TKey key)
    => RemoveItem(key);

  public bool TryTake(TKey key, [NotNullWhen(true)] out IDisposable? disposable)
    => RemoveItem(key, out disposable);

  public KeyValuePair<TKey, IDisposable>[] Take()
    => RemoveItems().ToArray();

  public bool Dispose(TKey key)
  {
    if (!RemoveItem(key, out var disposable))
      return false;
    disposable.Dispose();
    return true;
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  public IEnumerator<KeyValuePair<TKey, IDisposable>> GetEnumerator()
    => GetItemsEnumerator();

  #endregion
  #region Interfaces implementations

  IEnumerator IEnumerable.GetEnumerator()
    => GetItemsEnumerator();

  #endregion
}

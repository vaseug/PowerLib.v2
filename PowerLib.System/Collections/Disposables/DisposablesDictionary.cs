using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Disposables;

public abstract class DisposablesDictionary<TKey> : DisposablesDictionaryBase<TKey>
  where TKey : notnull
{
  private IDictionary<TKey, IDisposable>? _innerBase;

  #region Constructors

  protected DisposablesDictionary(IDictionary<TKey, IDisposable> innerBase)
  {
    _innerBase = Argument.That.NotNull(innerBase);
  }

  #endregion
  #region Internal properties

  protected IDictionary<TKey, IDisposable> InnerBase
    => _innerBase ?? throw new ObjectDisposedException(null);

  protected override int ItemsCount => InnerBase.Count;

  protected override IEnumerable<TKey> KeyItems => InnerBase.Keys;

  protected override IEnumerable<IDisposable> ValueItems => InnerBase.Values;

  #endregion
  #region Internal methods

  protected override bool ContainsKeyItem(TKey key)
    => InnerBase.ContainsKey(Argument.That.NotNull(key));

  protected override bool GetValueItem(TKey key, [NotNullWhen(true)] out IDisposable? disposable)
    => InnerBase.TryGetValue(Argument.That.NotNull(key), out disposable);

  protected override IDisposable GetValueItem(TKey key)
    => InnerBase[Argument.That.NotNull(key)];

  protected override void AddItem(TKey key, IDisposable disposable)
    => InnerBase.Add(Argument.That.NotNull(key), Argument.That.NotNull(disposable));

  protected override IDisposable RemoveItem(TKey key)
  {
    var value = InnerBase[Argument.That.NotNull(key)];
    var removed = InnerBase.Remove(key);
    Operation.That.IsValid(removed);
    return value;
  }

  protected override bool RemoveItem(TKey key, [NotNullWhen(true)] out IDisposable? disposable)
  {
    Argument.That.NotNull(key);

    var removed = InnerBase.TryGetValue(key, out var value) && InnerBase.Remove(key);
    disposable = removed ? value : default;
    return removed;
  }

  protected override IEnumerable<KeyValuePair<TKey, IDisposable>> RemoveItems()
  {
    var keys = InnerBase.Keys.ToList();
    while (keys.Count > 0)
    {
      var key = keys.TakeLast<TKey>();
      if (InnerBase.TryGetValue(key, out var disposable) && InnerBase.Remove(key))
        yield return new KeyValuePair<TKey, IDisposable>(key, disposable);
    }
  }

  protected override IEnumerator<KeyValuePair<TKey, IDisposable>> GetItemsEnumerator()
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

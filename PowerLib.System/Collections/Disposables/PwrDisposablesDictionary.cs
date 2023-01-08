using System;
using System.Collections.Generic;

namespace PowerLib.System.Collections.Disposables;

public sealed class PwrDisposablesDictionary<TKey> : DisposablesDictionary<TKey>
  where TKey : notnull
{
  #region Constructors

  public PwrDisposablesDictionary()
    : base(new Dictionary<TKey, IDisposable>())
  { }

  #endregion
}

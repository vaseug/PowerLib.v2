using System;
using System.Collections.Generic;
using System.Threading;

namespace PowerLib.System.Collections.Disposables;

public sealed class PwrConcurentDisposablesDictionary<TKey> : ConcurrentDisposablesDictionary<TKey>
  where TKey : notnull
{
  private readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

  #region Constructors

  public PwrConcurentDisposablesDictionary()
    : base(new Dictionary<TKey, IDisposable>())
  { }

  #endregion
  #region Internal methods

  protected override void EnterReadLock()
    => _locker.EnterReadLock();

  protected override void EnterWriteLock()
    => _locker.EnterWriteLock();

  protected override void ExitReadLock()
    => _locker.ExitReadLock();

  protected override void ExitWriteLock()
    => _locker.ExitWriteLock();

  protected override void Dispose(bool disposing)
  {
    base.Dispose(disposing);
    if (disposing)
      _locker.Dispose();
  }

  #endregion
}

using System;
using System.Threading;

namespace PowerLib.System.Collections.Disposables;

public sealed class PwrConcurentDisposablesList : ConcurrentDisposablesList
{
  private readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

  #region Constructors

  public PwrConcurentDisposablesList()
    : base(CollectionsController.CreateList<IDisposable>())
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

using System;
using System.IO;
using System.Threading;
using PowerLib.System.IO;
using PowerLib.System.Validation;

namespace PowerLib.System
{
  public sealed class FileSystemProgress : Progress<FileSystemCount>, IDisposable
  {
    private Timer? _timer;
    private volatile int _period = Timeout.Infinite;
    private readonly FileSystemAccumulator _counter;

    #region Constructors

    public FileSystemProgress()
    {
      _counter = new FileSystemAccumulator(FileSystemCount.MaxValue);
    }

    public FileSystemProgress(FileSystemCount delta)
    {
      _counter = new FileSystemAccumulator(delta);
    }

    public FileSystemProgress(FileSystemCount total, FileSystemCount delta)
    {
      _counter = new FileSystemAccumulator(total, delta);
    }

    public FileSystemProgress(FileSystemCount total, int directoryUnits, int fileUnits, int capacityUnits)
    {
      _counter = new FileSystemAccumulator(total, directoryUnits, fileUnits, capacityUnits);
    }

    public FileSystemProgress(Action<FileSystemCount> handler)
      : base(handler)
    {
      _counter = new FileSystemAccumulator(FileSystemCount.MaxValue);
    }

    public FileSystemProgress(Action<FileSystemCount> handler, FileSystemCount delta)
      : base(handler)
    {
      _counter = new FileSystemAccumulator(delta);
    }

    public FileSystemProgress(Action<FileSystemCount> handler, FileSystemCount total, FileSystemCount delta)
      : base(handler)
    {
      _counter = new FileSystemAccumulator(total, delta);
    }

    public FileSystemProgress(Action<FileSystemCount> handler, FileSystemCount total, int directoryUnits, int fileUnits, int capacityUnits)
      : base(handler)
    {
      _counter = new FileSystemAccumulator(total, directoryUnits, fileUnits, capacityUnits);
    }

    #endregion
    #region Properties

    private Timer Timer
      => LazyInitializer.EnsureInitialized(ref _timer, () => new Timer(OnTimer))!;

    public FileSystemCount Value
      => _counter.Value;

    public FileSystemCount Total
      => _counter.Total;

    public FileSystemCount Delta
      => _counter.Delta;

    #endregion
    #region Methods

    private void OnTimer(object state)
    {
      base.OnReport(_counter.Value);
      var period = _period;
      if (period >= 0)
        Timer.Change(_period, Timeout.Infinite);
    }

    protected override void OnReport(FileSystemCount value)
    {
      if (_counter.Accumulate(value, out var total))
        base.OnReport(total);
    }

    public void StartPeriodic(int period)
    {
      if (Interlocked.CompareExchange(ref _period, period, Timeout.Infinite) != Timeout.Infinite)
        Operation.That.Failed();
      Timer.Change(_period, Timeout.Infinite);
    }

    public void StopPeriodic()
    {
      var period = _period;
      if (Interlocked.CompareExchange(ref _period, Timeout.Infinite, period) == Timeout.Infinite)
        Operation.That.Failed();
      Timer.Change(0, Timeout.Infinite);
    }

    public void Dispose()
    {
      Disposable.Dispose(ref _timer);
    }

    #endregion
  }
}

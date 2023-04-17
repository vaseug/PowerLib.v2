using PowerLib.System.Validation;

namespace PowerLib.System;

public abstract class DeltaAccumulator<T>
  where T : struct
{
  private readonly object _lock = new();
  private readonly T _zero;
  private readonly T _total;
  private readonly T _delta;
  private T _value;
  private T _part;
  private bool _started;

  protected DeltaAccumulator(T zeroValue, T totalValue, T deltaValue)
  {
    Argument.That.GreaterThanOrEqual(deltaValue, zeroValue, Compare);
    Argument.That.GreaterThanOrEqual(totalValue, zeroValue, Compare);

    _delta = deltaValue;
    _zero = zeroValue;
    _total = totalValue;
    Reset();
  }

  protected abstract T Accumulate(T accum, T value);

  protected abstract int Compare(T xValue, T yValue);

  public T Value
    => _value;

  public T Zero
    => _zero;

  public T Total
    => Compare(_total, _zero) == 0 ? _value : _total;

  public T Delta
    => _delta;

  public void Reset()
  {
    lock (_lock)
    {
      _started = false;
      _part = _zero;
      _value = _zero;
    }
  }

  public bool Accumulate(T value, out T result)
  {
    lock (_lock)
    {
      _part = Accumulate(_part, value);
      _value = Accumulate(_value, value);
      var output = false;
      if (Compare(_part, _delta) >= 0)
      {
        _part = _zero;
        output = true;
      }
      else if (!_started)
      {
        _started = true;
        output = true;
      }
      else if (Compare(_value, _total) == 0)
      {
        output = true;
      }
      result = _value;
      return output;
    }
  }
}

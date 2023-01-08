namespace PowerLib.System.Accessors;

public sealed class ValueProvider<TValue> : IValueProvider<TValue>
{
  public ValueProvider(TValue value)
  {
    Value = value;
  }

  public TValue Value { get; }
}

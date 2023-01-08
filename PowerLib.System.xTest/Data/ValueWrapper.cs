namespace PowerLib.System.Test.Data;

internal readonly struct ValueWrapper<TValue>
{
  public TValue? Value { get; init; }
}

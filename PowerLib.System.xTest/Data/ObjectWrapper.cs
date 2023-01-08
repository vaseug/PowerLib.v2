namespace PowerLib.System.Test.Data;

internal sealed class ObjectWrapper<TValue>
{
  public TValue? Value { get; init; }
}

internal static class ObjectWrapper
{
  public static ObjectWrapper<TValue> Create<TValue>(TValue? value)
    => new ObjectWrapper<TValue> { Value = value };
}
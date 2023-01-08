namespace PowerLib.System.Accessors;

public interface IValueProvider<out TValue>
{
  TValue Value { get; }
}

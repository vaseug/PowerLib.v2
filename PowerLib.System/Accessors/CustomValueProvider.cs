using System;

namespace PowerLib.System.Accessors;

public sealed class CustomValueProvider<TValue> : IValueProvider<TValue>
{
  private readonly Func<TValue> _provider;

  public CustomValueProvider(Func<TValue> provider)
  {
    _provider = provider;
  }

  public TValue Value => _provider();
}

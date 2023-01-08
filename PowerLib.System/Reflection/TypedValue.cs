using System;
using System.Collections.Concurrent;
using PowerLib.System.Validation;

namespace PowerLib.System.Reflection;

public sealed class TypedValue
{
  private TypedValue(object? value, Type type)
  {
    Type = type;
    Value = value;
  }

  internal Type Type { get; }

  internal object? Value { get; }

  private static Lazy<ConcurrentDictionary<Type, TypedValue>> defaults = new (() => new ConcurrentDictionary<Type, TypedValue>());

  public static object DefaultOf<T>()
    => defaults.Value.GetOrAdd(typeof(T), type => new TypedValue(type.IsValueType ? Activator.CreateInstance(type) : null, type));

  public static object DefaultOf(Type type)
    => defaults.Value.GetOrAdd(Argument.That.NotNull(type), type => new TypedValue(type.IsValueType ? Activator.CreateInstance(type) : null, type));

  public static object ValueOf<T>(T? value)
    => value is null ? DefaultOf<T>() : new TypedValue(value, typeof(T));

  public static object ValueOf(object? value, Type type)
    => value is null ? DefaultOf(type) : new TypedValue(Argument.That.InstanceOf(value, type), type);

  public static object? GetValue(object? value)
    => value is TypedValue typedValue ? typedValue.Value : value;

  public static Type? GetType(object? value)
    => value is TypedValue typedValue ? typedValue.Type : value?.GetType();
}

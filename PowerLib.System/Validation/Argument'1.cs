using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace PowerLib.System.Validation;

public readonly struct Argument<T> : IEquatable<Argument<T>>
{
  public T Value { get; init; }

  public string? Name { get; init; }

  public override string ToString()
    => (string.IsNullOrEmpty(Name) ? string.Empty : $"{Name}: ") + Value?.ToString() ?? string.Empty;

  public static implicit operator T(Argument<T> argument)
    => argument.Value;

  public bool Equals(Argument<T> other)
    => EqualityComparer<T>.Default.Equals(Value, other.Value) && Name == other.Name;

  public override bool Equals([NotNullWhen(true)] object? obj)
    => obj is Argument<T> value && Equals(value);

  public override int GetHashCode()
    => CompositeHashing.Default.GetHashCode(Value, Name);

  public static bool operator ==(Argument<T> x, Argument<T> y)
    => x.Equals(y.Value);

  public static bool operator !=(Argument<T> x, Argument<T> y)
    => !x.Equals(y.Value);
}


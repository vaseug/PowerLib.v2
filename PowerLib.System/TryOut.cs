using System;
using System.Collections.Generic;

namespace PowerLib.System;

public readonly struct TryOut<T> : IEquatable<TryOut<T>>
{
  #region Constructor

  internal TryOut(T? value)
  {
    Value = value;
    Success = true;
  }

  #endregion
  #region Properties

  public static readonly TryOut<T> Failed;

  public T? Value { get; }

  public bool Success { get; }

  #endregion
  #region Methods

  public bool Equals(TryOut<T> other)
    => Success == other.Success && EqualityComparer<T>.Default.Equals(Value, other.Value);

  public override bool Equals(object? obj)
    => obj is TryOut<T> value && Equals(value);

  public override int GetHashCode()
    => CompositeHashing.Default.GetHashCode(Value, Success);

  public static bool operator ==(TryOut<T> left, TryOut<T> right)
    => left.Equals(right);

  public static bool operator !=(TryOut<T> left, TryOut<T> right)
    => !left.Equals(right);

  #endregion
}

public static class TryOut
{
  public static ref readonly TryOut<T> Failure<T>()
    => ref TryOut<T>.Failed;

  public static TryOut<T> Success<T>(T? value)
    => new(value);
}

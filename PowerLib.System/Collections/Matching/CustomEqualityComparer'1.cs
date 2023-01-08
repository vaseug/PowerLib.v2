using System.Collections;
using System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class CustomEqualityComparer<T> : IEqualityComparer<T>, IEqualityComparer
{
  private readonly Equality<T?> _equality;
  private readonly Hasher<T>? _hasher;

  #region Constructors

  public CustomEqualityComparer(Equality<T?> equality)
  {
    _equality = Argument.That.NotNull(equality);
    _hasher = default;
  }

  public CustomEqualityComparer(Equality<T?> equality, Hasher<T> hasher)
  {
    _equality = Argument.That.NotNull(equality);
    _hasher = Argument.That.NotNull(hasher);
  }

  public CustomEqualityComparer(IEqualityComparer<T> equalityComparer)
  {
    Argument.That.NotNull(equalityComparer);

    _equality = equalityComparer.AsEquality();
    _hasher = equalityComparer.AsHasher();
  }

  #endregion
  #region Methods

  public bool Equals(T? x, T? y)
    => _equality(x, y);

  public int GetHashCode(T obj)
    => obj is null ? 0 : (_hasher ?? EqualityComparer<T>.Default.GetHashCode)(obj);

  #endregion
  #region Interfaces implementations

  bool IEqualityComparer.Equals(object? x, object? y)
    => Equals(Argument.That.OfType<T>(x), Argument.That.OfType<T>(y));

  int IEqualityComparer.GetHashCode(object obj)
    => GetHashCode(Argument.That.OfType<T>(obj)!);

  #endregion
}

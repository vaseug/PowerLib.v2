using System.Collections;
using System.Collections.Generic;
using PowerLib.System.Collections.NonGeneric.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class CustomEqualityComparer : IEqualityComparer
{
  private readonly Equality<object?> _equality;
  private readonly Hasher<object>? _hasher;

  #region Constructors

  public CustomEqualityComparer(Equality<object?> equality)
  {
    _equality = Argument.That.NotNull(equality);
    _hasher = default;
  }

  public CustomEqualityComparer(Equality<object?> equality, Hasher<object> hasher)
  {
    _equality = Argument.That.NotNull(equality);
    _hasher = Argument.That.NotNull(hasher);
  }

  public CustomEqualityComparer(IEqualityComparer equalityComparer)
  {
    Argument.That.NotNull(equalityComparer);

    _equality = equalityComparer.AsEquality();
    _hasher = equalityComparer.AsHasher();
  }

  #endregion
  #region Methods

  public new bool Equals(object? x, object? y)
    => _equality(x, y);

  public int GetHashCode(object obj)
    => obj is null ? 0 : (_hasher ?? EqualityComparer<object>.Default.GetHashCode)(obj);

  #endregion
}

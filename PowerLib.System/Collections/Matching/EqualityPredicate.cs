using System.Collections;
using System.Collections.Generic;
using PowerLib.System.Collections.NonGeneric;
using PowerLib.System.Collections.NonGeneric.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class EqualityPredicate : IPredicate
{
  private readonly Equality<object?>? _equality;

  #region Constructors

  public EqualityPredicate(object? value)
  {
    _equality = default;
    Value = value;
  }

  public EqualityPredicate(object? value, Equality<object?> equality)
  {
    _equality = Argument.That.NotNull(equality);
    Value = value;
  }

  public EqualityPredicate(object? value, IEqualityComparer equalityComparer)
  {
    _equality = Argument.That.NotNull(equalityComparer).AsEquality();
    Value = value;
  }

  #endregion
  #region Properties

  public object? Value { get; }

  #endregion
  #region Methods

  public bool Match(object? obj)
    => (_equality ?? EqualityComparer<object>.Default.AsEquality())(obj, Value);

  #endregion
}

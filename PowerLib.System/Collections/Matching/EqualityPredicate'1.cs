using System.Collections.Generic;
using PowerLib.System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Collections.NonGeneric;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class EqualityPredicate<T> : IPredicate<T>, IPredicate
{
  private readonly Equality<T?>? _equality;

  #region Constructors

  public EqualityPredicate(T? value)
  {
    _equality = default;
    Value = value;
  }

  public EqualityPredicate(T? value, Equality<T?> equality)
  {
    _equality = Argument.That.NotNull(equality);
    Value = value;
  }

  public EqualityPredicate(T? value, IEqualityComparer<T> equalityComparer)
  {
    _equality = Argument.That.NotNull(equalityComparer).AsEquality<T>();
    Value = value;
  }

  #endregion
  #region Properties

  public T? Value { get; }

  #endregion
  #region Methods

  public bool Match(T? obj)
    => (_equality ?? EqualityComparer<T>.Default.AsEquality<T>())(obj, Value);

  #endregion
  #region Interfaces implementations

  bool IPredicate.Match(object? obj)
    => Match(Argument.That.OfType<T>(obj));

  #endregion
}

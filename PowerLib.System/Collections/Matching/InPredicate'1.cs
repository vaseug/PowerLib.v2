using System;
using System.Collections.Generic;
using System.Linq;
using PowerLib.System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Collections.NonGeneric;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class InPredicate<T> : IPredicate<T>, IPredicate
{
  private readonly Equality<T?>? _equality;

  #region Constructors

  public InPredicate(IEnumerable<T?> collection)
  {
    _equality = default;
    Collection = Argument.That.NotNull(collection);
  }

  public InPredicate(IEnumerable<T?> collection, Equality<T?> equality)
  {
    _equality = Argument.That.NotNull(equality);
    Collection = Argument.That.NotNull(collection);
  }

  public InPredicate(IEnumerable<T?> collection, IEqualityComparer<T> equalityComparer)
  {
    _equality = Argument.That.NotNull(equalityComparer).AsEquality();
    Collection = Argument.That.NotNull(collection);
  }

  #endregion
  #region Properties

  public IEnumerable<T?> Collection { get; }

  #endregion
  #region Methods

  public bool Match(T? obj)
    => Collection.Any(item => (_equality ?? EqualityComparer<T>.Default.AsEquality<T>())(item, obj));

  #endregion
  #region Interfaces implementations

  bool IPredicate.Match(object? obj)
    => Match(Argument.That.OfType<T>(obj));

  #endregion
}

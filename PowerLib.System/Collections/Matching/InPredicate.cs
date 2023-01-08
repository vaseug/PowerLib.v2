using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PowerLib.System.Collections.NonGeneric;
using PowerLib.System.Collections.NonGeneric.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class InPredicate : IPredicate
{
  private readonly Equality<object?>? _equality;

  #region Constructors

  public InPredicate(IEnumerable collection)
  {
    _equality = default;
    Collection = Argument.That.NotNull(collection);
  }

  public InPredicate(IEnumerable collection, Equality<object?> equality)
  {
    _equality = Argument.That.NotNull(equality);
    Collection = Argument.That.NotNull(collection);
  }

  public InPredicate(IEnumerable collection, IEqualityComparer equalityComparer)
  {
    _equality = Argument.That.NotNull(equalityComparer).AsEquality();
    Collection = Argument.That.NotNull(collection);
  }

  #endregion
  #region Properties

  public IEnumerable Collection { get; }

  #endregion
  #region Methods

  public bool Match(object? obj)
    => Collection.Cast<object?>().Any(item => (_equality ?? EqualityComparer<object>.Default.AsEquality())(item, obj));

  #endregion
}

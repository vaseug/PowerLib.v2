using System;
using PowerLib.System.Collections.Generic;
using PowerLib.System.Collections.NonGeneric;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class TypeOfPredicate<T> : IPredicate<object>, IPredicate
{
  private static readonly Lazy<TypeOfPredicate<T>> instance = new(() => new TypeOfPredicate<T>());

  #region Constructors

  private TypeOfPredicate()
  { }

  #endregion
  #region Properties

  public static TypeOfPredicate<T> Default
    => instance.Value;

  #endregion
  #region Methods

  public bool Match(object? obj)
    => Argument.That.NotNull(obj) is T;

  #endregion
}

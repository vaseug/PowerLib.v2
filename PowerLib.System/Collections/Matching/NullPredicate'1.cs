using System;
using PowerLib.System.Collections.Generic;
using PowerLib.System.Collections.NonGeneric;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class NullPredicate<T> : IPredicate<T>, IPredicate
{
  private static readonly Lazy<NullPredicate<T>> instance = new();

  #region Constructors

  private NullPredicate()
  { }

  #endregion
  #region Properties

  public static NullPredicate<T> Default
    => instance.Value;

  #endregion
  #region Methods

  public bool Match(T? obj)
    => obj is null;

  #endregion
  #region Interfaces implementations

  bool IPredicate.Match(object? obj)
    => Match(Argument.That.OfType<T>(obj));

  #endregion
}

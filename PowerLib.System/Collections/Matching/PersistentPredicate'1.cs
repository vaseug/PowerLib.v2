using System;
using PowerLib.System.Collections.Generic;

namespace PowerLib.System.Collections.Matching;

public sealed class PersistentPredicate<T> : IPredicate<T>
{
  private readonly bool _result;

  private static readonly Lazy<PersistentPredicate<T>> instanceFalse = new(() => new PersistentPredicate<T>(false));
  private static readonly Lazy<PersistentPredicate<T>> instanceTrue = new(() => new PersistentPredicate<T>(true));

  #region Constructors

  private PersistentPredicate(bool value)
  {
    _result = value;
  }

  #endregion
  #region Properties

  public static PersistentPredicate<T> False => instanceFalse.Value;

  public static PersistentPredicate<T> True => instanceTrue.Value;

  #endregion
  #region Methods

  public bool Match(T? obj)
    => _result;

  #endregion
}

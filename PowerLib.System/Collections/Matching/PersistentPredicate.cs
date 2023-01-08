using System;
using PowerLib.System.Collections.NonGeneric;

namespace PowerLib.System.Collections.Matching;

public sealed class PersistentPredicate : IPredicate
{
  private readonly bool _result;

  private static readonly Lazy<PersistentPredicate> instanceFalse = new Lazy<PersistentPredicate>(() => new PersistentPredicate(false));
  private static readonly Lazy<PersistentPredicate> instanceTrue = new Lazy<PersistentPredicate>(() => new PersistentPredicate(true));

  #region Constructors

  private PersistentPredicate(bool value)
  {
    _result = value;
  }

  #endregion
  #region Properties

  public static PersistentPredicate False => instanceFalse.Value;

  public static PersistentPredicate True => instanceTrue.Value;

  #endregion
  #region Methods

  public bool Match(object? obj)
    => _result;

  #endregion
}

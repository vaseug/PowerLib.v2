using System;
using PowerLib.System.Collections.NonGeneric;

namespace PowerLib.System.Collections.Matching;

public sealed class NullPredicate : IPredicate
{
  private static readonly Lazy<NullPredicate> instance = new();

  #region Constructors

  private NullPredicate()
  { }

  #endregion
  #region Properties

  public static NullPredicate Default
    => instance.Value;

  #endregion
  #region Methods

  public bool Match(object? obj)
    => obj is null;

  #endregion
}

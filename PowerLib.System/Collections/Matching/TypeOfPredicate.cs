using System;
using PowerLib.System.Collections.Generic;
using PowerLib.System.Collections.NonGeneric;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class TypeOfPredicate : IPredicate<object>, IPredicate
{
  #region Constructors

  public TypeOfPredicate(Type type)
  {
    Type = Argument.That.NotNull(type);
  }

  #endregion
  #region Properties

  public Type Type { get; }

  #endregion
  #region Methods

  public bool Match(object? obj)
    => Type.IsInstanceOfType(Argument.That.NotNull(obj));

  #endregion
}

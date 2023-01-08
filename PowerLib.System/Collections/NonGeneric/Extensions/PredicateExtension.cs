using System;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.NonGeneric.Extensions;

public static class PredicateExtension
{
  public static Predicate<object?> AsPredicate(this IPredicate predicate)
    => Argument.That.NotNull(predicate).Match;
}

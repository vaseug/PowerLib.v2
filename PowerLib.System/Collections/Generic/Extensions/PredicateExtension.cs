using System;
using PowerLib.System;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Generic.Extensions;

public static class PredicateExtension
{
  public static Predicate<T?> AsPredicate<T>(this IPredicate<T> predicate)
    => Argument.That.NotNull(predicate).Match;
}

using System;
using System.Collections.Generic;
using System.Linq;
using PowerLib.System.Validation;

namespace PowerLib.System;

public static class Safe
{
  public static IEnumerable<Exception> EnumerateExceptions(Exception exception, int maxDepth = -1)
  {
    Argument.That.NotNull(exception);

    yield return exception;
    if (maxDepth == 0)
      yield break;
    IReadOnlyCollection<Exception> innerExceptions = exception is AggregateException aggregateException
      ? aggregateException.InnerExceptions : exception.InnerException is not null ? new[] { exception.InnerException } : Array.Empty<Exception>();
    foreach (var innerException in innerExceptions)
      foreach (var itemException in EnumerateExceptions(innerException, maxDepth > 0 ? maxDepth - 1 : maxDepth))
        yield return itemException;
  }

  public static bool SuppressException(Exception exception, bool suppress, bool strong, params Type[] exceptionTypes)
  {
    Argument.That.NotNull(exception);
    Argument.That.NotNull(exceptionTypes);

    return !suppress ^ exceptionTypes.Any(type => strong ? type.Equals(exception.GetType()) : type.IsInstanceOfType(exception));
  }

  public static bool SuppressExceptions(IEnumerable<Exception> exceptions, bool suppress, bool strong, params Type[] exceptionTypes)
    => !suppress ^ Argument.That.NotEmpty(exceptions)//.NotNullElements().Value
      .All(exception => exceptionTypes.Any(type => strong ? type.Equals(exception.GetType()) : type.IsInstanceOfType(exception)));

  public static void Invoke(Action action, Predicate<Exception>? suppressPredicate = null, Action<Exception>? suppressAction = null)
  {
    Argument.That.NotNull(action);

    try
    {
      action();
    }
    catch (Exception ex)
    {
      if (!suppressPredicate?.Invoke(ex) ?? false)
        throw;
      suppressAction?.Invoke(ex);
    }
  }

  public static T? Invoke<T>(Func<T?> functor, T? defaultResult = default, Predicate<Exception>? suppressPredicate = null, Action<Exception>? suppressAction = null)
  {
    Argument.That.NotNull(functor);

    try
    {
      return functor();
    }
    catch (Exception ex)
    {
      if (!suppressPredicate?.Invoke(ex) ?? false)
        throw;
      suppressAction?.Invoke(ex);
      return defaultResult;
    }
  }

  public static T? Invoke<T>(Func<T?> functor, Predicate<Exception>? suppressPredicate = default, Func<Exception, T?>? suppressFunctor = default)
  {
    Argument.That.NotNull(functor);
    Argument.That.NotNull(suppressFunctor);

    try
    {
      return functor();
    }
    catch (Exception ex)
    {
      if (!suppressPredicate?.Invoke(ex) ?? false)
        throw;
      return suppressFunctor != null ? suppressFunctor(ex) : default;
    }
  }
}

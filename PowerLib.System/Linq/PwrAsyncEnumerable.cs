using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PowerLib.System.Collections;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Collections.NonGeneric.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Linq;

public static class PwrAsyncEnumerable
{
  private const int InvalidIndex = -1;

  #region Collection methods
  #region Enumerate methods

  public static async ValueTask EnumerateAsync<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken = default)
  {
    Argument.That.NotNull(source);

    var asyncEnumerator = source.GetAsyncEnumerator(cancellationToken);
    try
    {
      while (await asyncEnumerator.MoveNextAsync(cancellationToken)) ;
    }
    finally
    {
      await asyncEnumerator.DisposeAsync();
    }
  }

  #endregion
  #region IndexOf methods

  private static async ValueTask<int> IndexOfCoreAsync<TSource>(IAsyncEnumerable<TSource> source, ElementPredicate<TSource> predicate)
  {
    var enumerator = source.GetAsyncEnumerator();
    try
    {
      for (int index = 0; await enumerator.MoveNextAsync(); index++)
      {
        try
        {
          if (predicate(enumerator.Current, index))
            return index;
        }
        catch (Exception ex)
        {
          Argument.That.ElementFail(source, index, ex);
        }
      }
      return InvalidIndex;
    }
    finally
    {
      await AsyncDisposable.DisposeAsync(ref enumerator);
    }
  }

  public static ValueTask<int> IndexOfAsync<TSource>(this IAsyncEnumerable<TSource> source, Predicate<TSource> predicate)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(predicate);

    return IndexOfCoreAsync(source, (item, index) => predicate(item));
  }

  public static ValueTask<int> IndexOfAsync<TSource>(this IAsyncEnumerable<TSource> source, ElementPredicate<TSource> predicate)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(predicate);

    return IndexOfCoreAsync(source, predicate);
  }

  #endregion
  #region ForEach methods

  private static async ValueTask<int> ForEachCoreAsync<TSource>(IAsyncEnumerable<TSource> source, ElementAction<TSource> action)
  {
    var asyncEnumerator = source.GetAsyncEnumerator();
    try
    {
      var index = 0;
      for (; await asyncEnumerator.MoveNextAsync(); index++)
      {
        try
        {
          action(asyncEnumerator.Current, index);
        }
        catch (Exception ex)
        {
          Argument.That.ElementFail(source, index, ex);
        }
      }
      return index;
    }
    finally
    {
      await AsyncDisposable.DisposeAsync(ref asyncEnumerator);
    }
  }

  public static ValueTask<int> ForEachAsync<TSource>(this IAsyncEnumerable<TSource> source, Action<TSource> action)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(action);

    return ForEachCoreAsync(source, (item, index) => action(item));
  }

  public static ValueTask<int> ForEachAsync<TSource>(this IAsyncEnumerable<TSource> source, ElementAction<TSource> action)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(action);

    return ForEachCoreAsync(source, action);
  }

  #endregion
  #region Apply methods

  private static async IAsyncEnumerable<TSource> ApplyCoreAsync<TSource>(IAsyncEnumerable<TSource> source, ElementAction<TSource> action)
  {
    var asyncEnumerator = source.GetAsyncEnumerator();
    try
    {
      for (int index = 0; await asyncEnumerator.MoveNextAsync(); index++)
      {
        try
        {
          action(asyncEnumerator.Current, index);
        }
        catch (Exception ex)
        {
          Argument.That.ElementFail(source, index, ex);
        }
        yield return asyncEnumerator.Current;
      }
    }
    finally
    {
      await AsyncDisposable.DisposeAsync(ref asyncEnumerator);
    }
  }

  public static IAsyncEnumerable<TSource> ApplyAsync<TSource>(this IAsyncEnumerable<TSource> source, Action<TSource> action)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(action);

    return ApplyCoreAsync(source, (item, index) => action(item));
  }

  public static IAsyncEnumerable<TSource> ApplyAsync<TSource>(this IAsyncEnumerable<TSource> source, ElementAction<TSource> action)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(action);

    return ApplyCoreAsync(source, action);
  }

  #endregion
  #region Produce methods

  private static async IAsyncEnumerable<TSource> ProduceCoreAsync<TSource, TState>(TState state, Func<TState, TSource, int, TState> automation, Func<TState, int, bool> predicate, Func<TState, int, Task<TSource>> factory)
  {
    for (int index = 0; predicate(state, index); index++)
    {
      var source = await factory(state, index);
      yield return source;
      state = automation(state, source, index);
    }
  }

  private static async IAsyncEnumerable<TSource> ProduceCoreAsync<TSource, TState>(TState state, Action<TState, TSource, int> automation, Func<TState, int, bool> predicate, Func<TState, int, Task<TSource>> factory)
    where TState : class
  {
    for (int index = 0; predicate(state, index); )
    {
      var source = await factory(state, index);
      yield return source;
      automation(state, source, index);
    }
  }

  public static IAsyncEnumerable<TSource> ProduceAsync<TSource, TState>(TState state, Func<TState, TSource, TState> automation, Func<TState, bool> predicate, Func<TState, Task<TSource>> factory)
  {
    Argument.That.NotNull(factory);
    Argument.That.NotNull(predicate);
    Argument.That.NotNull(automation);

    return ProduceCoreAsync(state, (state, source, index) => automation(state, source), (state, index) => predicate(state), (state, Index) => factory(state));
  }

  public static IAsyncEnumerable<TSource> ProduceAsync<TSource, TState>(TState state, Action<TState, TSource> automation, Func<TState, bool> predicate, Func<TState, Task<TSource>> factory)
    where TState : class
  {
    Argument.That.NotNull(factory);
    Argument.That.NotNull(predicate);
    Argument.That.NotNull(automation);

    return ProduceCoreAsync(state, (state, source, index) => automation(state, source), (state, index) => predicate(state), (state, Index) => factory(state));
  }

  public static IAsyncEnumerable<TSource> ProduceAsync<TSource, TState>(TState state, Func<TState, TSource, int, TState> automation, Func<TState, int, bool> predicate, Func<TState, int, Task<TSource>> factory)
  {
    Argument.That.NotNull(factory);
    Argument.That.NotNull(predicate);
    Argument.That.NotNull(automation);

    return ProduceCoreAsync(state, automation, predicate, factory);
  }

  public static IAsyncEnumerable<TSource> ProduceAsync<TSource, TState>(TState state, Action<TState, TSource, int> automation, Func<TState, int, bool> predicate, Func<TState, int, Task<TSource>> factory)
    where TState : class
  {
    Argument.That.NotNull(factory);
    Argument.That.NotNull(predicate);
    Argument.That.NotNull(automation);

    return ProduceCoreAsync(state, automation, predicate, factory);
  }

  #endregion
  #region SkipWhile methods

  private static async IAsyncEnumerable<TSource> SkipWhileCoreAsync<TSource, TState>(IAsyncEnumerable<TSource> source, TState state, Func<TState, TSource, int, TState> automation, Func<TState, TSource, int, bool> predicate)
  {
    var asyncEnumerator = source.GetAsyncEnumerator();
    try
    {
      var success = await asyncEnumerator.MoveNextAsync();
      for (int index = 0; success && predicate(state, asyncEnumerator.Current, index); success = await asyncEnumerator.MoveNextAsync())
        state = automation(state, asyncEnumerator.Current, index);
      for (; success; success = await asyncEnumerator.MoveNextAsync())
        yield return asyncEnumerator.Current;
    }
    finally
    {
      await AsyncDisposable.DisposeAsync(ref asyncEnumerator);
    }
  }

  private static async IAsyncEnumerable<TSource> SkipWhileCoreAsync<TSource, TState>(IAsyncEnumerable<TSource> source, TState state, Action<TState, TSource, int> automation, Func<TState, TSource, int, bool> predicate)
    where TState : class
  {
    var asyncEnumerator = source.GetAsyncEnumerator();
    try
    {
      var success = await asyncEnumerator.MoveNextAsync();
      for (int index = 0; success && predicate(state, asyncEnumerator.Current, index); success = await asyncEnumerator.MoveNextAsync())
        automation(state, asyncEnumerator.Current, index);
      for (; success; success = await asyncEnumerator.MoveNextAsync())
        yield return asyncEnumerator.Current;
    }
    finally
    {
      await AsyncDisposable.DisposeAsync(ref asyncEnumerator);
    }
  }

  public static IAsyncEnumerable<TSource> SkipWhileAsync<TSource, TState>(this IAsyncEnumerable<TSource> source, TState state, Func<TState, TSource, TState> automation, Func<TState, TSource, bool> predicate)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(predicate);
    Argument.That.NotNull(automation);

    return SkipWhileCoreAsync(source, state, (state, item, index) => automation(state, item), (state, item, index) => predicate(state, item));
  }

  public static IAsyncEnumerable<TSource> SkipWhileAsync<TSource, TState>(this IAsyncEnumerable<TSource> source, TState state, Action<TState, TSource> automation, Func<TState, TSource, bool> predicate)
    where TState : class
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(automation);
    Argument.That.NotNull(predicate);

    return SkipWhileCoreAsync(source, state, (state, item, index) => automation(state, item), (state, item, index) => predicate(state, item));
  }

  public static IAsyncEnumerable<TSource> SkipWhileAsync<TSource, TState>(this IAsyncEnumerable<TSource> source, TState state, Func<TState, TSource, int, TState> automation, Func<TState, TSource, int, bool> predicate)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(predicate);
    Argument.That.NotNull(automation);

    return SkipWhileCoreAsync(source, state, automation, predicate);
  }

  public static IAsyncEnumerable<TSource> SkipWhileAsync<TSource, TState>(this IAsyncEnumerable<TSource> source, TState state, Action<TState, TSource, int> automation, Func<TState, TSource, int, bool> predicate)
    where TState : class
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(automation);
    Argument.That.NotNull(predicate);

    return SkipWhileCoreAsync(source, state, automation, predicate);
  }

  #endregion
  #region TakeWhile methods

  private static async IAsyncEnumerable<TSource> TakeWhileCoreAsync<TSource, TState>(IAsyncEnumerable<TSource> source, TState state, Func<TState, TSource, int, TState> automation, Func<TState, TSource, int, bool> predicate)
  {
    var asyncEnumerator = source.GetAsyncEnumerator();
    try
    {
      for (var index = 0; await asyncEnumerator.MoveNextAsync() && predicate(state, asyncEnumerator.Current, index); state = automation(state, asyncEnumerator.Current, index++))
        yield return asyncEnumerator.Current;
    }
    finally
    {
      await AsyncDisposable.DisposeAsync(ref asyncEnumerator);
    }
  }

  private static async IAsyncEnumerable<TSource> TakeWhileCoreAsync<TSource, TState>(IAsyncEnumerable<TSource> source, TState state, Action<TState, TSource, int> automation, Func<TState, TSource, int, bool> predicate)
    where TState : class
  {
    var asyncEnumerator = source.GetAsyncEnumerator();
    try
    {
      for (var index = 0; await asyncEnumerator.MoveNextAsync() && predicate(state, asyncEnumerator.Current, index); automation(state, asyncEnumerator.Current, index++))
        yield return asyncEnumerator.Current;
    }
    finally
    {
      await AsyncDisposable.DisposeAsync(ref asyncEnumerator);
    }
  }

  public static IAsyncEnumerable<TSource> TakeWhileAsync<TSource, TState>(this IAsyncEnumerable<TSource> source, TState state, Func<TState, TSource, TState> automation, Func<TState, TSource, bool> predicate)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(automation);
    Argument.That.NotNull(predicate);

    return TakeWhileCoreAsync(source, state, (state, item, index) => automation(state, item), (state, item, index) => predicate(state, item));
  }

  public static IAsyncEnumerable<TSource> TakeWhileAsync<TSource, TState>(this IAsyncEnumerable<TSource> source, TState state, Action<TState, TSource> automation, Func<TState, TSource, bool> predicate)
    where TState : class
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(automation);
    Argument.That.NotNull(predicate);

    return TakeWhileCoreAsync(source, state, (state, item, index) => automation(state, item), (state, item, index) => predicate(state, item));
  }

  public static IAsyncEnumerable<TSource> TakeWhileAsync<TSource, TState>(this IAsyncEnumerable<TSource> source, TState state, Func<TState, TSource, int, TState> automation, Func<TState, TSource, int, bool> predicate)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(automation);
    Argument.That.NotNull(predicate);

    return TakeWhileCoreAsync(source, state, automation, predicate);
  }

  public static IAsyncEnumerable<TSource> TakeWhileAsync<TSource, TState>(this IAsyncEnumerable<TSource> source, TState state, Action<TState, TSource, int> automation, Func<TState, TSource, int, bool> predicate)
    where TState : class
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(automation);
    Argument.That.NotNull(predicate);

    return TakeWhileCoreAsync(source, state, automation, predicate);
  }

  #endregion
  #endregion
  #region Sort methods

  public static async ValueTask<IList<TSource>> ToSortedListAsync<TSource>(this IAsyncEnumerable<TSource> source, Comparison<TSource> comparison, SortingOption sortingOption = SortingOption.None)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(comparison);

    var list = CollectionsController.CreateList<TSource>();
    var asyncEnumerator = source.GetAsyncEnumerator();
    for (int index = 0; await asyncEnumerator.MoveNextAsync(); index++)
      if (list.AddSorted(asyncEnumerator.Current, comparison, sortingOption) < 0)
        Argument.That.ElementFail(source, index, null,
          CollectionResources.Default.FormatString(CollectionMessage.DuplicateCollectionElement, nameof(source), nameof(index)));
    return list;
  }

  public static async ValueTask<IEnumerable<TSource>> SortAsync<TSource>(this IAsyncEnumerable<TSource> source, SortingOption sortingOption = SortingOption.None)
    => await source.ToSortedListAsync(Comparer<TSource>.Default.Compare, sortingOption);

  public static async ValueTask<IEnumerable<TSource>> SortAsync<TSource>(this IAsyncEnumerable<TSource> source, Comparison<TSource>? comparison, SortingOption sortingOption = SortingOption.None)
    => await source.ToSortedListAsync(comparison ?? Comparer<TSource>.Default.Compare, sortingOption);

  public static async ValueTask<IEnumerable<TSource>> SortAsync<TSource>(this IAsyncEnumerable<TSource> source, IComparer<TSource>? comparer, SortingOption sortingOption = SortingOption.None)
    => await source.ToSortedListAsync((comparer ?? Comparer<TSource>.Default).Compare, sortingOption);

  #endregion
  #region Equals methods

  private static async ValueTask<bool> SequenceEqualCoreAsync<T>(IAsyncEnumerable<T> xCollection, IEnumerable<T> yCollection, Equality<T?> equality)
  {
    if (xCollection == yCollection)
      return true;

    var xAsyncEnumerator = xCollection.GetAsyncEnumerator();
    try
    {
      using var yEnumerator = yCollection.GetEnumerator();
      bool xFlag, yFlag, result;
      do
      {
        xFlag = await xAsyncEnumerator.MoveNextAsync();
        yFlag = yEnumerator.MoveNext();
        result = xFlag && yFlag && equality(xAsyncEnumerator.Current, yEnumerator.Current) || !xFlag && !yFlag;
      }
      while (result && xFlag && yFlag);
      return result;
    }
    finally
    {
      await AsyncDisposable.DisposeAsync(ref xAsyncEnumerator);
    }
  }

  public static ValueTask<bool> SequenceEqualAsync<T>(this IAsyncEnumerable<T> source, IEnumerable<T> others)
    => SequenceEqualCoreAsync(Argument.That.NotNull(source), Argument.That.NotNull(others), EqualityComparer<T>.Default.AsEquality<T>());

  public static ValueTask<bool> SequenceEqualAsync<T>(this IAsyncEnumerable<T> source, IEnumerable<T> others, IEqualityComparer<T>? equalityComparer)
    => SequenceEqualCoreAsync(Argument.That.NotNull(source), Argument.That.NotNull(others), (equalityComparer ?? EqualityComparer<T>.Default).AsEquality());

  public static ValueTask<bool> SequenceEqualAsync<T>(this IAsyncEnumerable<T> source, IEnumerable<T> others, Equality<T?>? equality)
    => SequenceEqualCoreAsync(Argument.That.NotNull(source), Argument.That.NotNull(others), equality ?? EqualityComparer<T>.Default.AsEquality<T>());

  private static async ValueTask<bool> SequenceEqualCoreAsync<T>(IAsyncEnumerable<T> xCollection, IAsyncEnumerable<T> yCollection, Equality<T?> equality)
  {
    if (xCollection == yCollection)
      return true;

    var xAsyncEnumerator = xCollection.GetAsyncEnumerator();
    try
    {
      var yAsyncEnumerator = yCollection.GetAsyncEnumerator();
      try
      {
        bool xFlag, yFlag, result;
        do
        {
          xFlag = await xAsyncEnumerator.MoveNextAsync();
          yFlag = await yAsyncEnumerator.MoveNextAsync();
          result = xFlag && yFlag && equality(xAsyncEnumerator.Current, yAsyncEnumerator.Current) || !xFlag && !yFlag;
        }
        while (result && xFlag && yFlag);
        return result;
      }
      finally
      {
        await AsyncDisposable.DisposeAsync(ref yAsyncEnumerator);
      }
    }
    finally
    {
      await AsyncDisposable.DisposeAsync(ref xAsyncEnumerator);
    }
  }

  public static ValueTask<bool> SequenceEqualAsync<T>(this IAsyncEnumerable<T> source, IAsyncEnumerable<T> others)
    => SequenceEqualCoreAsync(Argument.That.NotNull(source), Argument.That.NotNull(others), EqualityComparer<T>.Default.AsEquality<T>());

  public static ValueTask<bool> SequenceEqualAsync<T>(this IAsyncEnumerable<T> source, IAsyncEnumerable<T> others, IEqualityComparer<T>? equalityComparer)
    => SequenceEqualCoreAsync(Argument.That.NotNull(source), Argument.That.NotNull(others), (equalityComparer ?? EqualityComparer<T>.Default).AsEquality());

  public static ValueTask<bool> SequenceEqualAsync<T>(this IAsyncEnumerable<T> source, IAsyncEnumerable<T> others, Equality<T?>? equality)
    => SequenceEqualCoreAsync(Argument.That.NotNull(source), Argument.That.NotNull(others), equality ?? EqualityComparer<T>.Default.AsEquality<T>());

  #endregion
  #region Compare methods

  private static async ValueTask<int> SequenceCompareCoreAsync<T>(IAsyncEnumerable<T> xCollection, IEnumerable<T> yCollection, Comparison<T> comparison, RelativeOrder emptyOrder)
  {
    var xAsyncEnumerator = xCollection.GetAsyncEnumerator();
    try
    {
      using var yEnumerator = yCollection.GetEnumerator();
      bool xFlag, yFlag;
      int result;
      do
      {
        xFlag = await xAsyncEnumerator.MoveNextAsync();
        yFlag = yEnumerator.MoveNext();
        result = xFlag ? yFlag ? comparison(xAsyncEnumerator.Current, yEnumerator.Current) :
          emptyOrder switch { RelativeOrder.Lower => 1, RelativeOrder.Upper => -1, _ => Argument.That.Invalid(emptyOrder) } :
          yFlag ? emptyOrder switch { RelativeOrder.Lower => -1, RelativeOrder.Upper => 1, _ => Argument.That.Invalid(emptyOrder) } : 0;
      }
      while (result == 0 && xFlag && yFlag);
      return result;
    }
    finally
    {
      await AsyncDisposable.DisposeAsync(ref xAsyncEnumerator);
    }
  }

  public static ValueTask<int> SequenceCompareAsync<T>(this IAsyncEnumerable<T> source, IEnumerable<T> others)
    => SequenceCompareCoreAsync(Argument.That.NotNull(source), Argument.That.NotNull(others), Comparer<T>.Default.Compare, RelativeOrder.Lower);

  public static ValueTask<int> SequenceCompareAsync<T>(this IAsyncEnumerable<T> source, IEnumerable<T> others, IComparer<T>? comparer)
    => SequenceCompareCoreAsync(Argument.That.NotNull(source), Argument.That.NotNull(others), (comparer ?? Comparer<T>.Default).Compare, RelativeOrder.Lower);

  public static ValueTask<int> SequenceCompareAsync<T>(this IAsyncEnumerable<T> source, IEnumerable<T> others, Comparison<T>? comparison)
    => SequenceCompareCoreAsync(Argument.That.NotNull(source), Argument.That.NotNull(others), comparison ?? Comparer<T>.Default.Compare, RelativeOrder.Lower);

  public static ValueTask<int> SequenceCompareAsync<T>(this IAsyncEnumerable<T> source, IEnumerable<T> others, RelativeOrder emptyOrder)
    => SequenceCompareCoreAsync(Argument.That.NotNull(source), Argument.That.NotNull(others), Comparer<T>.Default.Compare, emptyOrder);

  public static ValueTask<int> SequenceCompareAsync<T>(this IAsyncEnumerable<T> source, IEnumerable<T> others, IComparer<T>? comparer, RelativeOrder emptyOrder)
    => SequenceCompareCoreAsync(Argument.That.NotNull(source), Argument.That.NotNull(others), (comparer ?? Comparer<T>.Default).Compare, emptyOrder);

  public static ValueTask<int> SequenceCompareAsync<T>(this IAsyncEnumerable<T> source, IEnumerable<T> others, Comparison<T>? comparison, RelativeOrder emptyOrder)
    => SequenceCompareCoreAsync(Argument.That.NotNull(source), Argument.That.NotNull(others), comparison ?? Comparer<T>.Default.Compare, emptyOrder);

  private static async ValueTask<int> SequenceCompareCoreAsync<T>(IAsyncEnumerable<T> xCollection, IAsyncEnumerable<T> yCollection, Comparison<T> comparison, RelativeOrder emptyOrder)
  {
    var xAsyncEnumerator = xCollection.GetAsyncEnumerator();
    try
    {
      var yAsyncEnumerator = yCollection.GetAsyncEnumerator();
      try
      {
        bool xFlag, yFlag;
        int result;
        do
        {
          xFlag = await xAsyncEnumerator.MoveNextAsync();
          yFlag = await yAsyncEnumerator.MoveNextAsync();
          result = xFlag ? yFlag ? comparison(xAsyncEnumerator.Current, yAsyncEnumerator.Current) :
            emptyOrder switch { RelativeOrder.Lower => 1, RelativeOrder.Upper => -1, _ => Argument.That.Invalid(emptyOrder) } :
            yFlag ? emptyOrder switch { RelativeOrder.Lower => -1, RelativeOrder.Upper => 1, _ => Argument.That.Invalid(emptyOrder) } : 0;
        }
        while (result == 0 && xFlag && yFlag);
        return result;
      }
      finally
      {
        await AsyncDisposable.DisposeAsync(ref yAsyncEnumerator);
      }
    }
    finally
    {
      await AsyncDisposable.DisposeAsync(ref xAsyncEnumerator);
    }
  }

  public static ValueTask<int> SequenceCompareAsync<T>(this IAsyncEnumerable<T> source, IAsyncEnumerable<T> others)
    => SequenceCompareCoreAsync(Argument.That.NotNull(source), Argument.That.NotNull(others), Comparer<T>.Default.Compare, RelativeOrder.Lower);

  public static ValueTask<int> SequenceCompareAsync<T>(this IAsyncEnumerable<T> source, IAsyncEnumerable<T> others, IComparer<T>? comparer)
    => SequenceCompareCoreAsync(Argument.That.NotNull(source), Argument.That.NotNull(others), (comparer ?? Comparer<T>.Default).Compare, RelativeOrder.Lower);

  public static ValueTask<int> SequenceCompareAsync<T>(this IAsyncEnumerable<T> source, IAsyncEnumerable<T> others, Comparison<T>? comparison)
    => SequenceCompareCoreAsync(Argument.That.NotNull(source), Argument.That.NotNull(others), comparison ?? Comparer<T>.Default.Compare, RelativeOrder.Lower);

  public static ValueTask<int> SequenceCompareAsync<T>(this IAsyncEnumerable<T> source, IAsyncEnumerable<T> others, RelativeOrder emptyOrder)
    => SequenceCompareCoreAsync(Argument.That.NotNull(source), Argument.That.NotNull(others), Comparer<T>.Default.Compare, emptyOrder);

  public static ValueTask<int> SequenceCompareAsync<T>(this IAsyncEnumerable<T> source, IAsyncEnumerable<T> others, IComparer<T>? comparer, RelativeOrder emptyOrder)
    => SequenceCompareCoreAsync(Argument.That.NotNull(source), Argument.That.NotNull(others), (comparer ?? Comparer<T>.Default).Compare, emptyOrder);

  public static ValueTask<int> SequenceCompareAsync<T>(this IAsyncEnumerable<T> source, IAsyncEnumerable<T> others, Comparison<T>? comparison, RelativeOrder emptyOrder)
    => SequenceCompareCoreAsync(Argument.That.NotNull(source), Argument.That.NotNull(others), comparison ?? Comparer<T>.Default.Compare, emptyOrder);

  #endregion
  #region Aggregate methods
  #region Bound

  private static async ValueTask<T> BoundCoreAsync<T>(IAsyncEnumerable<T> source, Comparison<T> comparison, Bound bound)
  {
    var asyncEnumerator = source.GetAsyncEnumerator();
    try
    {
      T value = await asyncEnumerator.MoveNextAsync() ? asyncEnumerator.Current : Operation.That.Failed<T>();
      while (await asyncEnumerator.MoveNextAsync())
      {
        var result = comparison(value, asyncEnumerator.Current);
        if (bound == System.Bound.Lower && result < 0 || bound == System.Bound.Upper && result > 0)
          value = asyncEnumerator.Current;
      }
      return value;
    }
    finally
    {
      await AsyncDisposable.DisposeAsync(ref asyncEnumerator);
    }
  }

  public static ValueTask<T> BoundAsync<T>(this IAsyncEnumerable<T> source, Bound bound)
    => BoundCoreAsync(Argument.That.NotEmptyAsync(source), Comparer<T>.Default.AsComparison<T>(), bound);

  public static ValueTask<T> BoundAsync<T>(this IAsyncEnumerable<T> source, Comparison<T>? comparison, Bound bound)
    => BoundCoreAsync(Argument.That.NotEmptyAsync(source), (comparison ?? Comparer<T>.Default.Compare), bound);

  public static ValueTask<T> BoundAsync<T>(this IAsyncEnumerable<T> source, IComparer<T>? comparer, Bound bound)
    => BoundCoreAsync(Argument.That.NotEmptyAsync(source), (comparer ?? Comparer<T>.Default).AsComparison(), bound);

  #endregion
  #region Maximum

  public static ValueTask<T> MaxAsync<T>(this IAsyncEnumerable<T> source)
    => BoundCoreAsync(Argument.That.NotEmptyAsync(source), Comparer<T>.Default.AsComparison<T>(), Bound.Upper);

  public static ValueTask<T> MaxAsync<T>(this IAsyncEnumerable<T> source, Comparison<T>? comparison)
    => BoundCoreAsync(Argument.That.NotEmptyAsync(source), (comparison ?? Comparer<T>.Default.Compare).AsComparison(), Bound.Upper);

  public static ValueTask<T> MaxAsync<T>(this IAsyncEnumerable<T> source, IComparer<T>? comparer)
    => BoundCoreAsync(Argument.That.NotEmptyAsync(source), (comparer ?? Comparer<T>.Default).AsComparison(), Bound.Upper);

  #endregion
  #region Minimum

  public static ValueTask<T> MinAsync<T>(this IAsyncEnumerable<T> source)
    => BoundCoreAsync(Argument.That.NotEmptyAsync(source), Comparer<T>.Default.AsComparison<T>(), Bound.Lower);

  public static ValueTask<T> MinAsync<T>(this IAsyncEnumerable<T> source, Comparison<T>? comparison)
    => BoundCoreAsync(Argument.That.NotEmptyAsync(source), (comparison ?? Comparer<T>.Default.Compare).AsComparison(), Bound.Lower);

  public static ValueTask<T> MinAsync<T>(this IAsyncEnumerable<T> source, IComparer<T>? comparer)
    => BoundCoreAsync(Argument.That.NotEmptyAsync(source), (comparer ?? Comparer<T>.Default).AsComparison().AsComparison(), Bound.Lower);

  #endregion
  #endregion
}

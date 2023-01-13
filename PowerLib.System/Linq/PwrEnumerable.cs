using System;
using System.Collections.Generic;
using PowerLib.System.Collections;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Collections.NonGeneric.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Linq;

public static class PwrEnumerable
{
  private const int InvalidIndex = -1;
  private const int InvalidCount = -1;

  #region Collection methods
  #region Enumerate methods

  public static void Enumerate<TSource>(this IEnumerable<TSource> source)
  {
    Argument.That.NotNull(source);

    using var enumerator = source.GetEnumerator();
    while (enumerator.MoveNext()) ;
  }

  #endregion
  #region IndexOf methods

  private static int IndexOfCore<TSource>(IEnumerable<TSource> source, ElementPredicate<TSource> predicate)
  {
    using var enumerator = source.GetEnumerator();
    for (int index = 0; enumerator.MoveNext(); index++)
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

  public static int IndexOf<TSource>(this IEnumerable<TSource> source, Predicate<TSource> predicate)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(predicate);

    return IndexOfCore(source, (item, index) => predicate(item));
  }

  public static int IndexOf<TSource>(this IEnumerable<TSource> source, ElementPredicate<TSource> predicate)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(predicate);

    return IndexOfCore(source, predicate);
  }

  #endregion
  #region ForEach methods

  private static int ForEachCore<TSource>(IEnumerable<TSource> source, ElementAction<TSource> action)
  {
    using var enumerator = source.GetEnumerator();
    var index = 0;
    for (; enumerator.MoveNext(); index++)
    {
      try
      {
        action(enumerator.Current, index);
      }
      catch (Exception ex)
      {
        Argument.That.ElementFail(source, index, ex);
      }
    }
    return index;
  }

  public static int ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource> action)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(action);

    return ForEachCore(source, (item, index) => action(item));
  }

  public static int ForEach<TSource>(this IEnumerable<TSource> source, ElementAction<TSource> action)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(action);

    return ForEachCore(source, action);
  }

  #endregion
  #region Apply methods

  private static IEnumerable<TSource> ApplyCore<TSource>(IEnumerable<TSource> source, ElementAction<TSource> action)
  {
    using var enumerator = source.GetEnumerator();
    for (int index = 0; enumerator.MoveNext(); index++)
    {
      try
      {
        action(enumerator.Current, index);
      }
      catch (Exception ex)
      {
        Argument.That.ElementFail(source, index, ex);
      }
      yield return enumerator.Current;
    }
  }

  public static IEnumerable<TSource> Apply<TSource>(this IEnumerable<TSource> source, Action<TSource> action)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(action);

    return ApplyCore(source, (item, index) => action(item));
  }

  public static IEnumerable<TSource> Apply<TSource>(this IEnumerable<TSource> source, ElementAction<TSource> action)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(action);

    return ApplyCore(source, action);
  }

  #endregion
  #region Produce methods

  private static IEnumerable<TSource> ProduceCore<TSource, TState>(TState state, Func<TState, TSource, int, TState> automation, Func<TState, int, bool> predicate, Func<TState, int, TSource> factory)
  {
    for (int index = 0; predicate(state, index); index++)
    {
      var source = factory(state, index);
      yield return source;
      state = automation(state, source, index);
    }
  }

  private static IEnumerable<TSource> ProduceCore<TSource, TState>(TState state, Action<TState, TSource, int> automation, Func<TState, int, bool> predicate, Func<TState, int, TSource> factory)
    where TState : class
  {
    for (int index = 0; predicate(state, index); )
    {
      var source = factory(state, index);
      yield return source;
      automation(state, source, index);
    }
  }

  public static IEnumerable<TSource> Produce<TSource, TState>(TState state, Func<TState, TSource, TState> automation, Func<TState, bool> predicate, Func<TState, TSource> factory)
  {
    Argument.That.NotNull(factory);
    Argument.That.NotNull(predicate);
    Argument.That.NotNull(automation);

    return ProduceCore(state, (state, source, index) => automation(state, source), (state, index) => predicate(state), (state, Index) => factory(state));
  }

  public static IEnumerable<TSource> Produce<TSource, TState>(TState state, Action<TState, TSource> automation, Func<TState, bool> predicate, Func<TState, TSource> factory)
    where TState : class
  {
    Argument.That.NotNull(factory);
    Argument.That.NotNull(predicate);
    Argument.That.NotNull(automation);

    return ProduceCore(state, (state, source, index) => automation(state, source), (state, index) => predicate(state), (state, Index) => factory(state));
  }

  public static IEnumerable<TSource> Produce<TSource, TState>(TState state, Func<TState, TSource, int, TState> automation, Func<TState, int, bool> predicate, Func<TState, int, TSource> factory)
  {
    Argument.That.NotNull(factory);
    Argument.That.NotNull(predicate);
    Argument.That.NotNull(automation);

    return ProduceCore(state, automation, predicate, factory);
  }

  public static IEnumerable<TSource> Produce<TSource, TState>(TState state, Action<TState, TSource, int> automation, Func<TState, int, bool> predicate, Func<TState, int, TSource> factory)
    where TState : class
  {
    Argument.That.NotNull(factory);
    Argument.That.NotNull(predicate);
    Argument.That.NotNull(automation);

    return ProduceCore(state, automation, predicate, factory);
  }

  #endregion
  #region SkipWhile methods

  private static IEnumerable<TSource> SkipWhileCore<TSource, TState>(IEnumerable<TSource> source, TState state, Func<TState, TSource, int, TState> automation, Func<TState, TSource, int, bool> predicate)
  {
    using var enumerator = source.GetEnumerator();
    var success = enumerator.MoveNext();
    for (int index = 0; success && predicate(state, enumerator.Current, index); success = enumerator.MoveNext())
      state = automation(state, enumerator.Current, index);
    for (; success; success = enumerator.MoveNext())
      yield return enumerator.Current;
  }

  private static IEnumerable<TSource> SkipWhileCore<TSource, TState>(IEnumerable<TSource> source, TState state, Action<TState, TSource, int> automation, Func<TState, TSource, int, bool> predicate)
    where TState : class
  {
    using var enumerator = source.GetEnumerator();
    var success = enumerator.MoveNext();
    for (int index = 0; success && predicate(state, enumerator.Current, index); success = enumerator.MoveNext())
      automation(state, enumerator.Current, index);
    for (; success; success = enumerator.MoveNext())
      yield return enumerator.Current;
  }

  public static IEnumerable<TSource> SkipWhile<TSource, TState>(this IEnumerable<TSource> source, TState state, Func<TState, TSource, TState> automation, Func<TState, TSource, bool> predicate)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(predicate);
    Argument.That.NotNull(automation);

    return SkipWhileCore(source, state, (state, item, index) => automation(state, item), (state, item, index) => predicate(state, item));
  }

  public static IEnumerable<TSource> SkipWhile<TSource, TState>(this IEnumerable<TSource> source, TState state, Action<TState, TSource> automation, Func<TState, TSource, bool> predicate)
    where TState : class
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(automation);
    Argument.That.NotNull(predicate);

    return SkipWhileCore(source, state, (state, item, index) => automation(state, item), (state, item, index) => predicate(state, item));
  }

  public static IEnumerable<TSource> SkipWhile<TSource, TState>(this IEnumerable<TSource> source, TState state, Func<TState, TSource, int, TState> automation, Func<TState, TSource, int, bool> predicate)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(predicate);
    Argument.That.NotNull(automation);

    return SkipWhileCore(source, state, automation, predicate);
  }

  public static IEnumerable<TSource> SkipWhile<TSource, TState>(this IEnumerable<TSource> source, TState state, Action<TState, TSource, int> automation, Func<TState, TSource, int, bool> predicate)
    where TState : class
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(automation);
    Argument.That.NotNull(predicate);

    return SkipWhileCore(source, state, automation, predicate);
  }

  #endregion
  #region TakeWhile methods

  private static IEnumerable<TSource> TakeWhileCore<TSource, TState>(IEnumerable<TSource> source, TState state, Func<TState, TSource, int, TState> automation, Func<TState, TSource, int, bool> predicate)
  {
    using var enumerator = source.GetEnumerator();
    for (var index = 0; enumerator.MoveNext() && predicate(state, enumerator.Current, index); state = automation(state, enumerator.Current, index++))
      yield return enumerator.Current;
  }

  private static IEnumerable<TSource> TakeWhileCore<TSource, TState>(IEnumerable<TSource> source, TState state, Action<TState, TSource, int> automation, Func<TState, TSource, int, bool> predicate)
    where TState : class
  {
    using var enumerator = source.GetEnumerator();
    for (var index = 0; enumerator.MoveNext() && predicate(state, enumerator.Current, index); automation(state, enumerator.Current, index++))
      yield return enumerator.Current;
  }

  public static IEnumerable<TSource> TakeWhile<TSource, TState>(this IEnumerable<TSource> source, TState state, Func<TState, TSource, TState> automation, Func<TState, TSource, bool> predicate)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(automation);
    Argument.That.NotNull(predicate);

    return TakeWhileCore(source, state, (state, item, index) => automation(state, item), (state, item, index) => predicate(state, item));
  }

  public static IEnumerable<TSource> TakeWhile<TSource, TState>(this IEnumerable<TSource> source, TState state, Action<TState, TSource> automation, Func<TState, TSource, bool> predicate)
    where TState : class
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(automation);
    Argument.That.NotNull(predicate);

    return TakeWhileCore(source, state, (state, item, index) => automation(state, item), (state, item, index) => predicate(state, item));
  }

  public static IEnumerable<TSource> TakeWhile<TSource, TState>(this IEnumerable<TSource> source, TState state, Func<TState, TSource, int, TState> automation, Func<TState, TSource, int, bool> predicate)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(automation);
    Argument.That.NotNull(predicate);

    return TakeWhileCore(source, state, automation, predicate);
  }

  public static IEnumerable<TSource> TakeWhile<TSource, TState>(this IEnumerable<TSource> source, TState state, Action<TState, TSource, int> automation, Func<TState, TSource, int, bool> predicate)
    where TState : class
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(automation);
    Argument.That.NotNull(predicate);

    return TakeWhileCore(source, state, automation, predicate);
  }

  #endregion
  #endregion
  #region Sort methods

  public static IList<TSource> ToSortedList<TSource>(this IEnumerable<TSource> source, Comparison<TSource> comparison, SortingOption sortingOption = SortingOption.None)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNull(comparison);

    var capacity = source.PeekCount();
    var list = capacity < 0 ? CollectionsController.CreateList<TSource>() : CollectionsController.CreateList<TSource>(capacity);
    using var enumerator = source.GetEnumerator();
    for (int index = 0; enumerator.MoveNext(); index++)
      if (list.AddSorted(enumerator.Current, comparison, sortingOption) < 0)
        Argument.That.ElementFail(source, index, null,
          CollectionResources.Default.FormatString(CollectionMessage.DuplicateCollectionElement, nameof(source), nameof(index)));
    return list;
  }

  public static IEnumerable<TSource> Sort<TSource>(this IEnumerable<TSource> source, SortingOption sortingOption = SortingOption.None)
    => source.ToSortedList(Comparer<TSource>.Default.Compare, sortingOption);

  public static IEnumerable<TSource> Sort<TSource>(this IEnumerable<TSource> source, Comparison<TSource>? comparison, SortingOption sortingOption = SortingOption.None)
    => source.ToSortedList(comparison ?? Comparer<TSource>.Default.Compare, sortingOption);

  public static IEnumerable<TSource> Sort<TSource>(this IEnumerable<TSource> source, IComparer<TSource>? comparer, SortingOption sortingOption = SortingOption.None)
    => source.ToSortedList((comparer ?? Comparer<TSource>.Default).Compare, sortingOption);

  #endregion
  #region Equals methods

  private static bool SequenceEqualCore<T>(IEnumerable<T> xCollection, IEnumerable<T> yCollection, Equality<T?> equality)
  {
    if (xCollection == yCollection)
      return true;
    var xCount = xCollection.PeekCount();
    var yCount = yCollection.PeekCount();
    if (xCount > InvalidCount && yCount > InvalidCount && xCount != yCount)
      return false;

    using var xEnumerator = xCollection.GetEnumerator();
    using var yEnumerator = yCollection.GetEnumerator();

    bool xFlag, yFlag, result;
    do
    {
      xFlag = xEnumerator.MoveNext();
      yFlag = yEnumerator.MoveNext();
      result = xFlag && yFlag && equality(xEnumerator.Current, yEnumerator.Current) || !xFlag && !yFlag;
    }
    while (result && xFlag && yFlag);
    return result;
  }
/*
  public static bool SequenceEqual<T>(this IEnumerable<T> source, IEnumerable<T> others)
    => SequenceEqualCore(Argument.That.NotNull(source), Argument.That.NotNull(others), EqualityComparer<T>.Default.AsEquality<T>());

  public static bool SequenceEqual<T>(this IEnumerable<T> source, IEnumerable<T> others, IEqualityComparer<T>? equalityComparer)
    => SequenceEqualCore(Argument.That.NotNull(source), Argument.That.NotNull(others), (equalityComparer ?? EqualityComparer<T>.Default).AsEquality());
*/
  public static bool SequenceEqual<T>(this IEnumerable<T> source, IEnumerable<T> others, Equality<T?>? equality)
    => SequenceEqualCore(Argument.That.NotNull(source), Argument.That.NotNull(others), equality ?? EqualityComparer<T>.Default.AsEquality<T>());

  #endregion
  #region Compare methods

  private static int SequenceCompareCore<T>(IEnumerable<T> xCollection, IEnumerable<T> yCollection, Comparison<T> comparison, RelativeOrder emptyOrder)
  {
    using var xEnumerator = xCollection.GetEnumerator();
    using var yEnumerator = yCollection.GetEnumerator();
    bool xFlag, yFlag;
    int result;
    do
    {
      xFlag = xEnumerator.MoveNext();
      yFlag = yEnumerator.MoveNext();
      result = xFlag ? yFlag ? comparison(xEnumerator.Current, yEnumerator.Current) :
        emptyOrder switch { RelativeOrder.Lower => 1, RelativeOrder.Upper => -1, _ => Argument.That.Invalid(emptyOrder) } :
        yFlag ? emptyOrder switch { RelativeOrder.Lower => -1, RelativeOrder.Upper => 1, _ => Argument.That.Invalid(emptyOrder) } : 0;
    }
    while (result == 0 && xFlag && yFlag);
    return result;
  }

  public static int SequenceCompare<T>(this IEnumerable<T> source, IEnumerable<T> others)
    => SequenceCompareCore(Argument.That.NotNull(source), Argument.That.NotNull(others), Comparer<T>.Default.Compare, RelativeOrder.Lower);

  public static int SequenceCompare<T>(this IEnumerable<T> source, IEnumerable<T> others, IComparer<T>? comparer)
    => SequenceCompareCore(Argument.That.NotNull(source), Argument.That.NotNull(others), (comparer ?? Comparer<T>.Default).Compare, RelativeOrder.Lower);

  public static int SequenceCompare<T>(this IEnumerable<T> source, IEnumerable<T> others, Comparison<T>? comparison)
    => SequenceCompareCore(Argument.That.NotNull(source), Argument.That.NotNull(others), comparison ?? Comparer<T>.Default.Compare, RelativeOrder.Lower);

  public static int SequenceCompare<T>(this IEnumerable<T> source, IEnumerable<T> others, RelativeOrder emptyOrder)
    => SequenceCompareCore(Argument.That.NotNull(source), Argument.That.NotNull(others), Comparer<T>.Default.Compare, emptyOrder);

  public static int SequenceCompare<T>(this IEnumerable<T> source, IEnumerable<T> others, IComparer<T>? comparer, RelativeOrder emptyOrder)
    => SequenceCompareCore(Argument.That.NotNull(source), Argument.That.NotNull(others), (comparer ?? Comparer<T>.Default).Compare, emptyOrder);

  public static int SequenceCompare<T>(this IEnumerable<T> source, IEnumerable<T> others, Comparison<T>? comparison, RelativeOrder emptyOrder)
    => SequenceCompareCore(Argument.That.NotNull(source), Argument.That.NotNull(others), comparison ?? Comparer<T>.Default.Compare, emptyOrder);

  #endregion
  #region Aggregate methods
  #region Bound

  private static T BoundCore<T>(IEnumerable<T> source, Comparison<T> comparison, Bound bound)
  {
    using var enumerator = source.GetEnumerator();
    T value = enumerator.MoveNext() ? enumerator.Current : Operation.That.Failed<T>();
    while (enumerator.MoveNext())
    {
      var result = comparison(value, enumerator.Current);
      if (bound == System.Bound.Lower && result < 0 || bound == System.Bound.Upper && result > 0)
        value = enumerator.Current;
    }
    return value;
  }

  public static T Bound<T>(this IEnumerable<T> source, Bound bound)
    => BoundCore(Argument.That.NotEmpty(source), Comparer<T>.Default.AsComparison<T>(), bound);

  public static T Bound<T>(this IEnumerable<T> source, Comparison<T>? comparison, Bound bound)
    => BoundCore(Argument.That.NotEmpty(source), (comparison ?? Comparer<T>.Default.Compare), bound);

  public static T Bound<T>(this IEnumerable<T> source, IComparer<T>? comparer, Bound bound)
    => BoundCore(Argument.That.NotEmpty(source), (comparer ?? Comparer<T>.Default).AsComparison(), bound);

  #endregion
  #region Maximum
/*
  public static T Max<T>(this IEnumerable<T> source)
    => BoundCore(Argument.That.NotEmpty(source), Comparer<T>.Default.AsComparison<T>(), System.Bound.Upper);
*/
  public static T Max<T>(this IEnumerable<T> source, Comparison<T>? comparison)
    => BoundCore(Argument.That.NotEmpty(source), (comparison ?? Comparer<T>.Default.Compare).AsComparison(), System.Bound.Upper);

  public static T Max<T>(this IEnumerable<T> source, IComparer<T>? comparer)
    => BoundCore(Argument.That.NotEmpty(source), (comparer ?? Comparer<T>.Default).AsComparison(), System.Bound.Upper);

  #endregion
  #region Minimum
/*
  public static T Min<T>(this IEnumerable<T> source)
    => BoundCore(Argument.That.NotEmpty(source), Comparer<T>.Default.AsComparison<T>(), System.Bound.Lower);
*/
  public static T Min<T>(this IEnumerable<T> source, Comparison<T>? comparison)
    => BoundCore(Argument.That.NotEmpty(source), (comparison ?? Comparer<T>.Default.Compare).AsComparison(), System.Bound.Lower);

  public static T Min<T>(this IEnumerable<T> source, IComparer<T>? comparer)
    => BoundCore(Argument.That.NotEmpty(source), (comparer ?? Comparer<T>.Default).AsComparison().AsComparison(), System.Bound.Lower);

  #endregion
  #endregion
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PowerLib.System.Buffers;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Generic.Extensions;

public static class ListExtension
{
  private const int InvalidIndex = -1;

  #region Internal methods

  private static ElementPredicate<T> ElementPredicate<T>(Predicate<T> predicate)
    => (item, index) => predicate(item);

  private static ElementAction<T> ElementAction<T>(Action<T> action)
    => (item, index) => action(item);

  private static Func<int, T> ElementFiller<T>(Func<T> filler)
    => index => filler();

  private static int ExistingBoundIndex(int count, Bound bound)
    => count == 0 ? InvalidIndex : bound switch { Bound.Lower => 0, Bound.Upper => count - 1, _ => Argument.That.Invalid(bound) };

  private static int AddingBoundIndex(int count, Bound bound)
    => count == 0 ? 0 : bound switch { Bound.Lower => 0, Bound.Upper => count, _ => Argument.That.Invalid(bound) };

  #endregion
  #region Control methods

  public static void EnsureCapacity<TSource>(this IList<TSource> list, int capacity)
  {
    switch (list)
    {
      case List<TSource> fwList:
        if (fwList.Capacity < capacity)
          fwList.Capacity = capacity;
        break;
      case ICapacityControl capacitySupport:
        if (capacitySupport.Capacity < capacity)
          capacitySupport.Capacity = capacity;
        break;
    }
  }

  #endregion
  #region Manipulation methods
  #region Get

  public static TSource GetAt<TSource>(this IList<TSource> list, int index)
  {
    Argument.That.InRangeIn(list, index);

    return list[index];
  }

  public static TSource GetBound<TSource>(this IList<TSource> list, Bound bound)
  {
    Argument.That.NotEmpty(list);

    return list[ExistingBoundIndex(list.Count, bound)];
  }

  public static TSource GetFirst<TSource>(this IList<TSource> list)
    => list.GetBound(Bound.Lower);

  public static TSource GetLast<TSource>(this IList<TSource> list)
    => list.GetBound(Bound.Upper);

  public static bool TryGetBound<TSource>(this IList<TSource> list, Bound bound, out TSource? result)
  {
    Argument.That.NotNull(list);

    var isEmpty = list.Count == 0;
    result = isEmpty ? default : list[ExistingBoundIndex(list.Count, bound)];
    return !isEmpty;
  }

  public static bool TryGetFirst<TSource>(this IList<TSource> list, out TSource? result)
    => list.TryGetBound(Bound.Lower, out result);

  public static bool TryGetLast<TSource>(this IList<TSource> list, out TSource? result)
    => list.TryGetBound(Bound.Upper, out result);

  private static IReadOnlyList<TSource> GetRangeCore<TSource>(IList<TSource> list, int index, int count)
  {
    var resultList = CollectionsController.CreateList<TSource>(count);
    for (; count > 0; count--)
      resultList.Add(list[index++]);
    return resultList.AsReadOnlyList();
  }

  public static IReadOnlyList<TSource> GetRange<TSource>(this IList<TSource> list, int index, int count)
  {
    Argument.That.InRangeOut(list, index, count);

    return list switch
    {
      List<TSource> fwList => fwList.GetRange(index, count),
      _ => GetRangeCore(list, index, count),
    };
  }

  public static IReadOnlyList<TSource> GetRange<TSource>(this IList<TSource> list, (int index, int count) range)
  {
    Argument.That.InRangeOut(list, range);

    return list switch
    {
      List<TSource> fwList => fwList.GetRange(range.index, range.count),
      _ => GetRangeCore(list, range.index, range.count),
    };
  }

  #endregion
  #region Set

  public static void SetAt<TSource>(this IList<TSource> list, int index, TSource item)
  {
    Argument.That.InRangeIn(list, index);

    list[index] = item;
  }

  public static void SetBound<TSource>(this IList<TSource> list, Bound bound, TSource item)
  {
    Argument.That.NotEmpty(list);

    list[ExistingBoundIndex(list.Count, bound)] = item;
  }

  public static void SetFirst<TSource>(this IList<TSource> list, TSource item)
    => list.SetBound(Bound.Lower, item);

  public static void SetLast<TSource>(this IList<TSource> list, TSource item)
    => list.SetBound(Bound.Upper, item);

  public static bool TrySetBound<TSource>(this IList<TSource> list, Bound bound, TSource item)
  {
    Argument.That.NotNull(list);

    if (list.Count == 0)
      return false;
    list[ExistingBoundIndex(list.Count, bound)] = item;
    return true;
  }

  public static bool TryGetFirst<TSource>(this IList<TSource> list, TSource item)
    => list.TrySetBound(Bound.Lower, item);

  public static bool TrySetLast<TSource>(this IList<TSource> list, TSource item)
    => list.TrySetBound(Bound.Upper, item);

  private static void SetRepeatCore<TSource>(IList<TSource> list, int index, TSource value, int count)
  {
    if (count == 0)
      return;
    for (; count > 0; count--)
      list[index++] = value;
  }

  public static void SetRepeat<TSource>(this IList<TSource> list, int index, TSource value, int count)
  {
    Argument.That.InRangeOut(list, index, count);

    SetRepeatCore(list, index, value, count);
  }

  private static void SetRangeCore<TSource>(IList<TSource> list, int index, int count, IEnumerable<TSource> items)
  {
    if (items is IList<TSource> itemsList)
    {
      for (var itemsIndex = 0; index < list.Count && count > 0 && itemsIndex < itemsList.Count; itemsIndex++, index++, count--)
        list[index] = itemsList[itemsIndex];
    }
    else
    {
      using var enumerator = items.GetEnumerator();
      for (; index < list.Count && count > 0 && enumerator.MoveNext(); index++, count--)
        list[index] = enumerator.Current;
    }
  }

  public static void SetRange<TSource>(this IList<TSource> list, int index, IEnumerable<TSource> items)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(items);

    SetRangeCore(list, index, list.Count - index, items);
  }

  public static void SetRange<TSource>(this IList<TSource> list, int index, int count, IEnumerable<TSource> items)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(items);

    SetRangeCore(list, index, count, items);
  }

  public static void SetRange<TSource>(this IList<TSource> list, (int index, int count) range, IEnumerable<TSource> items)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(items);

    SetRangeCore(list, range.index, range.count, items);
  }

  #endregion
  #region Add

  public static void AddBound<TSource>(this IList<TSource> list, Bound bound, TSource item)
  {
    Argument.That.NotNull(list);

    list.Insert(AddingBoundIndex(list.Count, bound), item);
  }

  public static void AddFirst<TSource>(this IList<TSource> list, TSource item)
    => list.AddBound(Bound.Lower, item);

  public static void AddLast<TSource>(this IList<TSource> list, TSource item)
    => list.AddBound(Bound.Upper, item);

  private static void AddRepeatCore<TSource>(IList<TSource> list, TSource value, int count)
  {
    if (count == 0)
      return;
    list.EnsureCapacity(list.Count + count);
    for (; count > 0; count--)
      list.Add(value);
  }

  public static void AddRepeat<TSource>(this IList<TSource> list, TSource value, int count)
  {
    Argument.That.InLimitsOut(list, count);

    AddRepeatCore(list, value, count);
  }

  private static void AddRangeCore<TSource>(IList<TSource> list, IEnumerable<TSource> items)
  {
    int count = items.PeekCount();
    if (count == 0)
      return;
    if (count > 0)
      list.EnsureCapacity(list.Count + count);

    if (items is IList<TSource> itemsList)
    {
      for (var itemIndex = 0; itemIndex < itemsList.Count; itemIndex++)
        list.Add(itemsList[itemIndex]);
    }
    else
    {
      using var enumerator = items.GetEnumerator();
      while (enumerator.MoveNext())
        list.Add(enumerator.Current);
    }
  }

  public static void AddRange<TSource>(this IList<TSource> list, IEnumerable<TSource> items)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(items);

    switch (list)
    {
      case List<TSource> fwList:
        fwList.AddRange(items);
        break;
      default:
        AddRangeCore(list, items);
        break;
    }
  }

  #endregion
  #region Insert

  private static void InsertRepeatCore<TSource>(IList<TSource> list, int index, TSource value, int count)
  {
    if (count == 0)
      return;
    list.EnsureCapacity(list.Count + count);
    for (; count > 0; count--)
      list.Insert(index++, value);
  }

  public static void InsertRepeat<TSource>(this IList<TSource> list, int index, TSource value, int count)
  {
    Argument.That.InLimitsIn(list, index, count);

    InsertRepeatCore(list, index, value, count);
  }

  private static void InsertRangeCore<TSource>(IList<TSource> list, int index, IEnumerable<TSource> items)
  {
    var count = items.PeekCount();
    if (count == 0)
      return;
    if (count > 0)
      list.EnsureCapacity(list.Count + count);

    if (items is IList<TSource> itemsList)
    {
      for (var itemIndex = 0; itemIndex < itemsList.Count; itemIndex++)
        list.Insert(index++, itemsList[itemIndex]);
    }
    else
    {
      using var enumerator = items.GetEnumerator();
      while (enumerator.MoveNext())
        list.Insert(index++, enumerator.Current);
    }
  }

  public static void InsertRange<TSource>(this IList<TSource> list, int index, IEnumerable<TSource> items)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(items);

    switch (list)
    {
      case List<TSource> fwList:
        fwList.InsertRange(index, items);
        break;
      default:
        InsertRangeCore(list, index, items);
        break;
    }
  }

  #endregion
  #region Remove

  public static void RemoveBound<T>(this IList<T> list, Bound bound)
  {
    Argument.That.NotEmpty(list);

    list.RemoveAt(ExistingBoundIndex(list.Count, bound));
  }

  public static void RemoveFirst<T>(this IList<T> list)
    => list.RemoveBound(Bound.Lower);

  public static void RemoveLast<T>(this IList<T> list)
    => list.RemoveBound(Bound.Upper);

  public static bool TryRemoveBound<T>(this IList<T> list, Bound bound)
  {
    Argument.That.NotNull(list);

    if (list.Count == 0)
      return false;
    list.RemoveAt(ExistingBoundIndex(list.Count, bound));
    return true;
  }

  public static bool TryRemoveFirst<T>(this IList<T> list)
    => list.TryRemoveBound(Bound.Lower);

  public static bool TryRemoveLast<T>(this IList<T> list)
    => list.TryRemoveBound(Bound.Upper);

  private static void RemoveRangeCore<TSource>(IList<TSource> list, int index, int count)
  {
    for (index += count > 0 ? count - 1 : 0; count > 0; index--, count--)
      list.RemoveAt(index);
  }

  public static void RemoveRange<TSource>(this IList<TSource> list, int index, int count)
  {
    Argument.That.InRangeOut(list, index, count);

    switch (list)
    {
      case List<TSource> fwList:
        fwList.RemoveRange(index, count);
        break;
      default:
        RemoveRangeCore(list, index, count);
        break;
    }
  }

  public static void RemoveRange<TSource>(this IList<TSource> list, (int index, int count) range)
  {
    Argument.That.InRangeOut(list, range);

    switch (list)
    {
      case List<TSource> fwList:
        fwList.RemoveRange(range.index, range.count);
        break;
      default:
        RemoveRangeCore(list, range.index, range.count);
        break;
    }
  }

  #endregion
  #region Take

  private static T TakeCore<T>(IList<T> list, int index)
  {
    T result = list[index];
    list.RemoveAt(index);
    return result;
  }

  public static T TakeAt<T>(this IList<T> list, int index)
  {
    Argument.That.InRangeIn(list, index);

    return TakeCore(list, index);
  }

  public static T TakeBound<T>(this IList<T> list, Bound bound)
  {
    Argument.That.NotEmpty(list);

    return TakeCore(list, ExistingBoundIndex(list.Count, bound));
  }

  public static T TakeFirst<T>(this IList<T> list)
    => list.TakeBound(Bound.Lower);

  public static T TakeLast<T>(this IList<T> list)
    => list.TakeBound(Bound.Upper);

  public static bool TryTakeBound<T>(this IList<T> list, Bound bound, out T? result)
  {
    Argument.That.NotNull(list);

    var isEmpty = list.Count == 0;
    result = isEmpty ? default : TakeCore(list, ExistingBoundIndex(list.Count, bound));
    return !isEmpty;
  }

  public static bool TryTakeFirst<T>(this IList<T> list, out T? result)
    => list.TryTakeBound(Bound.Lower, out result);

  public static bool TryTakeLast<T>(this IList<T> list, out T? result)
    => list.TryTakeBound(Bound.Upper, out result);

  private static IReadOnlyList<T> TakeRangeCore<T>(IList<T> list, int index, int count)
  {
    var resultList = GetRangeCore(list, index, count);
    RemoveRangeCore(list, index, count);
    return resultList;
  }

  public static IReadOnlyList<T> TakeRange<T>(this IList<T> list, int index, int count)
  {
    Argument.That.InRangeOut(list, index, count);

    switch (list)
    {
      case List<T> fwList:
        {
          var resultList = fwList.GetRange(index, count);
          fwList.RemoveRange(index, count);
          return resultList;
        }
      default:
        return TakeRangeCore(list, index, count);
    }
  }

  public static IReadOnlyList<T> TakeRange<T>(this IList<T> list, (int index, int count) range)
  {
    Argument.That.InRangeOut(list, range);

    switch (list)
    {
      case List<T> fwList:
        {
          var resultList = fwList.GetRange(range.index, range.count);
          fwList.RemoveRange(range.index, range.count);
          return resultList;
        }
      default:
        return TakeRangeCore(list, range.index, range.count);
    }
  }

  #endregion
  #region Replace

  private static T ReplaceCore<T>(IList<T> list, int index, T item)
  {
    var result = list[index];
    list[index] = item;
    return result;
  }

  public static T ReplaceAt<T>(this IList<T> list, int index, T item)
  {
    Argument.That.InRangeIn(list, index);

    return ReplaceCore(list, index, item);
  }

  public static T ReplaceBound<T>(this IList<T> list, Bound bound, T item)
  {
    Argument.That.NotEmpty(list);

    return ReplaceCore(list, ExistingBoundIndex(list.Count, bound), item);
  }

  public static T ReplaceFirst<T>(this IList<T> list, T item)
    => list.ReplaceBound(Bound.Lower, item);

  public static T ReplaceLast<T>(this IList<T> list, T item)
    => list.ReplaceBound(Bound.Upper, item);

  public static bool TryReplaceBound<T>(this IList<T?> list, Bound bound, T? newItem, out T? oldItem)
  {
    Argument.That.NotEmpty(list);

    if (list.Count == 0)
    {
      oldItem = default;
      return false;
    }
    else
    {
      oldItem = ReplaceCore(list, ExistingBoundIndex(list.Count, bound), newItem);
      return true;
    }
  }

  public static bool TryReplaceFirst<T>(this IList<T?> list, T? newItem, out T? oldItem)
    => list.TryReplaceBound(Bound.Lower, newItem, out oldItem);

  public static bool TryReplaceLast<T>(this IList<T?> list, T? newItem, out T? oldItem)
    => list.TryReplaceBound(Bound.Upper, newItem, out oldItem);

  private static void ExchangeCore<T>(IList<T> list, int index, ref T item)
  {
    (item, list[index]) = (list[index], item);
  }

  public static void ExchangeAt<T>(this IList<T> list, int index, ref T item)
  {
    Argument.That.InRangeIn(list, index);

    ExchangeCore(list, index, ref item);
  }

  public static void ExchangeBound<T>(this IList<T> list, Bound bound, ref T item)
  {
    Argument.That.NotEmpty(list);

    item = ReplaceCore(list, ExistingBoundIndex(list.Count, bound), item);
  }

  public static void ExchangeFirst<T>(this IList<T> list, ref T item)
    => list.ExchangeBound(Bound.Lower, ref item);

  public static void ExchangeLast<T>(this IList<T> list, ref T item)
    => list.ExchangeBound(Bound.Upper, ref item);

  public static bool TryExchangeBound<T>(this IList<T> list, Bound bound, ref T item)
  {
    Argument.That.NotNull(list);

    if (list.Count == 0)
      return false;
    item = ReplaceCore(list, ExistingBoundIndex(list.Count, bound), item);
    return true;
  }

  public static bool TryExchangeFirst<T>(this IList<T> list, ref T item)
    => list.TryExchangeBound(Bound.Lower, ref item);

  public static bool TryExchangeLast<T>(this IList<T> list, ref T item)
    => list.TryExchangeBound(Bound.Upper, ref item);

  private static IReadOnlyList<T> ReplaceRangeCore<T>(IList<T> list, int index, int count, IEnumerable<T> items)
  {
    var resultList = CollectionsController.CreateList<T>(count);
    if (items is IList<T> itemsList)
    {
      var itemsIndex = 0;
      for (; count > 0 && itemsIndex < itemsList.Count; index++, count--)
      {
        resultList.Add(list[index]);
        list[index] = itemsList[itemsIndex++];
      }
      if (count > 0)
        for (; count > 0; count--)
          resultList.Add(list.TakeAt(index));
      else
        while (itemsIndex < itemsList.Count)
          list.Insert(index++, itemsList[itemsIndex++]);
    }
    else
    {
      using var enumerator = items.GetEnumerator();
      for (; count > 0 && enumerator.MoveNext(); index++, count--)
      {
        resultList.Add(list[index]);
        list[index] = enumerator.Current;
      }
      if (count > 0)
        for (; count > 0; count--)
          resultList.Add(list.TakeAt(index));
      else
        while (enumerator.MoveNext())
          list.Insert(index++, enumerator.Current);
    }
    return resultList.AsReadOnly();
  }

  public static IReadOnlyList<T> ReplaceRange<T>(this IList<T> list, int index, IEnumerable<T> items)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(items);

    return ReplaceRangeCore(list, index, list.Count - index, items);
  }

  public static IReadOnlyList<T> ReplaceRange<T>(this IList<T> list, int index, int count, IEnumerable<T> items)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(items);

    return ReplaceRangeCore(list, index, count, items);
  }

  public static IReadOnlyList<T> ReplaceRange<T>(this IList<T> list, (int index, int count) range, IEnumerable<T> items)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(items);

    return ReplaceRangeCore(list, range.index, range.count, items);
  }

  #endregion
  #region Move

  private static void MoveCore<T>(IList<T> list, int sIndex, int dIndex)
  {
    if (sIndex == dIndex)
      return;
    var value = list[sIndex];
    if (sIndex < dIndex)
      for (int index = sIndex; index < dIndex; index++)
        list[index] = list[index + 1];
    else if (sIndex > dIndex)
      for (int index = sIndex - 1; index >= dIndex; index--)
        list[index + 1] = list[index];
    list[dIndex] = value;
  }

  public static void Move<T>(this IList<T> list, int sIndex, int dIndex)
  {
    Argument.That.InRangeIn(list, sIndex);
    Argument.That.InRangeIn(list, dIndex);

    MoveCore(list, sIndex, dIndex);
  }

  private static void MoveRangeCore<T>(IList<T> list, int sIndex, int dIndex, int count)
  {
    if (sIndex == dIndex)
      return;
    var values = ArrayBuffer.Acquire<T>(count);
    try
    {
      CopyCore(list, sIndex, values, 0, count, false);
      if (sIndex < dIndex)
        for (int index = sIndex; index < dIndex; index++)
          list[index] = list[index + count];
      else if (sIndex > dIndex)
        for (int index = sIndex - 1; index >= dIndex; index--)
          list[index + count] = list[index];
      for (int index = 0; index < count; index++)
        list[dIndex + index] = values[index];
    }
    finally
    {
      ArrayBuffer.Release(values);
    }
  }

  public static void MoveRange<T>(this IList<T> list, int sIndex, int dIndex, int count)
  {
    Argument.That.InRangeOut(list, sIndex, count);
    Argument.That.InRangeOut(list, dIndex, count);

    MoveRangeCore(list, sIndex, dIndex, count);
  }

  #endregion
  #region Swap

  private static void SwapCore<T>(IList<T> list, int xIndex, int yIndex)
  {
    if (xIndex != yIndex)
      (list[yIndex], list[xIndex]) = (list[xIndex], list[yIndex]);
  }

  public static void Swap<T>(this IList<T> list, int xIndex, int yIndex)
  {
    Argument.That.InRangeIn(list, xIndex);
    Argument.That.InRangeIn(list, yIndex);

    SwapCore(list, xIndex, yIndex);
  }

  private static void SwapRangeCore<T>(IList<T> list, int xIndex, int yIndex, int count)
  {
    for (int index = 0; index < count; index++)
      (list[yIndex + index], list[xIndex + index]) = (list[xIndex + index], list[yIndex + index]);
  }

  public static void SwapRange<T>(this IList<T> list, int xIndex, int yIndex, int count)
  {
    Argument.That.InRangeOut(list, xIndex, count);
    Argument.That.InRangeOut(list, yIndex, count);
    Argument.That.AreConsistent(count < Math.Abs(yIndex - xIndex), null, nameof(xIndex), nameof(yIndex), nameof(count));

    SwapRangeCore(list, xIndex, yIndex, count);
  }

  private static void SwapRangesCore<T>(IList<T> list, int xIndex, int xCount, int yIndex, int yCount)
  {
    for (int index = 0; index < Comparable.Min(xCount, yCount); index++)
      (list[yIndex + index], list[xIndex + index]) = (list[xIndex + index], list[yIndex + index]);
    if (xCount == yCount)
      return;
    int lowerIndex, lowerCount, upperIndex, upperCount;
    if (xIndex < yIndex)
    {
      lowerIndex = xIndex;
      lowerCount = xCount;
      upperIndex = yIndex;
      upperCount = yCount;
    }
    else
    {
      lowerIndex = yIndex;
      lowerCount = yCount;
      upperIndex = xIndex;
      upperCount = xCount;
    }
    if (lowerCount > upperCount)
    {
      var buffer = ArrayBuffer.Acquire<T>(lowerCount - upperCount);
      try
      {
        for (int index = 0, count = lowerCount - upperCount, sIndex = lowerIndex + upperCount; index < count; index++)
          buffer[index] = list[sIndex + index];
        for (int index = 0, count = upperIndex + upperCount - (lowerIndex + lowerCount), sIndex = lowerIndex + lowerCount, dIndex = lowerIndex + upperCount; index < count; index++)
          list[dIndex + index] = list[sIndex + index];
        for (int index = 0, count = lowerCount - upperCount, dIndex = upperIndex + upperCount - (lowerCount - upperCount); index < count; index++)
          list[dIndex + index] = buffer[index];
      }
      finally
      {
        ArrayBuffer.Release(buffer);
      }
    }
    else if (lowerCount < upperCount)
    {
      var buffer = ArrayBuffer.Acquire<T>(upperCount - lowerCount);
      try
      {
        for (int index = 0, count = upperCount - lowerCount, sIndex = upperIndex + lowerCount; index < count; index++)
          buffer[index] = list[sIndex + index];
        for (int index = upperIndex - lowerIndex - 1, sIndex = lowerIndex + lowerCount, dIndex = lowerIndex + upperCount; index >= 0; index--)
          list[dIndex + index] = list[sIndex + index];
        for (int index = 0, count = upperCount - lowerCount, dIndex = lowerIndex + lowerCount; index < count; index++)
          list[dIndex + index] = buffer[index];
      }
      finally
      {
        ArrayBuffer.Release(buffer);
      }
    }
  }

  public static void SwapRanges<T>(this IList<T> list, int xIndex, int xCount, int yIndex, int yCount)
  {
    Argument.That.InRangeOut(list, xIndex, xCount);
    Argument.That.InRangeOut(list, yIndex, yCount);
    Argument.That.AreConsistent(!(xIndex == yIndex || xIndex < yIndex && xIndex + xCount > yIndex || xIndex > yIndex && yIndex + yCount > xIndex), null,
      nameof(xIndex), nameof(xCount), nameof(yIndex), nameof(yCount));

    SwapRangesCore(list, xIndex, xCount, yIndex, yCount);
  }

  #endregion
  #region Reverse

  private static void ReverseCore<T>(IList<T> list, int index, int count)
  {
    for (int i = 0; i < count / 2; i++)
      (list[index + count - i], list[index + i]) = (list[index + i], list[index + count - i]);
  }

  public static void Reverse<T>(this IList<T> list)
  {
    Argument.That.NotNull(list);

    ReverseCore(list, 0, list.Count);
  }

  public static void Reverse<T>(this IList<T> list, int index)
  {
    Argument.That.InRangeOut(list, index);

    ReverseCore(list, index, list.Count - index);
  }

  public static void Reverse<T>(this IList<T> list, int index, int count)
  {
    Argument.That.InRangeOut(list, index, count);

    ReverseCore(list, index, count);
  }

  public static void Reverse<T>(this IList<T> list, (int index, int count) range)
  {
    Argument.That.InRangeOut(list, range);

    ReverseCore(list, range.index, range.count);
  }

  #endregion
  #region Sorted items manipulation methods

  private static int AddSortedCore<T>(IList<T> list, T item, Comparison<T> comparison, SortingOption option)
  {
    int index = list.BinarySearch(t => comparison(t, item), option == SortingOption.First ? SearchingOption.First : option == SortingOption.Last ? SearchingOption.Last : SearchingOption.None);
    if (index < 0)
      index = ~index;
    else if (option == SortingOption.Single)
      return ~index;
    else if (option == SortingOption.Last)
      index++;
    list.Insert(index, item);
    return index;
  }

  public static int AddSorted<T>(this IList<T> list, T item, Comparison<T> comparison, SortingOption option = SortingOption.None)
  {
    Argument.That.InLimitsOut(list, 1);
    Argument.That.NotNull(comparison);

    return AddSortedCore(list, item, comparison, option);
  }

  private static bool InsertSortedCore<T>(IList<T> list, int index, T item, Comparison<T> comparison, SortingOption option)
  {
    if (index > 0 && (comparison(list[index - 1], item) > 0 || option == SortingOption.Single && comparison(list[index - 1], item) == 0) ||
      index < list.Count && (comparison(list[index], item) < 0 || option == SortingOption.Single && comparison(list[index], item) == 0))
      return false;
    list.Insert(index, item);
    return true;
  }

  public static bool InsertSorted<T>(this IList<T> list, int index, T item, Comparison<T> comparison, SortingOption option = SortingOption.None)
  {
    Argument.That.InLimitsIn(list, index, 1);
    Argument.That.NotNull(comparison);

    return InsertSortedCore(list, index, item, comparison, option);
  }

  private static bool SetSortedCore<T>(IList<T> list, int index, T item, Comparison<T> comparison, SortingOption option)
  {
    if (index > 0 && (comparison(list[index - 1], item) > 0 || option == SortingOption.Single && comparison(list[index - 1], item) == 0) ||
      index < list.Count - 1 && (comparison(list[index + 1], item) < 0 || option == SortingOption.Single && comparison(list[index + 1], item) == 0))
      return false;
    list[index] = item;
    return true;
  }

  public static bool SetSorted<T>(this IList<T> list, int index, T item, Comparison<T> comparison, SortingOption option = SortingOption.None)
  {
    Argument.That.InRangeIn(list, index);
    Argument.That.NotNull(comparison);

    return SetSortedCore(list, index, item, comparison, option);
  }

  #endregion
  #endregion
  #region Transformation methods
  #region Fill

  private static void FillCore<TSource>(IList<TSource> list, int index, int count, Func<int, TSource> filler)
  {
    for (; count > 0; index++, count--)
      list[index] = filler(index);
  }

  public static void Fill<TSource>(this IList<TSource> list, TSource value)
  {
    Argument.That.NotNull(list);

    FillCore(list, 0, list.Count, i => value);
  }

  public static void Fill<TSource>(this IList<TSource> list, int index, TSource value)
  {
    Argument.That.InRangeOut(list, index);

    FillCore(list, index, list.Count - index, i => value);
  }

  public static void Fill<TSource>(this IList<TSource> list, int index, int count, TSource value)
  {
    Argument.That.InRangeOut(list, index, count);

    FillCore(list, index, count, i => value);
  }

  public static void Fill<TSource>(this IList<TSource> list, (int index, int count) range, TSource value)
  {
    Argument.That.InRangeOut(list, range);

    FillCore(list, range.index, range.count, i => value);
  }

  public static void Fill<TSource>(this IList<TSource> list, Func<TSource> filler)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(filler);

    FillCore(list, 0, list.Count, ElementFiller(filler));
  }

  public static void Fill<TSource>(this IList<TSource> list, int index, Func<TSource> filler)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(filler);

    FillCore(list, index, list.Count - index, ElementFiller(filler));
  }

  public static void Fill<TSource>(this IList<TSource> list, int index, int count, Func<TSource> filler)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(filler);

    FillCore(list, index, count, ElementFiller(filler));
  }

  public static void Fill<TSource>(this IList<TSource> list, (int index, int count) range, Func<TSource> filler)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(filler);

    FillCore(list, range.index, range.count, ElementFiller(filler));
  }

  public static void Fill<TSource>(this IList<TSource> list, Func<int, TSource> filler)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(filler);

    FillCore(list, 0, list.Count, filler);
  }

  public static void Fill<TSource>(this IList<TSource> list, int index, Func<int, TSource> filler)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(filler);

    FillCore(list, index, list.Count - index, filler);
  }

  public static void Fill<TSource>(this IList<TSource> list, int index, int count, Func<int, TSource> filler)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(filler);

    FillCore(list, index, count, filler);
  }

  public static void Fill<TSource>(this IList<TSource> list, (int index, int count) range, Func<int, TSource> filler)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(filler);

    FillCore(list, range.index, range.count, filler);
  }

  #endregion
  #region Apply

  private static void ApplyCore<TSource>(IList<TSource> list, int index, int count, ElementAction<TSource> action)
  {
    for (; count > 0; index++, count--)
      action(list[index], index);
  }

  public static void Apply<TSource>(this IList<TSource> list, Action<TSource> action)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(action);

    ApplyCore(list, 0, list.Count, ElementAction(action));
  }

  public static void Apply<TSource>(this IList<TSource> list, int index, Action<TSource> action)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(action);

    ApplyCore(list, index, list.Count - index, ElementAction(action));
  }

  public static void Apply<TSource>(this IList<TSource> list, int index, int count, Action<TSource> action)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(action);

    ApplyCore(list, index, count, ElementAction(action));
  }

  public static void Apply<TSource>(this IList<TSource> list, (int index, int count) range, Action<TSource> action)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(action);

    ApplyCore(list, range.index, range.count, ElementAction(action));
  }

  public static void Apply<TSource>(this IList<TSource> list, ElementAction<TSource> action)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(action);

    ApplyCore(list, 0, list.Count, action);
  }

  public static void Apply<TSource>(this IList<TSource> list, int index, ElementAction<TSource> action)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(action);

    ApplyCore(list, index, list.Count - index, action);
  }

  public static void Apply<TSource>(this IList<TSource> list, int index, int count, ElementAction<TSource> action)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(action);

    ApplyCore(list, index, count, action);
  }

  public static void Apply<TSource>(this IList<TSource> list, (int index, int count) range, ElementAction<TSource> action)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(action);

    ApplyCore(list, range.index, range.count, action);
  }

  #endregion
  #region BubbleSort

  private static void BubbleSortCore<TSource>(IList<TSource> list, int index, int count, Comparison<TSource> comparison)
  {
    for (bool swapped = true; count > 1 && swapped; count--)
    {
      swapped = false;
      for (int j = index + 1, c = index + count; j < c; j++)
      {
        if (comparison(list[j - 1], list[j]) > 0)
        {
          (list[j - 1], list[j]) = (list[j], list[j - 1]);
          swapped = true;
        }
      }
    }
  }

  public static void BubbleSort<TSource>(this IList<TSource> list)
  {
    Argument.That.NotNull(list);

    BubbleSortCore(list, 0, list.Count, Comparer<TSource>.Default.Compare);
  }

  public static void BubbleSort<TSource>(this IList<TSource> list, int index)
  {
    Argument.That.InRangeOut(list, index);

    BubbleSortCore(list, index, list.Count - index, Comparer<TSource>.Default.Compare);
  }

  public static void BubbleSort<TSource>(this IList<TSource> list, int index, int count)
  {
    Argument.That.InRangeOut(list, index, count);

    BubbleSortCore(list, index, count, Comparer<TSource>.Default.Compare);
  }

  public static void BubbleSort<TSource>(this IList<TSource> list, (int index, int count) range)
  {
    Argument.That.InRangeOut(list, range);

    BubbleSortCore(list, range.index, range.count, Comparer<TSource>.Default.Compare);
  }

  public static void BubbleSort<TSource>(this IList<TSource> list, Comparison<TSource>? comparison)
  {
    Argument.That.NotNull(list);

    BubbleSortCore(list, 0, list.Count, comparison ?? Comparer<TSource>.Default.Compare);
  }

  public static void BubbleSort<T>(this IList<T> list, int index, Comparison<T>? comparison)
  {
    Argument.That.InRangeOut(list, index);

    BubbleSortCore(list, index, list.Count - index, comparison ?? Comparer<T>.Default.Compare);
  }

  public static void BubbleSort<T>(this IList<T> list, int index, int count, Comparison<T>? comparison)
  {
    Argument.That.InRangeOut(list, index, count);

    BubbleSortCore(list, index, count, comparison ?? Comparer<T>.Default.Compare);
  }

  public static void BubbleSort<TSource>(this IList<TSource> list, (int index, int count) range, Comparison<TSource>? comparison)
  {
    Argument.That.InRangeOut(list, range);

    BubbleSortCore(list, range.index, range.count, comparison ?? Comparer<TSource>.Default.Compare);
  }

  public static void BubbleSort<TSource>(this IList<TSource> list, IComparer<TSource>? comparer)
  {
    Argument.That.NotNull(list);

    BubbleSortCore(list, 0, list.Count, (comparer ?? Comparer<TSource>.Default).Compare);
  }

  public static void BubbleSort<TSource>(this IList<TSource> list, int index, IComparer<TSource>? comparer)
  {
    Argument.That.InRangeOut(list, index);

    BubbleSortCore(list, index, list.Count - index, (comparer ?? Comparer<TSource>.Default).Compare);
  }

  public static void BubbleSort<TSource>(this IList<TSource> list, int index, int count, IComparer<TSource>? comparer)
  {
    Argument.That.InRangeOut(list, index, count);

    BubbleSortCore(list, index, count, (comparer ?? Comparer<TSource>.Default).Compare);
  }

  public static void BubbleSort<TSource>(this IList<TSource> list, (int index, int count) range, IComparer<TSource>? comparer)
  {
    Argument.That.InRangeOut(list, range);

    BubbleSortCore(list, range.index, range.count, (comparer ?? Comparer<TSource>.Default).Compare);
  }

  private static void BubbleSortCore<TKey, TSource>(IList<TSource> list, IList<TKey> keys, int index, int count, Comparison<TKey> comparison)
  {
    for (bool swapped = true; count > 1 && swapped; count--)
    {
      swapped = false;
      for (int j = index + 1, c = index + count; j < c; j++)
      {
        if (comparison(keys[j - 1], keys[j]) > 0)
        {
          (keys[j - 1], keys[j]) = (keys[j], keys[j - 1]);
          (list[j - 1], list[j]) = (list[j], list[j - 1]);
          swapped = true;
        }
      }
    }
  }

  public static void BubbleSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys)
  {
    Argument.That.MatchCoupledCount(list, keys, null);

    BubbleSortCore(list, keys, 0, list.Count, Comparer<TKey>.Default.Compare);
  }

  public static void BubbleSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index)
  {
    Argument.That.MatchCoupledCount(list, keys, null);
    Argument.That.InRangeOut(list, index);

    BubbleSortCore(list, keys, index, list.Count - index, Comparer<TKey>.Default.Compare);
  }

  public static void BubbleSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, int count)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.InRangeOut(keys, index, count);

    BubbleSortCore(list, keys, index, count, Comparer<TKey>.Default.Compare);
  }

  public static void BubbleSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, (int index, int count) range)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.InRangeOut(keys, range);

    BubbleSortCore(list, keys, range.index, range.count, Comparer<TKey>.Default.Compare);
  }

  public static void BubbleSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, Comparison<TKey>? comparison)
  {
    Argument.That.MatchCoupledCount(list, keys, null);

    BubbleSortCore(list, keys, 0, list.Count, comparison ?? Comparer<TKey>.Default.Compare);
  }

  public static void BubbleSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, Comparison<TKey>? comparison)
  {
    Argument.That.MatchCoupledCount(list, keys, null);
    Argument.That.InRangeOut(list, index);

    BubbleSortCore(list, keys, index, list.Count - index, comparison ?? Comparer<TKey>.Default.Compare);
  }

  public static void BubbleSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, int count, Comparison<TKey>? comparison)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.InRangeOut(keys, index, count);

    BubbleSortCore(list, keys, index, count, comparison ?? Comparer<TKey>.Default.Compare);
  }

  public static void BubbleSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, (int index, int count) range, Comparison<TKey>? comparison)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.InRangeOut(keys, range);

    BubbleSortCore(list, keys, range.index, range.count, comparison ?? Comparer<TKey>.Default.Compare);
  }

  public static void BubbleSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, IComparer<TKey>? comparer)
  {
    Argument.That.MatchCoupledCount(list, keys, null);

    BubbleSortCore(list, keys, 0, list.Count, (comparer ?? Comparer<TKey>.Default).Compare);
  }

  public static void BubbleSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, IComparer<TKey>? comparer)
  {
    Argument.That.MatchCoupledCount(list, keys, null);
    Argument.That.InRangeOut(list, index);

    BubbleSortCore(list, keys, index, list.Count - index, (comparer ?? Comparer<TKey>.Default).Compare);
  }

  public static void BubbleSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, int count, IComparer<TKey>? comparer)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.InRangeOut(keys, index, count);

    BubbleSortCore(list, keys, index, count, (comparer ?? Comparer<TKey>.Default).Compare);
  }

  public static void BubbleSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, (int index, int count) range, IComparer<TKey>? comparer)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.InRangeOut(keys, range);

    BubbleSortCore(list, keys, range.index, range.count, (comparer ?? Comparer<TKey>.Default).Compare);
  }

  #endregion
  #region SelectionSort

  private static void SelectionSortCore<TSource>(IList<TSource> list, int index, int count, Comparison<TSource> comparison)
  {
    for (int i = index, m = i, c = index + count; i < c - 1; m = ++i)
    {
      for (int j = i + 1; j < c; j++)
      {
        if (comparison(list[j], list[m]) < 0)
        {
          m = j;
        }
      }
      if (m != i)
      {
        (list[i], list[m]) = (list[m], list[i]);
      }
    }
  }

  public static void SelectionSort<TSource>(this IList<TSource> list)
  {
    Argument.That.NotNull(list);

    SelectionSortCore(list, 0, list.Count, Comparer<TSource>.Default.Compare);
  }

  public static void SelectionSort<TSource>(this IList<TSource> list, int index)
  {
    Argument.That.InRangeOut(list, index);

    SelectionSortCore(list, index, list.Count - index, Comparer<TSource>.Default.Compare);
  }

  public static void SelectionSort<TSource>(this IList<TSource> list, int index, int count)
  {
    Argument.That.InRangeOut(list, index, count);

    SelectionSortCore(list, index, count, Comparer<TSource>.Default.Compare);
  }

  public static void SelectionSort<TSource>(this IList<TSource> list, (int index, int count) range)
  {
    Argument.That.InRangeOut(list, range);

    SelectionSortCore(list, range.index, range.count, Comparer<TSource>.Default.Compare);
  }

  public static void SelectionSort<TSource>(this IList<TSource> list, Comparison<TSource>? comparison)
  {
    Argument.That.NotNull(list);

    SelectionSortCore(list, 0, list.Count, comparison ?? Comparer<TSource>.Default.Compare);
  }

  public static void SelectionSort<TSource>(this IList<TSource> list, int index, Comparison<TSource>? comparison)
  {
    Argument.That.InRangeOut(list, index);

    SelectionSortCore(list, index, list.Count - index, comparison ?? Comparer<TSource>.Default.Compare);
  }

  public static void SelectionSort<TSource>(this IList<TSource> list, int index, int count, Comparison<TSource>? comparison)
  {
    Argument.That.InRangeOut(list, index, count);

    SelectionSortCore(list, index, count, comparison ?? Comparer<TSource>.Default.Compare);
  }

  public static void SelectionSort<TSource>(this IList<TSource> list, (int index, int count) range, Comparison<TSource>? comparison)
  {
    Argument.That.InRangeOut(list, range);

    SelectionSortCore(list, range.index, range.count, comparison ?? Comparer<TSource>.Default.Compare);
  }

  public static void SelectionSort<TSource>(this IList<TSource> list, IComparer<TSource>? comparer)
  {
    Argument.That.NotNull(list);

    SelectionSortCore(list, 0, list.Count, (comparer ?? Comparer<TSource>.Default).Compare);
  }

  public static void SelectionSort<TSource>(this IList<TSource> list, int index, IComparer<TSource>? comparer)
  {
    Argument.That.InRangeOut(list, index);

    SelectionSortCore(list, index, list.Count - index, (comparer ?? Comparer<TSource>.Default).Compare);
  }

  public static void SelectionSort<TSource>(this IList<TSource> list, int index, int count, IComparer<TSource>? comparer)
  {
    Argument.That.InRangeOut(list, index, count);

    SelectionSortCore(list, index, count, (comparer ?? Comparer<TSource>.Default).Compare);
  }

  public static void SelectionSort<TSource>(this IList<TSource> list, (int index, int count) range, IComparer<TSource>? comparer)
  {
    Argument.That.InRangeOut(list, range);

    SelectionSortCore(list, range.index, range.count, (comparer ?? Comparer<TSource>.Default).Compare);
  }

  private static void SelectionSortCore<TKey, TSource>(IList<TSource> list, IList<TKey> keys, int index, int count, Comparison<TKey> comparison)
  {
    for (int i = index, m = i, c = index + count; i < c - 1; m = ++i)
    {
      for (int j = i + 1; j < c; j++)
      {
        if (comparison(keys[j], keys[m]) < 0)
        {
          m = j;
        }
      }
      if (m != i)
      {
        (keys[i], keys[m]) = (keys[m], keys[i]);
        (list[i], list[m]) = (list[m], list[i]);
      }
    }
  }

  public static void SelectionSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys)
  {
    Argument.That.MatchCoupledCount(list, keys, null);

    SelectionSortCore(list, keys, 0, list.Count, Comparer<TKey>.Default.Compare);
  }

  public static void SelectionSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index)
  {
    Argument.That.MatchCoupledCount(list, keys, null);
    Argument.That.InRangeOut(list, index);

    SelectionSortCore(list, keys, index, list.Count - index, Comparer<TKey>.Default.Compare);
  }

  public static void SelectionSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, int count)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.InRangeOut(keys, index, count);

    SelectionSortCore(list, keys, index, count, Comparer<TKey>.Default.Compare);
  }

  public static void SelectionSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, (int index, int count) range)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.InRangeOut(keys, range);

    SelectionSortCore(list, keys, range.index, range.count, Comparer<TKey>.Default.Compare);
  }

  public static void SelectionSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, Comparison<TKey>? comparison)
  {
    Argument.That.MatchCoupledCount(list, keys, null);

    SelectionSortCore(list, keys, 0, list.Count, comparison ?? Comparer<TKey>.Default.Compare);
  }

  public static void SelectionSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, Comparison<TKey>? comparison)
  {
    Argument.That.MatchCoupledCount(list, keys, null);
    Argument.That.InRangeOut(list, index);

    SelectionSortCore(list, keys, index, list.Count - index, comparison ?? Comparer<TKey>.Default.Compare);
  }

  public static void SelectionSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, int count, Comparison<TKey>? comparison)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.InRangeOut(keys, index, count);

    SelectionSortCore(list, keys, index, count, comparison ?? Comparer<TKey>.Default.Compare);
  }

  public static void SelectionSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, (int index, int count) range, Comparison<TKey>? comparison)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.InRangeOut(keys, range);

    SelectionSortCore(list, keys, range.index, range.count, comparison ?? Comparer<TKey>.Default.Compare);
  }

  public static void SelectionSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, IComparer<TKey>? comparer)
  {
    Argument.That.MatchCoupledCount(list, keys, null);

    SelectionSortCore(list, keys, 0, list.Count, (comparer ?? Comparer<TKey>.Default).Compare);
  }

  public static void SelectionSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, IComparer<TKey>? comparer)
  {
    Argument.That.MatchCoupledCount(list, keys, null);
    Argument.That.InRangeOut(list, index);

    SelectionSortCore(list, keys, index, list.Count - index, (comparer ?? Comparer<TKey>.Default).Compare);
  }

  public static void SelectionSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, int count, IComparer<TKey>? comparer)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.InRangeOut(keys, index, count);

    SelectionSortCore(list, keys, index, count, (comparer ?? Comparer<TKey>.Default).Compare);
  }

  public static void SelectionSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, (int index, int count) range, IComparer<TKey>? comparer)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.InRangeOut(keys, range);

    SelectionSortCore(list, keys, range.index, range.count, (comparer ?? Comparer<TKey>.Default).Compare);
  }

  #endregion
  #region InsertionSort

  private static void InsertionSortCore<TSource>(IList<TSource> list, int index, int count, Comparison<TSource> comparison)
  {
    for (int i = index + 1, c = index + count; i < c; i++)
    {
      var item = list[i];
      var j = i;
      for (; j > index && comparison(list[j - 1], item) > 0; j--)
      {
        list[j] = list[j - 1];
      }
      if (j != i)
      {
        list[j] = item;
      }
    }
  }

  public static void InsertionSort<TSource>(this IList<TSource> list)
  {
    Argument.That.NotNull(list);

    InsertionSortCore(list, 0, list.Count, Comparer<TSource>.Default.Compare);
  }

  public static void InsertionSort<TSource>(this IList<TSource> list, int index)
  {
    Argument.That.InRangeOut(list, index);

    InsertionSortCore(list, index, list.Count - index, Comparer<TSource>.Default.Compare);
  }

  public static void InsertionSort<TSource>(this IList<TSource> list, int index, int count)
  {
    Argument.That.InRangeOut(list, index, count);

    InsertionSortCore(list, index, count, Comparer<TSource>.Default.Compare);
  }

  public static void InsertionSort<TSource>(this IList<TSource> list, (int index, int count) range)
  {
    Argument.That.InRangeOut(list, range);

    InsertionSortCore(list, range.index, range.count, Comparer<TSource>.Default.Compare);
  }

  public static void InsertionSort<TSource>(this IList<TSource> list, Comparison<TSource>? comparison)
  {
    Argument.That.NotNull(list);

    InsertionSortCore(list, 0, list.Count, comparison ?? Comparer<TSource>.Default.Compare);
  }

  public static void InsertionSort<TSource>(this IList<TSource> list, int index, Comparison<TSource>? comparison)
  {
    Argument.That.InRangeOut(list, index);

    InsertionSortCore(list, index, list.Count - index, comparison ?? Comparer<TSource>.Default.Compare);
  }

  public static void InsertionSort<TSource>(this IList<TSource> list, int index, int count, Comparison<TSource>? comparison)
  {
    Argument.That.InRangeOut(list, index, count);

    InsertionSortCore(list, index, count, comparison ?? Comparer<TSource>.Default.Compare);
  }

  public static void InsertionSort<TSource>(this IList<TSource> list, (int index, int count) range, Comparison<TSource>? comparison)
  {
    Argument.That.InRangeOut(list, range);

    InsertionSortCore(list, range.index, range.count, comparison ?? Comparer<TSource>.Default.Compare);
  }

  public static void InsertionSort<TSource>(this IList<TSource> list, IComparer<TSource>? comparer)
  {
    Argument.That.NotNull(list);

    InsertionSortCore(list, 0, list.Count, (comparer ?? Comparer<TSource>.Default).Compare);
  }

  public static void InsertionSort<TSource>(this IList<TSource> list, int index, IComparer<TSource>? comparer)
  {
    Argument.That.InRangeOut(list, index);

    InsertionSortCore(list, index, list.Count - index, (comparer ?? Comparer<TSource>.Default).Compare);
  }

  public static void InsertionSort<TSource>(this IList<TSource> list, int index, int count, IComparer<TSource>? comparer)
  {
    Argument.That.InRangeOut(list, index, count);

    InsertionSortCore(list, index, count, (comparer ?? Comparer<TSource>.Default).Compare);
  }

  public static void InsertionSort<TSource>(this IList<TSource> list, (int index, int count) range, IComparer<TSource>? comparer)
  {
    Argument.That.InRangeOut(list, range);

    InsertionSortCore(list, range.index, range.count, (comparer ?? Comparer<TSource>.Default).Compare);
  }

  private static void InsertionSortCore<TKey, TSource>(IList<TSource> list, IList<TKey> keys, int index, int count, Comparison<TKey> comparison)
  {
    for (int i = index + 1, c = index + count; i < c; i++)
    {
      var key = keys[i];
      var item = list[i];
      var j = i;
      for (; j > index && comparison(keys[j - 1], key) > 0; j--)
      {
        keys[j] = keys[j - 1];
        list[j] = list[j - 1];
      }
      if (j != i)
      {
        keys[j] = key;
        list[j] = item;
      }
    }
  }

  public static void InsertionSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys)
  {
    Argument.That.MatchCoupledCount(list, keys, null);

    InsertionSortCore(list, keys, 0, list.Count, Comparer<TKey>.Default.Compare);
  }

  public static void InsertionSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index)
  {
    Argument.That.MatchCoupledCount(list, keys, null);
    Argument.That.InRangeOut(list, index);

    InsertionSortCore(list, keys, index, list.Count - index, Comparer<TKey>.Default.Compare);
  }

  public static void InsertionSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, int count)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.InRangeOut(keys, index, count);

    InsertionSortCore(list, keys, index, count, Comparer<TKey>.Default.Compare);
  }

  public static void InsertionSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, (int index, int count) range)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.InRangeOut(keys, range);

    InsertionSortCore(list, keys, range.index, range.count, Comparer<TKey>.Default.Compare);
  }

  public static void InsertionSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, Comparison<TKey>? comparison)
  {
    Argument.That.MatchCoupledCount(list, keys, null);

    InsertionSortCore(list, keys, 0, list.Count, comparison ?? Comparer<TKey>.Default.Compare);
  }

  public static void InsertionSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, Comparison<TKey>? comparison)
  {
    Argument.That.MatchCoupledCount(list, keys, null);
    Argument.That.InRangeOut(list, index);

    InsertionSortCore(list, keys, index, list.Count - index, comparison ?? Comparer<TKey>.Default.Compare);
  }

  public static void InsertionSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, int count, Comparison<TKey>? comparison)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.InRangeOut(keys, index, count);

    InsertionSortCore(list, keys, index, count, comparison ?? Comparer<TKey>.Default.Compare);
  }

  public static void InsertionSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, (int index, int count) range, Comparison<TKey>? comparison)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.InRangeOut(keys, range);

    InsertionSortCore(list, keys, range.index, range.count, comparison ?? Comparer<TKey>.Default.Compare);
  }

  public static void InsertionSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, IComparer<TKey>? comparer)
  {
    Argument.That.MatchCoupledCount(list, keys, null);

    InsertionSortCore(list, keys, 0, list.Count, (comparer ?? Comparer<TKey>.Default).Compare);
  }

  public static void InsertionSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, IComparer<TKey>? comparer)
  {
    Argument.That.MatchCoupledCount(list, keys, null);
    Argument.That.InRangeOut(list, index);

    InsertionSortCore(list, keys, index, list.Count - index, (comparer ?? Comparer<TKey>.Default).Compare);
  }

  public static void InsertionSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, int count, IComparer<TKey>? comparer)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.InRangeOut(keys, index, count);

    InsertionSortCore(list, keys, index, count, (comparer ?? Comparer<TKey>.Default).Compare);
  }

  public static void InsertionSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, (int index, int count) range, IComparer<TKey>? comparer)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.InRangeOut(keys, range);

    InsertionSortCore(list, keys, range.index, range.count, (comparer ?? Comparer<TKey>.Default).Compare);
  }

  #endregion
  #region MergeSort

  private static void MergeSortCore<TSource>(IList<TSource> list, int index, int count, Comparison<TSource> comparison)
  {
    for (int bs = 1, bc = index + count; bs < count; bs <<= 1)
    {
      for (int bi = index; bi < bc - bs; bi += bs << 1)
      {
        int li = 0, ri = 0;
        int lb = bi, mb = bi + bs, rb = Comparable.Min(bi + (bs << 1), bc);
        var ta = ArrayBuffer.Acquire<TSource>(rb - lb);
        try
        {
          while (lb + li < mb && mb < rb - ri)
          {
            if (comparison(list[lb + li], list[mb + ri]) < 0)
            {
              ta[li + ri] = list[lb + li];
              li++;
            }
            else
            {
              ta[li + ri] = list[mb + ri];
              ri++;
            }
          }
          while (lb + li < mb)
          {
            ta[li + ri] = list[lb + li];
            li++;
          }
          while (mb + ri < rb)
          {
            ta[li + ri] = list[mb + ri];
            ri++;
          }
          for (int mi = 0; mi < li + ri; mi++)
          {
            list[lb + mi] = ta[mi];
          }
        }
        finally
        {
          ArrayBuffer.Release(ta);
        }
      }
    }
  }

  public static void MergeSort<TSource>(this IList<TSource> list)
  {
    Argument.That.NotNull(list);

    MergeSortCore(list, 0, list.Count, Comparer<TSource>.Default.Compare);
  }

  public static void MergeSort<TSource>(this IList<TSource> list, int index)
  {
    Argument.That.InRangeOut(list, index);

    MergeSortCore(list, index, list.Count - index, Comparer<TSource>.Default.Compare);
  }

  public static void MergeSort<TSource>(this IList<TSource> list, int index, int count)
  {
    Argument.That.InRangeOut(list, index, count);

    MergeSortCore(list, index, count, Comparer<TSource>.Default.Compare);
  }

  public static void MergeSort<TSource>(this IList<TSource> list, (int index, int count) range)
  {
    Argument.That.InRangeOut(list, range);

    MergeSortCore(list, range.index, range.count, Comparer<TSource>.Default.Compare);
  }

  public static void MergeSort<TSource>(this IList<TSource> list, Comparison<TSource>? comparison)
  {
    Argument.That.NotNull(list);

    MergeSortCore(list, 0, list.Count, comparison ?? Comparer<TSource>.Default.Compare);
  }

  public static void MergeSort<TSource>(this IList<TSource> list, int index, Comparison<TSource>? comparison)
  {
    Argument.That.InRangeOut(list, index);

    MergeSortCore(list, index, list.Count - index, comparison ?? Comparer<TSource>.Default.Compare);
  }

  public static void MergeSort<TSource>(this IList<TSource> list, int index, int count, Comparison<TSource>? comparison)
  {
    Argument.That.InRangeOut(list, index, count);

    MergeSortCore(list, index, count, comparison ?? Comparer<TSource>.Default.Compare);
  }

  public static void MergeSort<TSource>(this IList<TSource> list, (int index, int count) range, Comparison<TSource>? comparison)
  {
    Argument.That.InRangeOut(list, range);

    MergeSortCore(list, range.index, range.count, comparison ?? Comparer<TSource>.Default.Compare);
  }

  public static void MergeSort<TSource>(this IList<TSource> list, IComparer<TSource>? comparer)
  {
    Argument.That.NotNull(list);

    MergeSortCore(list, 0, list.Count, (comparer ?? Comparer<TSource>.Default).Compare);
  }

  public static void MergeSort<TSource>(this IList<TSource> list, int index, IComparer<TSource>? comparer)
  {
    Argument.That.InRangeOut(list, index);

    MergeSortCore(list, index, list.Count - index, (comparer ?? Comparer<TSource>.Default).Compare);
  }

  public static void MergeSort<TSource>(this IList<TSource> list, int index, int count, IComparer<TSource>? comparer)
  {
    Argument.That.InRangeOut(list, index, count);

    MergeSortCore(list, index, count, (comparer ?? Comparer<TSource>.Default).Compare);
  }

  public static void MergeSort<TSource>(this IList<TSource> list, (int index, int count) range, IComparer<TSource>? comparer)
  {
    Argument.That.InRangeOut(list, range);

    MergeSortCore(list, range.index, range.count, (comparer ?? Comparer<TSource>.Default).Compare);
  }

  private static void MergeSortCore<TKey, TSource>(IList<TSource> list, IList<TKey> keys, int index, int count, Comparison<TKey> comparison)
  {
    for (int bs = 1, bc = index + count; bs < count; bs <<= 1)
    {
      for (int bi = index; bi < bc - bs; bi += bs << 1)
      {
        int li = 0, ri = 0;
        int lb = bi, mb = bi + bs, rb = Comparable.Min(bi + (bs << 1), bc);
        var ka = new TKey[rb - lb];
        var va = new TSource[rb - lb];
        while (lb + li < mb && mb < rb - ri)
        {
          if (comparison(keys[lb + li], keys[mb + ri]) < 0)
          {
            ka[li + ri] = keys[lb + li];
            va[li + ri] = list[lb + li];
            li++;
          }
          else
          {
            ka[li + ri] = keys[mb + ri];
            va[li + ri] = list[mb + ri];
            ri++;
          }
        }
        while (lb + li < mb)
        {
          ka[li + ri] = keys[lb + li];
          va[li + ri] = list[lb + li];
          li++;
        }
        while (mb + ri < rb)
        {
          ka[li + ri] = keys[mb + ri];
          va[li + ri] = list[mb + ri];
          ri++;
        }
        for (int mi = 0; mi < li + ri; mi++)
        {
          keys[lb + mi] = ka[mi];
          list[lb + mi] = va[mi];
        }
      }
    }
  }

  public static void MergeSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys)
  {
    Argument.That.MatchCoupledCount(list, keys, null);

    MergeSortCore(list, keys, 0, list.Count, Comparer<TKey>.Default.Compare);
  }

  public static void MergeSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index)
  {
    Argument.That.MatchCoupledCount(list, keys, null);
    Argument.That.InRangeOut(list, index);

    MergeSortCore(list, keys, index, list.Count - index, Comparer<TKey>.Default.Compare);
  }

  public static void MergeSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, int count)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.InRangeOut(keys, index, count);

    MergeSortCore(list, keys, index, count, Comparer<TKey>.Default.Compare);
  }

  public static void MergeSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, (int index, int count) range)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.InRangeOut(keys, range);

    MergeSortCore(list, keys, range.index, range.count, Comparer<TKey>.Default.Compare);
  }

  public static void MergeSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, Comparison<TKey>? comparison)
  {
    Argument.That.MatchCoupledCount(list, keys, null);

    MergeSortCore(list, keys, 0, list.Count, comparison ?? Comparer<TKey>.Default.Compare);
  }

  public static void MergeSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, Comparison<TKey>? comparison)
  {
    Argument.That.MatchCoupledCount(list, keys, null);
    Argument.That.InRangeOut(list, index);

    MergeSortCore(list, keys, index, list.Count - index, comparison ?? Comparer<TKey>.Default.Compare);
  }

  public static void MergeSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, int count, Comparison<TKey>? comparison)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.InRangeOut(keys, index, count);

    MergeSortCore(list, keys, index, count, comparison ?? Comparer<TKey>.Default.Compare);
  }

  public static void MergeSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, (int index, int count) range, Comparison<TKey>? comparison)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.InRangeOut(keys, range);

    MergeSortCore(list, keys, range.index, range.count, comparison ?? Comparer<TKey>.Default.Compare);
  }

  public static void MergeSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, IComparer<TKey>? comparer)
  {
    Argument.That.MatchCoupledCount(list, keys, null);

    MergeSortCore(list, keys, 0, list.Count, (comparer ?? Comparer<TKey>.Default).Compare);
  }

  public static void MergeSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, IComparer<TKey>? comparer)
  {
    Argument.That.MatchCoupledCount(list, keys, null);
    Argument.That.InRangeOut(list, index);

    MergeSortCore(list, keys, index, list.Count - index, (comparer ?? Comparer<TKey>.Default).Compare);
  }

  public static void MergeSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, int count, IComparer<TKey>? comparer)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.InRangeOut(keys, index, count);

    MergeSortCore(list, keys, index, count, (comparer ?? Comparer<TKey>.Default).Compare);
  }

  public static void MergeSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, (int index, int count) range, IComparer<TKey>? comparer)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.InRangeOut(keys, range);

    MergeSortCore(list, keys, range.index, range.count, (comparer ?? Comparer<TKey>.Default).Compare);
  }

  #endregion
  #region QuickSort

  private static void QuickSortCore<TSource>(IList<TSource> list, int index, int count, Comparison<TSource> comparison)
  {
    var left = index;
    var right = index + count - 1;
    do
    {
      var lower = left;
      var upper = right;
      var middle = lower + (upper - lower >> 1);
      if (comparison(list[lower], list[middle]) > 0)
        SwapCore(list, lower, middle);
      if (comparison(list[lower], list[upper]) > 0)
        SwapCore(list, lower, upper);
      if (comparison(list[middle], list[upper]) > 0)
        SwapCore(list, middle, upper);
      var median = list[middle];
      do
      {
        while (comparison(list[lower], median) < 0)
          lower++;
        while (comparison(median, list[upper]) < 0)
          upper--;
        if (lower > upper)
          break;
        else if (lower < upper)
          SwapCore(list, lower, upper);
        lower++;
        upper--;
      }
      while (lower <= upper);
      if (upper - left <= right - lower)
      {
        if (left < upper)
          QuickSortCore(list, left, upper - left + 1, comparison);
        left = lower;
      }
      else
      {
        if (lower < right)
          QuickSortCore(list, lower, right - lower + 1, comparison);
        right = upper;
      }
    }
    while (left < right);
  }

  public static void QuickSort<TSource>(this IList<TSource> list)
  {
    Argument.That.NotNull(list);

    QuickSortCore(list, 0, list.Count, Comparer<TSource>.Default.Compare);
  }

  public static void QuickSort<TSource>(this IList<TSource> list, int index)
  {
    Argument.That.InRangeOut(list, index);

    QuickSortCore(list, index, list.Count - index, Comparer<TSource>.Default.Compare);
  }

  public static void QuickSort<TSource>(this IList<TSource> list, int index, int count)
  {
    Argument.That.InRangeOut(list, index, count);

    QuickSortCore(list, index, count, Comparer<TSource>.Default.Compare);
  }

  public static void QuickSort<TSource>(this IList<TSource> list, (int index, int count) range)
  {
    Argument.That.InRangeOut(list, range);

    QuickSortCore(list, range.index, range.count, Comparer<TSource>.Default.Compare);
  }

  public static void QuickSort<TSource>(this IList<TSource> list, Comparison<TSource>? comparison)
  {
    Argument.That.NotNull(list);

    QuickSortCore(list, 0, list.Count, comparison ?? Comparer<TSource>.Default.Compare);
  }

  public static void QuickSort<TSource>(this IList<TSource> list, int index, Comparison<TSource>? comparison)
  {
    Argument.That.InRangeOut(list, index);

    QuickSortCore(list, index, list.Count - index, comparison ?? Comparer<TSource>.Default.Compare);
  }

  public static void QuickSort<TSource>(this IList<TSource> list, int index, int count, Comparison<TSource>? comparison)
  {
    Argument.That.InRangeOut(list, index, count);

    QuickSortCore(list, index, count, comparison ?? Comparer<TSource>.Default.Compare);
  }

  public static void QuickSort<TSource>(this IList<TSource> list, (int index, int count) range, Comparison<TSource>? comparison)
  {
    Argument.That.InRangeOut(list, range);

    QuickSortCore(list, range.index, range.count, comparison ?? Comparer<TSource>.Default.Compare);
  }

  public static void QuickSort<TSource>(this IList<TSource> list, IComparer<TSource>? comparer)
  {
    Argument.That.NotNull(list);

    QuickSortCore(list, 0, list.Count, (comparer ?? Comparer<TSource>.Default).Compare);
  }

  public static void QuickSort<TSource>(this IList<TSource> list, int index, IComparer<TSource>? comparer)
  {
    Argument.That.InRangeOut(list, index);

    QuickSortCore(list, index, list.Count - index, (comparer ?? Comparer<TSource>.Default).Compare);
  }

  public static void QuickSort<TSource>(this IList<TSource> list, int index, int count, IComparer<TSource>? comparer)
  {
    Argument.That.InRangeOut(list, index, count);

    QuickSortCore(list, index, count, (comparer ?? Comparer<TSource>.Default).Compare);
  }

  public static void QuickSort<TSource>(this IList<TSource> list, (int index, int count) range, IComparer<TSource>? comparer)
  {
    Argument.That.InRangeOut(list, range);

    QuickSortCore(list, range.index, range.count, (comparer ?? Comparer<TSource>.Default).Compare);
  }

  private static void QuickSortCore<TKey, TSource>(IList<TSource> list, IList<TKey> keys, int index, int count, Comparison<TKey> comparison)
  {
    var left = index;
    var right = index + count - 1;
    do
    {
      var lower = left;
      var upper = right;
      var middle = lower + (upper - lower >> 1);
      if (comparison(keys[lower], keys[middle]) > 0)
      {
        SwapCore(keys, lower, middle);
        SwapCore(list, lower, middle);
      }
      if (comparison(keys[lower], keys[upper]) > 0)
      {
        SwapCore(keys, lower, upper);
        SwapCore(list, lower, upper);
      }
      if (comparison(keys[middle], keys[upper]) > 0)
      {
        SwapCore(keys, middle, upper);
        SwapCore(list, middle, upper);
      }
      var median = keys[middle];
      do
      {
        while (comparison(keys[lower], median) < 0)
          lower++;
        while (comparison(median, keys[upper]) < 0)
          upper--;
        if (lower > upper)
          break;
        else if (lower < upper)
        {
          SwapCore(keys, lower, upper);
          SwapCore(list, lower, upper);
        }
        lower++;
        upper--;
      }
      while (lower <= upper);
      if (upper - left <= right - lower)
      {
        if (left < upper)
          QuickSortCore(list, keys, left, upper - left + 1, comparison);
        left = lower;
      }
      else
      {
        if (lower < right)
          QuickSortCore(list, keys, lower, right - lower + 1, comparison);
        right = upper;
      }
    }
    while (left < right);
  }

  public static void QuickSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys)
  {
    Argument.That.MatchCoupledCount(list, keys, null);

    QuickSortCore(list, keys, 0, list.Count, Comparer<TKey>.Default.Compare);
  }

  public static void QuickSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index)
  {
    Argument.That.MatchCoupledCount(list, keys, null);
    Argument.That.InRangeOut(list, index);

    QuickSortCore(list, keys, index, list.Count - index, Comparer<TKey>.Default.Compare);
  }

  public static void QuickSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, int count)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.InRangeOut(keys, index, count);

    QuickSortCore(list, keys, index, count, Comparer<TKey>.Default.Compare);
  }

  public static void QuickSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, (int index, int count) range)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.InRangeOut(keys, range);

    QuickSortCore(list, keys, range.index, range.count, Comparer<TKey>.Default.Compare);
  }

  public static void QuickSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, Comparison<TKey>? comparison)
  {
    Argument.That.MatchCoupledCount(list, keys, null);

    QuickSortCore(list, keys, 0, list.Count, comparison ?? Comparer<TKey>.Default.Compare);
  }

  public static void QuickSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, Comparison<TKey>? comparison)
  {
    Argument.That.MatchCoupledCount(list, keys, null);
    Argument.That.InRangeOut(list, index);

    QuickSortCore(list, keys, index, list.Count, comparison ?? Comparer<TKey>.Default.Compare);
  }

  public static void QuickSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, int count, Comparison<TKey>? comparison)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.InRangeOut(keys, index, count);

    QuickSortCore(list, keys, index, count, comparison ?? Comparer<TKey>.Default.Compare);
  }

  public static void QuickSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, (int index, int count) range, Comparison<TKey>? comparison)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.InRangeOut(keys, range);

    QuickSortCore(list, keys, range.index, range.count, comparison ?? Comparer<TKey>.Default.Compare);
  }

  public static void QuickSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, IComparer<TKey>? comparer)
  {
    Argument.That.MatchCoupledCount(list, keys, null);

    QuickSortCore(list, keys, 0, list.Count, (comparer ?? Comparer<TKey>.Default).Compare);
  }

  public static void QuickSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, IComparer<TKey>? comparer)
  {
    Argument.That.MatchCoupledCount(list, keys, null);
    Argument.That.InRangeOut(list, index);

    QuickSortCore(list, keys, index, list.Count - index, (comparer ?? Comparer<TKey>.Default).Compare);
  }

  public static void QuickSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, int count, IComparer<TKey>? comparer)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.InRangeOut(keys, index, count);

    QuickSortCore(list, keys, index, count, (comparer ?? Comparer<TKey>.Default).Compare);
  }

  public static void QuickSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, (int index, int count) range, IComparer<TKey>? comparer)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.InRangeOut(keys, range);

    QuickSortCore(list, keys, range.index, range.count, (comparer ?? Comparer<TKey>.Default).Compare);
  }

  #endregion
  #region HeapSort

  private static int HeapifyDownCore<TSource>(IList<TSource> list, int index, int count, int offset, Comparison<TSource> comparison)
  {
    while (true)
    {
      var parent = offset;
      var left = (parent << 1) + 1;
      var right = (parent << 1) + 2;
      if (left < count && comparison(list[index + left], list[index + offset]) > 0)
        offset = left;
      if (right < count && comparison(list[index + right], list[index + offset]) > 0)
        offset = right;
      if (offset == parent)
        break;
      SwapCore(list, index + offset, index + parent);
    }
    return index + offset;
  }

  private static void HeapSortCore<TSource>(IList<TSource> list, int index, int count, Comparison<TSource> comparison)
  {
    for (var i = count >> 1 - 1; i >= 0; i--)
      HeapifyDownCore(list, index, count, i, comparison);
    for (var i = count - 1; i > 0; i--)
    {
      SwapCore(list, index, index + i);
      HeapifyDownCore(list, index, i, 0, comparison);
    }
  }

  public static void HeapSort<TSource>(this IList<TSource> list)
  {
    Argument.That.NotNull(list);

    HeapSortCore(list, 0, list.Count, Comparer<TSource>.Default.Compare);
  }

  public static void HeapSort<TSource>(this IList<TSource> list, int index)
  {
    Argument.That.InRangeOut(list, index);

    HeapSortCore(list, index, list.Count - index, Comparer<TSource>.Default.Compare);
  }

  public static void HeapSort<TSource>(this IList<TSource> list, int index, int count)
  {
    Argument.That.InRangeOut(list, index, count);

    HeapSortCore(list, index, count, Comparer<TSource>.Default.Compare);
  }

  public static void HeapSort<TSource>(this IList<TSource> list, (int index, int count) range)
  {
    Argument.That.InRangeOut(list, range);

    HeapSortCore(list, range.index, range.count, Comparer<TSource>.Default.Compare);
  }

  public static void HeapSort<TSource>(this IList<TSource> list, Comparison<TSource>? comparison)
  {
    Argument.That.NotNull(list);

    HeapSortCore(list, 0, list.Count, comparison ?? Comparer<TSource>.Default.Compare);
  }

  public static void HeapSort<TSource>(this IList<TSource> list, int index, Comparison<TSource>? comparison)
  {
    Argument.That.InRangeOut(list, index);

    HeapSortCore(list, index, list.Count - index, comparison ?? Comparer<TSource>.Default.Compare);
  }

  public static void HeapSort<TSource>(this IList<TSource> list, int index, int count, Comparison<TSource>? comparison)
  {
    Argument.That.InRangeOut(list, index, count);

    HeapSortCore(list, index, count, comparison ?? Comparer<TSource>.Default.Compare);
  }

  public static void HeapSort<TSource>(this IList<TSource> list, (int index, int count) range, Comparison<TSource>? comparison)
  {
    Argument.That.InRangeOut(list, range);

    HeapSortCore(list, range.index, range.count, comparison ?? Comparer<TSource>.Default.Compare);
  }

  public static void HeapSort<TSource>(this IList<TSource> list, IComparer<TSource>? comparer)
  {
    Argument.That.NotNull(list);

    HeapSortCore(list, 0, list.Count, (comparer ?? Comparer<TSource>.Default).Compare);
  }

  public static void HeapSort<TSource>(this IList<TSource> list, int index, IComparer<TSource>? comparer)
  {
    Argument.That.InRangeOut(list, index);

    HeapSortCore(list, index, list.Count - index, (comparer ?? Comparer<TSource>.Default).Compare);
  }

  public static void HeapSort<TSource>(this IList<TSource> list, int index, int count, IComparer<TSource>? comparer)
  {
    Argument.That.InRangeOut(list, index, count);

    HeapSortCore(list, index, count, (comparer ?? Comparer<TSource>.Default).Compare);
  }

  public static void HeapSort<TSource>(this IList<TSource> list, (int index, int count) range, IComparer<TSource>? comparer)
  {
    Argument.That.InRangeOut(list, range);

    HeapSortCore(list, range.index, range.count, (comparer ?? Comparer<TSource>.Default).Compare);
  }

  private static int HeapifyDownCore<TKey, TSource>(IList<TSource> list, IList<TKey> keys, int index, int count, int offset, Comparison<TKey> comparison)
  {
    while (true)
    {
      var parent = offset;
      var left = (parent << 1) + 1;
      var right = (parent << 1) + 2;
      if (left < count && comparison(keys[index + left], keys[index + offset]) > 0)
        offset = left;
      if (right < count && comparison(keys[index + right], keys[index + offset]) > 0)
        offset = right;
      if (offset == parent)
        break;
      SwapCore(keys, index + offset, index + parent);
      SwapCore(list, index + offset, index + parent);
    }
    return index + offset;
  }

  private static void HeapSortCore<TKey, TSource>(IList<TSource> list, IList<TKey> keys, int index, int count, Comparison<TKey> comparison)
  {
    for (var i = count >> 1 - 1; i >= 0; i--)
      HeapifyDownCore(list, keys, index, count, i, comparison);
    for (var i = count - 1; i > 0; i--)
    {
      SwapCore(keys, index, index + i);
      SwapCore(list, index, index + i);
      HeapifyDownCore(list, keys, index, i, 0, comparison);
    }
  }

  public static void HeapSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys)
  {
    Argument.That.MatchCoupledCount(list, keys, null);

    HeapSortCore(list, keys, 0, list.Count, Comparer<TKey>.Default.Compare);
  }

  public static void HeapSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index)
  {
    Argument.That.MatchCoupledCount(list, keys, null);
    Argument.That.InRangeOut(list, index);

    HeapSortCore(list, keys, index, list.Count - index, Comparer<TKey>.Default.Compare);
  }

  public static void HeapSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, int count)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.InRangeOut(keys, index, count);

    HeapSortCore(list, keys, index, count, Comparer<TKey>.Default.Compare);
  }

  public static void HeapSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, (int index, int count) range)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.InRangeOut(keys, range);

    HeapSortCore(list, keys, range.index, range.count, Comparer<TKey>.Default.Compare);
  }

  public static void HeapSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, Comparison<TKey>? comparison)
  {
    Argument.That.MatchCoupledCount(list, keys, null);

    HeapSortCore(list, keys, 0, list.Count, comparison ?? Comparer<TKey>.Default.Compare);
  }

  public static void HeapSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, Comparison<TKey>? comparison)
  {
    Argument.That.MatchCoupledCount(list, keys, null);
    Argument.That.InRangeOut(list, index);

    HeapSortCore(list, keys, index, list.Count - index, comparison ?? Comparer<TKey>.Default.Compare);
  }

  public static void HeapSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, int count, Comparison<TKey>? comparison)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.InRangeOut(keys, index, count);

    HeapSortCore(list, keys, index, count, comparison ?? Comparer<TKey>.Default.Compare);
  }

  public static void HeapSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, (int index, int count) range, Comparison<TKey>? comparison)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.InRangeOut(keys, range);

    HeapSortCore(list, keys, range.index, range.count, comparison ?? Comparer<TKey>.Default.Compare);
  }

  public static void HeapSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, IComparer<TKey>? comparer)
  {
    Argument.That.MatchCoupledCount(list, keys, null);

    HeapSortCore(list, keys, 0, list.Count, (comparer ?? Comparer<TKey>.Default).Compare);
  }

  public static void HeapSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, IComparer<TKey>? comparer)
  {
    Argument.That.MatchCoupledCount(list, keys, null);
    Argument.That.InRangeOut(list, index);

    HeapSortCore(list, keys, index, list.Count - index, (comparer ?? Comparer<TKey>.Default).Compare);
  }

  public static void HeapSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, int count, IComparer<TKey>? comparer)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.InRangeOut(keys, index, count);

    HeapSortCore(list, keys, index, count, (comparer ?? Comparer<TKey>.Default).Compare);
  }

  public static void HeapSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, (int index, int count) range, IComparer<TKey>? comparer)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.InRangeOut(keys, range);

    HeapSortCore(list, keys, range.index, range.count, (comparer ?? Comparer<TKey>.Default).Compare);
  }

  #endregion
  #endregion
  #region Extraction methods
  #region Enumerate

  private static IEnumerable<TSource> EnumerateCore<TSource>(IList<TSource> list, int index, int count, bool reverse)
  {
    for (index += reverse && count > 0 ? count - 1 : 0; count > 0; index += reverse ? -1 : 1, count--)
      yield return list[index];
  }

  public static IEnumerable<TSource> Enumerate<TSource>(this IList<TSource> list, bool reverse = false)
  {
    Argument.That.NotNull(list);

    return EnumerateCore(list, 0, list.Count, reverse);
  }

  public static IEnumerable<TSource> Enumerate<TSource>(this IList<TSource> list, int index, bool reverse = false)
  {
    Argument.That.InRangeOut(list, index);

    return EnumerateCore(list, index, list.Count - index, reverse);
  }

  public static IEnumerable<TSource> Enumerate<TSource>(this IList<TSource> list, int index, int count, bool reverse = false)
  {
    Argument.That.InRangeOut(list, index, count);

    return EnumerateCore(list, index, count, reverse);
  }

  public static IEnumerable<TSource> Enumerate<TSource>(this IList<TSource> list, (int index, int count) range, bool reverse = false)
  {
    Argument.That.InRangeOut(list, range);

    return EnumerateCore(list, range.index, range.count, reverse);
  }

  #endregion
  #region Copy

  private static void CopyCore<TSource>(IList<TSource> srcList, int srcIndex, IList<TSource> dstList, int dstIndex, int count, bool reverse)
  {
    for (srcIndex += reverse && count > 0 ? count - 1 : 0, dstIndex += reverse && count > 0 ? count - 1 : 0;
      count > 0; srcIndex += reverse ? -1 : 1, dstIndex += reverse ? -1 : 1, count--)
      dstList[dstIndex] = srcList[srcIndex];
  }

  public static void Copy<TSource>(this IList<TSource> srcList, IList<TSource> dstList, bool reverse = false)
  {
    Argument.That.NotNull(srcList);
    Argument.That.InRangeOut(dstList, 0, srcList.Count);

    CopyCore(srcList, 0, dstList, 0, srcList.Count, reverse);
  }

  public static void Copy<TSource>(this IList<TSource> srcList, IList<TSource> dstList, int dstIndex, int srcIndex, bool reverse = false)
  {
    Argument.That.InRangeOut(srcList, srcIndex);
    var count = Argument.That.InRangeOut(dstList, dstIndex, srcList.Count - srcIndex).count;

    CopyCore(srcList, srcIndex, dstList, dstIndex, count, reverse);
  }

  public static void Copy<TSource>(this IList<TSource> srcList, IList<TSource> dstList, int dstIndex, int srcIndex, int count, bool reverse = false)
  {
    Argument.That.InRangeOut(srcList, srcIndex, count);
    Argument.That.InRangeOut(dstList, dstIndex, count);

    CopyCore(srcList, srcIndex, dstList, dstIndex, count, reverse);
  }

  public static void Copy<TSource>(this IList<TSource> srcList, IList<TSource> dstList, int dstIndex, (int index, int count) srcRange, bool reverse = false)
  {
    Argument.That.InRangeOut(srcList, srcRange);
    Argument.That.InRangeOut(dstList, dstIndex, srcRange.count);

    CopyCore(srcList, srcRange.index, dstList, dstIndex, srcRange.count, reverse);
  }

  #endregion
  #endregion
  #region Retrieval methods
  #region Match

  private static bool MatchCore<T>(IList<T> list, int index, int count, ElementPredicate<T> predicate, bool all)
  {
    bool result = all;
    for (; count > 0 && result ^ !all; index++, count--)
      result = predicate(list[index], index);
    return result;
  }

  public static bool Match<T>(this IList<T> list, Predicate<T> predicate, bool all)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(predicate);

    return MatchCore(list, 0, list.Count, ElementPredicate(predicate), all);
  }

  public static bool Match<T>(this IList<T> list, int index, Predicate<T> predicate, bool all)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(predicate);

    return MatchCore(list, index, list.Count - index, ElementPredicate(predicate), all);
  }

  public static bool Match<T>(this IList<T> list, int index, int count, Predicate<T> predicate, bool all)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(predicate);

    return MatchCore(list, index, count, ElementPredicate(predicate), all);
  }

  public static bool Match<T>(this IList<T> list, (int index, int count) range, Predicate<T> predicate, bool all)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(predicate);

    return MatchCore(list, range.index, range.count, ElementPredicate(predicate), all);
  }

  public static bool Match<T>(this IList<T> list, ElementPredicate<T> predicate, bool all)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(predicate);

    return MatchCore(list, 0, list.Count, predicate, all);
  }

  public static bool Match<T>(this IList<T> list, int index, ElementPredicate<T> predicate, bool all)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(predicate);

    return MatchCore(list, index, list.Count - index, predicate, all);
  }

  public static bool Match<T>(this IList<T> list, int index, int count, ElementPredicate<T> predicate, bool all)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(predicate);

    return MatchCore(list, index, count, predicate, all);
  }

  public static bool Match<T>(this IList<T> list, (int index, int count) range, ElementPredicate<T> predicate, bool all)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(predicate);

    return MatchCore(list, range.index, range.count, predicate, all);
  }

  public static bool MatchAny<T>(this IList<T> list, Predicate<T> predicate)
    => list.Match(predicate, false);

  public static bool MatchAny<T>(this IList<T> list, int index, Predicate<T> predicate)
    => list.Match(index, predicate, false);

  public static bool MatchAny<T>(this IList<T> list, int index, int count, Predicate<T> predicate)
    => list.Match(index, count, predicate, false);

  public static bool MatchAny<T>(this IList<T> list, (int index, int count) range, Predicate<T> predicate)
    => list.Match(range, predicate, false);

  public static bool MatchAny<T>(this IList<T> list, ElementPredicate<T> predicate)
    => list.Match(predicate, false);

  public static bool MatchAny<T>(this IList<T> list, int index, ElementPredicate<T> predicate)
    => list.Match(index, predicate, false);

  public static bool MatchAny<T>(this IList<T> list, int index, int count, ElementPredicate<T> predicate)
    => list.Match(index, count, predicate, false);

  public static bool MatchAny<T>(this IList<T> list, (int index, int count) range, ElementPredicate<T> predicate)
    => list.Match(range, predicate, false);

  public static bool MatchAll<T>(this IList<T> list, Predicate<T> predicate)
    => list.Match(predicate, true);

  public static bool MatchAll<T>(this IList<T> list, int index, Predicate<T> predicate)
    => list.Match(index, predicate, true);

  public static bool MatchAll<T>(this IList<T> list, int index, int count, Predicate<T> predicate)
    => list.Match(index, count, predicate, true);

  public static bool MatchAll<T>(this IList<T> list, (int index, int count) range, Predicate<T> predicate)
    => list.Match(range, predicate, true);

  public static bool MatchAll<T>(this IList<T> list, ElementPredicate<T> predicate)
    => list.Match(predicate, true);

  public static bool MatchAll<T>(this IList<T> list, int index, ElementPredicate<T> predicate)
    => list.Match(index, predicate, true);

  public static bool MatchAll<T>(this IList<T> list, int index, int count, ElementPredicate<T> predicate)
    => list.Match(index, count, predicate, true);

  public static bool MatchAll<T>(this IList<T> list, (int index, int count) range, ElementPredicate<T> predicate)
    => list.Match(range, predicate, true);

  #endregion
  #region FindIndex

  private static int FindIndexCore<T>(IList<T> list, int index, int count, ElementPredicate<T> predicate)
  {
    for (; count > 0; index++, count--)
      if (predicate(list[index], index))
        return index;
    return InvalidIndex;
  }

  public static int FindIndex<T>(this IList<T> list, Predicate<T> predicate)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(predicate);

    return FindIndexCore(list, 0, list.Count, ElementPredicate(predicate));
  }

  public static int FindIndex<T>(this IList<T> list, int index, Predicate<T> predicate)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(predicate);

    return FindIndexCore(list, index, list.Count - index, ElementPredicate(predicate));
  }

  public static int FindIndex<T>(this IList<T> list, int index, int count, Predicate<T> predicate)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(predicate);

    return FindIndexCore(list, index, count, ElementPredicate(predicate));
  }

  public static int FindIndex<T>(this IList<T> list, (int index, int count) range, Predicate<T> predicate)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(predicate);

    return FindIndexCore(list, range.index, range.count, ElementPredicate(predicate));
  }

  public static int FindIndex<T>(this IList<T> list, ElementPredicate<T> predicate)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(predicate);

    return FindIndexCore(list, 0, list.Count, predicate);
  }

  public static int FindIndex<T>(this IList<T> list, int index, ElementPredicate<T> predicate)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(predicate);

    return FindIndexCore(list, index, list.Count - index, predicate);
  }

  public static int FindIndex<T>(this IList<T> list, int index, int count, ElementPredicate<T> predicate)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(predicate);

    return FindIndexCore(list, index, count, predicate);
  }

  public static int FindIndex<T>(this IList<T> list, (int index, int count) range, ElementPredicate<T> predicate)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(predicate);

    return FindIndexCore(list, range.index, range.count, predicate);
  }

  #endregion
  #region FindLastIndex

  private static int FindLastIndexCore<T>(IList<T> list, int index, int count, ElementPredicate<T> predicate)
  {
    for (; count > 0 && index >= 0; index--, count--)
      if (predicate(list[index], index))
        return index;
    return InvalidIndex;
  }

  public static int FindLastIndex<T>(this IList<T> list, Predicate<T> predicate)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(predicate);

    return FindLastIndexCore(list, list.Count == 0 ? 0 : list.Count - 1, list.Count, ElementPredicate(predicate));
  }

  public static int FindLastIndex<T>(this IList<T> list, int index, Predicate<T> predicate)
  {
    Argument.That.InRangeIn(list, index);
    Argument.That.NotNull(predicate);

    return FindLastIndexCore(list, index, index + 1, ElementPredicate(predicate));
  }

  public static int FindLastIndex<T>(this IList<T> list, int index, int count, Predicate<T> predicate)
  {
    Argument.That.InRangeRev(list, index, count);
    Argument.That.NotNull(predicate);

    return FindLastIndexCore(list, index, count, ElementPredicate(predicate));
  }

  public static int FindLastIndex<T>(this IList<T> list, (int index, int count) range, Predicate<T> predicate)
  {
    Argument.That.InRangeRev(list, range);
    Argument.That.NotNull(predicate);

    return FindLastIndexCore(list, range.index, range.count, ElementPredicate(predicate));
  }

  public static int FindLastIndex<T>(this IList<T> list, ElementPredicate<T> predicate)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(predicate);

    return FindLastIndexCore(list, list.Count == 0 ? 0 : list.Count - 1, list.Count, predicate);
  }

  public static int FindLastIndex<T>(this IList<T> list, int index, ElementPredicate<T> predicate)
  {
    Argument.That.InRangeIn(list, index);
    Argument.That.NotNull(predicate);

    return FindLastIndexCore(list, index, index + 1, predicate);
  }

  public static int FindLastIndex<T>(this IList<T> list, int index, int count, ElementPredicate<T> predicate)
  {
    Argument.That.InRangeRev(list, index, count);
    Argument.That.NotNull(predicate);

    return FindLastIndexCore(list, index, count, predicate);
  }

  public static int FindLastIndex<T>(this IList<T> list, (int index, int count) range, ElementPredicate<T> predicate)
  {
    Argument.That.InRangeRev(list, range);
    Argument.That.NotNull(predicate);

    return FindLastIndexCore(list, range.index, range.count, predicate);
  }

  #endregion
  #region FindAll

  private static IEnumerable<T> FindAllCore<T>(IList<T> list, int index, int count, ElementPredicate<T> predicate, bool reverse)
  {
    for (index += reverse && count > 0 ? count - 1 : 0; count > 0; index += reverse ? -1 : 1, count--)
      if (predicate(list[index], index))
        yield return list[index];
  }

  public static IEnumerable<T> FindAll<T>(this IList<T> list, Predicate<T> predicate, bool reverse = false)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(predicate);

    return FindAllCore(list, 0, list.Count, ElementPredicate(predicate), reverse);
  }

  public static IEnumerable<T> FindAll<T>(this IList<T> list, int index, Predicate<T> predicate, bool reverse = false)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(predicate);

    return FindAllCore(list, index, list.Count - index, ElementPredicate(predicate), reverse);
  }

  public static IEnumerable<T> FindAll<T>(this IList<T> list, int index, int count, Predicate<T> predicate, bool reverse = false)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(predicate);

    return FindAllCore(list, index, count, ElementPredicate(predicate), reverse);
  }

  public static IEnumerable<T> FindAll<T>(this IList<T> list, (int index, int count) range, Predicate<T> predicate, bool reverse = false)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(predicate);

    return FindAllCore(list, range.index, range.count, ElementPredicate(predicate), reverse);
  }

  public static IEnumerable<T> FindAll<T>(this IList<T> list, ElementPredicate<T> predicate, bool reverse = false)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(predicate);

    return FindAllCore(list, 0, list.Count, predicate, reverse);
  }

  public static IEnumerable<T> FindAll<T>(this IList<T> list, int index, ElementPredicate<T> predicate, bool reverse = false)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(predicate);

    return FindAllCore(list, index, list.Count - index, predicate, reverse);
  }

  public static IEnumerable<T> FindAll<T>(this IList<T> list, int index, int count, ElementPredicate<T> predicate, bool reverse = false)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(predicate);

    return FindAllCore(list, index, count, predicate, reverse);
  }

  public static IEnumerable<T> FindAll<T>(this IList<T> list, (int index, int count) range, ElementPredicate<T> predicate, bool reverse = false)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(predicate);

    return FindAllCore(list, range.index, range.count, predicate, reverse);
  }

  #endregion
  #region FindAllIndices

  private static IEnumerable<int> FindAllIndicesCore<T>(IList<T> list, int index, int count, ElementPredicate<T> predicate, bool reverse)
  {
    for (index += reverse && count > 0 ? count - 1 : 0; count > 0; index += reverse ? -1 : 1, count--)
      if (predicate(list[index], index))
        yield return index;
  }

  public static IEnumerable<int> FindAllIndices<T>(this IList<T> list, Predicate<T> predicate, bool reverse = false)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(predicate);

    return FindAllIndicesCore(list, 0, list.Count, ElementPredicate(predicate), reverse);
  }

  public static IEnumerable<int> FindAllIndices<T>(this IList<T> list, int index, Predicate<T> predicate, bool reverse = false)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(predicate);

    return FindAllIndicesCore(list, index, list.Count - index, ElementPredicate(predicate), reverse);
  }

  public static IEnumerable<int> FindAllIndices<T>(this IList<T> list, int index, int count, Predicate<T> predicate, bool reverse = false)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(predicate);

    return FindAllIndicesCore(list, index, count, ElementPredicate(predicate), reverse);
  }

  public static IEnumerable<int> FindAllIndices<T>(this IList<T> list, (int index, int count) range, Predicate<T> predicate, bool reverse = false)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(predicate);

    return FindAllIndicesCore(list, range.index, range.count, ElementPredicate(predicate), reverse);
  }

  public static IEnumerable<int> FindAllIndices<T>(this IList<T> list, ElementPredicate<T> predicate, bool reverse = false)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(predicate);

    return FindAllIndicesCore(list, 0, list.Count, predicate, reverse);
  }

  public static IEnumerable<int> FindAllIndices<T>(this IList<T> list, int index, ElementPredicate<T> predicate, bool reverse = false)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(predicate);

    return FindAllIndicesCore(list, index, list.Count - index, predicate, reverse);
  }

  public static IEnumerable<int> FindAllIndices<T>(this IList<T> list, int index, int count, ElementPredicate<T> predicate, bool reverse = false)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(predicate);

    return FindAllIndicesCore(list, index, count, predicate, reverse);
  }

  public static IEnumerable<int> FindAllIndices<T>(this IList<T> list, (int index, int count) range, ElementPredicate<T> predicate, bool reverse = false)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(predicate);

    return FindAllIndicesCore(list, range.index, range.count, predicate, reverse);
  }

  #endregion
  #region BinarySearch

  private static int BinarySearchCore<T>(IList<T> list, int index, int count, Comparator<T> comparator, SearchingOption option)
  {
    int found = -1;
    int lower = index;
    if (count > 0)
    {
      int upper = index + count - 1;
      while (lower <= upper)
      {
        int middle = lower + (upper - lower >> 1);
        int result = comparator(list[middle]);
        if (result > 0)
          upper = middle - 1;
        else if (result < 0)
          lower = middle + 1;
        else
        {
          found = middle;
          if (option == SearchingOption.First)
            upper = middle - 1;
          else if (option == SearchingOption.Last)
            lower = middle + 1;
          else
            break;
        }
      }
    }
    return found < 0 ? ~lower : found;
  }

  public static int BinarySearch<T>(this IList<T> list, Comparator<T> comparator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(comparator);

    return BinarySearchCore(list, 0, list.Count, comparator, option);
  }

  public static int BinarySearch<T>(this IList<T> list, int index, Comparator<T> comparator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(comparator);

    return BinarySearchCore(list, index, list.Count - index, comparator, option);
  }

  public static int BinarySearch<T>(this IList<T> list, int index, int count, Comparator<T> comparator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(comparator);

    return BinarySearchCore(list, index, count, comparator, option);
  }

  public static int BinarySearch<T>(this IList<T> list, (int index, int count) range, Comparator<T> comparator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(comparator);

    return BinarySearchCore(list, range.index, range.count, comparator, option);
  }

  public static int BinarySearch<T>(this IList<T> list, IComparator<T> comparator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(comparator);

    return BinarySearchCore(list, 0, list.Count, comparator.Compare, option);
  }

  public static int BinarySearch<T>(this IList<T> list, int index, IComparator<T> comparator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(comparator);

    return BinarySearchCore(list, index, list.Count - index, comparator.Compare, option);
  }

  public static int BinarySearch<T>(this IList<T> list, int index, int count, IComparator<T> comparator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(comparator);

    return BinarySearchCore(list, index, count, comparator.Compare, option);
  }

  public static int BinarySearch<T>(this IList<T> list, (int index, int count) range, IComparator<T> comparator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(comparator);

    return BinarySearchCore(list, range.index, range.count, comparator.Compare, option);
  }

  #endregion
  #region InterpolationSearch

  private static int InterpolationSearchCore<T>(IList<T> list, int index, int count, Comparator<T> comparator, Func<T, T, float> interpolator, SearchingOption option)
  {
    int found = -1;
    int lower = index;
    if (count > 0)
    {
      int upper = index + count - 1;
      while (lower <= upper)
      {
        float weight = interpolator(list[lower], list[upper]);
        if (weight > 1.0f)
          Operation.That.Failed();
        int middle = lower + (int)((upper - lower) * weight);
        int result = comparator(list[middle]);
        if (result > 0)
          upper = middle - 1;
        else if (result < 0)
          lower = middle + 1;
        else
        {
          found = middle;
          if (option == SearchingOption.First)
            upper = middle - 1;
          else if (option == SearchingOption.Last)
            lower = middle + 1;
          else
            break;
        }
      }
    }
    return found < 0 ? ~lower : found;
  }

  public static int InterpolationSearch<T>(this IList<T> list, Comparator<T> comparator, Func<T, T, float> interpolator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(comparator);
    Argument.That.NotNull(interpolator);

    return InterpolationSearchCore(list, 0, list.Count, comparator, interpolator, option);
  }

  public static int InterpolationSearch<T>(this IList<T> list, int index, Comparator<T> comparator, Func<T, T, float> interpolator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(comparator);
    Argument.That.NotNull(interpolator);

    return InterpolationSearchCore(list, index, list.Count - index, comparator, interpolator, option);
  }

  public static int InterpolationSearch<T>(this IList<T> list, int index, int count, Comparator<T> comparator, Func<T, T, float> interpolator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(comparator);
    Argument.That.NotNull(interpolator);

    return InterpolationSearchCore(list, index, count, comparator, interpolator, option);
  }

  public static int InterpolationSearch<T>(this IList<T> list, (int index, int count) range, Comparator<T> comparator, Func<T, T, float> interpolator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(comparator);
    Argument.That.NotNull(interpolator);

    return InterpolationSearchCore(list, range.index, range.count, comparator, interpolator, option);
  }

  public static int InterpolationSearch<T>(this IList<T> list, IComparator<T> comparator, Func<T, T, float> interpolator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(comparator);
    Argument.That.NotNull(interpolator);

    return InterpolationSearchCore(list, 0, list.Count, comparator.Compare, interpolator, option);
  }

  public static int InterpolationSearch<T>(this IList<T> list, int index, IComparator<T> comparator, Func<T, T, float> interpolator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(comparator);
    Argument.That.NotNull(interpolator);

    return InterpolationSearchCore(list, index, list.Count - index, comparator.Compare, interpolator, option);
  }

  public static int InterpolationSearch<T>(this IList<T> list, int index, int count, IComparator<T> comparator, Func<T, T, float> interpolator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(comparator);
    Argument.That.NotNull(interpolator);

    return InterpolationSearchCore(list, index, count, comparator.Compare, interpolator, option);
  }

  public static int InterpolationSearch<T>(this IList<T> list, (int index, int count) range, IComparator<T> comparator, Func<T, T, float> interpolator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(comparator);
    Argument.That.NotNull(interpolator);

    return InterpolationSearchCore(list, range.index, range.count, comparator.Compare, interpolator, option);
  }

  #endregion
  #region SequenceFind

  private static int SequenceFindCore<T>(IList<T> list, int index, int count, IList<T> search, bool partial, Equality<T> equality)
  {
    int matched = 0;
    while (count > 0 && matched < count && matched < search.Count && (partial || count >= search.Count))
    {
      if (equality(list[index + matched], search[matched]))
        matched++;
      else
      {
        matched = 0;
        count--;
        index++;
      }
    }
    return matched == search.Count ? index : -matched - 1;
  }

  public static int SequenceFind<T>(this IList<T> list, IList<T> search, bool partial, Equality<T> equality)
    => list.SequenceFind(0, list is not null ? list.Count : 0, search, partial, equality);

  public static int SequenceFind<T>(this IList<T> list, int index, IList<T> search, bool partial, Equality<T> equality)
    => list.SequenceFind(index, list is not null ? list.Count - index : 0, search, partial, equality);

  public static int SequenceFind<T>(this IList<T> list, int index, int count, IList<T> search, bool partial, Equality<T> equality)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(search);
    Argument.That.NotNull(equality);

    return SequenceFindCore(list, index, count, search, partial, equality);
  }

  public static int SequenceFind<T>(this IList<T> list, (int index, int count) range, IList<T> search, bool partial, Equality<T> equality)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(search);
    Argument.That.NotNull(equality);

    return SequenceFindCore(list, range.index, range.count, search, partial, equality);
  }

  private static int SequenceFindLastCore<T>(IList<T> list, int index, int count, IList<T> search, bool partial, Equality<T> equality)
  {
    int matched = 0;
    while (count > 0 && matched < count && matched < search.Count && (partial || count >= search.Count))
    {
      if (equality(list[index + count - 1 - matched], search[search.Count - 1 - matched]))
        matched++;
      else
      {
        matched = 0;
        count--;
      }
    }
    return matched == search.Count ? index + count - matched : -matched - 1;
  }

  public static int SequenceFindLast<T>(this IList<T> list, IList<T> search, bool partial, Equality<T> equality)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(search);
    Argument.That.NotNull(equality);

    return SequenceFindLastCore(list, 0, list.Count, search, partial, equality);
  }

  public static int SequenceFindLast<T>(this IList<T> list, int index, IList<T> search, bool partial, Equality<T> equality)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(search);
    Argument.That.NotNull(equality);

    return SequenceFindLastCore(list, index, list.Count - index, search, partial, equality);
  }

  public static int SequenceFindLast<T>(this IList<T> list, int index, int count, IList<T> search, bool partial, Equality<T> equality)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(search);
    Argument.That.NotNull(equality);

    return SequenceFindLastCore(list, index, count, search, partial, equality);
  }

  public static int SequenceFindLast<T>(this IList<T> list, (int index, int count) range, IList<T> search, bool partial, Equality<T> equality)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(search);
    Argument.That.NotNull(equality);

    return SequenceFindLastCore(list, range.index, range.count, search, partial, equality);
  }

  private static int SequenceFindCore<T>(IList<T> list, int index, int count, int search, bool partial, ElementPredicate<T> predicate)
  {
    int matched = 0;
    while (count > 0 && matched < count && matched < search && (partial || count >= search))
    {
      if (predicate(list[index + matched], matched))
        matched++;
      else
      {
        matched = 0;
        count--;
        index++;
      }
    }
    return matched == search ? index : -matched - 1;
  }

  public static int SequenceFind<T>(this IList<T> list, int search, bool partial, ElementPredicate<T> predicate)
  {
    Argument.That.NotNull(list);
    Argument.That.Positive(search);
    Argument.That.NotNull(predicate);

    return SequenceFindCore(list, 0, list.Count, search, partial, predicate);
  }

  public static int SequenceFind<T>(this IList<T> list, int index, int search, bool partial, ElementPredicate<T> predicate)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.Positive(search);
    Argument.That.NotNull(predicate);

    return SequenceFindCore(list, index, list.Count - index, search, partial, predicate);
  }

  public static int SequenceFind<T>(this IList<T> list, int index, int count, int search, bool partial, ElementPredicate<T> predicate)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.Positive(search);
    Argument.That.NotNull(predicate);

    return SequenceFindCore(list, index, count, search, partial, predicate);
  }

  public static int SequenceFind<T>(this IList<T> list, (int index, int count) range, int search, bool partial, ElementPredicate<T> predicate)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.Positive(search);
    Argument.That.NotNull(predicate);

    return SequenceFindCore(list, range.index, range.count, search, partial, predicate);
  }

  private static int SequenceFindLastCore<T>(IList<T> list, int index, int count, int search, bool partial, ElementPredicate<T> predicate)
  {
    int matched = 0;
    while (count > 0 && matched < count && matched < search && (partial || count >= search))
    {
      if (predicate(list[index + count - 1 - matched], search - 1 - matched))
        matched++;
      else
      {
        matched = 0;
        count--;
      }
    }
    return matched == search ? index + count - matched : -matched - 1;
  }

  public static int SequenceFindLast<T>(this IList<T> list, int search, bool partial, ElementPredicate<T> predicate)
  {
    Argument.That.NotNull(list);
    Argument.That.Positive(search);
    Argument.That.NotNull(predicate);

    return SequenceFindLastCore(list, 0, list.Count, search, partial, predicate);
  }

  public static int SequenceFindLast<T>(this IList<T> list, int index, int search, bool partial, ElementPredicate<T> predicate)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.Positive(search);
    Argument.That.NotNull(predicate);

    return SequenceFindLastCore(list, index, list.Count - index, search, partial, predicate);
  }

  public static int SequenceFindLast<T>(this IList<T> list, int index, int count, int search, bool partial, ElementPredicate<T> predicate)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.Positive(search);
    Argument.That.NotNull(predicate);

    return SequenceFindLastCore(list, index, count, search, partial, predicate);
  }

  public static int SequenceFindLast<T>(this IList<T> list, (int index, int count) range, int search, bool partial, ElementPredicate<T> predicate)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.Positive(search);
    Argument.That.NotNull(predicate);

    return SequenceFindLastCore(list, range.index, range.count, search, partial, predicate);
  }

  #endregion
  #region SequenceCompare

  private static int SequenceCompareCore<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, Comparison<T> comparison, RelativeOrder emptyOrder)
  {
    var result = 0;
    var offset = 0;
    for (; offset < count && xIndex < xList.Count && yIndex < yList.Count; offset++, xIndex++, yIndex++)
      if ((result = comparison(xList[xIndex], yList[yIndex])) != 0)
        break;
    return Comparison.Result(result, offset, xIndex < xList.Count, yIndex < yList.Count, emptyOrder);
  }

  public static int SequenceCompare<T>(this IList<T> xList, IList<T> yList)
  {
    Argument.That.NotNull(xList);
    Argument.That.NotNull(yList);

    return xList.SequenceCompareCore(0, yList, 0, int.MaxValue, Comparer<T>.Default.Compare, RelativeOrder.Lower);
  }

  public static int SequenceCompare<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex)
  {
    Argument.That.InRangeOut(xList, xIndex);
    Argument.That.InRangeOut(yList, yIndex);

    return xList.SequenceCompareCore(xIndex, yList, yIndex, int.MaxValue, Comparer<T>.Default.Compare, RelativeOrder.Lower);
  }

  public static int SequenceCompare<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, int count)
  {
    Argument.That.InRangeOut(xList, xIndex, count);
    Argument.That.InRangeOut(yList, yIndex, count);

    return xList.SequenceCompareCore(xIndex, yList, yIndex, count, Comparer<T>.Default.Compare, RelativeOrder.Lower);
  }

  public static int SequenceCompare<T>(this IList<T> xList, IList<T> yList, Comparison<T>? comparison)
  {
    Argument.That.NotNull(xList);
    Argument.That.NotNull(yList);

    return xList.SequenceCompareCore(0, yList, 0, int.MaxValue, comparison ?? Comparer<T>.Default.Compare, RelativeOrder.Lower);
  }

  public static int SequenceCompare<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, Comparison<T>? comparison)
  {
    Argument.That.InRangeOut(xList, xIndex);
    Argument.That.InRangeOut(yList, yIndex);

    return xList.SequenceCompareCore(xIndex, yList, yIndex, int.MaxValue, comparison ?? Comparer<T>.Default.Compare, RelativeOrder.Lower);
  }

  public static int SequenceCompare<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, Comparison<T> comparison)
  {
    Argument.That.InRangeOut(xList, xIndex, count);
    Argument.That.InRangeOut(yList, yIndex, count);

    return xList.SequenceCompareCore(xIndex, yList, yIndex, count, comparison ?? Comparer<T>.Default.Compare, RelativeOrder.Lower);
  }

  public static int SequenceCompare<T>(this IList<T> xList, IList<T> yList, IComparer<T>? comparer)
  {
    Argument.That.NotNull(xList);
    Argument.That.NotNull(yList);

    return xList.SequenceCompareCore(0, yList, 0, int.MaxValue, (comparer ?? Comparer<T>.Default).Compare, RelativeOrder.Lower);
  }

  public static int SequenceCompare<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, IComparer<T>? comparer)
  {
    Argument.That.InRangeOut(xList, xIndex);
    Argument.That.InRangeOut(yList, yIndex);

    return xList.SequenceCompareCore(xIndex, yList, yIndex, int.MaxValue, (comparer ?? Comparer<T>.Default).Compare, RelativeOrder.Lower);
  }

  public static int SequenceCompare<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, IComparer<T>? comparer)
  {
    Argument.That.InRangeOut(xList, xIndex, count);
    Argument.That.InRangeOut(yList, yIndex, count);

    return xList.SequenceCompareCore(xIndex, yList, yIndex, count, (comparer ?? Comparer<T>.Default).Compare, RelativeOrder.Lower);
  }

  public static int SequenceCompare<T>(this IList<T> xList, IList<T> yList, RelativeOrder emptyOrder)
  {
    Argument.That.NotNull(xList);
    Argument.That.NotNull(yList);

    return xList.SequenceCompareCore(0, yList, 0, int.MaxValue, Comparer<T>.Default.Compare, emptyOrder);
  }

  public static int SequenceCompare<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, RelativeOrder emptyOrder)
  {
    Argument.That.InRangeOut(xList, xIndex);
    Argument.That.InRangeOut(yList, yIndex);

    return xList.SequenceCompareCore(xIndex, yList, yIndex, int.MaxValue, Comparer<T>.Default.Compare, emptyOrder);
  }

  public static int SequenceCompare<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, RelativeOrder emptyOrder)
  {
    Argument.That.InRangeOut(xList, xIndex, count);
    Argument.That.InRangeOut(yList, yIndex, count);

    return xList.SequenceCompareCore(xIndex, yList, yIndex, count, Comparer<T>.Default.Compare, emptyOrder);
  }

  public static int SequenceCompare<T>(this IList<T> xList, IList<T> yList, Comparison<T>? comparison, RelativeOrder emptyOrder)
  {
    Argument.That.NotNull(xList);
    Argument.That.NotNull(yList);

    return xList.SequenceCompareCore(0, yList, 0, int.MaxValue, comparison ?? Comparer<T>.Default.Compare, emptyOrder);
  }

  public static int SequenceCompare<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, Comparison<T>? comparison, RelativeOrder emptyOrder)
  {
    Argument.That.InRangeOut(xList, xIndex);
    Argument.That.InRangeOut(yList, yIndex);

    return xList.SequenceCompareCore(xIndex, yList, yIndex, int.MaxValue, comparison ?? Comparer<T>.Default.Compare, emptyOrder);
  }

  public static int SequenceCompare<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, Comparison<T> comparison, RelativeOrder emptyOrder)
  {
    Argument.That.InRangeOut(xList, xIndex, count);
    Argument.That.InRangeOut(yList, yIndex, count);

    return xList.SequenceCompareCore(xIndex, yList, yIndex, count, comparison ?? Comparer<T>.Default.Compare, emptyOrder);
  }

  public static int SequenceCompare<T>(this IList<T> xList, IList<T> yList, IComparer<T>? comparer, RelativeOrder emptyOrder)
  {
    Argument.That.NotNull(xList);
    Argument.That.NotNull(yList);

    return xList.SequenceCompareCore(0, yList, 0, int.MaxValue, (comparer ?? Comparer<T>.Default).Compare, emptyOrder);
  }

  public static int SequenceCompare<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, IComparer<T>? comparer, RelativeOrder emptyOrder)
  {
    Argument.That.InRangeOut(xList, xIndex);
    Argument.That.InRangeOut(yList, yIndex);

    return xList.SequenceCompareCore(xIndex, yList, yIndex, int.MaxValue, (comparer ?? Comparer<T>.Default).Compare, emptyOrder);
  }

  public static int SequenceCompare<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, IComparer<T>? comparer, RelativeOrder emptyOrder)
  {
    Argument.That.InRangeOut(xList, xIndex, count);
    Argument.That.InRangeOut(yList, yIndex, count);

    return xList.SequenceCompareCore(xIndex, yList, yIndex, count, (comparer ?? Comparer<T>.Default).Compare, emptyOrder);
  }

  #endregion
  #region SequenceEqual

  private static bool SequenceEqualCore<T>(IList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, Equality<T> equality)
  {
    var xCount = Comparable.Min(xList.Count - xIndex, count);
    var yCount = Comparable.Min(yList.Count - yIndex, count);
    if (xCount != yCount)
      return false;
    bool equals = true;
    for (; equals && count > 0 && xIndex < xList.Count && yIndex < yList.Count; count--, xIndex++, yIndex++)
      equals = equality(xList[xIndex], yList[yIndex]);
    return equals;
  }

  public static bool SequenceEqual<T>(this IList<T> xList, IList<T> yList)
  {
    Argument.That.NotNull(xList);
    Argument.That.NotNull(yList);

    return SequenceEqualCore(xList, 0, yList, 0, int.MaxValue, EqualityComparer<T>.Default.Equals);
  }

  public static bool SequenceEqual<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex)
  {
    Argument.That.InRangeOut(xList, xIndex);
    Argument.That.InRangeOut(yList, yIndex);

    return SequenceEqualCore(xList, xIndex, yList, yIndex, int.MaxValue, EqualityComparer<T>.Default.Equals);
  }

  public static bool SequenceEqual<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, int count)
  {
    Argument.That.InRangeOut(xList, xIndex, count);
    Argument.That.InRangeOut(yList, yIndex, count);

    return SequenceEqualCore(xList, xIndex, yList, yIndex, count, EqualityComparer<T>.Default.Equals);
  }

  public static bool SequenceEqual<T>(this IList<T> xList, IList<T> yList, Equality<T> equality)
  {
    Argument.That.NotNull(xList);
    Argument.That.NotNull(yList);

    return SequenceEqualCore(xList, 0, yList, 0, int.MaxValue, equality ?? EqualityComparer<T>.Default.Equals);
  }

  public static bool SequenceEqual<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, Equality<T>? equality)
  {
    Argument.That.InRangeOut(xList, xIndex);
    Argument.That.InRangeOut(yList, yIndex);

    return SequenceEqualCore(xList, xIndex, yList, yIndex, int.MaxValue, equality ?? EqualityComparer<T>.Default.Equals);
  }

  public static bool SequenceEqual<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, Equality<T>? equality)
  {
    Argument.That.InRangeOut(xList, xIndex, count);
    Argument.That.InRangeOut(yList, yIndex, count);

    return SequenceEqualCore(xList, xIndex, yList, yIndex, count, equality ?? EqualityComparer<T>.Default.Equals);
  }

  public static bool SequenceEqual<T>(this IList<T> xList, IList<T> yList, IEqualityComparer<T>? equalityComparer)
  {
    Argument.That.NotNull(xList);
    Argument.That.NotNull(yList);

    return SequenceEqualCore(xList, 0, yList, 0, int.MaxValue, (equalityComparer ?? EqualityComparer<T>.Default).Equals);
  }

  public static bool SequenceEqual<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, IEqualityComparer<T>? equalityComparer)
  {
    Argument.That.InRangeOut(xList, xIndex);
    Argument.That.InRangeOut(yList, yIndex);

    return SequenceEqualCore(xList, xIndex, yList, yIndex, int.MaxValue, (equalityComparer ?? EqualityComparer<T>.Default).Equals);
  }

  public static bool SequenceEqual<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, IEqualityComparer<T>? equalityComparer)
  {
    Argument.That.InRangeOut(xList, xIndex, count);
    Argument.That.InRangeOut(yList, yIndex, count);

    return SequenceEqualCore(xList, xIndex, yList, yIndex, count, (equalityComparer ?? EqualityComparer<T>.Default).Equals);
  }

  #endregion
  #endregion
  #region Cast methods

  public static IList<T> AsList<T>(this IList<T> list)
    => list;

#if !NET7_0_OR_GREATER

  public static IReadOnlyList<T> AsReadOnly<T>(this IList<T> list)
    => Argument.That.NotNull(list) as IReadOnlyList<T> ?? new ReadOnlyCollection<T>(list);

#endif

  #endregion
}

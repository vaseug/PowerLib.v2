using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PowerLib.System.Buffers;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Generic.Extensions;

public static class ReadOnlyListExtension
{
  private const int InvalidIndex = -1;

  #region Internal methods

  private static ElementPredicate<T> ElementPredicate<T>(Predicate<T> predicate)
    => (item, index) => predicate(item);

  private static int ExistingBoundIndex(int count, Bound bound)
    => count == 0 ? InvalidIndex : bound switch { Bound.Lower => 0, Bound.Upper => count - 1, _ => Argument.That.Invalid(bound) };

  #endregion
  #region Manipulation methods
  #region Get

  public static TSource GetAt<TSource>(this IReadOnlyList<TSource> list, int index)
  {
    Argument.That.InRangeIn(list, index);

    return list[index];
  }

  public static TSource GetBound<TSource>(this IReadOnlyList<TSource> list, Bound bound)
  {
    Argument.That.NotEmpty(list);

    return list[ExistingBoundIndex(list.Count, bound)];
  }

  public static TSource GetFirst<TSource>(this IReadOnlyList<TSource> list)
    => list.GetBound(Bound.Lower);

  public static TSource GetLast<TSource>(this IReadOnlyList<TSource> list)
    => list.GetBound(Bound.Upper);

  public static bool TryGetBound<TSource>(this IReadOnlyList<TSource> list, Bound bound, out TSource? result)
  {
    Argument.That.NotNull(list);

    var isEmpty = list.Count == 0;
    result = isEmpty ? default : list[ExistingBoundIndex(list.Count, bound)];
    return !isEmpty;
  }

  public static bool TryGetFirst<TSource>(this IReadOnlyList<TSource> list, out TSource? result)
    => list.TryGetBound(Bound.Lower, out result);

  public static bool TryGetLast<TSource>(this IReadOnlyList<TSource> list, out TSource? result)
    => list.TryGetBound(Bound.Upper, out result);

  private static IReadOnlyList<TSource> GetRangeCore<TSource>(IReadOnlyList<TSource> list, int index, int count)
  {
    var resultList = CollectionsController.CreateList<TSource>(count);
    for (; count > 0; count--)
      resultList.Add(list[index++]);
    return resultList.AsReadOnlyList();
  }

  public static IReadOnlyList<TSource> GetRange<TSource>(this IReadOnlyList<TSource> list, int index, int count)
  {
    Argument.That.InRangeOut(list, index, count);

    return list switch
    {
      List<TSource> fwList => fwList.GetRange(index, count),
      _ => GetRangeCore(list, index, count),
    };
  }

  public static IReadOnlyList<TSource> GetRange<TSource>(this IReadOnlyList<TSource> list, (int index, int count) range)
  {
    Argument.That.InRangeOut(list, range);

    return list switch
    {
      List<TSource> fwList => fwList.GetRange(range.index, range.count),
      _ => GetRangeCore(list, range.index, range.count),
    };
  }


  #endregion
  #endregion
  #region Extraction methods
  #region Enumerate

  private static IEnumerable<TSource> EnumerateCore<TSource>(IReadOnlyList<TSource> list, int index, int count, bool reverse)
  {
    for (index += reverse && count > 0 ? count - 1 : 0; count > 0; index += reverse ? -1 : 1, count--)
      yield return list[index];
  }

  public static IEnumerable<TSource> Enumerate<TSource>(this IReadOnlyList<TSource> list, bool reverse = false)
  {
    Argument.That.NotNull(list);

    return EnumerateCore(list, 0, list.Count, reverse);
  }

  public static IEnumerable<TSource> Enumerate<TSource>(this IReadOnlyList<TSource> list, int index, bool reverse = false)
  {
    Argument.That.InRangeOut(list, index);

    return EnumerateCore(list, index, list.Count - index, reverse);
  }

  public static IEnumerable<TSource> Enumerate<TSource>(this IReadOnlyList<TSource> list, int index, int count, bool reverse = false)
  {
    Argument.That.InRangeOut(list, index, count);

    return EnumerateCore(list, index, count, reverse);
  }

  public static IEnumerable<TSource> Enumerate<TSource>(this IReadOnlyList<TSource> list, (int index, int count) range, bool reverse = false)
  {
    Argument.That.InRangeOut(list, range);

    return EnumerateCore(list, range.index, range.count, reverse);
  }

  #endregion
  #region Copy

  private static void CopyCore<TSource>(IReadOnlyList<TSource> srcList, int srcIndex, IList<TSource> dstList, int dstIndex, int count, bool reverse)
  {
    for (srcIndex += reverse && count > 0 ? count - 1 : 0, dstIndex += reverse && count > 0 ? count - 1 : 0;
      count > 0; srcIndex += reverse ? -1 : 1, dstIndex += reverse ? -1 : 1, count--)
      dstList[dstIndex] = srcList[srcIndex];
  }

  public static void Copy<TSource>(this IReadOnlyList<TSource> srcList, IList<TSource> dstList, bool reverse = false)
  {
    Argument.That.NotNull(srcList);
    Argument.That.InRangeOut(dstList, 0, srcList.Count);

    CopyCore(srcList, 0, dstList, 0, srcList.Count, reverse);
  }

  public static void Copy<TSource>(this IReadOnlyList<TSource> srcList, IList<TSource> dstList, int dstIndex, int srcIndex, bool reverse = false)
  {
    Argument.That.InRangeOut(srcList, srcIndex);
    var count = Argument.That.InRangeOut(dstList, dstIndex, srcList.Count - srcIndex).count;

    CopyCore(srcList, srcIndex, dstList, dstIndex, count, reverse);
  }

  public static void Copy<TSource>(this IReadOnlyList<TSource> srcList, IList<TSource> dstList, int dstIndex, int srcIndex, int count, bool reverse = false)
  {
    Argument.That.InRangeOut(srcList, srcIndex, count);
    Argument.That.InRangeOut(dstList, dstIndex, count);

    CopyCore(srcList, srcIndex, dstList, dstIndex, count, reverse);
  }

  public static void Copy<TSource>(this IReadOnlyList<TSource> srcList, IList<TSource> dstList, int dstIndex, (int index, int count) srcRange, bool reverse = false)
  {
    Argument.That.InRangeOut(srcList, srcRange);
    Argument.That.InRangeOut(dstList, dstIndex, srcRange.count);

    CopyCore(srcList, srcRange.index, dstList, dstIndex, srcRange.count, reverse);
  }

  #endregion
  #endregion
  #region Retrieval methods
  #region Match

  private static bool MatchCore<T>(IReadOnlyList<T> list, int index, int count, ElementPredicate<T> predicate, bool all)
  {
    bool result = all;
    for (; count > 0 && result ^ !all; index++, count--)
      result = predicate(list[index], index);
    return result;
  }

  public static bool Match<T>(this IReadOnlyList<T> list, Predicate<T> predicate, bool all)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(predicate);

    return MatchCore(list, 0, list.Count, ElementPredicate(predicate), all);
  }

  public static bool Match<T>(this IReadOnlyList<T> list, int index, Predicate<T> predicate, bool all)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(predicate);

    return MatchCore(list, index, list.Count - index, ElementPredicate(predicate), all);
  }

  public static bool Match<T>(this IReadOnlyList<T> list, int index, int count, Predicate<T> predicate, bool all)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(predicate);

    return MatchCore(list, index, count, ElementPredicate(predicate), all);
  }

  public static bool Match<T>(this IReadOnlyList<T> list, (int index, int count) range, Predicate<T> predicate, bool all)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(predicate);

    return MatchCore(list, range.index, range.count, ElementPredicate(predicate), all);
  }

  public static bool Match<T>(this IReadOnlyList<T> list, ElementPredicate<T> predicate, bool all)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(predicate);

    return MatchCore(list, 0, list.Count, predicate, all);
  }

  public static bool Match<T>(this IReadOnlyList<T> list, int index, ElementPredicate<T> predicate, bool all)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(predicate);

    return MatchCore(list, index, list.Count - index, predicate, all);
  }

  public static bool Match<T>(this IReadOnlyList<T> list, int index, int count, ElementPredicate<T> predicate, bool all)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(predicate);

    return MatchCore(list, index, count, predicate, all);
  }

  public static bool Match<T>(this IReadOnlyList<T> list, (int index, int count) range, ElementPredicate<T> predicate, bool all)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(predicate);

    return MatchCore(list, range.index, range.count, predicate, all);
  }

  public static bool MatchAny<T>(this IReadOnlyList<T> list, Predicate<T> predicate)
    => list.Match(predicate, false);

  public static bool MatchAny<T>(this IReadOnlyList<T> list, int index, Predicate<T> predicate)
    => list.Match(index, predicate, false);

  public static bool MatchAny<T>(this IReadOnlyList<T> list, int index, int count, Predicate<T> predicate)
    => list.Match(index, count, predicate, false);

  public static bool MatchAny<T>(this IReadOnlyList<T> list, (int index, int count) range, Predicate<T> predicate)
    => list.Match(range, predicate, false);

  public static bool MatchAny<T>(this IReadOnlyList<T> list, ElementPredicate<T> predicate)
    => list.Match(predicate, false);

  public static bool MatchAny<T>(this IReadOnlyList<T> list, int index, ElementPredicate<T> predicate)
    => list.Match(index, predicate, false);

  public static bool MatchAny<T>(this IReadOnlyList<T> list, int index, int count, ElementPredicate<T> predicate)
    => list.Match(index, count, predicate, false);

  public static bool MatchAny<T>(this IReadOnlyList<T> list, (int index, int count) range, ElementPredicate<T> predicate)
    => list.Match(range, predicate, false);

  public static bool MatchAll<T>(this IReadOnlyList<T> list, Predicate<T> predicate)
    => list.Match(predicate, true);

  public static bool MatchAll<T>(this IReadOnlyList<T> list, int index, Predicate<T> predicate)
    => list.Match(index, predicate, true);

  public static bool MatchAll<T>(this IReadOnlyList<T> list, int index, int count, Predicate<T> predicate)
    => list.Match(index, count, predicate, true);

  public static bool MatchAll<T>(this IReadOnlyList<T> list, (int index, int count) range, Predicate<T> predicate)
    => list.Match(range, predicate, true);

  public static bool MatchAll<T>(this IReadOnlyList<T> list, ElementPredicate<T> predicate)
    => list.Match(predicate, true);

  public static bool MatchAll<T>(this IReadOnlyList<T> list, int index, ElementPredicate<T> predicate)
    => list.Match(index, predicate, true);

  public static bool MatchAll<T>(this IReadOnlyList<T> list, int index, int count, ElementPredicate<T> predicate)
    => list.Match(index, count, predicate, true);

  public static bool MatchAll<T>(this IReadOnlyList<T> list, (int index, int count) range, ElementPredicate<T> predicate)
    => list.Match(range, predicate, true);

  #endregion
  #region FindIndex

  private static int FindIndexCore<T>(IReadOnlyList<T> list, int index, int count, ElementPredicate<T> predicate)
  {
    for (; count > 0; index++, count--)
      if (predicate(list[index], index))
        return index;
    return InvalidIndex;
  }

  public static int FindIndex<T>(this IReadOnlyList<T> list, Predicate<T> predicate)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(predicate);

    return FindIndexCore(list, 0, list.Count, ElementPredicate(predicate));
  }

  public static int FindIndex<T>(this IReadOnlyList<T> list, int index, Predicate<T> predicate)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(predicate);

    return FindIndexCore(list, index, list.Count - index, ElementPredicate(predicate));
  }

  public static int FindIndex<T>(this IReadOnlyList<T> list, int index, int count, Predicate<T> predicate)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(predicate);

    return FindIndexCore(list, index, count, ElementPredicate(predicate));
  }

  public static int FindIndex<T>(this IReadOnlyList<T> list, (int index, int count) range, Predicate<T> predicate)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(predicate);

    return FindIndexCore(list, range.index, range.count, ElementPredicate(predicate));
  }

  public static int FindIndex<T>(this IReadOnlyList<T> list, ElementPredicate<T> predicate)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(predicate);

    return FindIndexCore(list, 0, list.Count, predicate);
  }

  public static int FindIndex<T>(this IReadOnlyList<T> list, int index, ElementPredicate<T> predicate)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(predicate);

    return FindIndexCore(list, index, list.Count - index, predicate);
  }

  public static int FindIndex<T>(this IReadOnlyList<T> list, int index, int count, ElementPredicate<T> predicate)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(predicate);

    return FindIndexCore(list, index, count, predicate);
  }

  public static int FindIndex<T>(this IReadOnlyList<T> list, (int index, int count) range, ElementPredicate<T> predicate)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(predicate);

    return FindIndexCore(list, range.index, range.count, predicate);
  }

  #endregion
  #region FindLastIndex

  private static int FindLastIndexCore<T>(IReadOnlyList<T> list, int index, int count, ElementPredicate<T> predicate)
  {
    for (; count > 0 && index >= 0; index--, count--)
      if (predicate(list[index], index))
        return index;
    return InvalidIndex;
  }

  public static int FindLastIndex<T>(this IReadOnlyList<T> list, Predicate<T> predicate)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(predicate);

    return FindLastIndexCore(list, list.Count == 0 ? 0 : list.Count - 1, list.Count, ElementPredicate(predicate));
  }

  public static int FindLastIndex<T>(this IReadOnlyList<T> list, int index, Predicate<T> predicate)
  {
    Argument.That.InRangeIn(list, index);
    Argument.That.NotNull(predicate);

    return FindLastIndexCore(list, index, index + 1, ElementPredicate(predicate));
  }

  public static int FindLastIndex<T>(this IReadOnlyList<T> list, int index, int count, Predicate<T> predicate)
  {
    Argument.That.InRangeRev(list, index, count);
    Argument.That.NotNull(predicate);

    return FindLastIndexCore(list, index, count, ElementPredicate(predicate));
  }

  public static int FindLastIndex<T>(this IReadOnlyList<T> list, (int index, int count) range, Predicate<T> predicate)
  {
    Argument.That.InRangeRev(list, range);
    Argument.That.NotNull(predicate);

    return FindLastIndexCore(list, range.index, range.count, ElementPredicate(predicate));
  }

  public static int FindLastIndex<T>(this IReadOnlyList<T> list, ElementPredicate<T> predicate)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(predicate);

    return FindLastIndexCore(list, list.Count == 0 ? 0 : list.Count - 1, list.Count, predicate);
  }

  public static int FindLastIndex<T>(this IReadOnlyList<T> list, int index, ElementPredicate<T> predicate)
  {
    Argument.That.InRangeIn(list, index);
    Argument.That.NotNull(predicate);

    return FindLastIndexCore(list, index, index + 1, predicate);
  }

  public static int FindLastIndex<T>(this IReadOnlyList<T> list, int index, int count, ElementPredicate<T> predicate)
  {
    Argument.That.InRangeRev(list, index, count);
    Argument.That.NotNull(predicate);

    return FindLastIndexCore(list, index, count, predicate);
  }

  public static int FindLastIndex<T>(this IReadOnlyList<T> list, (int index, int count) range, ElementPredicate<T> predicate)
  {
    Argument.That.InRangeRev(list, range);
    Argument.That.NotNull(predicate);

    return FindLastIndexCore(list, range.index, range.count, predicate);
  }

  #endregion
  #region FindAll

  private static IEnumerable<T> FindAllCore<T>(IReadOnlyList<T> list, int index, int count, ElementPredicate<T> predicate, bool reverse)
  {
    for (index += reverse && count > 0 ? count - 1 : 0; count > 0; index += reverse ? -1 : 1, count--)
      if (predicate(list[index], index))
        yield return list[index];
  }

  public static IEnumerable<T> FindAll<T>(this IReadOnlyList<T> list, Predicate<T> predicate, bool reverse = false)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(predicate);

    return FindAllCore(list, 0, list.Count, ElementPredicate(predicate), reverse);
  }

  public static IEnumerable<T> FindAll<T>(this IReadOnlyList<T> list, int index, Predicate<T> predicate, bool reverse = false)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(predicate);

    return FindAllCore(list, index, list.Count - index, ElementPredicate(predicate), reverse);
  }

  public static IEnumerable<T> FindAll<T>(this IReadOnlyList<T> list, int index, int count, Predicate<T> predicate, bool reverse = false)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(predicate);

    return FindAllCore(list, index, count, ElementPredicate(predicate), reverse);
  }

  public static IEnumerable<T> FindAll<T>(this IReadOnlyList<T> list, (int index, int count) range, Predicate<T> predicate, bool reverse = false)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(predicate);

    return FindAllCore(list, range.index, range.count, ElementPredicate(predicate), reverse);
  }

  public static IEnumerable<T> FindAll<T>(this IReadOnlyList<T> list, ElementPredicate<T> predicate, bool reverse = false)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(predicate);

    return FindAllCore(list, 0, list.Count, predicate, reverse);
  }

  public static IEnumerable<T> FindAll<T>(this IReadOnlyList<T> list, int index, ElementPredicate<T> predicate, bool reverse = false)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(predicate);

    return FindAllCore(list, index, list.Count - index, predicate, reverse);
  }

  public static IEnumerable<T> FindAll<T>(this IReadOnlyList<T> list, int index, int count, ElementPredicate<T> predicate, bool reverse = false)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(predicate);

    return FindAllCore(list, index, count, predicate, reverse);
  }

  public static IEnumerable<T> FindAll<T>(this IReadOnlyList<T> list, (int index, int count) range, ElementPredicate<T> predicate, bool reverse = false)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(predicate);

    return FindAllCore(list, range.index, range.count, predicate, reverse);
  }

  #endregion
  #region FindAllIndices

  private static IEnumerable<int> FindAllIndicesCore<T>(IReadOnlyList<T> list, int index, int count, ElementPredicate<T> predicate, bool reverse)
  {
    for (index += reverse && count > 0 ? count - 1 : 0; count > 0; index += reverse ? -1 : 1, count--)
      if (predicate(list[index], index))
        yield return index;
  }

  public static IEnumerable<int> FindAllIndices<T>(this IReadOnlyList<T> list, Predicate<T> predicate, bool reverse = false)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(predicate);

    return FindAllIndicesCore(list, 0, list.Count, ElementPredicate(predicate), reverse);
  }

  public static IEnumerable<int> FindAllIndices<T>(this IReadOnlyList<T> list, int index, Predicate<T> predicate, bool reverse = false)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(predicate);

    return FindAllIndicesCore(list, index, list.Count - index, ElementPredicate(predicate), reverse);
  }

  public static IEnumerable<int> FindAllIndices<T>(this IReadOnlyList<T> list, int index, int count, Predicate<T> predicate, bool reverse = false)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(predicate);

    return FindAllIndicesCore(list, index, count, ElementPredicate(predicate), reverse);
  }

  public static IEnumerable<int> FindAllIndices<T>(this IReadOnlyList<T> list, (int index, int count) range, Predicate<T> predicate, bool reverse = false)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(predicate);

    return FindAllIndicesCore(list, range.index, range.count, ElementPredicate(predicate), reverse);
  }

  public static IEnumerable<int> FindAllIndices<T>(this IReadOnlyList<T> list, ElementPredicate<T> predicate, bool reverse = false)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(predicate);

    return FindAllIndicesCore(list, 0, list.Count, predicate, reverse);
  }

  public static IEnumerable<int> FindAllIndices<T>(this IReadOnlyList<T> list, int index, ElementPredicate<T> predicate, bool reverse = false)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(predicate);

    return FindAllIndicesCore(list, index, list.Count - index, predicate, reverse);
  }

  public static IEnumerable<int> FindAllIndices<T>(this IReadOnlyList<T> list, int index, int count, ElementPredicate<T> predicate, bool reverse = false)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(predicate);

    return FindAllIndicesCore(list, index, count, predicate, reverse);
  }

  public static IEnumerable<int> FindAllIndices<T>(this IReadOnlyList<T> list, (int index, int count) range, ElementPredicate<T> predicate, bool reverse = false)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(predicate);

    return FindAllIndicesCore(list, range.index, range.count, predicate, reverse);
  }

  #endregion
  #region BinarySearch

  private static int BinarySearchCore<T>(IReadOnlyList<T> list, int index, int count, Comparator<T> comparator, SearchingOption option)
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

  public static int BinarySearch<T>(this IReadOnlyList<T> list, Comparator<T> comparator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(comparator);

    return BinarySearchCore(list, 0, list.Count, comparator, option);
  }

  public static int BinarySearch<T>(this IReadOnlyList<T> list, int index, Comparator<T> comparator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(comparator);

    return BinarySearchCore(list, index, list.Count - index, comparator, option);
  }

  public static int BinarySearch<T>(this IReadOnlyList<T> list, int index, int count, Comparator<T> comparator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(comparator);

    return BinarySearchCore(list, index, count, comparator, option);
  }

  public static int BinarySearch<T>(this IReadOnlyList<T> list, (int index, int count) range, Comparator<T> comparator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(comparator);

    return BinarySearchCore(list, range.index, range.count, comparator, option);
  }

  public static int BinarySearch<T>(this IReadOnlyList<T> list, IComparator<T> comparator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(comparator);

    return BinarySearchCore(list, 0, list.Count, comparator.Compare, option);
  }

  public static int BinarySearch<T>(this IReadOnlyList<T> list, int index, IComparator<T> comparator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(comparator);

    return BinarySearchCore(list, index, list.Count - index, comparator.Compare, option);
  }

  public static int BinarySearch<T>(this IReadOnlyList<T> list, int index, int count, IComparator<T> comparator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(comparator);

    return BinarySearchCore(list, index, count, comparator.Compare, option);
  }

  public static int BinarySearch<T>(this IReadOnlyList<T> list, (int index, int count) range, IComparator<T> comparator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(comparator);

    return BinarySearchCore(list, range.index, range.count, comparator.Compare, option);
  }

  #endregion
  #region InterpolationSearch

  private static int InterpolationSearchCore<T>(IReadOnlyList<T> list, int index, int count, Comparator<T> comparator, Func<T, T, float> interpolator, SearchingOption option)
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

  public static int InterpolationSearch<T>(this IReadOnlyList<T> list, Comparator<T> comparator, Func<T, T, float> interpolator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(comparator);
    Argument.That.NotNull(interpolator);

    return InterpolationSearchCore(list, 0, list.Count, comparator, interpolator, option);
  }

  public static int InterpolationSearch<T>(this IReadOnlyList<T> list, int index, Comparator<T> comparator, Func<T, T, float> interpolator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(comparator);
    Argument.That.NotNull(interpolator);

    return InterpolationSearchCore(list, index, list.Count - index, comparator, interpolator, option);
  }

  public static int InterpolationSearch<T>(this IReadOnlyList<T> list, int index, int count, Comparator<T> comparator, Func<T, T, float> interpolator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(comparator);
    Argument.That.NotNull(interpolator);

    return InterpolationSearchCore(list, index, count, comparator, interpolator, option);
  }

  public static int InterpolationSearch<T>(this IReadOnlyList<T> list, (int index, int count) range, Comparator<T> comparator, Func<T, T, float> interpolator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(comparator);
    Argument.That.NotNull(interpolator);

    return InterpolationSearchCore(list, range.index, range.count, comparator, interpolator, option);
  }

  public static int InterpolationSearch<T>(this IReadOnlyList<T> list, IComparator<T> comparator, Func<T, T, float> interpolator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(comparator);
    Argument.That.NotNull(interpolator);

    return InterpolationSearchCore(list, 0, list.Count, comparator.Compare, interpolator, option);
  }

  public static int InterpolationSearch<T>(this IReadOnlyList<T> list, int index, IComparator<T> comparator, Func<T, T, float> interpolator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(comparator);
    Argument.That.NotNull(interpolator);

    return InterpolationSearchCore(list, index, list.Count - index, comparator.Compare, interpolator, option);
  }

  public static int InterpolationSearch<T>(this IReadOnlyList<T> list, int index, int count, IComparator<T> comparator, Func<T, T, float> interpolator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(comparator);
    Argument.That.NotNull(interpolator);

    return InterpolationSearchCore(list, index, count, comparator.Compare, interpolator, option);
  }

  public static int InterpolationSearch<T>(this IReadOnlyList<T> list, (int index, int count) range, IComparator<T> comparator, Func<T, T, float> interpolator, SearchingOption option = SearchingOption.None)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(comparator);
    Argument.That.NotNull(interpolator);

    return InterpolationSearchCore(list, range.index, range.count, comparator.Compare, interpolator, option);
  }

  #endregion
  #region SequenceFind

  private static int SequenceFindCore<T>(IReadOnlyList<T> list, int index, int count, IList<T> search, bool partial, Equality<T> equality)
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

  public static int SequenceFind<T>(this IReadOnlyList<T> list, IList<T> search, bool partial, Equality<T> equality)
    => list.SequenceFind(0, list is not null ? list.Count : 0, search, partial, equality);

  public static int SequenceFind<T>(this IReadOnlyList<T> list, int index, IList<T> search, bool partial, Equality<T> equality)
    => list.SequenceFind(index, list is not null ? list.Count - index : 0, search, partial, equality);

  public static int SequenceFind<T>(this IReadOnlyList<T> list, int index, int count, IList<T> search, bool partial, Equality<T> equality)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(search);
    Argument.That.NotNull(equality);

    return SequenceFindCore(list, index, count, search, partial, equality);
  }

  public static int SequenceFind<T>(this IReadOnlyList<T> list, (int index, int count) range, IList<T> search, bool partial, Equality<T> equality)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(search);
    Argument.That.NotNull(equality);

    return SequenceFindCore(list, range.index, range.count, search, partial, equality);
  }

  private static int SequenceFindLastCore<T>(IReadOnlyList<T> list, int index, int count, IList<T> search, bool partial, Equality<T> equality)
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

  public static int SequenceFindLast<T>(this IReadOnlyList<T> list, IList<T> search, bool partial, Equality<T> equality)
  {
    Argument.That.NotNull(list);
    Argument.That.NotNull(search);
    Argument.That.NotNull(equality);

    return SequenceFindLastCore(list, 0, list.Count, search, partial, equality);
  }

  public static int SequenceFindLast<T>(this IReadOnlyList<T> list, int index, IList<T> search, bool partial, Equality<T> equality)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.NotNull(search);
    Argument.That.NotNull(equality);

    return SequenceFindLastCore(list, index, list.Count - index, search, partial, equality);
  }

  public static int SequenceFindLast<T>(this IReadOnlyList<T> list, int index, int count, IList<T> search, bool partial, Equality<T> equality)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.NotNull(search);
    Argument.That.NotNull(equality);

    return SequenceFindLastCore(list, index, count, search, partial, equality);
  }

  public static int SequenceFindLast<T>(this IReadOnlyList<T> list, (int index, int count) range, IList<T> search, bool partial, Equality<T> equality)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.NotNull(search);
    Argument.That.NotNull(equality);

    return SequenceFindLastCore(list, range.index, range.count, search, partial, equality);
  }

  private static int SequenceFindCore<T>(IReadOnlyList<T> list, int index, int count, int search, bool partial, ElementPredicate<T> predicate)
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

  public static int SequenceFind<T>(this IReadOnlyList<T> list, int search, bool partial, ElementPredicate<T> predicate)
  {
    Argument.That.NotNull(list);
    Argument.That.Positive(search);
    Argument.That.NotNull(predicate);

    return SequenceFindCore(list, 0, list.Count, search, partial, predicate);
  }

  public static int SequenceFind<T>(this IReadOnlyList<T> list, int index, int search, bool partial, ElementPredicate<T> predicate)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.Positive(search);
    Argument.That.NotNull(predicate);

    return SequenceFindCore(list, index, list.Count - index, search, partial, predicate);
  }

  public static int SequenceFind<T>(this IReadOnlyList<T> list, int index, int count, int search, bool partial, ElementPredicate<T> predicate)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.Positive(search);
    Argument.That.NotNull(predicate);

    return SequenceFindCore(list, index, count, search, partial, predicate);
  }

  public static int SequenceFind<T>(this IReadOnlyList<T> list, (int index, int count) range, int search, bool partial, ElementPredicate<T> predicate)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.Positive(search);
    Argument.That.NotNull(predicate);

    return SequenceFindCore(list, range.index, range.count, search, partial, predicate);
  }

  private static int SequenceFindLastCore<T>(IReadOnlyList<T> list, int index, int count, int search, bool partial, ElementPredicate<T> predicate)
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

  public static int SequenceFindLast<T>(this IReadOnlyList<T> list, int search, bool partial, ElementPredicate<T> predicate)
  {
    Argument.That.NotNull(list);
    Argument.That.Positive(search);
    Argument.That.NotNull(predicate);

    return SequenceFindLastCore(list, 0, list.Count, search, partial, predicate);
  }

  public static int SequenceFindLast<T>(this IReadOnlyList<T> list, int index, int search, bool partial, ElementPredicate<T> predicate)
  {
    Argument.That.InRangeOut(list, index);
    Argument.That.Positive(search);
    Argument.That.NotNull(predicate);

    return SequenceFindLastCore(list, index, list.Count - index, search, partial, predicate);
  }

  public static int SequenceFindLast<T>(this IReadOnlyList<T> list, int index, int count, int search, bool partial, ElementPredicate<T> predicate)
  {
    Argument.That.InRangeOut(list, index, count);
    Argument.That.Positive(search);
    Argument.That.NotNull(predicate);

    return SequenceFindLastCore(list, index, count, search, partial, predicate);
  }

  public static int SequenceFindLast<T>(this IReadOnlyList<T> list, (int index, int count) range, int search, bool partial, ElementPredicate<T> predicate)
  {
    Argument.That.InRangeOut(list, range);
    Argument.That.Positive(search);
    Argument.That.NotNull(predicate);

    return SequenceFindLastCore(list, range.index, range.count, search, partial, predicate);
  }

  #endregion
  #region SequenceCompare

  private static int SequenceCompareCore<T>(this IReadOnlyList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, Comparison<T> comparison, RelativeOrder emptyOrder)
  {
    var result = 0;
    var offset = 0;
    for (; offset < count && xIndex < xList.Count && yIndex < yList.Count; offset++, xIndex++, yIndex++)
      if ((result = comparison(xList[xIndex], yList[yIndex])) != 0)
        break;
    return Comparison.Result(result, offset, xIndex < xList.Count, yIndex < yList.Count, emptyOrder);
  }

  public static int SequenceCompare<T>(this IReadOnlyList<T> xList, IList<T> yList)
  {
    Argument.That.NotNull(xList);
    Argument.That.NotNull(yList);

    return xList.SequenceCompareCore(0, yList, 0, int.MaxValue, Comparer<T>.Default.Compare, RelativeOrder.Lower);
  }

  public static int SequenceCompare<T>(this IReadOnlyList<T> xList, int xIndex, IList<T> yList, int yIndex)
  {
    Argument.That.InRangeOut(xList, xIndex);
    Argument.That.InRangeOut(yList, yIndex);

    return xList.SequenceCompareCore(xIndex, yList, yIndex, int.MaxValue, Comparer<T>.Default.Compare, RelativeOrder.Lower);
  }

  public static int SequenceCompare<T>(this IReadOnlyList<T> xList, int xIndex, IList<T> yList, int yIndex, int count)
  {
    Argument.That.InRangeOut(xList, xIndex, count);
    Argument.That.InRangeOut(yList, yIndex, count);

    return xList.SequenceCompareCore(xIndex, yList, yIndex, count, Comparer<T>.Default.Compare, RelativeOrder.Lower);
  }

  public static int SequenceCompare<T>(this IReadOnlyList<T> xList, IList<T> yList, Comparison<T>? comparison)
  {
    Argument.That.NotNull(xList);
    Argument.That.NotNull(yList);

    return xList.SequenceCompareCore(0, yList, 0, int.MaxValue, comparison ?? Comparer<T>.Default.Compare, RelativeOrder.Lower);
  }

  public static int SequenceCompare<T>(this IReadOnlyList<T> xList, int xIndex, IList<T> yList, int yIndex, Comparison<T>? comparison)
  {
    Argument.That.InRangeOut(xList, xIndex);
    Argument.That.InRangeOut(yList, yIndex);

    return xList.SequenceCompareCore(xIndex, yList, yIndex, int.MaxValue, comparison ?? Comparer<T>.Default.Compare, RelativeOrder.Lower);
  }

  public static int SequenceCompare<T>(this IReadOnlyList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, Comparison<T> comparison)
  {
    Argument.That.InRangeOut(xList, xIndex, count);
    Argument.That.InRangeOut(yList, yIndex, count);

    return xList.SequenceCompareCore(xIndex, yList, yIndex, count, comparison ?? Comparer<T>.Default.Compare, RelativeOrder.Lower);
  }

  public static int SequenceCompare<T>(this IReadOnlyList<T> xList, IList<T> yList, IComparer<T>? comparer)
  {
    Argument.That.NotNull(xList);
    Argument.That.NotNull(yList);

    return xList.SequenceCompareCore(0, yList, 0, int.MaxValue, (comparer ?? Comparer<T>.Default).Compare, RelativeOrder.Lower);
  }

  public static int SequenceCompare<T>(this IReadOnlyList<T> xList, int xIndex, IList<T> yList, int yIndex, IComparer<T>? comparer)
  {
    Argument.That.InRangeOut(xList, xIndex);
    Argument.That.InRangeOut(yList, yIndex);

    return xList.SequenceCompareCore(xIndex, yList, yIndex, int.MaxValue, (comparer ?? Comparer<T>.Default).Compare, RelativeOrder.Lower);
  }

  public static int SequenceCompare<T>(this IReadOnlyList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, IComparer<T>? comparer)
  {
    Argument.That.InRangeOut(xList, xIndex, count);
    Argument.That.InRangeOut(yList, yIndex, count);

    return xList.SequenceCompareCore(xIndex, yList, yIndex, count, (comparer ?? Comparer<T>.Default).Compare, RelativeOrder.Lower);
  }

  public static int SequenceCompare<T>(this IReadOnlyList<T> xList, IList<T> yList, RelativeOrder emptyOrder)
  {
    Argument.That.NotNull(xList);
    Argument.That.NotNull(yList);

    return xList.SequenceCompareCore(0, yList, 0, int.MaxValue, Comparer<T>.Default.Compare, emptyOrder);
  }

  public static int SequenceCompare<T>(this IReadOnlyList<T> xList, int xIndex, IList<T> yList, int yIndex, RelativeOrder emptyOrder)
  {
    Argument.That.InRangeOut(xList, xIndex);
    Argument.That.InRangeOut(yList, yIndex);

    return xList.SequenceCompareCore(xIndex, yList, yIndex, int.MaxValue, Comparer<T>.Default.Compare, emptyOrder);
  }

  public static int SequenceCompare<T>(this IReadOnlyList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, RelativeOrder emptyOrder)
  {
    Argument.That.InRangeOut(xList, xIndex, count);
    Argument.That.InRangeOut(yList, yIndex, count);

    return xList.SequenceCompareCore(xIndex, yList, yIndex, count, Comparer<T>.Default.Compare, emptyOrder);
  }

  public static int SequenceCompare<T>(this IReadOnlyList<T> xList, IList<T> yList, Comparison<T>? comparison, RelativeOrder emptyOrder)
  {
    Argument.That.NotNull(xList);
    Argument.That.NotNull(yList);

    return xList.SequenceCompareCore(0, yList, 0, int.MaxValue, comparison ?? Comparer<T>.Default.Compare, emptyOrder);
  }

  public static int SequenceCompare<T>(this IReadOnlyList<T> xList, int xIndex, IList<T> yList, int yIndex, Comparison<T>? comparison, RelativeOrder emptyOrder)
  {
    Argument.That.InRangeOut(xList, xIndex);
    Argument.That.InRangeOut(yList, yIndex);

    return xList.SequenceCompareCore(xIndex, yList, yIndex, int.MaxValue, comparison ?? Comparer<T>.Default.Compare, emptyOrder);
  }

  public static int SequenceCompare<T>(this IReadOnlyList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, Comparison<T> comparison, RelativeOrder emptyOrder)
  {
    Argument.That.InRangeOut(xList, xIndex, count);
    Argument.That.InRangeOut(yList, yIndex, count);

    return xList.SequenceCompareCore(xIndex, yList, yIndex, count, comparison ?? Comparer<T>.Default.Compare, emptyOrder);
  }

  public static int SequenceCompare<T>(this IReadOnlyList<T> xList, IList<T> yList, IComparer<T>? comparer, RelativeOrder emptyOrder)
  {
    Argument.That.NotNull(xList);
    Argument.That.NotNull(yList);

    return xList.SequenceCompareCore(0, yList, 0, int.MaxValue, (comparer ?? Comparer<T>.Default).Compare, emptyOrder);
  }

  public static int SequenceCompare<T>(this IReadOnlyList<T> xList, int xIndex, IList<T> yList, int yIndex, IComparer<T>? comparer, RelativeOrder emptyOrder)
  {
    Argument.That.InRangeOut(xList, xIndex);
    Argument.That.InRangeOut(yList, yIndex);

    return xList.SequenceCompareCore(xIndex, yList, yIndex, int.MaxValue, (comparer ?? Comparer<T>.Default).Compare, emptyOrder);
  }

  public static int SequenceCompare<T>(this IReadOnlyList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, IComparer<T>? comparer, RelativeOrder emptyOrder)
  {
    Argument.That.InRangeOut(xList, xIndex, count);
    Argument.That.InRangeOut(yList, yIndex, count);

    return xList.SequenceCompareCore(xIndex, yList, yIndex, count, (comparer ?? Comparer<T>.Default).Compare, emptyOrder);
  }

  #endregion
  #region SequenceEqual

  private static bool SequenceEqualCore<T>(IReadOnlyList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, Equality<T> equality)
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

  public static bool SequenceEqual<T>(this IReadOnlyList<T> xList, IList<T> yList)
  {
    Argument.That.NotNull(xList);
    Argument.That.NotNull(yList);

    return SequenceEqualCore(xList, 0, yList, 0, int.MaxValue, EqualityComparer<T>.Default.Equals);
  }

  public static bool SequenceEqual<T>(this IReadOnlyList<T> xList, int xIndex, IList<T> yList, int yIndex)
  {
    Argument.That.InRangeOut(xList, xIndex);
    Argument.That.InRangeOut(yList, yIndex);

    return SequenceEqualCore(xList, xIndex, yList, yIndex, int.MaxValue, EqualityComparer<T>.Default.Equals);
  }

  public static bool SequenceEqual<T>(this IReadOnlyList<T> xList, int xIndex, IList<T> yList, int yIndex, int count)
  {
    Argument.That.InRangeOut(xList, xIndex, count);
    Argument.That.InRangeOut(yList, yIndex, count);

    return SequenceEqualCore(xList, xIndex, yList, yIndex, count, EqualityComparer<T>.Default.Equals);
  }

  public static bool SequenceEqual<T>(this IReadOnlyList<T> xList, IList<T> yList, Equality<T> equality)
  {
    Argument.That.NotNull(xList);
    Argument.That.NotNull(yList);

    return SequenceEqualCore(xList, 0, yList, 0, int.MaxValue, equality ?? EqualityComparer<T>.Default.Equals);
  }

  public static bool SequenceEqual<T>(this IReadOnlyList<T> xList, int xIndex, IList<T> yList, int yIndex, Equality<T>? equality)
  {
    Argument.That.InRangeOut(xList, xIndex);
    Argument.That.InRangeOut(yList, yIndex);

    return SequenceEqualCore(xList, xIndex, yList, yIndex, int.MaxValue, equality ?? EqualityComparer<T>.Default.Equals);
  }

  public static bool SequenceEqual<T>(this IReadOnlyList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, Equality<T>? equality)
  {
    Argument.That.InRangeOut(xList, xIndex, count);
    Argument.That.InRangeOut(yList, yIndex, count);

    return SequenceEqualCore(xList, xIndex, yList, yIndex, count, equality ?? EqualityComparer<T>.Default.Equals);
  }

  public static bool SequenceEqual<T>(this IReadOnlyList<T> xList, IList<T> yList, IEqualityComparer<T>? equalityComparer)
  {
    Argument.That.NotNull(xList);
    Argument.That.NotNull(yList);

    return SequenceEqualCore(xList, 0, yList, 0, int.MaxValue, (equalityComparer ?? EqualityComparer<T>.Default).Equals);
  }

  public static bool SequenceEqual<T>(this IReadOnlyList<T> xList, int xIndex, IList<T> yList, int yIndex, IEqualityComparer<T>? equalityComparer)
  {
    Argument.That.InRangeOut(xList, xIndex);
    Argument.That.InRangeOut(yList, yIndex);

    return SequenceEqualCore(xList, xIndex, yList, yIndex, int.MaxValue, (equalityComparer ?? EqualityComparer<T>.Default).Equals);
  }

  public static bool SequenceEqual<T>(this IReadOnlyList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, IEqualityComparer<T>? equalityComparer)
  {
    Argument.That.InRangeOut(xList, xIndex, count);
    Argument.That.InRangeOut(yList, yIndex, count);

    return SequenceEqualCore(xList, xIndex, yList, yIndex, count, (equalityComparer ?? EqualityComparer<T>.Default).Equals);
  }

  #endregion
  #endregion
  #region Cast methods

  public static IReadOnlyList<T> AsReadOnlyList<T>(this IReadOnlyList<T> list)
    => list;

  #endregion
}

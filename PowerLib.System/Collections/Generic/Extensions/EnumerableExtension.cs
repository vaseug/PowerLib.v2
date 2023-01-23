using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Generic.Extensions;

public static class EnumerableExtension
{
  private const int InvalidCount = -1;

  #region Methods

  public static int CopyTo<T>(this IEnumerable<T> collection, T[] array, int arrayIndex)
  {
    Argument.That.NotNull(collection);
    Argument.That.InRangeOut(array, arrayIndex);

    if (arrayIndex == array.Length)
      return 0;
    using var e = collection.GetEnumerator();
    int count = 0;
    for (; arrayIndex < array.Length && e.MoveNext(); arrayIndex++, count++)
      array[arrayIndex] = e.Current;
    return count;
  }

  public static string Format<T>(this IEnumerable<T> collection, Func<T, string> itemFormatter, string itemDelimiter)
    => collection.Format((item, index) => itemFormatter(item), itemDelimiter);

  public static string Format<T>(this IEnumerable<T> collection, Func<T, int, string> itemFormatter, string itemDelimiter)
  {
    Argument.That.NotNull(itemFormatter);
    Argument.That.NotNull(collection);

    var stringBuilder = new StringBuilder();
    using var enumerator = collection.GetEnumerator();
    if (enumerator.MoveNext())
    {
      var index = 0;
      stringBuilder.Append(itemFormatter(enumerator.Current, index++));
      while (enumerator.MoveNext())
      {
        stringBuilder.Append(itemDelimiter);
        stringBuilder.Append(itemFormatter(enumerator.Current, index++));
      }
    }
    return stringBuilder.ToString();
  }

  public static int PeekCount<T>(this IEnumerable<T> collection)
    => Argument.That.NotNull(collection) switch
    {
      ICollection<T> coll => coll.Count,
      IReadOnlyCollection<T> coll => coll.Count,
      ICollection coll => coll.Count,
      _ => InvalidCount,
    };

  #endregion
  #region Cast methods

  public static IList<T> AsList<T>(this IEnumerable<T> collection)
    => Argument.That.NotNull(collection) as IList<T> ?? collection.ToList();

  public static IReadOnlyList<T> AsReadOnlyList<T>(this IEnumerable<T> collection)
    => Argument.That.NotNull(collection) as IReadOnlyList<T> ?? new ReadOnlyCollection<T>(collection as IList<T> ?? collection.ToList());

  public static ICollection<T> AsCollection<T>(this IEnumerable<T> collection)
    => Argument.That.NotNull(collection) as ICollection<T> ?? collection.ToList();

  public static IReadOnlyCollection<T> AsReadOnlyCollection<T>(this IEnumerable<T> collection)
    => Argument.That.NotNull(collection) as IReadOnlyCollection<T> ?? new ReadOnlyCollection<T>(collection as IList<T> ?? collection.ToList());

  public static IDictionary<TKey, TValue> AsDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> collection)
    where TKey : notnull
    => collection.AsDictionary(null);

  public static IDictionary<TKey, TValue> AsDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey>? equalityComparer)
    where TKey : notnull
  {
    return Argument.That.NotNull(collection) as IDictionary<TKey, TValue> ??
#if NETCOREAPP2_0_OR_GREATER
      new Dictionary<TKey, TValue>(collection, equalityComparer);
#else
      collection.ToDictionary(equalityComparer);
#endif
  }

  public static IReadOnlyDictionary<TKey, TValue> AsReadOnlyDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> collection)
    where TKey : notnull
  => Argument.That.NotNull(collection) as IReadOnlyDictionary<TKey, TValue> ?? new ReadOnlyDictionary<TKey, TValue>(collection as IDictionary<TKey, TValue> ??
#if NETCOREAPP2_0_OR_GREATER
      new Dictionary<TKey, TValue>(collection, null));
#else
      collection.ToDictionary(null));
#endif

  public static IReadOnlyDictionary<TKey, TValue> AsReadOnlyDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey>? equalityComparer)
    where TKey : notnull
    => Argument.That.NotNull(collection) as IReadOnlyDictionary<TKey, TValue> ?? new ReadOnlyDictionary<TKey, TValue>(collection as IDictionary<TKey, TValue> ??
#if NETCOREAPP2_0_OR_GREATER
      new Dictionary<TKey, TValue>(collection, equalityComparer));
#else
      collection.ToDictionary(equalityComparer));
#endif

  public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> collection)
    where TKey : notnull
    => collection.ToDictionary(null);

  public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey>? equalityComparer)
    where TKey : notnull
  {
    Argument.That.NotNull(collection);

    var count = collection.PeekCount();
    var dictionary = count < 0 ? new Dictionary<TKey, TValue>(equalityComparer) : new Dictionary<TKey, TValue>(count, equalityComparer);
    using var enumerator = collection.GetEnumerator();
    while (enumerator.MoveNext())
      dictionary.Add(enumerator.Current.Key, enumerator.Current.Value);
    return dictionary;
  }

  #endregion
}

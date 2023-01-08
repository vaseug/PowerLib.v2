using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Generic.Extensions;

public static class CollectionExtension
{
  #region Methods

  public static ICollection<TSource> Append<TSource>(this ICollection<TSource> coll, params TSource[] items)
  {
    coll.AddRange(items);
    return coll;
  }

  public static ICollection<TSource> Append<TSource>(this ICollection<TSource> coll, IEnumerable<TSource> items)
  {
    coll.AddRange(items);
    return coll;
  }

  public static ICollection<TSource> Remove<TSource>(this ICollection<TSource> coll, params TSource[] items)
  {
    coll.RemoveRange(items);
    return coll;
  }

  public static ICollection<TSource> Remove<TSource>(this ICollection<TSource> coll, IEnumerable<TSource> items)
  {
    coll.RemoveRange(items);
    return coll;
  }

  public static void AddRange<TSource>(this ICollection<TSource> coll, IEnumerable<TSource> items)
  {
    Argument.That.NotNull(coll);
    Argument.That.NotNull(items);

    using var enumerator = items.GetEnumerator();
    while (enumerator.MoveNext())
      coll.Add(enumerator.Current);
  }

  public async static ValueTask AddRange<TSource>(this ICollection<TSource> collection, IAsyncEnumerable<TSource> items, CancellationToken cancellationToken = default)
  {
    Argument.That.NotNull(collection);
    Argument.That.NotNull(items);

    if (cancellationToken.IsCancellationRequested)
      cancellationToken.ThrowIfCancellationRequested();

    var asyncEnumerator = items.GetAsyncEnumerator(cancellationToken);
    try
    {
      if (cancellationToken.IsCancellationRequested)
        cancellationToken.ThrowIfCancellationRequested();

      while (await asyncEnumerator.MoveNextAsync())
        if (cancellationToken.IsCancellationRequested)
          cancellationToken.ThrowIfCancellationRequested();
        else
          collection.Add(asyncEnumerator.Current);
    }
    finally
    {
      await AsyncDisposable.DisposeAsync(ref asyncEnumerator);
    }
  }

  public static void RemoveRange<TSource>(this ICollection<TSource> coll, IEnumerable<TSource> items)
  {
    Argument.That.NotNull(coll);
    Argument.That.NotNull(items);

    using var enumerator = items.GetEnumerator();
    while (enumerator.MoveNext())
      coll.Remove(enumerator.Current);
  }

  #endregion
  #region Cast methods

  public static IReadOnlyCollection<TSource> AsReadOnlyCollection<TSource>(this IReadOnlyCollection<TSource> coll)
    => coll;

  public static ICollection<TSource> AsCollection<TSource>(this ICollection<TSource> coll)
    => coll;

#if !NET7_0_OR_GREATER

  public static IReadOnlyCollection<TSource> AsReadOnly<TSource>(this ICollection<TSource> collection)
    => Argument.That.NotNull(collection) as IReadOnlyCollection<TSource> ?? collection.AsReadOnlyCollection();

#endif

  #endregion
}

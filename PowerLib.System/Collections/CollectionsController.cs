using System.Collections;
using System.Collections.Generic;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections;

internal static class CollectionsController
{
  internal static IList CreateList()
    => new ArrayList();

  internal static IList CreateList(int capacity)
    => new ArrayList(Argument.That.NonNegative(capacity));

  internal static IList CreateList(IEnumerable collection)
  {
    if (collection is ICollection coll)
      return new ArrayList(coll);

    var enumerator = collection.GetEnumerator();
    try
    {
      var list = new ArrayList();
      while (enumerator.MoveNext())
        list.Add(enumerator.Current);
      return list;
    }
    finally
    {
      System.Disposable.BlindDispose(ref enumerator);
    }
  }

  internal static IList<T> CreateList<T>()
    => new List<T>();

  internal static IList<T> CreateList<T>(int capacity)
    => new List<T>(Argument.That.NonNegative(capacity));

  internal static IList<T> CreateList<T>(IEnumerable<T> collection)
    => new List<T>(Argument.That.NotNull(collection));
}

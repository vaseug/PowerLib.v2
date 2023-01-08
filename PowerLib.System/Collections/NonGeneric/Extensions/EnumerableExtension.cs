using System;
using System.Collections;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.NonGeneric.Extensions;

public static class EnumerableExtension
{
  private const int InvalidCount = -1;

  #region Methods

  public static int CopyTo(this IEnumerable collection, Array array, int arrayIndex)
  {
    Argument.That.NotNull(collection);
    Argument.That.InRangeOut(array, arrayIndex);

    if (arrayIndex == array.Length)
      return 0;
    var enumerator = collection.GetEnumerator();
    try
    {
      int count = 0;
      for (; arrayIndex < array.Length && enumerator.MoveNext(); arrayIndex++, count++)
        array.SetValue(enumerator.Current, arrayIndex);
      return count;
    }
    finally
    {
      Disposable.BlindDispose(ref enumerator);
    }
  }

  public static int Count(this IEnumerable collection)
  {
    var count = collection.PeekCount();
    if (count >= 0)
      return count;
    var enumerator = collection.GetEnumerator();
    try
    {
      for (count = 0; enumerator.MoveNext(); count++) ;
    }
    finally
    {
      Disposable.BlindDispose(ref enumerator);
    }
    return count;
  }

  public static int PeekCount(this IEnumerable collection)
    => Argument.That.NotNull(collection) is ICollection coll ? coll.Count : InvalidCount;

  #endregion
}

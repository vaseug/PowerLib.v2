using System.Collections.Generic;
using System.Collections.ObjectModel;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Generic.Extensions;

public static class DictionaryExtension
{
  #region Manipulation methods

  public static TryOut<TValue> TryGet<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
    => Argument.That.NotNull(dictionary).TryGetValue(key, out var value) ? TryOut.Success(value) : TryOut<TValue>.Failed;

  public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
  {
    Argument.That.NotNull(dictionary);

    if (dictionary.ContainsKey(key))
      return false;
    dictionary.Add(key, value);
    return true;
  }

  public static bool TryUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
  {
    Argument.That.NotNull(dictionary);

    if (!dictionary.ContainsKey(key))
      return false;
    dictionary[key] = value;
    return true;
  }

  public static IDictionary<TKey, TValue> TryAppend<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
  {
    Argument.That.NotNull(dictionary);

    if (!dictionary.ContainsKey(key))
      dictionary.Add(key, value);
    return dictionary;
  }

  public static IDictionary<TKey, TValue> TrySet<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
  {
    Argument.That.NotNull(dictionary);

    if (dictionary.ContainsKey(key))
      dictionary[key] = value;
    return dictionary;
  }

  public static IDictionary<TKey, TValue> Append<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
  {
    Argument.That.NotNull(dictionary);

    dictionary.Add(key, value);
    return dictionary;
  }

  public static IDictionary<TKey, TValue> Set<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
  {
    Argument.That.NotNull(dictionary);

    dictionary[key] = value;
    return dictionary;
  }

  #endregion
  #region Cast methods

  public static IReadOnlyDictionary<TKey, TValue> AsReadOnlyDictionary<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary)
    => dictionary;

  public static IDictionary<TKey, TValue> AsDictionary<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
    => dictionary;

#if !NET7_0_OR_GREATER

  public static IReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
    where TKey : notnull
    => Argument.That.NotNull(dictionary) as IReadOnlyDictionary<TKey, TValue> ?? new ReadOnlyDictionary<TKey, TValue>(dictionary);

#endif

  #endregion
}

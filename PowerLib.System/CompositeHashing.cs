using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PowerLib.System.Validation;
using PowerLib.System.Collections.NonGeneric.Extensions;

namespace PowerLib.System
{
    public sealed class CompositeHashing
  {
    private readonly Hasher<object>? _hasher;
    private readonly Func<int, int, int> _composer;
    private readonly int _seed;
    private readonly int _nullHashCode;
    private static readonly Lazy<CompositeHashing> _default = new Lazy<CompositeHashing>(() => new CompositeHashing(ComposeHashCode, 23));

    #region Constructors

    public CompositeHashing(Func<int, int, int> composer, int seed, int nullHashCode = default)
      : this(default(Hasher<object>), composer, seed, nullHashCode)
    { }

    public CompositeHashing(Hasher<object>? hasher, Func<int, int, int> composer, int seed, int nullHashCode = default)
    {
      Argument.That.NotNull(composer);

      _hasher = hasher;
      _composer = composer;
      _seed = seed;
      _nullHashCode = nullHashCode;
    }

    public CompositeHashing(IEqualityComparer equalityComparer, Func<int, int, int> composer, int seed, int nullHashCode = default)
      : this(equalityComparer.AsHasher(), composer, seed, nullHashCode)
    { }

    #endregion
    #region Properties

    public static CompositeHashing Default
      => _default.Value;

    #endregion
    #region Methods

    private static int ComposeHashCode(int accum, int code)
      => unchecked(accum * 31 + code);

    public int GetHashCode(params object?[] objects)
      => Argument.That.NotNull(objects)
        .Select(item => item is null ? _nullHashCode : _hasher is not null ? _hasher(item) : item.GetHashCode())
        .Aggregate(_seed, (accumHash, itemHash) => _composer(accumHash, itemHash));

    public int GetHashCode(IEnumerable<object?> objects)
      => Argument.That.NotNull(objects)
        .Select(item => item is null ? _nullHashCode : _hasher is not null ? _hasher(item) : item.GetHashCode())
        .Aggregate(_seed, (accumHash, itemHash) => _composer(accumHash, itemHash));

    public int GetHashCode<T>(params T[] objects)
      => Argument.That.NotNull(objects)
        .Select(item => item is null ? _nullHashCode : _hasher is not null ? _hasher(item) : item.GetHashCode())
        .Aggregate(_seed, (accumHash, itemHash) => _composer(accumHash, itemHash));

    public int GetHashCode<T>(IEnumerable<T> objects)
      => Argument.That.NotNull(objects)
        .Select(item => item is null ? _nullHashCode : _hasher is not null ? _hasher(item) : item.GetHashCode())
        .Aggregate(_seed, (accumHash, itemHash) => _composer(accumHash, itemHash));

    public int ComposeHashCodes(params int[] hashCodes)
      => hashCodes.Aggregate(_seed, (accumHash, itemHash) => _composer(accumHash, itemHash));

    public int ComposeHashCodes(IEnumerable<int> hashCodes)
      => hashCodes.Aggregate(_seed, (accumHash, itemHash) => _composer(accumHash, itemHash));

    #endregion
  }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using PowerLib.System.Validation;

namespace PowerLib.System.Arrays;

/// <summary>
/// Represents information about regular array (rank, total length, dimensional lengths, lower and upper bounds, factors).
/// </summary>
public sealed class RegularArrayLongInfo : ArrayLongInfo
{
  private readonly int _rank;
  private readonly long _length;
  private readonly long[] _lengths;
  private readonly long[] _bases;
  private readonly long[] _factors;
  private IReadOnlyList<long>? _lengthsAccessor;
  private IReadOnlyList<long>? _lowerBoundsAccessor;
  private IReadOnlyList<long>? _upperBoundsAccessor;
  private IReadOnlyList<long>? _factorsAccessor;

  #region Constructors

  public RegularArrayLongInfo(params long[] lengths)
  {
    Argument.That.MatchCollection(lengths,
      total => total > 0L,
      length => length >= 0L);

    _rank = lengths.Length;
    _bases = new long[_rank];
    _lengths = new long[_rank];
    _factors = new long[_rank];
    _length = 1L;
    for (int i = _rank - 1; i >= 0; i--)
    {
      _lengths[i] = lengths[i];
      _bases[i] = 0L;
      _factors[i] = i == _rank - 1 ? 1 : lengths[i + 1] * _factors[i + 1];
      _length *= lengths[i];
    }
  }

  public RegularArrayLongInfo(params ArrayLongDimension[] dimensions)
  {
    Argument.That.NotEmpty(dimensions);

    _rank = dimensions.Length;
    _bases = new long[_rank];
    _lengths = new long[_rank];
    _factors = new long[_rank];
    _length = 1L;
    for (int i = _rank - 1; i >= 0; i--)
    {
      _lengths[i] = dimensions[i].Length;
      _bases[i] = dimensions[i].LowerBound;
      _factors[i] = i == _rank - 1 ? 1L : dimensions[i + 1].Length * _factors[i + 1];
      _length *= dimensions[i].Length;
    }
  }

  public RegularArrayLongInfo(long[] lengths, long[] lowerBounds)
  {
    Argument.That.MatchCoupled(lowerBounds, lengths,
      total => total > 0,
      lowerBound => lowerBound >= 0,
      length => length >= 0,
      item => item.y <= int.MaxValue - item.x);

    _rank = lengths.Length;
    _bases = new long[_rank];
    _lengths = new long[_rank];
    _factors = new long[_rank];
    _length = 1L;
    for (int i = _rank - 1; i >= 0; i--)
    {
      _lengths[i] = lengths[i];
      _bases[i] = lowerBounds[i];
      _factors[i] = i == _rank - 1 ? 1L : lengths[i + 1] * _factors[i + 1];
      _length *= lengths[i];
    }
  }

  #endregion
  #region Instance properties

  /// <summary>
  /// Array rank
  /// </summary>
  public override int Rank
    => _rank;

  /// <summary>
  /// Array total length
  /// </summary>
  public override long Length
    => _length;

  /// <summary>
  /// Array dimensional lengths accessor
  /// </summary>
  public IReadOnlyList<long> Lengths
    => LazyInitializer.EnsureInitialized(ref _lengthsAccessor, () => new ReadOnlyCollection<long>(_lengths));

  /// <summary>
  /// Array dimensionalowebounds accessor
  /// </summary>
  public IReadOnlyList<long> LowerBounds
    => LazyInitializer.EnsureInitialized(ref _lowerBoundsAccessor, () => new ReadOnlyCollection<long>(_bases));

  /// <summary>
  /// Array dimensionauppebounds accessor
  /// </summary>
  public IReadOnlyList<long> UpperBounds
    => LazyInitializer.EnsureInitialized(ref _upperBoundsAccessor, () => new ReadOnlyCollection<long>(_lengths.Select((length, index) => _bases[index] + length - 1L).ToArray()));

  /// <summary>
  /// Array dimensionafactors
  /// </summary>
  public IReadOnlyList<long> Factors
    => LazyInitializer.EnsureInitialized(ref _factorsAccessor, () => new ReadOnlyCollection<long>(_factors));

  #endregion
  #region Instance methods

  /// <summary>
  /// Create new array specified by this ArrayInfo
  /// </summary>
  /// <param name="elementType">Element type of created array.</param>
  /// <returns>Created array.</returns>
  public override Array CreateArray(Type elementType)
  {
    Argument.That.NotNull(elementType);
    Argument.That.MatchElements(LowerBounds, item => item > 0);

    return Array.CreateInstance(elementType, _lengths);
  }

  public override object? GetValue(Array array, bool asRanges, bool zeroBased, params long[] dimIndices)
  {
    Argument.That.MatchArray(array,
      rank => rank == _rank,
      (long length) => !asRanges && length == _length || asRanges && length >= _length,
      (long[] dimLengths) => dimLengths
        .Where((dimLength, index) => (asRanges || _bases[index] == 0) &&
          (!asRanges && dimLength == _lengths[index] || asRanges && dimLength >= _bases[index] + _lengths[index]))
        .Count() == dimLengths.Length,
      null);

    Argument.That.MatchCollection(dimIndices,
      count => count == _rank,
      (index, dim) => index >= (zeroBased ? 0 : _bases[dim]) && index < _length + (zeroBased ? 0 : _bases[dim]));

    if (!zeroBased)
    {
      return array.GetValue(dimIndices);
    }
    else
    {
      long[] indices = new long[_rank];
      for (int i = 0; i < _rank; i++)
        indices[i] = dimIndices[i] + _bases[i];
      return array.GetValue(indices);
    }
  }

  public override void SetValue(Array array, object? value, bool asRanges, bool zeroBased, params long[] dimIndices)
  {
    Argument.That.MatchArray(array,
      rank => rank == _rank,
      (long length) => !asRanges && length == _length || asRanges && length >= _length,
      (long[] dimLengths) => dimLengths
        .Where((dimLength, index) => (asRanges || _bases[index] == 0) &&
          (!asRanges && dimLength == _lengths[index] || asRanges && dimLength >= _bases[index] + _lengths[index]))
        .Count() == dimLengths.Length,
      null);

    Argument.That.MatchCollection(dimIndices,
      count => count == _rank,
      (index, dim) => index >= (zeroBased ? 0 : _bases[dim]) && index < _length + (zeroBased ? 0 : _bases[dim]));

    if (!zeroBased)
    {
      array.SetValue(value, dimIndices);
    }
    else
    {
      long[] indices = new long[_rank];
      for (int i = 0; i < _rank; i++)
        indices[i] = dimIndices[i] + _bases[i];
      array.SetValue(value, indices);
    }
  }

  public override T GetValue<T>(Array array, bool asRanges, bool zeroBased, params long[] dimIndices)
  {
    Argument.That.MatchArray(array,
      rank => rank == _rank,
      (long length) => !asRanges && length == _length || asRanges && length >= _length,
      (long[] dimLengths) => dimLengths
        .Where((dimLength, index) => (asRanges || _bases[index] == 0) &&
          (!asRanges && dimLength == _lengths[index] || asRanges && dimLength >= _bases[index] + _lengths[index]))
        .Count() == dimLengths.Length,
      null);

    Argument.That.MatchCollection(dimIndices,
      count => count == _rank,
      (index, dim) => index >= (zeroBased ? 0 : _bases[dim]) && index < _length + (zeroBased ? 0 : _bases[dim]));

    if (!zeroBased)
    {
      return (T)array.GetValue(dimIndices)!;
    }
    else
    {
      long[] indices = new long[_rank];
      for (int i = 0; i < _rank; i++)
        indices[i] = dimIndices[i] + _bases[i];
      return (T)array.GetValue(indices)!;
    }
  }

  public override void SetValue<T>(Array array, T value, bool asRanges, bool zeroBased, params long[] dimIndices)
  {
    Argument.That.MatchArray(array,
      rank => rank == _rank,
      (long length) => !asRanges && length == _length || asRanges && length >= _length,
      (long[] dimLengths) => dimLengths
        .Where((dimLength, index) => (asRanges || _bases[index] == 0) &&
          (!asRanges && dimLength == _lengths[index] || asRanges && dimLength >= _bases[index] + _lengths[index]))
        .Count() == dimLengths.Length,
      null);

    Argument.That.MatchCollection(dimIndices,
      count => count == _rank,
      (index, dim) => index >= (zeroBased ? 0 : _bases[dim]) && index < _length + (zeroBased ? 0 : _bases[dim]));

    if (!zeroBased)
    {
      array.SetValue(value, dimIndices);
    }
    else
    {
      long[] indices = new long[_rank];
      for (int i = 0; i < _rank; i++)
        indices[i] = dimIndices[i] + _bases[i];
      array.SetValue(value, indices);
    }
  }

  public override long CalcFlatIndex(bool zeroBased, params long[] dimIndices)
  {
    Argument.That.MatchCollection(dimIndices,
      count => count == _rank,
      (index, dim) => index >= (zeroBased ? 0 : _bases[dim]) && index < _length + (zeroBased ? 0 : _bases[dim]));

    long flatIndex = 0;
    for (int i = 0; i < dimIndices.Length; i++)
      flatIndex += (dimIndices[i] - (zeroBased ? 0 : _bases[i])) * _factors[i];
    return flatIndex;
  }

  public override void CalcDimIndices(long flatIndex, bool zeroBased, long[] dimIndices)
  {
    Argument.That.InRangeIn(_length, flatIndex);
    Argument.That.ExactCount(dimIndices, _rank);

    for (int i = 0; i < dimIndices.Length; i++)
    {
      dimIndices[i] = flatIndex / _factors[i] + (zeroBased ? 0 : _bases[i]);
      flatIndex %= _factors[i];
    }
  }

  public override void GetMinDimIndices(bool zeroBased, long[] dimIndices)
  {
    Argument.That.ExactCount(dimIndices, _rank);

    for (int i = 0; i < dimIndices.Length; i++)
      dimIndices[i] = zeroBased ? 0 : _bases[i];
  }

  public override void GetMaxDimIndices(bool zeroBased, long[] dimIndices)
  {
    Argument.That.ExactCount(dimIndices, _rank);

    for (int i = 0; i < dimIndices.Length; i++)
      dimIndices[i] = (zeroBased ? 0 : _bases[i]) + _lengths[i] - 1;
  }

  public override bool IncDimIndices(bool zeroBased, long[] dimIndices)
  {
    Argument.That.MatchCollection(dimIndices,
      length => length == _rank,
      (index, dim) => index >= (zeroBased ? 0 : _bases[dim]) && index < (zeroBased ? 0 : _bases[dim]) + _lengths[dim]);

    int c = 1;
    for (int j = _rank - 1; j >= 0 && c > 0; j--)
      dimIndices[j] = dimIndices[j] + c < _lengths[j] + (zeroBased ? 0 : _bases[j]) ? dimIndices[j] + c-- : zeroBased ? 0 : _bases[j];
    return c > 0;
  }

  public override bool DecDimIndices(bool zeroBased, long[] dimIndices)
  {
    Argument.That.MatchCollection(dimIndices,
      length => length == _rank,
      (index, dim) => index >= (zeroBased ? 0 : _bases[dim]) && index < (zeroBased ? 0 : _bases[dim]) + _lengths[dim]);

    int c = 1;
    for (int j = _rank - 1; j >= 0 && c > 0; j--)
      dimIndices[j] = dimIndices[j] - c >= (zeroBased ? 0 : _bases[j]) ? dimIndices[j] - c-- : _lengths[j] + (zeroBased ? 0 : _bases[j]) - 1;
    return c > 0;
  }

  public long GetLength(int dimension)
  {
    Argument.That.InRangeIn(_rank, dimension);

    return _lengths[dimension];
  }

  public long GetLowerBound(int dimension)
  {
    Argument.That.InRangeIn(_rank, dimension);

    return _bases[dimension];
  }

  public long GetUpperBound(int dimension)
  {
    Argument.That.InRangeIn(_rank, dimension);

    return _bases[dimension] + _lengths[dimension] - 1;
  }

  public long GetFactor(int dimension)
  {
    Argument.That.InRangeIn(_rank, dimension);

    return _factors[dimension];
  }

  public void GetLengths(long[] lengths)
  {
    Argument.That.ExactCount(lengths, _rank);

    for (int i = 0; i < _lengths.Length; i++)
      lengths[i] = _lengths[i];
  }

  public void GetLowerBounds(long[] bounds)
  {
    Argument.That.ExactCount(bounds, _rank);

    for (int i = 0; i < _bases.Length; i++)
      bounds[i] = _bases[i];
  }

  public void GetUpperBounds(long[] bounds)
  {
    Argument.That.ExactCount(bounds, _rank);

    for (int i = 0; i < _bases.Length; i++)
      bounds[i] = _bases[i] + _lengths[i] - 1;
  }

  public void GetFactors(long[] factors)
  {
    Argument.That.ExactCount(factors, _rank);

    for (int i = 0; i < _factors.Length; i++)
      factors[i] = _factors[i];
  }

  #endregion
}

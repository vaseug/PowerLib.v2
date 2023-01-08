using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using PowerLib.System.Validation;

namespace PowerLib.System.Arrays;

/// <summary>
/// 
/// </summary>
public sealed class RegularArrayInfo : ArrayInfo
{
  private readonly int _rank;
  private readonly int _length;
  private readonly int[] _lengths;
  private readonly int[] _bases;
  private readonly int[] _limits;
  private readonly int[] _factors;
  private IReadOnlyList<int>? _lengthsAccessor;
  private IReadOnlyList<int>? _lowerBoundsAccessor;
  private IReadOnlyList<int>? _upperBoundsAccessor;
  private IReadOnlyList<int>? _factorsAccessor;

  #region Constructors

  public RegularArrayInfo(params int[] lengths)
  {
    Argument.That.MatchCollection(lengths,
      total => total > 0,
      length => length >= 0);

    _rank = lengths.Length;
    _bases = new int[_rank];
    _limits = new int[_rank];
    _lengths = new int[_rank];
    _factors = new int[_rank];
    _length = 1;
    for (int i = _rank - 1; i >= 0; i--)
    {
      _lengths[i] = lengths[i];
      _bases[i] = 0;
      _limits[i] = (lengths[i] == 0 ? 0 : lengths[i] - 1);
      _factors[i] = i == _rank - 1 ? 1 : lengths[i + 1] * _factors[i + 1];
      _length *= lengths[i];
    }
  }

  public RegularArrayInfo(params ArrayDimension[] dimensions)
  {
    Argument.That.NotEmpty(dimensions);

    _rank = dimensions.Length;
    _bases = new int[_rank];
    _limits = new int[_rank];
    _lengths = new int[_rank];
    _factors = new int[_rank];
    _length = 1;
    for (int i = _rank - 1; i >= 0; i--)
    {
      _lengths[i] = dimensions[i].Length;
      _bases[i] = dimensions[i].LowerBound;
      _limits[i] = dimensions[i].LowerBound + (dimensions[i].Length == 0 ? 0 : dimensions[i].Length - 1);
      _factors[i] = i == _rank - 1 ? 1 : dimensions[i + 1].Length * _factors[i + 1];
      _length *= dimensions[i].Length;
    }
  }

  public RegularArrayInfo(int[] lengths, int[] lowerBounds)
  {
    Argument.That.MatchCoupled(lowerBounds, lengths,
      total => total > 0,
      lowerBound => lowerBound >= 0,
      length => length >= 0,
      item => item.y <= int.MaxValue - item.x);

    _rank = lengths.Length;
    _bases = new int[_rank];
    _limits = new int[_rank];
    _lengths = new int[_rank];
    _factors = new int[_rank];
    _length = 1;
    for (int i = _rank - 1; i >= 0; i--)
    {
      _lengths[i] = lengths[i];
      _bases[i] = lowerBounds[i];
      _limits[i] = lowerBounds[i] + (lengths[i] == 0 ? 0 : lengths[i] - 1);
      _factors[i] = i == _rank - 1 ? 1 : lengths[i + 1] * _factors[i + 1];
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
  public override int Length
    => _length;

  /// <summary>
  /// Array dimensional lengths accessor
  /// </summary>
  public IReadOnlyList<int> Lengths
    => LazyInitializer.EnsureInitialized(ref _lengthsAccessor, () => new ReadOnlyCollection<int>(_lengths));

  /// <summary>
  /// Array dimensionalowebounds accessor
  /// </summary>
  public IReadOnlyList<int> LowerBounds
    => LazyInitializer.EnsureInitialized(ref _lowerBoundsAccessor, () => new ReadOnlyCollection<int>(_bases));

  /// <summary>
  /// Array dimensionauppebounds accessor
  /// </summary>
  public IReadOnlyList<int> UpperBounds
    => LazyInitializer.EnsureInitialized(ref _upperBoundsAccessor, () => new ReadOnlyCollection<int>(_lengths.Select((length, index) => _bases[index] + length - 1).ToArray()));

  /// <summary>
  /// Array dimensionafactors
  /// </summary>
  public IReadOnlyList<int> Factors
    => LazyInitializer.EnsureInitialized(ref _factorsAccessor, () => new ReadOnlyCollection<int>(_factors));

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

    return Array.CreateInstance(elementType, _lengths, _bases);
  }

  public override object? GetValue(Array array, bool asRanges, bool zeroBased, params int[] dimIndices)
  {
    Argument.That.MatchArray(array,
      rank => rank == _rank,
      length => !asRanges && length == _length || asRanges && length >= _length,
      (ArrayDimension[] dimensions) => dimensions
        .Where((dimension, index) => (!asRanges && dimension.LowerBound == _bases[index] || asRanges && dimension.LowerBound <= _bases[index]) &&
          (!asRanges && dimension.Length == _lengths[index] || asRanges && dimension.LowerBound + dimension.Length >= _bases[index] + _lengths[index]))
        .Count() == dimensions.Length,
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
      int[] indices = new int[_rank];
      for (int i = 0; i < _rank; i++)
        indices[i] = dimIndices[i] + _bases[i];
      return array.GetValue(indices);
    }
  }

  public override void SetValue(Array array, object? value, bool asRanges, bool zeroBased, params int[] dimIndices)
  {
    Argument.That.MatchArray(array,
      rank => rank == _rank,
      length => !asRanges && length == _length || asRanges && length >= _length,
      (ArrayDimension[] dimensions) => dimensions
        .Where((dimension, index) => (!asRanges && dimension.LowerBound == _bases[index] || asRanges && dimension.LowerBound <= _bases[index]) &&
          (!asRanges && dimension.Length == _lengths[index] || asRanges && dimension.LowerBound + dimension.Length >= _bases[index] + _lengths[index]))
        .Count() == dimensions.Length,
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
      int[] indices = new int[_rank];
      for (int i = 0; i < _rank; i++)
        indices[i] = dimIndices[i] + _bases[i];
      array.SetValue(value, indices);
    }
  }

  public override T GetValue<T>(Array array, bool asRanges, bool zeroBased, params int[] dimIndices)
  {
    Argument.That.MatchArray(array,
      rank => rank == _rank,
      length => !asRanges && length == _length || asRanges && length >= _length,
      (ArrayDimension[] dimensions) => dimensions
        .Where((dimension, index) => (!asRanges && dimension.LowerBound == _bases[index] || asRanges && dimension.LowerBound <= _bases[index]) &&
          (!asRanges && dimension.Length == _lengths[index] || asRanges && dimension.LowerBound + dimension.Length >= _bases[index] + _lengths[index]))
        .Count() == dimensions.Length,
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
      int[] indices = new int[_rank];
      for (int i = 0; i < _rank; i++)
        indices[i] = dimIndices[i] + _bases[i];
      return (T)array.GetValue(indices)!;
    }
  }

  public override void SetValue<T>(Array array, T value, bool asRanges, bool zeroBased, params int[] dimIndices)
    where T : default
  {
    Argument.That.MatchArray(array,
      rank => rank == _rank,
      length => !asRanges && length == _length || asRanges && length >= _length,
      (ArrayDimension[] dimensions) => dimensions
        .Where((dimension, index) => (!asRanges && dimension.LowerBound == _bases[index] || asRanges && dimension.LowerBound <= _bases[index]) &&
          (!asRanges && dimension.Length == _lengths[index] || asRanges && dimension.LowerBound + dimension.Length >= _bases[index] + _lengths[index]))
        .Count() == dimensions.Length,
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
      int[] indices = new int[_rank];
      for (int i = 0; i < _rank; i++)
        indices[i] = dimIndices[i] + _bases[i];
      array.SetValue(value, indices);
    }
  }

  public override int CalcFlatIndex(bool zeroBased, params int[] dimIndices)
  {
    Argument.That.MatchCollection(dimIndices,
      count => count == _rank,
      (index, dimension) => index >= (zeroBased ? 0 : _bases[dimension]) && index < (zeroBased ? 0 : _bases[dimension]) + _lengths[dimension]);

    int flatIndex = 0;
    for (int i = 0; i < dimIndices.Length; i++)
      flatIndex += (dimIndices[i] - (zeroBased ? 0 : _bases[i])) * _factors[i];
    return flatIndex;
  }

  public override void CalcDimIndices(int flatIndex, bool zeroBased, int[] dimIndices)
  {
    Argument.That.InRangeIn(_length, flatIndex);
    Argument.That.ExactCount(dimIndices, _rank);

    for (int i = 0; i < dimIndices.Length; i++)
    {
      dimIndices[i] = flatIndex / _factors[i] + (zeroBased ? 0 : _bases[i]);
      flatIndex %= _factors[i];
    }
  }

  public override void GetMinDimIndices(bool zeroBased, int[] dimIndices)
  {
    Argument.That.ExactCount(dimIndices, _rank);

    for (int i = 0; i < dimIndices.Length; i++)
      dimIndices[i] = zeroBased ? 0 : _bases[i];
  }

  public override void GetMaxDimIndices(bool zeroBased, int[] dimIndices)
  {
    Argument.That.ExactCount(dimIndices, _rank);

    for (int i = 0; i < dimIndices.Length; i++)
      dimIndices[i] = (zeroBased ? 0 : _bases[i]) + _lengths[i] - 1;
  }

  public override bool IncDimIndices(bool zeroBased, int[] dimIndices)
  {
    Argument.That.MatchCollection(dimIndices,
      length => length == _rank,
      (index, dim) => index >= (zeroBased ? 0 : _bases[dim]) && index < (zeroBased ? 0 : _bases[dim]) + _lengths[dim]);

    int c = 1;
    for (int j = _rank - 1; j >= 0 && c > 0; j--)
      dimIndices[j] = dimIndices[j] + c < _lengths[j] + (zeroBased ? 0 : _bases[j]) ? dimIndices[j] + c-- : zeroBased ? 0 : _bases[j];
    return c > 0;
  }

  public override bool DecDimIndices(bool zeroBased, int[] dimIndices)
  {
    Argument.That.MatchCollection(dimIndices,
      length => length == _rank,
      (index, dim) => index >= (zeroBased ? 0 : _bases[dim]) && index < (zeroBased ? 0 : _bases[dim]) + _lengths[dim]);

    int c = 1;
    for (int j = _rank - 1; j >= 0 && c > 0; j--)
      dimIndices[j] = dimIndices[j] - c >= (zeroBased ? 0 : _bases[j]) ? dimIndices[j] - c-- : _lengths[j] + (zeroBased ? 0 : _bases[j]) - 1;
    return c > 0;
  }

  public int GetLength(int dimension)
  {
    Argument.That.InRangeIn(_rank, dimension);

    return _lengths[dimension];
  }

  public int GetLowerBound(int dimension)
  {
    Argument.That.InRangeIn(_rank, dimension);

    return _bases[dimension];
  }

  public int GetUpperBound(int dimension)
  {
    Argument.That.InRangeIn(_rank, dimension);

    return _limits[dimension];
  }

  public int GetFactor(int dimension)
  {
    Argument.That.InRangeIn(_rank, dimension);

    return _factors[dimension];
  }

  public void GetLengths(int[] lengths)
  {
    Argument.That.ExactCount(lengths, _rank);

    for (int i = 0; i < _lengths.Length; i++)
      lengths[i] = _lengths[i];
  }

  public void GetLowerBounds(int[] bounds)
  {
    Argument.That.ExactCount(bounds, _rank);

    for (int i = 0; i < _bases.Length; i++)
      bounds[i] = _bases[i];
  }

  public void GetUpperBounds(int[] bounds)
  {
    Argument.That.ExactCount(bounds, _rank);

    for (int i = 0; i < _limits.Length; i++)
      bounds[i] = _limits[i];
  }

  public void GetFactors(int[] factors)
  {
    Argument.That.ExactCount(factors, _rank);

    for (int i = 0; i < _factors.Length; i++)
      factors[i] = _factors[i];
  }

  #endregion
}

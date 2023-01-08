using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using PowerLib.System.Validation;

namespace PowerLib.System.Arrays;

/// <summary>
///	ArrayIndex is allow addressing any dimensional arrays even with non zero lower boundaries.
/// ArrayIndex contains consistent values of flat index and dimensional indices.
/// </summary>
/// <summary xml:lang="ru">
/// ArrayIndex позволяет производить адресацию массивов любой размерности, даже с ненулевой нижней границей.
/// ArrayIndex содержит согласованные значения сквозного индекса и индексов каждой из размерности массива.
/// </summary>
public sealed class ArrayIndex
{
  private readonly ArrayInfo _arrayInfo;
  private bool _asRanges;
  private bool _zeroBased;
  private bool _checkOut;
  private int _carry;
  private int _flatIndex;
  private readonly int[] _dimIndices;
  private IReadOnlyList<int>? _dimIndicesAccessor;

  #region Constructors

  /// <summary>
  /// Construct array index
  /// </summary>
  /// <param name="arrayInfo">Array information class.</param>
  public ArrayIndex(ArrayInfo arrayInfo)
    : this(arrayInfo, false, Bound.Lower)
  { }

  /// <summary>
  /// Construct array index
  /// </summary>
  /// <param name="arrayInfo">Array information class.</param>
  public ArrayIndex(ArrayInfo arrayInfo, bool zeroBased)
    : this(arrayInfo, zeroBased, Bound.Lower)
  { }

  /// <summary>
  /// Construct array index
  /// </summary>
  /// <param name="arrayInfo">Array information class.</param>
  /// <param name="bound">If truthen.</param>
  public ArrayIndex(ArrayInfo arrayInfo, bool zeroBased, Bound bound)
  {
    _arrayInfo = arrayInfo;
    _zeroBased = zeroBased;
    _dimIndices = new int[_arrayInfo.Rank];
    switch (bound)
    {
      case Bound.Lower:
        _flatIndex = 0;
        _arrayInfo.GetMinDimIndices(zeroBased, _dimIndices);
        break;
      case Bound.Upper:
        _flatIndex = _arrayInfo.Length == 0 ? 0 : _arrayInfo.Length - 1;
        _arrayInfo.GetMaxDimIndices(zeroBased, _dimIndices);
        break;
      default:
        Operation.That.Failed();
        break;
    }
  }

  #endregion
  #region Properties

  /// <summary>
  /// 
  /// </summary>
  public bool AsRanges
  {
    get => _asRanges;
    set => _asRanges = value;
  }

  /// <summary>
  /// Dimensional indices would be represented as zerbased (if it's nones).
  /// </summary>
  public bool ZeroBased
  {
    get => _zeroBased;
    set
    {
      if (_arrayInfo.Length == 0)
        _arrayInfo.GetMinDimIndices(value, _dimIndices);
      else
        _arrayInfo.CalcDimIndices(_flatIndex, value, _dimIndices);
      _zeroBased = value;
    }
  }

  /// <summary>
  /// Check out range of dimensional index
  /// </summary>
  public bool CheckOut
  {
    get => _checkOut;
    set => _checkOut = value;
  }

  /// <summary>
  /// Carry value from previous flat index operation
  /// </summary>
  public int Carry
    => _carry;

  /// <summary>
  /// Array info
  /// </summary>
  public ArrayInfo ArrayInfo
    => _arrayInfo;

  public int FlatIndex
  {
    get => _flatIndex;
    set
    {
      if (value == FlatIndex)
        return;
      _arrayInfo.CalcDimIndices(value, _zeroBased, _dimIndices);
      _flatIndex = value;
      _carry = 0;
    }
  }

  public IReadOnlyList<int> DimIndices
    => LazyInitializer.EnsureInitialized(ref _dimIndicesAccessor, () => new ReadOnlyCollection<int>(_dimIndices));

  #endregion
  #region Methods
  #region Internal methods

  /// <summary>
  /// Adds value to the value of the flat delta index. 
  /// If the value of property module false and indices out of range then throwing an exception.
  /// Otherwise, the addition is performed by module the length of the array and exception is not throw.
  /// </summary>
  /// <param name="delta"></param>
  /// <returns></returns>
  private int AddFlatIndex(int delta)
  {
    int carry = 0;
    bool checkOut = _checkOut;
    if (!checkOut && (delta > _arrayInfo.Length || delta < -_arrayInfo.Length))
      delta %= _arrayInfo.Length;
    else
      Operation.That.IsInRange(_arrayInfo.Length > 0);
    if (delta > 0 && _arrayInfo.Length - _flatIndex < delta + 1)
    {
      if (!checkOut)
        carry = delta - (_arrayInfo.Length - _flatIndex - 1);
      else
        Operation.That.OutOfRange();
    }
    else if (delta < 0 && _flatIndex < -delta)
    {
      if (!checkOut)
        carry = -delta - _flatIndex;
      else
        Operation.That.OutOfRange();
    }
    _flatIndex +=
      delta > 0 && _arrayInfo.Length - _flatIndex < delta + 1 ? delta - _arrayInfo.Length :
      delta < 0 && _flatIndex < -delta ? delta + _arrayInfo.Length :
      delta;
    if (delta == 1)
      _arrayInfo.IncDimIndices(_zeroBased, _dimIndices);
    else if (delta == -1)
      _arrayInfo.DecDimIndices(_zeroBased, _dimIndices);
    else
      _arrayInfo.CalcDimIndices(_flatIndex, _zeroBased, _dimIndices);
    return _carry = carry;
  }

  #endregion
  #region Public methods

  public object? GetValue(Array array)
    => _arrayInfo.GetValue(array, _asRanges, _zeroBased, _dimIndices);

  public void SetValue(Array array, object? value)
    => _arrayInfo.SetValue(array, value, _asRanges, _zeroBased, _dimIndices);

  public T GetValue<T>(Array array)
    => _arrayInfo.GetValue<T>(array, _asRanges, _zeroBased, _dimIndices);

  public void SetValue<T>(Array array, T value)
    => _arrayInfo.SetValue(array, value, _asRanges, _zeroBased, _dimIndices);

  public void GetDimIndices(int[] dimIndices)
  {
    Argument.That.ExactCount(dimIndices, ArrayInfo.Rank);

    _dimIndices.CopyTo(dimIndices, 0);
  }

  public void SetDimIndices(int[] dimIndices)
  {
    Argument.That.ExactCount(dimIndices, ArrayInfo.Rank);

    _flatIndex = _arrayInfo.CalcFlatIndex(dimIndices);
    _carry = 0;
    dimIndices.CopyTo(_dimIndices, 0);
  }

  public int GetDimIndex(int dimension)
  {
    Argument.That.InRangeIn(DimIndices, dimension);

    return _dimIndices[dimension];
  }

  public int Add(int delta)
    => AddFlatIndex(delta);

  public int Sub(int delta)
    => AddFlatIndex(-delta);

  public bool Inc()
    => AddFlatIndex(1) != 0;

  public bool Dec()
    => AddFlatIndex(-1) != 0;

  public void SetMin()
  {
    _carry = 0;
    _flatIndex = 0;
    _arrayInfo.GetMinDimIndices(_zeroBased, _dimIndices);
  }

  public void SetMax()
  {
    _carry = 0;
    _flatIndex = _arrayInfo.Length == 0 ? 0 : _arrayInfo.Length - 1;
    _arrayInfo.GetMaxDimIndices(_zeroBased, _dimIndices);
  }

  public bool IsMin
    => _arrayInfo.Length > 0 && _flatIndex == 0;

  public bool IsMax
    => _arrayInfo.Length > 0 && _flatIndex == _arrayInfo.Length - 1;

  #endregion
  #endregion
  #region Operators

  /// <summary>
  /// Conversation operator to flat index value.
  /// </summary>
  /// <param name="arrayIndex">Array index.</param>
  /// <returns>Flaindex value.</returns>
  public static implicit operator int(ArrayIndex arrayIndex)
  {
    Argument.That.NotNull(arrayIndex);

    return arrayIndex.FlatIndex;
  }

  /// <summary>
  /// Increment operator
  /// </summary>
  /// <param name="arrayIndex"></param>
  /// <returns></returns>
  public static ArrayIndex operator ++(ArrayIndex arrayIndex)
  {
    Argument.That.NotNull(arrayIndex);
    
    arrayIndex.Inc();
    return arrayIndex;
  }

  /// <summary>
  /// Decrement operator
  /// </summary>
  /// <param name="arrayIndex"></param>
  /// <returns></returns>
  public static ArrayIndex operator --(ArrayIndex arrayIndex)
  {
    Argument.That.NotNull(arrayIndex);
    
    arrayIndex.Dec();
    return arrayIndex;
  }

  /// <summary>
  /// Addition operator
  /// </summary>
  /// <param name="arrayIndex"></param>
  /// <param name="delta"></param>
  /// <returns></returns>
  public static ArrayIndex operator +(ArrayIndex arrayIndex, int delta)
  {
    Argument.That.NotNull(arrayIndex);

    arrayIndex.Add(delta);
    return arrayIndex;
  }

  /// <summary>
  /// Subtraction operator
  /// </summary>
  /// <param name="arrayIndex"></param>
  /// <param name="delta"></param>
  /// <returns></returns>
  public static ArrayIndex operator -(ArrayIndex arrayIndex, int delta)
  {
    Argument.That.NotNull(arrayIndex);
    
    arrayIndex.Sub(delta);
    return arrayIndex;
  }

  #endregion
}

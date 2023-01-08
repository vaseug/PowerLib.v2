using System;

namespace PowerLib.System.Arrays;

/// <summary>
/// Base class that represents information about array.
/// </summary>
/// <summary xml:lang="ru">
/// Базовый класс который предоставляет служебную информацию о массиве.
/// </summary>
public abstract class ArrayInfo
{
  #region Instance properties

  public abstract int Rank { get; }

  public abstract int Length { get; }

  #endregion
  #region Instance methods

  public Array CreateArray<T>()
    => CreateArray(typeof(T));

  public abstract Array CreateArray(Type elementType);

  public virtual object? GetValue(Array array, params int[] dimIndices)
    => GetValue(array, false, false, dimIndices);

  public abstract object? GetValue(Array array, bool asRanges, bool zeroBased, params int[] dimIndices);

  public virtual void SetValue(Array array, object? value, params int[] dimIndices)
    => SetValue(array, value, false, false, dimIndices);

  public abstract void SetValue(Array array, object? value, bool asRanges, bool zeroBased, params int[] dimIndices);

  public virtual T GetValue<T>(Array array, params int[] dimIndices)
    => GetValue<T>(array, false, false, dimIndices);

  public abstract T GetValue<T>(Array array, bool asRanges, bool zeroBased, params int[] dimIndices);

  public virtual void SetValue<T>(Array array, T value, params int[] dimIndices)
    => SetValue(array, value, false, false, dimIndices);

  public abstract void SetValue<T>(Array array, T value, bool asRanges, bool zeroBased, params int[] dimIndices);

  public virtual int CalcFlatIndex(params int[] dimIndices)
    => CalcFlatIndex(false, dimIndices);

  public abstract int CalcFlatIndex(bool zeroBased, params int[] dimIndices);

  public virtual void CalcDimIndices(int flatIndex, int[] dimIndices)
    => CalcDimIndices(flatIndex, false, dimIndices);

  public abstract void CalcDimIndices(int flatIndex, bool zeroBased, int[] dimIndices);

  public virtual void GetMinDimIndices(int[] dimIndices)
    => GetMinDimIndices(false, dimIndices);

  public abstract void GetMinDimIndices(bool zeroBased, int[] dimIndices);

  public virtual void GetMaxDimIndices(int[] dimIndices)
    => GetMaxDimIndices(false, dimIndices);

  public abstract void GetMaxDimIndices(bool zeroBased, int[] dimIndices);

  public virtual bool IncDimIndices(int[] dimIndices)
    => IncDimIndices(false, dimIndices);

  public abstract bool IncDimIndices(bool zeroBased, int[] dimIndices);

  public virtual bool DecDimIndices(int[] dimIndices)
    => DecDimIndices(false, dimIndices);

  public abstract bool DecDimIndices(bool zeroBased, int[] dimIndices);

  #endregion
}

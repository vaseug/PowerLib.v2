using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;
using PowerLib.System.Validation;

namespace PowerLib.System.Arrays;

/// <summary>
/// ArgumentRegularArrayElementException exception
/// </summary>
[SuppressMessage("Design", "CA1032: Implement standard exception constructors", Justification = "By design")]
[Serializable]
public class ArgumentRegularArrayElementException : ArgumentException
{
  private const int InvalidIndex = -1;
  private static string? defaultMessage;
  private readonly int _index;
  private readonly int[]? _indices;
  private Lazy<IReadOnlyList<int>?> _indicesAccessor;

  #region Constructors

  public ArgumentRegularArrayElementException(int index, params int[] indices)
    : base(DefaultMessage(index, indices))
  {
    _index = index;
    _indices = (int[]?)indices?.Clone() ?? new[] { index };
    _indicesAccessor = new Lazy<IReadOnlyList<int>?>(() => _indices is null ? null : new ReadOnlyCollection<int>(_indices));
  }

  public ArgumentRegularArrayElementException(params int[] indices)
    : base(DefaultMessage(InvalidIndex, indices))
  {
    _index = indices?.Length == 1 ? indices[0] : InvalidIndex;
    _indices = (int[]?)indices?.Clone();
    _indicesAccessor = new Lazy<IReadOnlyList<int>?>(() => _indices is null ? null : new ReadOnlyCollection<int>(_indices));
  }

  public ArgumentRegularArrayElementException(int index)
    : base(DefaultMessage(index, null))
  {
    _index = index;
    _indices = new [] { index };
    _indicesAccessor = new Lazy<IReadOnlyList<int>?>(() => _indices is null ? null : new ReadOnlyCollection<int>(_indices));
  }

  public ArgumentRegularArrayElementException(string? paramName, int index, params int[] indices)
    : base(DefaultMessage(index, indices), paramName)
  {
    _index = index;
    _indices = (int[]?)indices?.Clone() ?? new [] { index };
    _indicesAccessor = new Lazy<IReadOnlyList<int>?>(() => _indices is null ? null : new ReadOnlyCollection<int>(_indices));
  }

  public ArgumentRegularArrayElementException(string? paramName, params int[] indices)
    : base(DefaultMessage(InvalidIndex, indices), paramName)
  {
    _index = indices?.Length == 1 ? indices[0] : InvalidIndex;
    _indices = (int[]?)indices?.Clone();
    _indicesAccessor = new Lazy<IReadOnlyList<int>?>(() => _indices is null ? null : new ReadOnlyCollection<int>(_indices));
  }

  public ArgumentRegularArrayElementException(string? paramName, int index)
    : base(DefaultMessage(index, null), paramName)
  {
    _index= index;
    _indices = new int[] { index };
    _indicesAccessor = new Lazy<IReadOnlyList<int>?>(() => _indices is null ? null : new ReadOnlyCollection<int>(_indices));
  }

  public ArgumentRegularArrayElementException(string? paramName, Exception? innerException, int index, params int[] indices)
    : base(DefaultMessage(index, indices), paramName, innerException)
  {
    _index = index;
    _indices = (int[]?)indices?.Clone() ?? new[] { index };
    _indicesAccessor = new Lazy<IReadOnlyList<int>?>(() => _indices is null ? null : new ReadOnlyCollection<int>(_indices));
  }

  public ArgumentRegularArrayElementException(string? paramName, Exception? innerException, params int[] indices)
    : base(DefaultMessage(InvalidIndex, indices), paramName, innerException)
  {
    _index = indices?.Length == 1 ? indices[0] : InvalidIndex;
    _indices = (int[]?)indices?.Clone();
    _indicesAccessor = new Lazy<IReadOnlyList<int>?>(() => _indices is null ? null : new ReadOnlyCollection<int>(_indices));
  }

  public ArgumentRegularArrayElementException(string? paramName, Exception? innerException, int index)
    : base(DefaultMessage(index, null), paramName, innerException)
  {
    _index = index;
    _indices = new int[] { index };
    _indicesAccessor = new Lazy<IReadOnlyList<int>?>(() => _indices is null ? null : new ReadOnlyCollection<int>(_indices));
  }

  public ArgumentRegularArrayElementException(string? paramName, string? message, int index, params int[] indices)
    : base(message ?? DefaultMessage(index, indices), paramName)
  {
    _index = index;
    _indices = (int[]?)indices?.Clone() ?? new[] { index };
    _indicesAccessor = new Lazy<IReadOnlyList<int>?>(() => _indices is null ? null : new ReadOnlyCollection<int>(_indices));
  }

  public ArgumentRegularArrayElementException(string? paramName, string? message, params int[] indices)
    : base(message ?? DefaultMessage(InvalidIndex, Argument.That.NotNull(indices)), paramName)
  {
    _index = indices?.Length == 1 ? indices[0] : InvalidIndex;
    _indices = (int[]?)indices?.Clone();
    _indicesAccessor = new Lazy<IReadOnlyList<int>?>(() => _indices is null ? null : new ReadOnlyCollection<int>(_indices));
  }

  public ArgumentRegularArrayElementException(string? paramName, string? message, int index)
    : base(message ?? DefaultMessage(index, null), paramName)
  {
    _index = index;
    _indices = new int[] { index };
    _indicesAccessor = new Lazy<IReadOnlyList<int>?>(() => _indices is null ? null : new ReadOnlyCollection<int>(_indices));
  }

  public ArgumentRegularArrayElementException(string? paramName, string? message, Exception? innerException, int index, params int[] indices)
    : base(message ?? DefaultMessage(index, indices), paramName, innerException)
  {
    _index = index;
    _indices = (int[]?)indices?.Clone() ?? new[] { index };
    _indicesAccessor = new Lazy<IReadOnlyList<int>?>(() => _indices is null ? null : new ReadOnlyCollection<int>(_indices));
  }

  public ArgumentRegularArrayElementException(string? paramName, string? message, Exception? innerException, params int[] indices)
    : base(message ?? DefaultMessage(InvalidIndex, indices), paramName, innerException)
  {
    _index = indices?.Length == 1 ? indices[0] : InvalidIndex;
    _indices = (int[]?)indices?.Clone();
    _indicesAccessor = new Lazy<IReadOnlyList<int>?>(() => _indices is null ? null : new ReadOnlyCollection<int>(_indices));
  }

  public ArgumentRegularArrayElementException(string? paramName, string? message, Exception? innerException, int index)
    : base(message ?? DefaultMessage(index, null), paramName, innerException)
  {
    _index = index;
    _indices = new int[] { index };
    _indicesAccessor = new Lazy<IReadOnlyList<int>?>(() => _indices is null ? null : new ReadOnlyCollection<int>(_indices));
  }

  protected ArgumentRegularArrayElementException(SerializationInfo info, StreamingContext context)
    : base(info, context)
  {
    Argument.That.NotNull(info);

    _index = (int?)info.GetValue(nameof(Index), typeof(int)) ?? InvalidIndex;
    _indices = (int[]?)info.GetValue(nameof(Indices), typeof(int[]));
    _indicesAccessor = new Lazy<IReadOnlyList<int>?>(() => _indices is null ? null : new ReadOnlyCollection<int>(_indices));
  }

  #endregion
  #region Internal methods

  protected static string DefaultMessage(int index, int[]? indices)
    => defaultMessage ??= ArrayResources.Default.FormatString(CultureInfo.CurrentCulture, ArrayMessage.InvalidArrayElement, index, indices == null ? string.Empty : PwrArray.FormatAsRegularIndices(indices));

  #endregion
  #region Properties

  public int Index
    => _index;

  /// <summary>
  /// Array element indices
  /// </summary>
  public IReadOnlyList<int>? Indices
    => _indicesAccessor.Value;

  #endregion
  #region Methods

  public override void GetObjectData(SerializationInfo info, StreamingContext context)
  {
    Argument.That.NotNull(info);

    base.GetObjectData(info, context);
    info.AddValue(nameof(Index), _index);
    info.AddValue(nameof(Indices), _indices);
  }

  #endregion
}

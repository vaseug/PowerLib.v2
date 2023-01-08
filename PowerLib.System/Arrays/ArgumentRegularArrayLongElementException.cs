using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using PowerLib.System.Validation;

namespace PowerLib.System.Arrays;

/// <summary>
/// ArgumentRegularArrayLongElementException exception
/// </summary>
[SuppressMessage("Design", "CA1032: Implement standard exception constructors", Justification = "By design")]
[Serializable]
public class ArgumentRegularArrayLongElementException : ArgumentException
{
  private const long InvalidIndex = -1L;
  private static string? defaultMessage;
  private readonly long _index;
  private readonly long[]? _indices;
  private Lazy<IReadOnlyList<long>?> _indicesAccessor;

  #region Constructors

  public ArgumentRegularArrayLongElementException(long index, params long[] indices)
    : base(DefaultMessage(index, indices))
  {
    _index = index;
    _indices = (long[]?)indices?.Clone() ?? new[] { index };
    _indicesAccessor = new Lazy<IReadOnlyList<long>?>(() => _indices is null ? null : new ReadOnlyCollection<long>(_indices));
  }

  public ArgumentRegularArrayLongElementException(params long[] indices)
    : base(DefaultMessage(InvalidIndex, indices))
  {
    _index = indices?.Length == 1 ? indices[0] : InvalidIndex;
    _indices = (long[]?)indices?.Clone();
    _indicesAccessor = new Lazy<IReadOnlyList<long>?>(() => _indices is null ? null : new ReadOnlyCollection<long>(_indices));
  }

  public ArgumentRegularArrayLongElementException(long index)
    : base(DefaultMessage(index, null))
  {
    _index = index;
    _indices = new[] { index };
    _indicesAccessor = new Lazy<IReadOnlyList<long>?>(() => _indices is null ? null : new ReadOnlyCollection<long>(_indices));
  }

  public ArgumentRegularArrayLongElementException(string? paramName, long index, params long[] indices)
    : base(DefaultMessage(index, indices), paramName)
  {
    _index = index;
    _indices = (long[]?)indices?.Clone() ?? new[] { index };
    _indicesAccessor = new Lazy<IReadOnlyList<long>?>(() => _indices is null ? null : new ReadOnlyCollection<long>(_indices));
  }

  public ArgumentRegularArrayLongElementException(string? paramName, params long[] indices)
    : base(DefaultMessage(InvalidIndex, indices), paramName)
  {
    _index = indices?.Length == 1 ? indices[0] : InvalidIndex;
    _indices = (long[]?)indices?.Clone();
    _indicesAccessor = new Lazy<IReadOnlyList<long>?>(() => _indices is null ? null : new ReadOnlyCollection<long>(_indices));
  }

  public ArgumentRegularArrayLongElementException(string? paramName, long index)
    : base(DefaultMessage(index, null), paramName)
  {
    _index = index;
    _indices = new long[] { index };
    _indicesAccessor = new Lazy<IReadOnlyList<long>?>(() => _indices is null ? null : new ReadOnlyCollection<long>(_indices));
  }

  public ArgumentRegularArrayLongElementException(string? paramName, Exception? innerException, long index, params long[] indices)
    : base(DefaultMessage(index, indices), paramName, innerException)
  {
    _index = index;
    _indices = (long[]?)indices?.Clone() ?? new[] { index };
    _indicesAccessor = new Lazy<IReadOnlyList<long>?>(() => _indices is null ? null : new ReadOnlyCollection<long>(_indices));
  }

  public ArgumentRegularArrayLongElementException(string? paramName, Exception? innerException, params long[] indices)
    : base(DefaultMessage(InvalidIndex, indices), paramName, innerException)
  {
    _index = indices?.Length == 1 ? indices[0] : InvalidIndex;
    _indices = (long[]?)indices?.Clone();
    _indicesAccessor = new Lazy<IReadOnlyList<long>?>(() => _indices is null ? null : new ReadOnlyCollection<long>(_indices));
  }

  public ArgumentRegularArrayLongElementException(string? paramName, Exception? innerException, long index)
    : base(DefaultMessage(index, null), paramName, innerException)
  {
    _index = index;
    _indices = new long[] { index };
    _indicesAccessor = new Lazy<IReadOnlyList<long>?>(() => _indices is null ? null : new ReadOnlyCollection<long>(_indices));
  }

  public ArgumentRegularArrayLongElementException(string? paramName, string? message, long index, params long[] indices)
    : base(message ?? DefaultMessage(index, indices), paramName)
  {
    _index = index;
    _indices = (long[]?)indices?.Clone() ?? new[] { index };
    _indicesAccessor = new Lazy<IReadOnlyList<long>?>(() => _indices is null ? null : new ReadOnlyCollection<long>(_indices));
  }

  public ArgumentRegularArrayLongElementException(string? paramName, string? message, params long[] indices)
    : base(message ?? DefaultMessage(InvalidIndex, Argument.That.NotNull(indices)), paramName)
  {
    _index = indices?.Length == 1 ? indices[0] : InvalidIndex;
    _indices = (long[]?)indices?.Clone();
    _indicesAccessor = new Lazy<IReadOnlyList<long>?>(() => _indices is null ? null : new ReadOnlyCollection<long>(_indices));
  }

  public ArgumentRegularArrayLongElementException(string? paramName, string? message, long index)
    : base(message ?? DefaultMessage(index, null), paramName)
  {
    _index = index;
    _indices = new long[] { index };
    _indicesAccessor = new Lazy<IReadOnlyList<long>?>(() => _indices is null ? null : new ReadOnlyCollection<long>(_indices));
  }

  public ArgumentRegularArrayLongElementException(string? paramName, string? message, Exception? innerException, long index, params long[] indices)
    : base(message ?? DefaultMessage(index, indices), paramName, innerException)
  {
    _index = index;
    _indices = (long[]?)indices?.Clone() ?? new[] { index };
    _indicesAccessor = new Lazy<IReadOnlyList<long>?>(() => _indices is null ? null : new ReadOnlyCollection<long>(_indices));
  }

  public ArgumentRegularArrayLongElementException(string? paramName, string? message, Exception? innerException, params long[] indices)
    : base(message ?? DefaultMessage(InvalidIndex, indices), paramName, innerException)
  {
    _index = indices?.Length == 1 ? indices[0] : InvalidIndex;
    _indices = (long[]?)indices?.Clone();
    _indicesAccessor = new Lazy<IReadOnlyList<long>?>(() => _indices is null ? null : new ReadOnlyCollection<long>(_indices));
  }

  public ArgumentRegularArrayLongElementException(string? paramName, string? message, Exception? innerException, long index)
    : base(message ?? DefaultMessage(index, null), paramName, innerException)
  {
    _index = index;
    _indices = new long[] { index };
    _indicesAccessor = new Lazy<IReadOnlyList<long>?>(() => _indices is null ? null : new ReadOnlyCollection<long>(_indices));
  }

  protected ArgumentRegularArrayLongElementException(SerializationInfo info, StreamingContext context)
    : base(info, context)
  {
    Argument.That.NotNull(info);

    _index = (long?)info.GetValue(nameof(Index), typeof(long)) ?? InvalidIndex;
    _indices = (long[]?)info.GetValue(nameof(Indices), typeof(long[]));
    _indicesAccessor = new Lazy<IReadOnlyList<long>?>(() => _indices is null ? null : new ReadOnlyCollection<long>(_indices));
  }

  #endregion
  #region Internal methods

  protected static string DefaultMessage(long index, long[]? indices)
    => defaultMessage ??= ArrayResources.Default.FormatString(CultureInfo.CurrentCulture, ArrayMessage.InvalidArrayElement, index, indices == null ? string.Empty : PwrArray.FormatAsLongRegularIndices(indices));

  #endregion
  #region Properties

  public long Index
    => _index;

  /// <summary>
  /// Array element indices
  /// </summary>
  public IReadOnlyList<long>? Indices
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

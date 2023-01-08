using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;

namespace PowerLib.System.Collections;

/// <summary>
/// ArgumentCollectionElementException exception
/// </summary>
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "<Pending>")]
[Serializable]
public class ArgumentCollectionElementException : ArgumentException
{
  private static string? defaultMessage;
  private int _index;

  #region Constructors

  public ArgumentCollectionElementException(int index)
    : base(DefaultMessage(index))
  {
    _index = index;
  }

  public ArgumentCollectionElementException(string? paramName, int index)
    : base(DefaultMessage(index), paramName)
  {
    _index = index;
  }

  public ArgumentCollectionElementException(string? paramName, string? message, int index)
    : base(message ?? DefaultMessage(index), paramName)
  {
    _index = index;
  }

  public ArgumentCollectionElementException(Exception? innerException, int index)
    : base(DefaultMessage(index), innerException)
  {
    _index = index;
  }

  public ArgumentCollectionElementException(string? paramName, Exception? innerException, int index)
    : base(DefaultMessage(index), paramName, innerException)
  {
    _index = index;
  }

  public ArgumentCollectionElementException(string? paramName, string? message, Exception? innerException, int index)
    : base(message ?? DefaultMessage(index), paramName, innerException)
  {
    _index = index;
  }

  protected ArgumentCollectionElementException(SerializationInfo info, StreamingContext context)
    : base(info, context)
  {
    _index = info.GetInt32(nameof(Index));
  }

  public override void GetObjectData(SerializationInfo info, StreamingContext context)
  {
    base.GetObjectData(info, context);
    info.AddValue(nameof(Index), _index);
  }

  #endregion
  #region Properties
  #region Internal methods

  protected static string DefaultMessage(int index)
    => defaultMessage ??= CollectionResources.Default.FormatString(CultureInfo.CurrentCulture, CollectionMessage.CollectionElementError, index);

  #endregion

  /// <summary>
  /// Index of array element
  /// </summary>
  public int Index
    => _index;

  #endregion
}

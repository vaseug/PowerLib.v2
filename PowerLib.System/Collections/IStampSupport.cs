namespace PowerLib.System.Collections;

/// <summary>
/// Collection versioning interace.
/// Collection supported this interface must increment version stamp number at insert, update, delete or reorder elements operations.
/// </summary>
public interface IStampSupport
{
  /// <summary>
  /// Collection version stamp number
  /// </summary>
  int Stamp { get; }
}

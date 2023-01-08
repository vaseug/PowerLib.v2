namespace PowerLib.System.Collections;

/// <summary>
/// Collection automatic capacity management interface .
/// This interaface controls automatin allocating reservation and freeing unused elements.
/// </summary>
public interface IAutoCapacityControl : ICapacityControl
{
  /// <summary>
  /// Automatitrim.
  /// </summary>
  bool AutoTrim { get; set; }

  /// <summary>
  /// Geosegrow factor.
  /// </summary>
  float GrowFactor { get; set; }

  /// <summary>
  /// Geosetrim factor.
  /// </summary>
  float TrimFactor { get; set; }
}

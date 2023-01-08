using PowerLib.System.ComponentModel;

namespace PowerLib.System;

/// <summary>
/// Quantify criteria
/// </summary>
public enum QuantifyCriteria
{
  [DisplayStringResource(typeof(QuantifyCriteria), nameof(Any))]
  Any,
  [DisplayStringResource(typeof(QuantifyCriteria), nameof(All))]
  All
}
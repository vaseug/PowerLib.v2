using PowerLib.System.ComponentModel;

namespace PowerLib.System;

/// <summary>
/// Group criteria
/// </summary>
public enum GroupCriteria
{
  [DisplayStringResource(typeof(GroupCriteria), nameof(And))]
  And,
  [DisplayStringResource(typeof(GroupCriteria), nameof(Or))]
  Or
}
using System;
using PowerLib.System.ComponentModel;

namespace PowerLib.System.Collections
{
  /// <summary>
  /// 
  /// </summary>
  [Flags]
  public enum CollectionRestrictions
  {
    /// <summary>
    /// None restrictions
    /// </summary>
    [DisplayStringResource(typeof(CollectionRestrictions), nameof(None))]
    None = 0x00,
    /// <summary>
    /// Disable add items
    /// </summary>
    [DisplayStringResource(typeof(CollectionRestrictions), nameof(DisableAdd))]
    DisableAdd = 0x01,
    /// <summary>
    /// Disable delete items
    /// </summary>
    [DisplayStringResource(typeof(CollectionRestrictions), nameof(DisableRemove))]
    DisableRemove = 0x02,
    /// <summary>
    /// Disable move items
    /// </summary>
    [DisplayStringResource(typeof(CollectionRestrictions), nameof(DisableMove))]
    DisableMove = 0x04,
    /// <summary>
    /// Disable edit items
    /// </summary>
    [DisplayStringResource(typeof(CollectionRestrictions), nameof(DisableChange))]
    DisableChange = 0x08,
    /// <summary>
    /// Fixed collection size restriction
    /// </summary>
    [DisplayStringResource(typeof(CollectionRestrictions), nameof(FixedSize))]
    FixedSize = DisableAdd | DisableRemove,
    /// <summary>
    /// Fixed collection layout restriction
    /// </summary>
    [DisplayStringResource(typeof(CollectionRestrictions), nameof(FixedLayout))]
    FixedLayout = FixedSize | DisableMove,
    /// <summary>
    /// Read only 
    /// </summary>
    [DisplayStringResource(typeof(CollectionRestrictions), nameof(ReadOnly))]
    ReadOnly = FixedLayout | DisableChange,
  }
}

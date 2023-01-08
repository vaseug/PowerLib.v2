using PowerLib.System.ComponentModel;

namespace PowerLib.System;

public enum DateTimeIntervalMatchResult
{
  [DisplayStringResource(typeof(DateTimeIntervalMatchResult), nameof(Before))]
  Before,
  [DisplayStringResource(typeof(DateTimeIntervalMatchResult), nameof(After))]
  After,
  [DisplayStringResource(typeof(DateTimeIntervalMatchResult), nameof(OverlapBefore))]
  OverlapBefore,
  [DisplayStringResource(typeof(DateTimeIntervalMatchResult), nameof(OverlapAfter))]
  OverlapAfter,
  [DisplayStringResource(typeof(DateTimeIntervalMatchResult), nameof(Equal))]
  Equal,
  [DisplayStringResource(typeof(DateTimeIntervalMatchResult), nameof(Belong))]
  Belong,
  [DisplayStringResource(typeof(DateTimeIntervalMatchResult), nameof(Enclose))]
  Enclose,
}

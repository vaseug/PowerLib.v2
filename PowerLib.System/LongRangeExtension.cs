using System;
using PowerLib.System.Validation;

namespace PowerLib.System;

public static class LongRangeExtension
{
  public static LongRange Find(this LongRange range, Func<long, long, long> match)
  {
    Argument.That.NotNull(match);

    var index = range.Index;
    var count = range.Count;
    var matched = 0L;
    var accepted = 0L;
    while (count > 0L && matched < count && accepted <= 0L)
    {
      accepted = match(index, matched);
      if (accepted == 0L)
        matched++;
      else if (accepted < 0L)
      {
        matched = 0L;
        count--;
        index++;
      }
    }
    Argument.That.Compare(accepted, matched + 1L, ComparisonCriteria.LessThanOrEqual, "Accepted value is greater matched length.");
    return new LongRange(index, accepted > 0L ? accepted : -matched - 1L);
  }

  public static LongRange FindLast(this LongRange range, Func<long, long, long> match)
  {
    Argument.That.NotNull(match);

    var index = range.Index;
    var count = range.Count;
    var matched = 0L;
    var accepted = 0L;
    while (count > 0L && matched < count && accepted <= 0L)
    {
      accepted = match(index + count - 1L - matched, matched);
      if (accepted == 0L)
        matched++;
      else if (accepted < 0L)
      {
        matched = 0L;
        count--;
      }
    }
    Argument.That.Compare(accepted, matched + 1L, ComparisonCriteria.LessThanOrEqual, "Accepted value is greater matched length.");
    return new LongRange(index + count - (accepted > 0L ? accepted : matched), accepted > 0L ? matched : -matched - 1L);
  }
}

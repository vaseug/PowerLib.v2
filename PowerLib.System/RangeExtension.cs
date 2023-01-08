using System;
using PowerLib.System.Validation;

namespace PowerLib.System;

public static class RangeExtension
{
  public static Range Find(this Range range, Func<int, int, int> match)
  {
    Argument.That.NotNull(match);

    var index = range.Index;
    var count = range.Count;
    var matched = 0;
    var accepted = 0;
    while (count > 0 && matched < count && accepted <= 0)
    {
      accepted = match(index, matched);
      if (accepted == 0)
        matched++;
      else if (accepted < 0)
      {
        matched = 0;
        count--;
        index++;
      }
    }
    Operation.That.IsValid(accepted <= matched + 1);
    return new Range(index, accepted > 0 ? accepted : -matched - 1);
  }

  public static Range FindLast(this Range range, Func<int, int, int> match)
  {
    Argument.That.NotNull(match);

    var index = range.Index;
    var count = range.Count;
    var matched = 0;
    var accepted = 0;
    while (count > 0 && matched < count && accepted <= 0)
    {
      accepted = match(index + count - 1 - matched, matched);
      if (accepted == 0)
        matched++;
      else if (accepted < 0)
      {
        matched = 0;
        count--;
      }
    }
    Operation.That.IsValid(accepted <= matched + 1);
    return new Range(index + count - (accepted > 0 ? accepted : matched), accepted > 0 ? matched : -matched - 1);
  }
}

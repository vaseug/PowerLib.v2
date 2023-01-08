using System;
using System.Collections.Generic;

namespace PowerLib.System.Test.Data;

internal sealed class ObjectData
{
  public DateTime DateTime { get; init; }

  public string? Name { get; init; }

  public int Number { get; init; }

  public double Total { get; init; }

  public static bool AllEquals(ObjectData? x, ObjectData? y)
    => x is null && y is null || x is not null && y is not null && x.Name == y.Name && x.Number == y.Number && x.DateTime == y.DateTime && x.Total == y.Total;

  public static bool KeyEquals(ObjectData? x, ObjectData? y)
    => x is null && y is null || x is not null && y is not null && x.Name == y.Name && x.Number == y.Number;

  public static int ByDateTimeCompare(ObjectData? x, ObjectData? y)
  {
    if (x is null)
      if (y is null)
        return 0;
      else
        return -1;
    else
      if (y is null)
        return 1;
    var result = Comparer<string>.Default.Compare(x.Name, y.Name);
    if (result == 0)
    {
      result = Comparer<DateTime>.Default.Compare(x.DateTime, y.DateTime);
    }
    return result;
  }

  public static int ByNumberCompare(ObjectData? x, ObjectData? y)
  {
    if (x is null)
      if (y is null)
        return 0;
      else
        return -1;
    else
      if (y is null)
      return 1;
    var result = Comparer<string>.Default.Compare(x.Name, y.Name);
    if (result == 0)
    {
      result = Comparer<int>.Default.Compare(x.Number, y.Number);
    }
    return result;
  }
}

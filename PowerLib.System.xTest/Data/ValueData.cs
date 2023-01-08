using System;
using System.Collections.Generic;

namespace PowerLib.System.Test.Data;

internal struct ValueData
{
  public DateTime DateTime { get; init; }

  public string? Name { get; init; }

  public int Number { get; init; }

  public double Total { get; init; }

  public static bool AllEquals(ValueData x, ValueData y)
    => x.Name == y.Name && x.Number == y.Number && x.DateTime == y.DateTime && x.Total == y.Total;

  public static bool KeyEquals(ValueData x, ValueData y)
    => x.Name == y.Name && x.Number == y.Number;

  public static int ByDateTimeCompare(ValueData x, ValueData y)
  {
    var result = Comparer<string>.Default.Compare(x.Name, y.Name);
    if (result == 0)
    {
      result = Comparer<DateTime>.Default.Compare(x.DateTime, y.DateTime);
    }
    return result;
  }

  public static int ByNumberCompare(ValueData x, ValueData y)
  {
    var result = Comparer<string>.Default.Compare(x.Name, y.Name);
    if (result == 0)
    {
      result = Comparer<int>.Default.Compare(x.Number, y.Number);
    }
    return result;
  }
}

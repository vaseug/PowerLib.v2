using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PowerLib.System.Linq;
using PowerLib.System.Reflection;
using PowerLib.System.Validation;

namespace PowerLib.System.Test.Linq;

[TestClass]
public class PwrEnumerableUnitTest
{
  #region Enumerable result

  static IEnumerable<object?[]> EnumerableResultData
    => new object?[][]
    {
      new object?[] { nameof(PwrEnumerable.Produce), new Type?[2],
        new object?[] { 10, (int state, double item) => state + 1, (int state) => state < 16, (int state) => (double)(state + 1000) },
        Enumerable.Range(1010, 6).Select(item => (double)item) },

      new object?[] { nameof(PwrEnumerable.TakeWhile), new Type?[2],
        new object?[] { Enumerable.Range(0, 20), 0, (int state, int item) => state + 1, (int state, int item) => state < 10 },
        Enumerable.Range(0, 10) },

      new object?[] { nameof(PwrEnumerable.SkipWhile), new Type?[2],
        new object?[] { Enumerable.Range(0, 20), 0, (int state, int item) => state + 1, (int state, int item) => state < 10 },
        Enumerable.Range(10, 10) },
    };

  [TestMethod]
  [DynamicData(nameof(EnumerableResultData), DynamicDataSourceType.Property)]
  public void EnumerableResultTest(object method, object? arguments, object parameters, object? expectedResult)
  {
    var methodName = Argument.That.InstanceOf<string>(method);
    var genericArgs = Argument.That.OfType<IList<Type?>>(arguments);
    var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
    var actualResult = Reflector.CallMethod(typeof(PwrEnumerable), methodName, MemberAccessibility.Public, null, genericArgs, positionalParams, null, null);

    Argument.That.EqualCoupled((IEnumerable)expectedResult!, (IEnumerable)actualResult!);
  }

  #endregion
  #region Scalar result

  static IEnumerable<object?[]> ScalarResultData
    => new object?[][]
    {
      //new object?[] { nameof(PwrEnumerable.SequenceEqual), new Type?[1],
      //  new object?[] { Enumerable.Range(10, 0), Enumerable.Range(0, 0) }, true },

      //new object?[] { nameof(PwrEnumerable.SequenceEqual), new Type?[1],
      //  new object?[] { Enumerable.Range(10, 10), Enumerable.Range(0, 10).Select(n => n + 10) }, true },

      //new object?[] { nameof(PwrEnumerable.SequenceEqual), new Type?[1],
      //  new object?[] { Enumerable.Range(10, 10), Enumerable.Range(10, 10).Concat(Enumerable.Range(20, 1)) }, false },

      //new object?[] { nameof(PwrEnumerable.SequenceEqual), new Type?[1],
      //  new object?[] { Enumerable.Range(10, 10), Enumerable.Range(9, 1).Concat(Enumerable.Range(10, 10)) }, false },

      new object?[] { nameof(PwrEnumerable.SequenceCompare), new Type?[1],
        new object?[] { Enumerable.Range(10, 10), Enumerable.Range(0, 10).Select(n => n + 10) }, 0 },

      new object?[] { nameof(PwrEnumerable.SequenceCompare), new Type?[1],
        new object?[] { Enumerable.Range(10, 10), Enumerable.Range(10, 5).Concat(Enumerable.Repeat(5, 1)).Concat(Enumerable.Range(16, 4)) }, 1 },

      new object?[] { nameof(PwrEnumerable.SequenceCompare), new Type?[1],
        new object?[] { Enumerable.Range(10, 10), Enumerable.Range(10, 5).Concat(Enumerable.Repeat(25, 1)).Concat(Enumerable.Range(16, 4)) }, -1 },

      new object?[] { nameof(PwrEnumerable.SequenceCompare), new Type?[1],
        new object?[] { Enumerable.Range(10, 10), Enumerable.Range(10, 9), RelativeOrder.Lower }, 1 },

      new object?[] { nameof(PwrEnumerable.SequenceCompare), new Type?[1],
        new object?[] { Enumerable.Range(10, 10), Enumerable.Range(10, 11), RelativeOrder.Lower }, -1 },

      new object?[] { nameof(PwrEnumerable.SequenceCompare), new Type?[1],
        new object?[] { Enumerable.Range(10, 10), Enumerable.Range(10, 9), RelativeOrder.Upper }, -1 },

      new object?[] { nameof(PwrEnumerable.SequenceCompare), new Type?[1],
        new object?[] { Enumerable.Range(10, 10), Enumerable.Range(10, 11), RelativeOrder.Upper }, 1 },
    };

  [TestMethod]
  [DynamicData(nameof(ScalarResultData), DynamicDataSourceType.Property)]
  public void ScalarResultTest(object method, object? arguments, object parameters, object? expectedResult)
  {
    var methodName = Argument.That.InstanceOf<string>(method);
    var genericArgs = Argument.That.OfType<IList<Type?>>(arguments);
    var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
    var actualResult = Reflector.CallMethod(typeof(PwrEnumerable), methodName, MemberAccessibility.Public, null, genericArgs, positionalParams, null, null);

    Assert.AreEqual(expectedResult, actualResult);
  }

  #endregion
  #region Sorting

  static readonly Random Random = new ();

  static IEnumerable<object?[]> SortingData
    => new object?[][]
    {
      new object?[] { nameof(PwrEnumerable.Sort), new Type?[1],
        new object?[] { Enumerable.Range(0, 100).Select(i => Random.Next(0, 1000)) } }
    };

  [TestMethod]
  [DynamicData(nameof(SortingData), DynamicDataSourceType.Property)]
  public void SortingTest(object method, object? arguments, object parameters)
  {
    var methodName = Argument.That.InstanceOf<string>(method);
    var genericArgs = Argument.That.OfType<IList<Type?>>(arguments);
    var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
    var actualResult = Reflector.CallMethod(typeof(PwrEnumerable), methodName, MemberAccessibility.Public, null, genericArgs, positionalParams, null, null)!;

    using var enumerator = ((IEnumerable<int>)actualResult).GetEnumerator();
    Assert.IsTrue(enumerator.MoveNext());
    var prev = enumerator.Current;
    while (enumerator.MoveNext())
    {
      Assert.IsTrue(enumerator.Current >= prev);
      prev = enumerator.Current;
    }
  }

  #endregion
}

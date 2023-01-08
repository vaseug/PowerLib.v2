using System;
using System.Collections.Generic;
using System.Linq;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Collections.Matching;
using PowerLib.System.Reflection;
using PowerLib.System.Test.Data;
using PowerLib.System.Validation;

namespace PowerLib.System.Test.Collections;

[TestClass]
public class ListExtensionUnitTest
{
  static readonly int[] IntegerArray = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

  #region Match

  static IEnumerable<object?[]> MatchData
    => new object?[][]
    {
      new object?[] { null, new object?[] { IntegerArray, new Predicate<int>(item => item == 10), false }, false },
      new object?[] { null, new object?[] { IntegerArray, new Predicate<int>(item => item == 9), false }, true },
      new object?[] { null, new object?[] { IntegerArray, new Predicate<int>(item => item == 5), true }, false },
      new object?[] { null, new object?[] { IntegerArray, new Predicate<int>(item => item < 10), true }, true },

      new object?[] { null, new object?[] { IntegerArray, 5, new Predicate<int>(item => item == 10), false }, false },
      new object?[] { null, new object?[] { IntegerArray, 5, new Predicate<int>(item => item == 9), false }, true },
      new object?[] { null, new object?[] { IntegerArray, 5, new Predicate<int>(item => item == 5), true }, false },
      new object?[] { null, new object?[] { IntegerArray, 5, new Predicate<int>(item => item < 10), true }, true },
    };

  [TestMethod]
  [DynamicData(nameof(MatchData), DynamicDataSourceType.Property)]
  public void MatchTest(object? arguments, object parameters, object? expectedResult)
  {
    var genericArgs = Argument.That.OfType<IList<Type?>>(arguments);
    var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
    var actualResult = Reflector.CallMethod(typeof(ListExtension), "Match", MemberAccessibility.Public, null, genericArgs, positionalParams, null, expectedResult?.GetType());
    Assert.AreEqual(expectedResult, actualResult);
  }

  #endregion
  #region Sorting

  static readonly int[] UnsortedArray = { 9, 2, 81, 49, 50, 32, 11, 99, 75, 55, 26, 39, 12, 68, 19, 43, 17, 89, 91, 76 };

  static readonly ObjectWrapper<int>[] WrappersArray = UnsortedArray
    .Select(item => new ObjectWrapper<int>() { Value = item })
    .ToArray();

  static readonly IComparer<int> InverseComparer = new InverseComparer<int>(Comparer<int>.Default);

  static readonly IComparer<ObjectWrapper<int>> WrapperComparer = new SelectComparer<ObjectWrapper<int>, int>(wrapper => wrapper?.Value ?? 0, Comparer<int>.Default);

  static IEnumerable<object?[]> SortingData
    => new object?[][]
    {
      new object?[] { nameof(ListExtension.BubbleSort), null, new object?[] { UnsortedArray }, (0, -1), null },
      new object?[] { nameof(ListExtension.BubbleSort), null, new object?[] { UnsortedArray, 5 }, (5, -1), null },
      new object?[] { nameof(ListExtension.BubbleSort), null, new object?[] { UnsortedArray, 5, 10 }, (5, 10), null },
      new object?[] { nameof(ListExtension.BubbleSort), null, new object?[] { UnsortedArray, (5, 10) }, (5, 10), null },

      new object?[] { nameof(ListExtension.BubbleSort), null, new object?[] { UnsortedArray, InverseComparer }, (0, -1), InverseComparer },
      new object?[] { nameof(ListExtension.BubbleSort), null, new object?[] { UnsortedArray, 5, InverseComparer }, (5, -1), InverseComparer },
      new object?[] { nameof(ListExtension.BubbleSort), null, new object?[] { UnsortedArray, 5, 10, InverseComparer }, (5, 10), InverseComparer },
      new object?[] { nameof(ListExtension.BubbleSort), null, new object?[] { UnsortedArray, (5, 10), InverseComparer }, (5, 10), InverseComparer },

      new object?[] { nameof(ListExtension.BubbleSort), null, new object?[] { WrappersArray, UnsortedArray }, (0, -1), WrapperComparer },
      new object?[] { nameof(ListExtension.BubbleSort), null, new object?[] { WrappersArray, UnsortedArray, 5 }, (5, -1), WrapperComparer },
      new object?[] { nameof(ListExtension.BubbleSort), null, new object?[] { WrappersArray, UnsortedArray, 5, 10 }, (5, 10), WrapperComparer },
      new object?[] { nameof(ListExtension.BubbleSort), null, new object?[] { WrappersArray, UnsortedArray, (5, 10) }, (5, 10), WrapperComparer },

      new object?[] { nameof(ListExtension.SelectionSort), null, new object?[] { UnsortedArray }, (0, -1), null },
      new object?[] { nameof(ListExtension.SelectionSort), null, new object?[] { UnsortedArray, 5 }, (5, -1), null },
      new object?[] { nameof(ListExtension.SelectionSort), null, new object?[] { UnsortedArray, 5, 10 }, (5, 10), null },
      new object?[] { nameof(ListExtension.SelectionSort), null, new object?[] { UnsortedArray, (5, 10) }, (5, 10), null },

      new object?[] { nameof(ListExtension.SelectionSort), null, new object?[] { UnsortedArray, InverseComparer }, (0, -1), InverseComparer },
      new object?[] { nameof(ListExtension.SelectionSort), null, new object?[] { UnsortedArray, 5, InverseComparer }, (5, -1), InverseComparer },
      new object?[] { nameof(ListExtension.SelectionSort), null, new object?[] { UnsortedArray, 5, 10, InverseComparer }, (5, 10), InverseComparer },
      new object?[] { nameof(ListExtension.SelectionSort), null, new object?[] { UnsortedArray, (5, 10), InverseComparer }, (5, 10), InverseComparer },

      new object?[] { nameof(ListExtension.SelectionSort), null, new object?[] { WrappersArray, UnsortedArray }, (0, -1), WrapperComparer },
      new object?[] { nameof(ListExtension.SelectionSort), null, new object?[] { WrappersArray, UnsortedArray, 5 }, (5, -1), WrapperComparer },
      new object?[] { nameof(ListExtension.SelectionSort), null, new object?[] { WrappersArray, UnsortedArray, 5, 10 }, (5, 10), WrapperComparer },
      new object?[] { nameof(ListExtension.SelectionSort), null, new object?[] { WrappersArray, UnsortedArray, (5, 10) }, (5, 10), WrapperComparer },

      new object?[] { nameof(ListExtension.InsertionSort), null, new object?[] { UnsortedArray }, (0, -1), null },
      new object?[] { nameof(ListExtension.InsertionSort), null, new object?[] { UnsortedArray, 5 }, (5, -1), null },
      new object?[] { nameof(ListExtension.InsertionSort), null, new object?[] { UnsortedArray, 5, 10 }, (5, 10), null },
      new object?[] { nameof(ListExtension.InsertionSort), null, new object?[] { UnsortedArray, (5, 10) }, (5, 10), null },

      new object?[] { nameof(ListExtension.InsertionSort), null, new object?[] { UnsortedArray, InverseComparer }, (0, -1), InverseComparer },
      new object?[] { nameof(ListExtension.InsertionSort), null, new object?[] { UnsortedArray, 5, InverseComparer }, (5, -1), InverseComparer },
      new object?[] { nameof(ListExtension.InsertionSort), null, new object?[] { UnsortedArray, 5, 10, InverseComparer }, (5, 10), InverseComparer },
      new object?[] { nameof(ListExtension.InsertionSort), null, new object?[] { UnsortedArray, (5, 10), InverseComparer }, (5, 10), InverseComparer },

      new object?[] { nameof(ListExtension.InsertionSort), null, new object?[] { WrappersArray, UnsortedArray }, (0, -1), WrapperComparer },
      new object?[] { nameof(ListExtension.InsertionSort), null, new object?[] { WrappersArray, UnsortedArray, 5 }, (5, -1), WrapperComparer },
      new object?[] { nameof(ListExtension.InsertionSort), null, new object?[] { WrappersArray, UnsortedArray, 5, 10 }, (5, 10), WrapperComparer },
      new object?[] { nameof(ListExtension.InsertionSort), null, new object?[] { WrappersArray, UnsortedArray, (5, 10) }, (5, 10), WrapperComparer },

      new object?[] { nameof(ListExtension.MergeSort), null, new object?[] { UnsortedArray }, (0, -1), null },
      new object?[] { nameof(ListExtension.MergeSort), null, new object?[] { UnsortedArray, 5 }, (5, -1), null },
      new object?[] { nameof(ListExtension.MergeSort), null, new object?[] { UnsortedArray, 5, 10 }, (5, 10), null },
      new object?[] { nameof(ListExtension.MergeSort), null, new object?[] { UnsortedArray, (5, 10) }, (5, 10), null },

      new object?[] { nameof(ListExtension.MergeSort), null, new object?[] { UnsortedArray, InverseComparer }, (0, -1), InverseComparer },
      new object?[] { nameof(ListExtension.MergeSort), null, new object?[] { UnsortedArray, 5, InverseComparer }, (5, -1), InverseComparer },
      new object?[] { nameof(ListExtension.MergeSort), null, new object?[] { UnsortedArray, 5, 10, InverseComparer }, (5, 10), InverseComparer },
      new object?[] { nameof(ListExtension.MergeSort), null, new object?[] { UnsortedArray, (5, 10), InverseComparer }, (5, 10), InverseComparer },

      new object?[] { nameof(ListExtension.MergeSort), null, new object?[] { WrappersArray, UnsortedArray }, (0, -1), WrapperComparer },
      new object?[] { nameof(ListExtension.MergeSort), null, new object?[] { WrappersArray, UnsortedArray, 5 }, (5, -1), WrapperComparer },
      new object?[] { nameof(ListExtension.MergeSort), null, new object?[] { WrappersArray, UnsortedArray, 5, 10 }, (5, 10), WrapperComparer },
      new object?[] { nameof(ListExtension.MergeSort), null, new object?[] { WrappersArray, UnsortedArray, (5, 10) }, (5, 10), WrapperComparer },

      new object?[] { nameof(ListExtension.QuickSort), null, new object?[] { UnsortedArray }, (0, -1), null },
      new object?[] { nameof(ListExtension.QuickSort), null, new object?[] { UnsortedArray, 5 }, (5, -1), null },
      new object?[] { nameof(ListExtension.QuickSort), null, new object?[] { UnsortedArray, 5, 10 }, (5, 10), null },
      new object?[] { nameof(ListExtension.QuickSort), null, new object?[] { UnsortedArray, (5, 10) }, (5, 10), null },

      new object?[] { nameof(ListExtension.QuickSort), null, new object?[] { UnsortedArray, InverseComparer }, (0, -1), InverseComparer },
      new object?[] { nameof(ListExtension.QuickSort), null, new object?[] { UnsortedArray, 5, InverseComparer }, (5, -1), InverseComparer },
      new object?[] { nameof(ListExtension.QuickSort), null, new object?[] { UnsortedArray, 5, 10, InverseComparer }, (5, 10), InverseComparer },
      new object?[] { nameof(ListExtension.QuickSort), null, new object?[] { UnsortedArray, (5, 10), InverseComparer }, (5, 10), InverseComparer },

      new object?[] { nameof(ListExtension.QuickSort), null, new object?[] { WrappersArray, UnsortedArray }, (0, -1), WrapperComparer },
      new object?[] { nameof(ListExtension.QuickSort), null, new object?[] { WrappersArray, UnsortedArray, 5 }, (5, -1), WrapperComparer },
      new object?[] { nameof(ListExtension.QuickSort), null, new object?[] { WrappersArray, UnsortedArray, 5, 10 }, (5, 10), WrapperComparer },
      new object?[] { nameof(ListExtension.QuickSort), null, new object?[] { WrappersArray, UnsortedArray, (5, 10) }, (5, 10), WrapperComparer },

      new object?[] { nameof(ListExtension.HeapSort), null, new object?[] { UnsortedArray }, (0, -1), null },
      new object?[] { nameof(ListExtension.HeapSort), null, new object?[] { UnsortedArray, 5 }, (5, -1), null },
      new object?[] { nameof(ListExtension.HeapSort), null, new object?[] { UnsortedArray, 5, 10 }, (5, 10), null },
      new object?[] { nameof(ListExtension.HeapSort), null, new object?[] { UnsortedArray, (5, 10) }, (5, 10), null },

      new object?[] { nameof(ListExtension.HeapSort), null, new object?[] { UnsortedArray, InverseComparer }, (0, -1), InverseComparer },
      new object?[] { nameof(ListExtension.HeapSort), null, new object?[] { UnsortedArray, 5, InverseComparer }, (5, -1), InverseComparer },
      new object?[] { nameof(ListExtension.HeapSort), null, new object?[] { UnsortedArray, 5, 10, InverseComparer }, (5, 10), InverseComparer },
      new object?[] { nameof(ListExtension.HeapSort), null, new object?[] { UnsortedArray, (5, 10), InverseComparer }, (5, 10), InverseComparer },

      new object?[] { nameof(ListExtension.HeapSort), null, new object?[] { WrappersArray, UnsortedArray }, (0, -1), WrapperComparer },
      new object?[] { nameof(ListExtension.HeapSort), null, new object?[] { WrappersArray, UnsortedArray, 5 }, (5, -1), WrapperComparer },
      new object?[] { nameof(ListExtension.HeapSort), null, new object?[] { WrappersArray, UnsortedArray, 5, 10 }, (5, 10), WrapperComparer },
      new object?[] { nameof(ListExtension.HeapSort), null, new object?[] { WrappersArray, UnsortedArray, (5, 10) }, (5, 10), WrapperComparer },
    };

  [TestMethod]
  [DynamicData(nameof(SortingData), DynamicDataSourceType.Property)]
  public void SortingTest(object name, object? arguments, object parameters, object range, object? comparer)
  {
    var methodName = Argument.That.InstanceOf<string>(name);
    var genericArgs = Argument.That.OfType<IList<Type?>>(arguments);
    var originalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
    var positionalParams = originalParams
      .Select(item => item is Array array ? array.Clone() : item)
      .ToArray();
    Reflector.CallMethod(typeof(ListExtension), methodName, MemberAccessibility.Public, null, genericArgs, positionalParams, null);
    AssertSorted(originalParams[0], positionalParams[0], range, comparer);
  }

  private static void AssertSorted(object? originalArray, object? actualArray, object range, object? comparer)
  {
    var orgArray = Argument.That.NotNull(originalArray as Array);
    var actArray = Argument.That.NotNull(actualArray as Array);
    Assert.AreEqual(orgArray.Length, actArray.Length);
    if (comparer != null)
      Assert.IsInstanceOfType(comparer, typeof(IComparer<>).MakeGenericType(actArray.GetType().GetElementType()!));
    else
      comparer = Reflector.GetProperty(typeof(Comparer<>).MakeGenericType(actArray.GetType().GetElementType()!), nameof(Comparer<dynamic>.Default), MemberAccessibility.Public, null, null);
    var rangeTuple = ((int index, int count))range;
    int index = rangeTuple.index, count = rangeTuple.count < 0 ? actArray.Length - index : rangeTuple.count;
    for (int i = 0; i < actArray.Length; i++)
    {
      if (i < index || i >= index + count)
      {
        var result = Reflector.CallMethod(comparer!, "Compare", MemberAccessibility.Public, null, new[] { orgArray.GetValue(i), actArray.GetValue(i) }, null, typeof(int));
        Assert.IsInstanceOfType(result, typeof(int));
        Assert.IsTrue((int)result! == 0);
      }
      else if (i > index)
      {
        var result = Reflector.CallMethod(comparer!, "Compare", MemberAccessibility.Public, null, new[] { actArray.GetValue(i - 1), actArray.GetValue(i) }, null, typeof(int));
        Assert.IsInstanceOfType(result, typeof(int));
        Assert.IsTrue((int)result! <= 0);
      }
    }
  }

  #endregion
  #region Sequence comparison

  static readonly int[] Sequence_1 = { 9, 2, 81, 49, 50, 32, 11, 99, 75, 55, 26, 39, 12, 68, 19, 43, 17, 89, 91, 76 };

  static readonly int[] Sequence_2 = Sequence_1
    .ToArray();

  static readonly int[] Sequence_3 = Sequence_1
    .Select((item, index) => item + (index == 4 ? 100 : 0))
    .ToArray();

  static readonly int[] Sequence_4 = Sequence_1
    .Select((item, index) => item + (index == 4 || index == 16 ? 100 : 0))
    .ToArray();

  static readonly int[] Sequence_5 = Sequence_1
    .Take(15)
    .ToArray();

  static IEnumerable<object?[]> SequenceData
    => new object?[][]
    {
      new object?[] { nameof(ListExtension.SequenceCompare), null, new object?[] { Sequence_1, Sequence_2 }, 0 },
      new object?[] { nameof(ListExtension.SequenceCompare), null, new object?[] { Sequence_1, 5, Sequence_3, 5 }, 0 },
      new object?[] { nameof(ListExtension.SequenceCompare), null, new object?[] { Sequence_1, 5, Sequence_4, 5, 10 }, 0 },
      new object?[] { nameof(ListExtension.SequenceCompare), null, new object?[] { Sequence_1, 0, Sequence_4, 0 }, -5 },
      new object?[] { nameof(ListExtension.SequenceCompare), null, new object?[] { Sequence_4, 0, Sequence_1, 0 }, 5 },
      new object?[] { nameof(ListExtension.SequenceCompare), null, new object?[] { Sequence_1, 0, Sequence_5, 0 }, 16 },
      new object?[] { nameof(ListExtension.SequenceCompare), null, new object?[] { Sequence_5, 3, Sequence_1, 3 }, -13 },

      new object?[] { nameof(ListExtension.SequenceEqual), null, new object?[] { Sequence_1, Sequence_2 }, true },
      new object?[] { nameof(ListExtension.SequenceEqual), null, new object?[] { Sequence_1, 5, Sequence_3, 5 }, true },
      new object?[] { nameof(ListExtension.SequenceEqual), null, new object?[] { Sequence_1, 5, Sequence_4, 5, 10 }, true },
      new object?[] { nameof(ListExtension.SequenceEqual), null, new object?[] { Sequence_1, 0, Sequence_4, 0 }, false },
      new object?[] { nameof(ListExtension.SequenceEqual), null, new object?[] { Sequence_4, 0, Sequence_1, 0 }, false },
      new object?[] { nameof(ListExtension.SequenceEqual), null, new object?[] { Sequence_1, 0, Sequence_5, 0 }, false },
      new object?[] { nameof(ListExtension.SequenceEqual), null, new object?[] { Sequence_5, 3, Sequence_1, 3 }, false },
    };

  [TestMethod]
  [DynamicData(nameof(SequenceData), DynamicDataSourceType.Property)]
  public void SequenceTest(object name, object? arguments, object parameters, object expectedResult)
  {
    var methodName = Argument.That.InstanceOf<string>(name);
    var genericArgs = Argument.That.OfType<IList<Type?>>(arguments);
    var originalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
    var positionalParams = originalParams
      .Select(item => item is Array array ? array.Clone() : item)
      .ToArray();

    var actualResult = Reflector.CallMethod(typeof(ListExtension), methodName, MemberAccessibility.Public, null, genericArgs, positionalParams, null, expectedResult?.GetType());
    Assert.AreEqual(expectedResult, actualResult);
  }

  #endregion
}

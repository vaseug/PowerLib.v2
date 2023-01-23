using System;
using System.Collections.Generic;
using System.Linq;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Collections.Matching;
using PowerLib.System.Reflection;
using PowerLib.System.Test.Data;
using PowerLib.System.Validation;

namespace PowerLib.System.Test.Collections;

public class ListExtensionUnitTest
{
  static readonly int[] IntegerArray = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

  #region Match

  public static IEnumerable<object?[]> MatchData
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

  [Theory]
  [MemberData(nameof(MatchData))]
  public void MatchTest(Type?[] genericArgs, object?[] positionalParams, object? expectedResult)
  {
    var actualResult = Reflector.CallStaticMethod(typeof(ListExtension), "Match", MemberAccessibility.Public, null, genericArgs, positionalParams, null, expectedResult?.GetType());

    Assert.Equal(expectedResult, actualResult);
  }

  #endregion
  #region Sorting

  static readonly int[] UnsortedArray = { 9, 2, 81, 49, 50, 32, 11, 99, 75, 55, 26, 39, 12, 68, 19, 43, 17, 89, 91, 76 };

  static readonly ObjectWrapper<int>[] WrappersArray = UnsortedArray
    .Select(item => new ObjectWrapper<int>() { Value = item })
    .ToArray();

  static readonly IComparer<int> InverseComparer = new InverseComparer<int>(Comparer<int>.Default);

  static readonly IComparer<ObjectWrapper<int>> WrapperComparer = new SelectComparer<ObjectWrapper<int>, int>(wrapper => wrapper?.Value ?? 0, Comparer<int>.Default);

  public static IEnumerable<object?[]> SortingData
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

  [Theory]
  [MemberData(nameof(SortingData))]
  public void SortingTest(string methodName, Type?[] genericArgs, object?[] originalParams, (int index, int count) range, object? comparer)
  {
    var positionalParams = originalParams
      .Select(item => item is Array array ? array.Clone() : item)
      .ToArray();
    Reflector.CallStaticMethod(typeof(ListExtension), methodName, MemberAccessibility.Public, null, genericArgs, positionalParams, null);

    AssertSorted((Array?)originalParams[0], (Array?)positionalParams[0], range, comparer);
  }

  private static void AssertSorted(Array? originalArray, Array? actualArray, object range, object? comparer)
  {
    Assert.Equal(originalArray.Length, actualArray.Length);
    if (comparer != null)
      Assert.IsAssignableFrom(typeof(IComparer<>).MakeGenericType(actualArray.GetType().GetElementType()!), comparer);
    else
      comparer = Reflector.GetStaticPropertyValue(typeof(Comparer<>).MakeGenericType(actualArray.GetType().GetElementType()!), nameof(Comparer<dynamic>.Default), MemberAccessibility.Public, null, null);
    var rangeTuple = ((int index, int count))range;
    int index = rangeTuple.index, count = rangeTuple.count < 0 ? actualArray.Length - index : rangeTuple.count;
    for (int i = 0; i < actualArray.Length; i++)
    {
      if (i < index || i >= index + count)
      {
        var result = Reflector.CallInstanceMethod(comparer!, "Compare", MemberAccessibility.Public, null, new[] { originalArray.GetValue(i), actualArray.GetValue(i) }, null, typeof(int));
        Assert.IsType<int>(result);
        Assert.True((int)result! == 0);
      }
      else if (i > index)
      {
        var result = Reflector.CallInstanceMethod(comparer!, "Compare", MemberAccessibility.Public, null, new[] { actualArray.GetValue(i - 1), actualArray.GetValue(i) }, null, typeof(int));
        Assert.IsType<int>(result);
        Assert.True((int)result! <= 0);
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

  public static IEnumerable<object?[]> SequenceData
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

  [Theory]
  [MemberData(nameof(SequenceData))]
  public void SequenceTest(string methodName, Type?[] genericArgs, object?[] originalParams, object expectedResult)
  {
    var positionalParams = originalParams
      .Select(item => item is Array array ? array.Clone() : item)
      .ToArray();

    var actualResult = Reflector.CallStaticMethod(typeof(ListExtension), methodName, MemberAccessibility.Public, null, genericArgs, positionalParams, null, expectedResult?.GetType());
    Assert.Equal(expectedResult, actualResult);
  }

  #endregion
}

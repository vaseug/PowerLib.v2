using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using PowerLib.System.Collections;
using PowerLib.System.Collections.Matching;
using PowerLib.System.Reflection;
using PowerLib.System.Test.Data;
using PowerLib.System.Validation;

namespace PowerLib.System.Test.Validation
{
  public class ArgumentUnitTest
  {
    #region Null validation

    public static IEnumerable<object?[]> NotNullData
      => new object?[][]
      {
        new object?[] { null, new object?[] { 100 } },
        new object?[] { null, new object?[] { DateTime.Now } },
        new object?[] { null, new object?[] { ObjectWrapper.Create(101.5M) } },
      };

    public static IEnumerable<object?[]> NullData
      => new object?[][]
      {
        new object?[] { new[] { typeof(object) }, new object?[] { null } },
        new object?[] { null, new object?[] { TypedValue.DefaultOf<ObjectWrapper<int>>() } },
        new object?[] { new[] { typeof(double) }, new object?[] { default(double?) } },
      };

    [Theory]
    [MemberData(nameof(NotNullData))]
    public void NotNullSuccessful(Type?[] genericArgs, object?[] positionalParams)
    {
      var result = Reflector.CallInstanceMethod(Argument.That, nameof(Argument.NotNull), MemberAccessibility.Public, genericArgs, positionalParams, null, null);

      Assert.Equal(positionalParams?[0], result);
    }

    [Theory]
    [MemberData(nameof(NullData))]
    public void NotNullFailed(Type?[] genericArgs, object?[] positionalParams)
    {
      try
      {
        Reflector.CallInstanceMethod(Argument.That, nameof(Argument.NotNull), MemberAccessibility.Public, genericArgs, positionalParams, null, null);
        throw new InvalidOperationException();
      }
      catch (TargetInvocationException ex) when (ex.InnerException?.GetType() == typeof(ArgumentNullException))
      { }
    }

    #endregion
    #region Type validation

    public static IEnumerable<object?[]> TypeSuccessfulData
      => new object?[][]
      {
        new object?[] { nameof(Argument.InstanceOf), null, new object?[] { 10, TypeCode.Int32 } },
        new object?[] { nameof(Argument.NotInstanceOf), null, new object?[] { 10, TypeCode.Int16 } },
        new object?[] { nameof(Argument.InstanceOf), null, new object?[] { DateTime.Now, TypeCode.DateTime } },
        new object?[] { nameof(Argument.NotInstanceOf), null, new object?[] { DateTime.Now, TypeCode.Int64 } },
        new object?[] { nameof(Argument.InstanceOf), null, new object?[] { "String", TypeCode.String } },
        new object?[] { nameof(Argument.NotInstanceOf), null, new object?[] { "String", TypeCode.Object } },
        new object?[] { nameof(Argument.InstanceOf), null, new object?[] { DateTime.Now, typeof(DateTime) } },
        new object?[] { nameof(Argument.NotInstanceOf), null, new object?[] { DateTime.Now, typeof(Int64) } },
        new object?[] { nameof(Argument.InstanceOf), new[] { typeof(TimeSpan) }, new object?[] { DateTime.Now.TimeOfDay } },
        new object?[] { nameof(Argument.NotInstanceOf), new[] { typeof(DateTime) }, new object?[] { DateTime.Now.TimeOfDay } },
        new object?[] { nameof(Argument.MadeOf), Type.EmptyTypes, new object?[] { new List<int>(), typeof(List<>) } },
        new object?[] { nameof(Argument.NotMadeOf), Type.EmptyTypes, new object?[] { new Collection<decimal>(), typeof(List<>) } },
      };

    public static IEnumerable<object?[]> TypeFailedData
      => new object?[][]
      {
        new object?[] { nameof(Argument.InstanceOf), null, new object?[] { (short)10, TypeCode.Int32 }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.NotInstanceOf), null, new object?[] { (short)10, TypeCode.Int16 }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.InstanceOf), null, new object?[] { DateTime.Now, TypeCode.Decimal}, typeof(ArgumentException) },
        new object?[] { nameof(Argument.NotInstanceOf), null, new object?[] { DateTime.Now, TypeCode.DateTime}, typeof(ArgumentException) },
        new object?[] { nameof(Argument.InstanceOf), null, new object?[] { "String", TypeCode.Object }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.NotInstanceOf), null, new object?[] { "String", TypeCode.String }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.InstanceOf), null, new object?[] { DateTime.Now, typeof(Int64) }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.NotInstanceOf), null, new object?[] { DateTime.Now, typeof(DateTime) }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.InstanceOf), new[] { typeof(DateTime) }, new object?[] { DateTime.Now.TimeOfDay }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.NotInstanceOf), new[] { typeof(TimeSpan) }, new object?[] { DateTime.Now.TimeOfDay }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.MadeOf), Type.EmptyTypes, new object?[] { new List<int>(), typeof(Collection<>) }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.NotMadeOf), Type.EmptyTypes, new object?[] { new List<decimal>(), typeof(List<>) }, typeof(ArgumentException) },
      };

    [Theory]
    [MemberData(nameof(TypeSuccessfulData))]
    public void TypeValidationSuccessful(string methodName, Type?[] genericArgs, object?[] positionalParams)
    {
      var result = Reflector.CallInstanceMethod(Argument.That, methodName, MemberAccessibility.Public, genericArgs, positionalParams, null, null);

      Assert.Equal(positionalParams?[0], result);
    }

    [Theory]
    [MemberData(nameof(TypeFailedData))]
    public void TypeValidationFailed(string methodName, Type?[] genericArgs, object?[] positionalParams, Type exceptionType)
    {
      try
      {
        Reflector.CallInstanceMethod(Argument.That, methodName, MemberAccessibility.Public, genericArgs, positionalParams, null, null);
        throw new InvalidOperationException();
      }
      catch (TargetInvocationException ex) when (ex.InnerException?.GetType() == exceptionType)
      { }
    }

    #endregion
    #region Boolean validation

    public static IEnumerable<object?[]> BooleanDataSuccessful
      => new object?[][]
      {
        new object?[] { nameof(Argument.False), new object?[] { false } },
        new object?[] { nameof(Argument.True), new object?[] { true } },
      };

    public static IEnumerable<object?[]> BooleanDataFailed
      => new object?[][]
      {
        new object?[] { nameof(Argument.False), new object?[] { true }, typeof(ArgumentOutOfRangeException) },
        new object?[] { nameof(Argument.True), new object?[] { false }, typeof(ArgumentOutOfRangeException) },
      };

    [Theory]
    [MemberData(nameof(BooleanDataSuccessful))]
    public void BooleanValidationSuccessful(string methodName, object?[] positionalParams)
    {
      var result = Reflector.CallInstanceMethod(Argument.That, methodName, MemberAccessibility.Public, null, positionalParams, null, null);

      Assert.Equal(positionalParams?[0], result);
    }

    [Theory]
    [MemberData(nameof(BooleanDataFailed))]
    public void BooleanValidationFailed(string methodName, object?[] positionalParams, Type exceptionType)
    {
      try
      {
        Reflector.CallInstanceMethod(Argument.That, methodName, MemberAccessibility.Public, null, positionalParams, null, null);
        throw new InvalidOperationException();
      }
      catch (TargetInvocationException ex) when (ex.InnerException?.GetType() == exceptionType)
      { }
    }

    #endregion
    #region DateTime range validation

    static readonly DateTime OriginDateTime = new(2020, 10, 20, 11, 22, 33, 444);

    public static IEnumerable<object?[]> DateTimeRangeSuccessfulData
      => new object?[][]
      {
        new object?[] { nameof(Argument.InRange), new object?[] { OriginDateTime, DateTime.MaxValue - OriginDateTime } },
        new object?[] { nameof(Argument.InRange), new object?[] { OriginDateTime, DateTime.MinValue - OriginDateTime } },
      };

    public static IEnumerable<object?[]> DateTimeRangeFailedData
      => new object?[][]
      {
        new object?[] { nameof(Argument.InRange), new object?[] { OriginDateTime, DateTime.MaxValue - OriginDateTime + TimeSpan.FromMilliseconds(1) }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.InRange), new object?[] { OriginDateTime, DateTime.MinValue - OriginDateTime - TimeSpan.FromMilliseconds(1) }, typeof(ArgumentException) },
      };

    [Theory]
    [MemberData(nameof(DateTimeRangeSuccessfulData))]
    public void DateTimeRangeValidationSuccessful(string methodName, object?[] positionalParams)
    {
      var result = Reflector.CallInstanceMethod(Argument.That, methodName, MemberAccessibility.Public, null, positionalParams, null, null);
      Assert.NotNull(result);
      Assert.IsType<DateTimeInterval>(result);
      var interval = (DateTimeInterval)result;
      Assert.Equal(positionalParams?[0], interval.DateTime);
      Assert.Equal(positionalParams?[1], interval.TimeSpan);
    }

    [Theory]
    [MemberData(nameof(DateTimeRangeFailedData))]
    public void DateTimeRangeValidationFailed(string methodName, object?[] positionalParams, Type exceptionType)
    {
      try
      {
        Reflector.CallInstanceMethod(Argument.That, methodName, MemberAccessibility.Public, null, positionalParams, null, null);
        throw new InvalidOperationException();
      }
      catch (TargetInvocationException ex) when (ex.InnerException?.GetType() == exceptionType)
      { }
    }

    #endregion
    #region String validation

    const string AlphaNumericSymbols = " 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    const string RegexSymbols = "(Name: John Doe)";

    public static IEnumerable<object?[]> StringSuccessfulData
      => new object?[][]
      {
        new object?[] { nameof(Argument.NotNullOrEmpty), new object?[] { "X" } },
        new object?[] { nameof(Argument.NotNullOrEmpty), new object?[] { "ABC" } },
        new object?[] { nameof(Argument.NotNullOrWhitespace), new object?[] { "X" } },
        new object?[] { nameof(Argument.NotNullOrWhitespace), new object?[] { " Y " } },
        new object?[] { nameof(Argument.Empty), new object?[] { "" } },
        new object?[] { nameof(Argument.NotEmpty), new object?[] { " " } },
        new object?[] { nameof(Argument.NotEmpty), new object?[] { "A" } },
        new object?[] { nameof(Argument.Whitespace), new object?[] { "" } },
        new object?[] { nameof(Argument.Whitespace), new object?[] { "  " } },
        new object?[] { nameof(Argument.NotWhitespace), new object?[] { "  0" } },
        new object?[] { nameof(Argument.Contains), new object?[] { AlphaNumericSymbols, "456", TypedValue.DefaultOf<CultureInfo>() } },
        new object?[] { nameof(Argument.Contains), new object?[] { AlphaNumericSymbols, "FgHiJ", CultureInfo.CurrentCulture, CompareOptions.IgnoreCase } },
        new object?[] { nameof(Argument.NotContains), new object?[] { AlphaNumericSymbols, "465", TypedValue.DefaultOf<CultureInfo>() } },
        new object?[] { nameof(Argument.NotContains), new object?[] { AlphaNumericSymbols, "FgHiJ", CultureInfo.CurrentCulture, CompareOptions.None } },
        new object?[] { nameof(Argument.StartsWith), new object?[] { AlphaNumericSymbols, " 0123456789ABC", TypedValue.DefaultOf<CultureInfo>() } },
        new object?[] { nameof(Argument.StartsWith), new object?[] { AlphaNumericSymbols, " 0123456789abc", CultureInfo.CurrentCulture, CompareOptions.IgnoreCase } },
        new object?[] { nameof(Argument.NotStartsWith), new object?[] { AlphaNumericSymbols, "0123456789", TypedValue.DefaultOf<CultureInfo>() } },
        new object?[] { nameof(Argument.NotStartsWith), new object?[] { AlphaNumericSymbols, " 0123456789abc", CultureInfo.CurrentCulture, CompareOptions.None } },
        new object?[] { nameof(Argument.EndsWith), new object?[] { AlphaNumericSymbols, "MNOPQRSTUVWXYZ", TypedValue.DefaultOf<CultureInfo>() } },
        new object?[] { nameof(Argument.EndsWith), new object?[] { AlphaNumericSymbols, "PQRStUVwXyZ", CultureInfo.CurrentCulture, CompareOptions.IgnoreCase } },
        new object?[] { nameof(Argument.NotEndsWith), new object?[] { AlphaNumericSymbols, "STUVXYZ", TypedValue.DefaultOf<CultureInfo>() } },
        new object?[] { nameof(Argument.NotEndsWith), new object?[] { AlphaNumericSymbols, "PQRStUVwXyZ", CultureInfo.CurrentCulture, CompareOptions.None } },
        new object?[] { nameof(Argument.Match), new object?[] { RegexSymbols, @"^\(\w+[:](?:\s+\w+)+\)$", RegexOptions.None } },
        new object?[] { nameof(Argument.NotMatch), new object?[] { RegexSymbols, @"^\(\w+[-](?:\s+\w+)+\)$", RegexOptions.None } },
      };

    public static IEnumerable<object?[]> StringFailedData
      => new object?[][]
      {
        new object?[] { nameof(Argument.NotNullOrEmpty), new object?[] { null }, typeof(ArgumentNullException) },
        new object?[] { nameof(Argument.NotNullOrEmpty), new object?[] { string.Empty }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.NotNullOrWhitespace), new object?[] { null }, typeof(ArgumentNullException) },
        new object?[] { nameof(Argument.NotNullOrWhitespace), new object?[] { string.Empty }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.NotNullOrWhitespace), new object?[] { "    " }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.Empty), new object?[] { null }, typeof(ArgumentNullException) },
        new object?[] { nameof(Argument.Empty), new object?[] { " " }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.NotEmpty), new object?[] { null }, typeof(ArgumentNullException) },
        new object?[] { nameof(Argument.NotEmpty), new object?[] { "" }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.Whitespace), new object?[] { null }, typeof(ArgumentNullException) },
        new object?[] { nameof(Argument.Whitespace), new object?[] { "  X" }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.Whitespace), new object?[] { "A" }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.NotWhitespace), new object?[] { null }, typeof(ArgumentNullException) },
        new object?[] { nameof(Argument.NotWhitespace), new object?[] { "" }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.NotWhitespace), new object?[] { "  " }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.Contains), new object?[] { null, "456", TypedValue.DefaultOf<CultureInfo>() }, typeof(ArgumentNullException) },
        new object?[] { nameof(Argument.Contains), new object?[] { AlphaNumericSymbols, null, TypedValue.DefaultOf<CultureInfo>() }, typeof(ArgumentNullException) },
        new object?[] { nameof(Argument.Contains), new object?[] { AlphaNumericSymbols, "476", TypedValue.DefaultOf<CultureInfo>() }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.Contains), new object?[] { AlphaNumericSymbols, "FgHiJ", CultureInfo.CurrentCulture, CompareOptions.None }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.NotContains), new object?[] { null, "456", TypedValue.DefaultOf<CultureInfo>() }, typeof(ArgumentNullException) },
        new object?[] { nameof(Argument.NotContains), new object?[] { AlphaNumericSymbols, null, TypedValue.DefaultOf<CultureInfo>() }, typeof(ArgumentNullException) },
        new object?[] { nameof(Argument.NotContains), new object?[] { AlphaNumericSymbols, "456", TypedValue.DefaultOf<CultureInfo>() }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.NotContains), new object?[] { AlphaNumericSymbols, "FgHiJ", CultureInfo.CurrentCulture, CompareOptions.IgnoreCase }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.StartsWith), new object?[] { null, " 0123456789ABC", TypedValue.DefaultOf<CultureInfo>() }, typeof(ArgumentNullException) },
        new object?[] { nameof(Argument.StartsWith), new object?[] { AlphaNumericSymbols, null, CultureInfo.CurrentCulture, CompareOptions.None }, typeof(ArgumentNullException) },
        new object?[] { nameof(Argument.StartsWith), new object?[] { AlphaNumericSymbols, " 0123456X789ABC", TypedValue.DefaultOf<CultureInfo>() }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.StartsWith), new object?[] { AlphaNumericSymbols, " 0123456789abc", CultureInfo.CurrentCulture, CompareOptions.None }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.NotStartsWith), new object?[] { null, "0123456789", TypedValue.DefaultOf<CultureInfo>() }, typeof(ArgumentNullException) },
        new object?[] { nameof(Argument.NotStartsWith), new object?[] { AlphaNumericSymbols, null, CultureInfo.CurrentCulture, CompareOptions.None }, typeof(ArgumentNullException) },
        new object?[] { nameof(Argument.NotStartsWith), new object?[] { AlphaNumericSymbols, " 0123456789", TypedValue.DefaultOf<CultureInfo>() }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.NotStartsWith), new object?[] { AlphaNumericSymbols, " 0123456789abc", CultureInfo.CurrentCulture, CompareOptions.IgnoreCase }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.EndsWith), new object?[] { null, "MNOPQRSTUVWXYZ", TypedValue.DefaultOf<CultureInfo>() }, typeof(ArgumentNullException) },
        new object?[] { nameof(Argument.EndsWith), new object?[] { AlphaNumericSymbols, null, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase }, typeof(ArgumentNullException) },
        new object?[] { nameof(Argument.EndsWith), new object?[] { AlphaNumericSymbols, "MNOPQRSTUVWXYZ!", TypedValue.DefaultOf<CultureInfo>() }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.EndsWith), new object?[] { AlphaNumericSymbols, "PQRStUVwXyZ", CultureInfo.CurrentCulture, CompareOptions.None }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.NotEndsWith), new object?[] { null, "STUVXYZ", TypedValue.DefaultOf<CultureInfo>() }, typeof(ArgumentNullException) },
        new object?[] { nameof(Argument.NotEndsWith), new object?[] { AlphaNumericSymbols, null, CultureInfo.CurrentCulture, CompareOptions.None }, typeof(ArgumentNullException) },
        new object?[] { nameof(Argument.NotEndsWith), new object?[] { AlphaNumericSymbols, "STUVWXYZ", TypedValue.DefaultOf<CultureInfo>() }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.NotEndsWith), new object?[] { AlphaNumericSymbols, "PQRStUVwXyZ", CultureInfo.CurrentCulture, CompareOptions.IgnoreCase }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.Match), new object?[] { null, @"^\(\w+[-](?:\s+\w+)+\)$", RegexOptions.None }, typeof(ArgumentNullException) },
        new object?[] { nameof(Argument.Match), new object?[] { RegexSymbols, null, RegexOptions.None }, typeof(ArgumentNullException) },
        new object?[] { nameof(Argument.Match), new object?[] { RegexSymbols, @"^\(\w+[-](?:\s+\w+)+\)$", RegexOptions.None }, typeof(ArgumentException) },
        new object?[] { nameof(Argument.NotMatch), new object?[] { null, @"^\(\w+[:](?:\s+\w+)+\)$", RegexOptions.None }, typeof(ArgumentNullException) },
        new object?[] { nameof(Argument.NotMatch), new object?[] { RegexSymbols, null, RegexOptions.None }, typeof(ArgumentNullException) },
        new object?[] { nameof(Argument.NotMatch), new object?[] { RegexSymbols, @"^\(\w+[:](?:\s+\w+)+\)$", RegexOptions.None }, typeof(ArgumentException) },
      };

    [Theory]
    [MemberData(nameof(StringSuccessfulData))]
    public void StringValidationSuccessful(object name, object parameters)
    {
      var methodName = Argument.That.InstanceOf<string>(name);
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      var result = Reflector.CallInstanceMethod(Argument.That, methodName, MemberAccessibility.Public, null, positionalParams, null, null);
      Assert.Equal(positionalParams[0], result);
    }

    [Theory]
    [MemberData(nameof(StringFailedData))]
    public void StringValidationFailed(object name, object parameters, object exception)
    {
      var methodName = Argument.That.InstanceOf<string>(name);
      var exceptionType = Argument.That.OfType<Type>(exception);
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      try
      {
        Reflector.CallInstanceMethod(Argument.That, methodName, MemberAccessibility.Public, null, positionalParams, null, null);
        throw new InvalidOperationException();
      }
      catch (TargetInvocationException ex) when (ex.InnerException?.GetType() == exceptionType)
      { }
    }

    #endregion
    #region Equality validation

    public static IEnumerable<object?[]> EqaulsData
      => new object?[][]
      {
        new object?[] { null, new object?[] { 1, 1, TypedValue.DefaultOf<Equality<int>>() } },
        new object?[] { null, new object?[] { 10L, 10L, EqualityComparer<long>.Default } },
        new object?[] { null, new object?[] { 100F, 100F, TypedValue.DefaultOf<IEqualityComparer<float>>() } },
        new object?[] { null, new object?[] { 1000D, 1000D, EqualityComparer<double>.Default } },
        new object?[] { null, new object?[] { 2000M, 2000M, EqualityComparer<decimal>.Default } },
        new object?[]
        {
          null, new object?[]
          {
            new DateTime(2020, 1, 2, 3, 4, 5, 60),
            new DateTime(2020, 1, 2, 3, 4, 5, 60),
            EqualityComparer<DateTime>.Default
          }
        },
        new object?[]
        {
          null, new object?[]
          {
            new ValueData { Name = "Same", Number = 1, DateTime = new DateTime(2020, 1, 7), Total = 1000D },
            new ValueData { Name = "Same", Number = 1, DateTime = new DateTime(2020, 1, 7), Total = 1000D },
            new CustomEqualityComparer<ValueData>(ValueData.AllEquals)
          }
        },
        new object?[]
        {
          null, new object?[]
          {
            TypedValue.ValueOf<ValueData?>(new ValueData { Name = "Same", Number = 1, DateTime = new DateTime(2020, 12, 20), Total = 500D }),
            TypedValue.ValueOf<ValueData?>(new ValueData { Name = "Same", Number = 1, DateTime = new DateTime(2020, 12, 20), Total = 500D }),
            Equality.AsNullableEquality<ValueData>(ValueData.AllEquals)
          }
        },
        new object?[]
        {
          new[] { typeof(ValueData?) }, new object?[]
          {
            new ValueData { Name = "Same", Number = 1, DateTime = new DateTime(2020, 12, 20), Total = 500D },
            new ValueData { Name = "Same", Number = 1, DateTime = new DateTime(2020, 12, 20), Total = 500D },
            new NullableEqualityComparer<ValueData>(ValueData.AllEquals, false),
          }
        },
        new object?[]
        {
          new[] { typeof(ValueData?) }, new object?[]
          {
            new ValueData { Name = "Same", Number = 1, DateTime = new DateTime(2020, 12, 20), Total = 500D },
            new ValueData { Name = "Same", Number = 1, DateTime = new DateTime(2020, 12, 20), Total = 500D },
            new NullableEqualityComparer<ValueData>(ValueData.AllEquals, false)
          }
        },
        new object?[]
        {
          new[] { typeof(ValueData?) }, new object?[]
          {
            new ValueData { Name = "Same", Number = 1, DateTime = new DateTime(2020, 12, 20), Total = 600D },
            new ValueData { Name = "Same", Number = 1, DateTime = new DateTime(2020, 12, 20), Total = 600D },
            new NullableEqualityComparer<ValueData>(ValueData.AllEquals, true)
          }
        },
        new object?[]
        {
          null, new object?[]
          {
            null, null,
            new NullableEqualityComparer<ValueData>(ValueData.AllEquals, false)
          }
        },
        new object?[]
        {
          null, new object?[]
          {
            new ObjectData { Name = "First", Number = 1, DateTime = new DateTime(2020, 1, 7), Total = 1000D },
            new ObjectData { Name = "First", Number = 1, DateTime = new DateTime(2020, 1, 7), Total = 1000D },
            new CustomEqualityComparer<ObjectData>(ObjectData.AllEquals)
          }
        },
        new object?[]
        {
          null, new object?[]
          {
            null, null,
            new CustomEqualityComparer<ObjectData?>(ObjectData.AllEquals),
          }
        },
        new object?[]
        {
          null, new object?[]
          {
            new ObjectData { Name = "First", Number = 1, DateTime = new DateTime(2020, 1, 7), Total = 1000D },
            new ObjectData { Name = "First", Number = 1, DateTime = new DateTime(2020, 1, 7), Total = 1000D },
            new ObjectEqualityComparer<ObjectData>(ObjectData.AllEquals, false)
          }
        },
        new object?[]
        {
          null, new object?[]
          {
            null, null,
            new ObjectEqualityComparer<ObjectData>(ObjectData.AllEquals, false)
          }
        },
      };

    public static IEnumerable<object?[]> NotEqaulsData
      => new object?[][]
      {
        new object?[] { null, new object?[] { 1, -100, EqualityComparer<int>.Default } },
        new object?[] { null, new object?[] { -200L, long.MinValue, EqualityComparer<long>.Default } },
        new object?[] { null, new object?[] { 100D, -55D, EqualityComparer<double>.Default } },
        new object?[] { null, new object?[] { 2000F, 8000F, EqualityComparer<float>.Default } },
        new object?[] { null, new object?[] { new DateTime(2010, 1, 1), new DateTime(2009, 12, 31), EqualityComparer<DateTime>.Default } },
        new object?[]
        {
          null, new object?[]
          {
            new ValueData { Name = "First", Number = 1, DateTime = new DateTime(2020, 1, 7), Total = 1000D },
            new ValueData { Name = "Second", Number = 1, DateTime = new DateTime(2020, 12, 20), Total = 400D },
            new CustomEqualityComparer<ValueData>(ValueData.AllEquals)
          }
        },
        new object?[]
        {
          new[] { typeof(ValueData?) }, new object?[]
          {
            new ValueData { Name = "First", Number = 1, DateTime = new DateTime(2020, 12, 20), Total = 500D },
            new ValueData { Name = "Second", Number = 1, DateTime = new DateTime(2020, 12, 20), Total = 400D },
            new CustomEqualityComparer<ValueData?>(Equality.AsNullableEquality<ValueData>(ValueData.AllEquals))
          }
        },
        new object?[]
        {
          new[] { typeof(ValueData?) }, new object?[]
          {
            null, new ValueData { Name = "Second", Number = 1, DateTime = new DateTime(2020, 12, 20), Total = 400D },
            new CustomEqualityComparer<ValueData?>(Equality.AsNullableEquality<ValueData>(ValueData.AllEquals))
          }
        },
        new object?[]
        {
          new[] { typeof(ValueData?) }, new object?[]
          {
            new ValueData { Name = "First", Number = 1, DateTime = new DateTime(2020, 12, 20), Total = 500D }, null,
            new CustomEqualityComparer<ValueData?>(Equality.AsNullableEquality<ValueData>(ValueData.AllEquals))
          }
        },
        new object?[]
        {
          new[] { typeof(ValueData?) }, new object?[]
          {
            new ValueData { Name = "First", Number = 1, DateTime = new DateTime(2020, 12, 20), Total = 500D },
            new ValueData { Name = "Second", Number = 1, DateTime = new DateTime(2020, 12, 20), Total = 400D },
            new NullableEqualityComparer<ValueData>(ValueData.AllEquals, false)
          }
        },
        new object?[]
        {
          new Type?[] { null }, new object?[]
          {
            TypedValue.DefaultOf<ValueData?>(),
            new ValueData { Name = "Second", Number = 1, DateTime = new DateTime(2020, 12, 20), Total = 400D },
            new NullableEqualityComparer<ValueData>(ValueData.AllEquals, false)
          }
        },
        new object?[]
        {
          new[] { typeof(ValueData?) }, new object?[]
          {
            new ValueData { Name = "First", Number = 1, DateTime = new DateTime(2020, 12, 20), Total = 500D },
            null,
            new NullableEqualityComparer<ValueData>(ValueData.AllEquals, true)
          }
        },
        new object?[]
        {
          new[] { typeof(ValueData?) }, new object?[]
          {
            null, null, new CustomEqualityComparer<ValueData?>(Equality.AsNullableEquality<ValueData>(ValueData.AllEquals, true)),
          }
        },
        new object?[]
        {
          null, new object?[]
          {
            new ObjectData { Name = "First", Number = 1, DateTime = new DateTime(2020, 1, 7), Total = 1000D },
            new ObjectData { Name = "Second", Number = 1, DateTime = new DateTime(2020, 12, 20), Total = 400D },
            new ObjectEqualityComparer<ObjectData>(ObjectData.AllEquals, false)
          }
        },
        new object?[]
        {
          null, new object?[]
          {
            null, new ObjectData { Name = "Second", Number = 1, DateTime = new DateTime(2020, 12, 20), Total = 400D },
            new ObjectEqualityComparer<ObjectData>(ObjectData.AllEquals, false)
          }
        },
        new object?[]
        {
          null, new object?[]
          {
            new ObjectData { Name = "First", Number = 1, DateTime = new DateTime(2020, 12, 20), Total = 400D }, null,
            new ObjectEqualityComparer<ObjectData>(ObjectData.AllEquals, true)
          }
        },
        new object?[]
        {
          null, new object?[]
          {
            null, null, new ObjectEqualityComparer<ObjectData>(ObjectData.AllEquals, true)
          }
        },
      };

    [Theory]
    [MemberData(nameof(EqaulsData))]
    public void EqualsSuccessful(object? arguments, object parameters)
    {
      var genericArgs = Argument.That.OfType<IList<Type?>>(arguments);
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      var result = Reflector.CallInstanceMethod(Argument.That, nameof(Argument.Equals), MemberAccessibility.Public, genericArgs, positionalParams, null, null);
      Assert.Equal(TypedValue.GetValue(positionalParams[0]), result);
    }

    [Theory]
    [MemberData(nameof(NotEqaulsData))]
    public void EqualsFailed(object? arguments, object parameters)
    {
      var genericArgs = Argument.That.OfType<IList<Type?>>(arguments);
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      try
      {
        Reflector.CallInstanceMethod(Argument.That, nameof(Argument.Equals), MemberAccessibility.Public, genericArgs, positionalParams, null, null);
        throw new InvalidOperationException();
      }
      catch (TargetInvocationException ex) when (ex.InnerException?.GetType() == typeof(ArgumentOutOfRangeException))
      { }
    }

    [Theory]
    [MemberData(nameof(NotEqaulsData))]
    public void NotEqualsSuccessful(object? arguments, object parameters)
    {
      var genericArgs = Argument.That.OfType<IList<Type?>>(arguments);
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      var result = Reflector.CallInstanceMethod(Argument.That, nameof(Argument.NotEquals), MemberAccessibility.Public, genericArgs, positionalParams, null, null);
      Assert.Equal(TypedValue.GetValue(positionalParams[0]), result);
    }

    [Theory]
    [MemberData(nameof(EqaulsData))]
    public void NotEqualsFailed(object? arguments, object parameters)
    {
      var genericArgs = Argument.That.OfType<IList<Type?>>(arguments);
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      try
      {
        Reflector.CallInstanceMethod(Argument.That, nameof(Argument.NotEquals), MemberAccessibility.Public, genericArgs, positionalParams, null, null);
        throw new InvalidOperationException();
      }
      catch (TargetInvocationException ex) when (ex.InnerException?.GetType() == typeof(ArgumentOutOfRangeException))
      { }
    }

    #endregion
    #region In validation

    public static IEnumerable<object?[]> InData
      => new object?[][]
      {
        new object?[] { null, new object?[] { 0, new[] { 1, 2, 0, 3, 4 } } },
      };

    public static IEnumerable<object?[]> NotInData
      => new object?[][]
      {
        new object?[] { null, new object?[] { 0, new[] { 1, 2, 3, 4 } } },
      };

    [Theory]
    [MemberData(nameof(InData))]
    public void InSuccessful(object? arguments, object parameters)
    {
      var genericArgs = Argument.That.OfType<IList<Type?>>(arguments);
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      var result = Reflector.CallInstanceMethod(Argument.That, nameof(Argument.In), MemberAccessibility.Public, genericArgs, positionalParams, null, null);
      Assert.Equal(positionalParams[0], result);
    }

    [Theory]
    [MemberData(nameof(NotInData))]
    public void InFailed(object? arguments, object parameters)
    {
      var genericArgs = Argument.That.OfType<IList<Type?>>(arguments);
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      try
      {
        Reflector.CallInstanceMethod(Argument.That, nameof(Argument.In), MemberAccessibility.Public, genericArgs, positionalParams, null, null);
        throw new InvalidOperationException();
      }
      catch (TargetInvocationException ex) when (ex.InnerException is ArgumentOutOfRangeException)
      { }
    }

    [Theory]
    [MemberData(nameof(NotInData))]
    public void NotInSuccessful(object? arguments, object parameters)
    {
      var genericArgs = Argument.That.OfType<IList<Type?>>(arguments);
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      var result = Reflector.CallInstanceMethod(Argument.That, nameof(Argument.NotIn), MemberAccessibility.Public, genericArgs, positionalParams, null, null);
      Assert.Equal(positionalParams[0], result);
    }

    [Theory]
    [MemberData(nameof(InData))]
    public void NotInFailed(object? arguments, object parameters)
    {
      var genericArgs = Argument.That.OfType<IList<Type?>>(arguments);
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      try
      {
        Reflector.CallInstanceMethod(Argument.That, nameof(Argument.NotIn), MemberAccessibility.Public, genericArgs, positionalParams, null, null);
        throw new InvalidOperationException();
      }
      catch (TargetInvocationException ex) when (ex.InnerException is ArgumentOutOfRangeException)
      { }
    }

    #endregion
    #region Compare validation

    public static IEnumerable<object?[]> CompareData
      => new object?[][]
      {
        new object?[] { null, new object?[] { 0, 0, ComparisonCriteria.Equal } },
        new object?[] { null, new object?[] { 0, 1, ComparisonCriteria.LessThan } },
        new object?[] { null, new object?[] { 0, -1, ComparisonCriteria.GreaterThan } },
      };

    public static IEnumerable<object?[]> NotCompareData
      => new object?[][]
      {
        new object?[] { null, new object?[] { 0, 0, ComparisonCriteria.NotEqual } },
        new object?[] { null, new object?[] { 0, 1, ComparisonCriteria.GreaterThan } },
        new object?[] { null, new object?[] { 0, -1, ComparisonCriteria.LessThan } },
      };

    [Theory]
    [MemberData(nameof(CompareData))]
    public void CompareSuccessful(object? arguments, object parameters)
    {
      var genericArgs = Argument.That.OfType<IList<Type?>>(arguments);
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      var result = Reflector.CallInstanceMethod(Argument.That, nameof(Argument.Compare), MemberAccessibility.Public, genericArgs, positionalParams, null, null);
      Assert.Equal(positionalParams[0], result);
    }

    [Theory]
    [MemberData(nameof(NotCompareData))]
    public void CompareFailed(object? arguments, object parameters)
    {
      var genericArgs = Argument.That.OfType<IList<Type?>>(arguments);
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      try
      {
        Reflector.CallInstanceMethod(Argument.That, nameof(Argument.Compare), MemberAccessibility.Public, genericArgs, positionalParams, null, null);
        throw new InvalidOperationException();
      }
      catch (TargetInvocationException ex) when (ex.InnerException is ArgumentOutOfRangeException)
      { }
    }

    [Theory]
    [MemberData(nameof(NotCompareData))]
    public void NotCompareSuccessful(object? arguments, object parameters)
    {
      var genericArgs = Argument.That.OfType<IList<Type?>>(arguments);
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      var result = Reflector.CallInstanceMethod(Argument.That, nameof(Argument.NotCompare), MemberAccessibility.Public, genericArgs, positionalParams, null, null);
      Assert.Equal(positionalParams[0], result);
    }

    [Theory]
    [MemberData(nameof(CompareData))]
    public void NotCompareFailed(object? arguments, object parameters)
    {
      var genericArgs = Argument.That.OfType<IList<Type?>>(arguments);
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      try
      {
        Reflector.CallInstanceMethod(Argument.That, nameof(Argument.NotCompare), MemberAccessibility.Public, genericArgs, positionalParams, null, null);
        throw new InvalidOperationException();
      }
      catch (TargetInvocationException ex) when (ex.InnerException is ArgumentOutOfRangeException)
      { }
    }

    #endregion
    #region Between validation

    public static IEnumerable<object?[]> BetweenData
      => new object?[][]
      {
        new object?[] { null, new object?[] { 0, 0, 0, BetweenCriteria.IncludeBoth } },
        new object?[] { null, new object?[] { 0, 0, 1, BetweenCriteria.ExcludeUpper } },
        new object?[] { null, new object?[] { 0, -1, 0, BetweenCriteria.ExcludeLower } },
        new object?[] { null, new object?[] { 0, -1, 1, BetweenCriteria.ExcludeBoth } },
      };

    public static IEnumerable<object?[]> NotBetweenData
      => new object?[][]
      {
        new object?[] { null, new object?[] { 0, 0, 0, BetweenCriteria.ExcludeBoth } },
        new object?[] { null, new object?[] { 0, 0, 1, BetweenCriteria.ExcludeLower } },
        new object?[] { null, new object?[] { 0, -1, 0, BetweenCriteria.ExcludeUpper } },
        new object?[] { null, new object?[] { 2, -1, 1, BetweenCriteria.IncludeBoth } },
      };

    [Theory]
    [MemberData(nameof(BetweenData))]
    public void BetweenSuccessful(object? arguments, object parameters)
    {
      var genericArgs = Argument.That.OfType<IList<Type?>>(arguments);
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      var result = Reflector.CallInstanceMethod(Argument.That, nameof(Argument.Between), MemberAccessibility.Public, genericArgs, positionalParams, null, null);
      Assert.Equal(positionalParams[0], result);
    }

    [Theory]
    [MemberData(nameof(NotBetweenData))]
    public void BetweenFailed(object? arguments, object parameters)
    {
      var genericArgs = Argument.That.OfType<IList<Type?>>(arguments);
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      try
      {
        Reflector.CallInstanceMethod(Argument.That, nameof(Argument.Between), MemberAccessibility.Public, genericArgs, positionalParams, null, null);
        throw new InvalidOperationException();
      }
      catch (TargetInvocationException ex) when (ex.InnerException is ArgumentOutOfRangeException)
      { }
    }

    [Theory]
    [MemberData(nameof(NotBetweenData))]
    public void NotBetweenSuccessful(object? arguments, object parameters)
    {
      var genericArgs = Argument.That.OfType<IList<Type?>>(arguments);
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      var result = Reflector.CallInstanceMethod(Argument.That, nameof(Argument.NotBetween), MemberAccessibility.Public, genericArgs, positionalParams, null, null);
      Assert.Equal(positionalParams[0], result);
    }

    [Theory]
    [MemberData(nameof(BetweenData))]
    public void NotBetweenFailed(object? arguments, object parameters)
    {
      var genericArgs = Argument.That.OfType<IList<Type?>>(arguments);
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      try
      {
        Reflector.CallInstanceMethod(Argument.That, nameof(Argument.NotBetween), MemberAccessibility.Public, genericArgs, positionalParams, null, null);
        throw new InvalidOperationException();
      }
      catch (TargetInvocationException ex) when (ex.InnerException is ArgumentOutOfRangeException)
      { }
    }

    #endregion
    #region Numeric validation tests

    public static IEnumerable<object?[]> NegativeValues
      => new[]
      {
        new object?[] { new object[] { (sbyte)-1 } },
        new object?[] { new object[] { (short)-1 } },
        new object?[] { new object[] { -1 } },
        new object?[] { new object[] { -1L } },
#if NET5_0_OR_GREATER
        new object?[] { new object[] { (Half)(-1.0) } },
#endif
        new object?[] { new object[] { -1.0F } },
        new object?[] { new object[] { -1.0D } },
        new object?[] { new object[] { -1.0M } },
        new object?[] { new object[] { BigInteger.MinusOne } },
        new object?[] { new object[] { TimeSpan.FromMilliseconds(-1) } }
      };

    public static IEnumerable<object?[]> ZeroValues
      => new[]
      {
        new object?[] { new object[] { (sbyte)0 } },
        new object?[] { new object[] { (short)0 } },
        new object?[] { new object[] { 0 } },
        new object?[] { new object[] { 0L } },
#if NET5_0_OR_GREATER
        new object?[] { new object[] { (Half)0 } },
#endif
        new object?[] { new object[] { 0F } },
        new object?[] { new object[] { 0D } },
        new object?[] { new object[] { 0M } },
        new object?[] { new object[] { BigInteger.Zero } },
        new object?[] { new object[] { TimeSpan.Zero } }
      };

    public static IEnumerable<object?[]> PositiveValues
      => new[]
      {
        new object?[] { new object[] { (sbyte)1 } },
        new object?[] { new object[] { (short)1 } },
        new object?[] { new object[] { 1 } },
        new object?[] { new object[] { 1L } },
#if NET5_0_OR_GREATER
        new object?[] { new object[] { (Half)1 } },
#endif
        new object?[] { new object[] { 1F } },
        new object?[] { new object[] { 1D } },
        new object?[] { new object[] { 1M } },
        new object?[] { new object[] { BigInteger.One } },
        new object?[] { new object[] { TimeSpan.FromMilliseconds(1) } }
      };

    public static IEnumerable<object?[]> NonPositiveValues
      => NegativeValues.Concat(ZeroValues);

    public static IEnumerable<object?[]> NonNegativeValues
      => PositiveValues.Concat(ZeroValues);

    public static IEnumerable<object?[]> NonZeroValues
      => NegativeValues.Concat(PositiveValues);

    private const string NumericParamValue = "value";

    [Theory]
    [MemberData(nameof(NegativeValues))]
    public void NegativeSuccessful(object parameters)
    {
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      var result = Reflector.CallInstanceMethod(Argument.That, nameof(Argument.Negative), MemberAccessibility.Public, null, positionalParams, null, null);
      Assert.Equal(positionalParams[0], result);
    }

    [Theory]
    [MemberData(nameof(NonNegativeValues))]
    public void NegativeFailed(object parameters)
    {
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      try
      {
        Reflector.CallInstanceMethod(Argument.That, nameof(Argument.Negative), MemberAccessibility.Public, null, positionalParams, null, null);
        throw new InvalidOperationException();
      }
      catch (TargetInvocationException ex) when (ex.InnerException is ArgumentOutOfRangeException exInner)
      {
        Assert.Equal(NumericParamValue, exInner.ParamName);
      }
    }

    [Theory]
    [MemberData(nameof(NonNegativeValues))]
    public void NonNegativeSuccessful(object parameters)
    {
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      var result = Reflector.CallInstanceMethod(Argument.That, nameof(Argument.NonNegative), MemberAccessibility.Public, null, positionalParams, null, null);
      Assert.Equal(positionalParams[0], result);
    }

    [Theory]
    [MemberData(nameof(NegativeValues))]
    public void NonNegativeFailed(object parameters)
    {
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      try
      {
        Reflector.CallInstanceMethod(Argument.That, nameof(Argument.NonNegative), MemberAccessibility.Public, null, positionalParams, null, null);
        throw new InvalidOperationException();
      }
      catch (TargetInvocationException ex) when (ex.InnerException is ArgumentOutOfRangeException exInner)
      {
        Assert.Equal(NumericParamValue, exInner.ParamName);
      }
    }

    [Theory]
    [MemberData(nameof(PositiveValues))]
    public void PositiveSuccessful(object parameters)
    {
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      var result = Reflector.CallInstanceMethod(Argument.That, nameof(Argument.Positive), MemberAccessibility.Public, null, positionalParams, null, null);
      Assert.Equal(positionalParams[0], result);
    }

    [Theory]
    [MemberData(nameof(NonPositiveValues))]
    public void PositiveFailed(object parameters)
    {
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      try
      {
        Reflector.CallInstanceMethod(Argument.That, nameof(Argument.Positive), MemberAccessibility.Public, null, positionalParams, null, null);
        throw new InvalidOperationException();
      }
      catch (TargetInvocationException ex) when (ex.InnerException is ArgumentOutOfRangeException exInner)
      {
        Assert.Equal(NumericParamValue, exInner.ParamName);
      }
    }

    [Theory]
    [MemberData(nameof(NonPositiveValues))]
    public void NonPositiveSuccessful(object parameters)
    {
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      var result = Reflector.CallInstanceMethod(Argument.That, nameof(Argument.NonPositive), MemberAccessibility.Public, null, positionalParams, null, null);
      Assert.Equal(positionalParams[0], result);
    }

    [Theory]
    [MemberData(nameof(PositiveValues))]
    public void NonPositiveFailed(object parameters)
    {
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      try
      {
        Reflector.CallInstanceMethod(Argument.That, nameof(Argument.NonPositive), MemberAccessibility.Public, null, positionalParams, null, null);
        throw new InvalidOperationException();
      }
      catch (TargetInvocationException ex) when (ex.InnerException is ArgumentOutOfRangeException exInner)
      {
        Assert.Equal(NumericParamValue, exInner.ParamName);
      }
    }

    [Theory]
    [MemberData(nameof(NonZeroValues))]
    public void NonZeroSuccessful(object parameters)
    {
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      var result = Reflector.CallInstanceMethod(Argument.That, nameof(Argument.NonZero), MemberAccessibility.Public, null, positionalParams, null, null);
      Assert.Equal(positionalParams[0], result);
    }

    [Theory]
    [MemberData(nameof(ZeroValues))]
    public void NonZeroFailed(object parameters)
    {
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      try
      {
        Reflector.CallInstanceMethod(Argument.That, nameof(Argument.NonZero), MemberAccessibility.Public, null, positionalParams, null, null);
        throw new InvalidOperationException();
      }
      catch (TargetInvocationException ex) when (ex.InnerException is ArgumentOutOfRangeException exInner)
      {
        Assert.Equal(NumericParamValue, exInner.ParamName);
      }
    }

    #endregion
    #region Range validation

    public static IEnumerable<object?[]> InRangeDataSuccessful
      => new object?[][]
      {
        new object?[] { nameof(Argument.InRangeOut), new object[] { 0, 0 }, 0 },
        new object?[] { nameof(Argument.InRangeOut), new object[] { 10, 0 }, 0 },
        new object?[] { nameof(Argument.InRangeOut), new object[] { 10, 5 }, 5 },
        new object?[] { nameof(Argument.InRangeIn), new object[] { 10, 9 }, 9 },
        new object?[] { nameof(Argument.InRangeOut), new object[] { 10, 10 }, 10 },
        new object?[] { nameof(Argument.InRangeOut), new object[] { 10, 0, 0 }, (0, 0) },
        new object?[] { nameof(Argument.InRangeOut), new object[] { 10, 0, 10 }, (0, 10) },
        new object?[] { nameof(Argument.InRangeOut), new object[] { 10, 5, 5 }, (5, 5) },
        new object?[] { nameof(Argument.InRangeOut), new object[] { 10, 9, 1 }, (9, 1) },
        new object?[] { nameof(Argument.InRangeOut), new object[] { 10, 10, 0 }, (10, 0) },
        new object?[] { nameof(Argument.InRangeIn), new object[] { 10, 0, 0 }, (0, 0) },
        new object?[] { nameof(Argument.InRangeIn), new object[] { 10, 0, 10 }, (0, 10) },
        new object?[] { nameof(Argument.InRangeIn), new object[] { 10, 5, 5 }, (5, 5) },
        new object?[] { nameof(Argument.InRangeIn), new object[] { 10, 9, 1 }, (9, 1) },
        new object?[] { nameof(Argument.InRangeOut), new object[] { 10, (0, 10) }, (0, 10) },
        new object?[] { nameof(Argument.InRangeOut), new object[] { 10, (5, 5) }, (5, 5) },
        new object?[] { nameof(Argument.InRangeOut), new object[] { 10, (9, 1) }, (9, 1) },
        new object?[] { nameof(Argument.InRangeOut), new object[] { 10, (10, 0) }, (10, 0) },
      };

    private const string InRangeParamIndex = "index";
    private const string InRangeParamCount = "count";

    public static IEnumerable<object?[]> InRangeDataFailed
      => new object?[][]
      {
        new object?[] { nameof(Argument.InRangeIn), new object[] { 0, 0 }, InRangeParamIndex },
        new object?[] { nameof(Argument.InRangeOut), new object[] { 0, 1 }, InRangeParamIndex },
        new object?[] { nameof(Argument.InRangeOut), new object[] { 10, 11 }, InRangeParamIndex },
        new object?[] { nameof(Argument.InRangeIn), new object[] { 10, 10 }, InRangeParamIndex },
        new object?[] { nameof(Argument.InRangeOut), new object[] { 10, 5, 6 }, InRangeParamCount },
        new object?[] { nameof(Argument.InRangeOut), new object[] { 10, 11, 0 }, InRangeParamIndex },
        new object?[] { nameof(Argument.InRangeIn), new object[] { 10, 10, 0 }, InRangeParamIndex },
      };

    [Theory]
    [MemberData(nameof(InRangeDataSuccessful))]
    public void InRangeSuccessful(object name, object parameters, object? expectedResult)
    {
      var methodName = Argument.That.InstanceOf<string>(name);
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      var actualResult = Reflector.CallInstanceMethod(Argument.That, methodName, MemberAccessibility.Public, null, positionalParams, null, null);
      Assert.Equal(expectedResult, actualResult);
    }

    [Theory]
    [MemberData(nameof(InRangeDataFailed))]
    public void InRangeFailed(object name, object parameters, object? expectedParam)
    {
      var methodName = Argument.That.InstanceOf<string>(name);
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      try
      {
        Reflector.CallInstanceMethod(Argument.That, methodName, MemberAccessibility.Public, null, positionalParams, null, null);
        throw new InvalidOperationException();
      }
      catch (TargetInvocationException ex) when (ex.InnerException is ArgumentOutOfRangeException exInner)
      {
        if (expectedParam != null)
          Assert.Equal((string)expectedParam, exInner.ParamName);
      }
    }

    #endregion
    #region Collection validation


    #endregion
    #region Collections validation

    public static IEnumerable<object?[]> CollectionsDataSuccessful
      => new object?[][]
      {
        new object?[] { nameof(Argument.CompareCoupled), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 1, 2, 3, 4, 5 }, TypedValue.DefaultOf<IComparer<int>>(), ComparisonCriteria.Equal } },
        new object?[] { nameof(Argument.CompareCoupled), null, new object?[] { new[] { 0, 1, 2, 3, 4, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, TypedValue.DefaultOf<IComparer<int>>(), ComparisonCriteria.NotEqual } },
        new object?[] { nameof(Argument.CompareCoupled), null, new object?[] { new[] { 0, 1, 2, 3, 4, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, TypedValue.DefaultOf<IComparer<int>>(), ComparisonCriteria.GreaterThan } },
        new object?[] { nameof(Argument.CompareCoupled), null, new object?[] { new[] { 0, 1, 2, 3, 4, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, TypedValue.DefaultOf<Comparison<int>>(), ComparisonCriteria.GreaterThanOrEqual } },
        new object?[] { nameof(Argument.CompareCoupled), null, new object?[] { new[] { 0, 1, 2, 3, 4, 6 }, new[] { 0, 1, 2, 3, 4, 7 }, TypedValue.DefaultOf<Comparison<int>>(), ComparisonCriteria.LessThan } },
        new object?[] { nameof(Argument.CompareCoupled), null, new object?[] { new[] { 0, 1, 2, 3, 4, 6 }, new[] { 0, 1, 2, 3, 4, 7 }, TypedValue.DefaultOf<Comparison<int>>(), ComparisonCriteria.LessThanOrEqual } },

        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 1, 2, 3, 4, 5 }, TypedValue.DefaultOf<IComparer<int>>(), ComparisonCriteria.Equal } },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, TypedValue.DefaultOf<IComparer<int>>(), ComparisonCriteria.NotEqual } },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, TypedValue.DefaultOf<IComparer<int>>(), ComparisonCriteria.GreaterThan } },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, TypedValue.DefaultOf<Comparison<int>>(), ComparisonCriteria.GreaterThanOrEqual } },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 6 }, new[] { 0, 1, 2, 3, 4, 7 }, TypedValue.DefaultOf<Comparison<int>>(), ComparisonCriteria.LessThan } },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 6 }, new[] { 0, 1, 2, 3, 4, 7 }, TypedValue.DefaultOf<Comparison<int>>(), ComparisonCriteria.LessThanOrEqual } },

        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, TypedValue.DefaultOf<IComparer<int>>(), ComparisonCriteria.NotEqual } },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, TypedValue.DefaultOf<IComparer<int>>(), ComparisonCriteria.NotEqual, RelativeOrder.Upper } },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, TypedValue.DefaultOf<IComparer<int>>(), ComparisonCriteria.GreaterThan } },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, TypedValue.DefaultOf<Comparison<int>>(), ComparisonCriteria.GreaterThanOrEqual } },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, TypedValue.DefaultOf<Comparison<int>>(), ComparisonCriteria.LessThan, RelativeOrder.Upper } },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, TypedValue.DefaultOf<Comparison<int>>(), ComparisonCriteria.LessThanOrEqual, RelativeOrder.Upper } },

        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 1, 2, 3, 4, 5, 6 }, TypedValue.DefaultOf<IComparer<int>>(), ComparisonCriteria.NotEqual } },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 1, 2, 3, 4, 5, 6 }, TypedValue.DefaultOf<IComparer<int>>(), ComparisonCriteria.NotEqual, RelativeOrder.Upper } },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 1, 2, 3, 4, 5, 6 }, TypedValue.DefaultOf<IComparer<int>>(), ComparisonCriteria.GreaterThan, RelativeOrder.Upper } },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 1, 2, 3, 4, 5, 6 }, TypedValue.DefaultOf<Comparison<int>>(), ComparisonCriteria.GreaterThanOrEqual, RelativeOrder.Upper } },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 1, 2, 3, 4, 5, 6 }, TypedValue.DefaultOf<Comparison<int>>(), ComparisonCriteria.LessThan } },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 1, 2, 3, 4, 5, 6 }, TypedValue.DefaultOf<Comparison<int>>(), ComparisonCriteria.LessThanOrEqual } },
      };

    public static IEnumerable<object?[]> CollectionsDataFailed
      => new object?[][]
      {
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 1, 2, 3, 4, 5 }, Comparer<int>.Default, ComparisonCriteria.NotEqual }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, Comparer<int>.Default, ComparisonCriteria.Equal }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, TypedValue.DefaultOf<IComparer<int>>(), ComparisonCriteria.LessThan }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, TypedValue.DefaultOf<Comparison<int>>(), ComparisonCriteria.LessThanOrEqual }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 6 }, new[] { 0, 1, 2, 3, 4, 7 }, Comparer<int>.Default, ComparisonCriteria.GreaterThan }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 6 }, new[] { 0, 1, 2, 3, 4, 7 }, Comparer<int>.Default, ComparisonCriteria.GreaterThanOrEqual }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, Comparer<int>.Default, ComparisonCriteria.Equal }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, Comparer<int>.Default, ComparisonCriteria.Equal, RelativeOrder.Upper }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, Comparer<int>.Default, ComparisonCriteria.LessThan }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, Comparer<int>.Default, ComparisonCriteria.LessThanOrEqual }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, Comparer<int>.Default, ComparisonCriteria.GreaterThan, RelativeOrder.Upper }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, Comparer<int>.Default, ComparisonCriteria.GreaterThanOrEqual, RelativeOrder.Upper }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 1, 2, 3, 4, 5, 6 }, Comparer<int>.Default, ComparisonCriteria.Equal }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 1, 2, 3, 4, 5, 6 }, Comparer<int>.Default, ComparisonCriteria.Equal, RelativeOrder.Upper }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 1, 2, 3, 4, 5, 6 }, Comparer<int>.Default, ComparisonCriteria.LessThan, RelativeOrder.Upper }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 1, 2, 3, 4, 5, 6 }, Comparer<int>.Default, ComparisonCriteria.LessThanOrEqual, RelativeOrder.Upper }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 1, 2, 3, 4, 5, 6 }, Comparer<int>.Default, ComparisonCriteria.GreaterThan }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.ComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 1, 2, 3, 4, 5, 6 }, Comparer<int>.Default, ComparisonCriteria.GreaterThanOrEqual }, typeof(ArgumentCollectionElementException) },

        new object?[] { nameof(Argument.NotComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 1, 2, 3, 4, 5 }, Comparer<int>.Default, ComparisonCriteria.Equal }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.NotComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, Comparer<int>.Default, ComparisonCriteria.NotEqual }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.NotComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, TypedValue.DefaultOf<IComparer<int>>(), ComparisonCriteria.GreaterThan }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.NotComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, TypedValue.DefaultOf<Comparison<int>>(), ComparisonCriteria.GreaterThanOrEqual }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.NotComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 6 }, new[] { 0, 1, 2, 3, 4, 7 }, Comparer<int>.Default, ComparisonCriteria.LessThan }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.NotComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 6 }, new[] { 0, 1, 2, 3, 4, 7 }, Comparer<int>.Default, ComparisonCriteria.LessThanOrEqual }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.NotComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, Comparer<int>.Default, ComparisonCriteria.NotEqual }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.NotComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, Comparer<int>.Default, ComparisonCriteria.NotEqual, RelativeOrder.Upper }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.NotComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, Comparer<int>.Default, ComparisonCriteria.GreaterThan }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.NotComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, Comparer<int>.Default, ComparisonCriteria.GreaterThanOrEqual }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.NotComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, Comparer<int>.Default, ComparisonCriteria.LessThan, RelativeOrder.Upper }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.NotComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, Comparer<int>.Default, ComparisonCriteria.LessThanOrEqual, RelativeOrder.Upper }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.NotComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 1, 2, 3, 4, 5, 6 }, Comparer<int>.Default, ComparisonCriteria.NotEqual }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.NotComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 1, 2, 3, 4, 5, 6 }, Comparer<int>.Default, ComparisonCriteria.NotEqual, RelativeOrder.Upper }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.NotComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 1, 2, 3, 4, 5, 6 }, Comparer<int>.Default, ComparisonCriteria.GreaterThan, RelativeOrder.Upper }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.NotComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 1, 2, 3, 4, 5, 6 }, Comparer<int>.Default, ComparisonCriteria.GreaterThanOrEqual, RelativeOrder.Upper }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.NotComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 1, 2, 3, 4, 5, 6 }, Comparer<int>.Default, ComparisonCriteria.LessThan }, typeof(ArgumentCollectionElementException) },
        new object?[] { nameof(Argument.NotComparePaired), null, new object?[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 1, 2, 3, 4, 5, 6 }, Comparer<int>.Default, ComparisonCriteria.LessThanOrEqual }, typeof(ArgumentCollectionElementException) },
      };

    [Theory]
    [MemberData(nameof(CollectionsDataSuccessful))]
    public void CollectionsSuccessful(object name, object? arguments, object parameters)
    {
      var methodName = Argument.That.InstanceOf<string>(name);
      var genericArgs = Argument.That.OfType<IList<Type?>>(arguments);
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      var result = Reflector.CallInstanceMethod(Argument.That, methodName, MemberAccessibility.Public, genericArgs, positionalParams, null, null);
      Assert.NotNull(result);
    }

    [Theory]
    [MemberData(nameof(CollectionsDataFailed))]
    public void CollectionsFailed(object name, object? arguments, object parameters, object exception)
    {
      var methodName = Argument.That.InstanceOf<string>(name);
      var genericArgs = Argument.That.OfType<IList<Type?>>(arguments);
      var positionalParams = Argument.That.InstanceOf<IList<object?>>(parameters);
      var expectedException = Argument.That.InstanceOf<Type>(exception);
      try
      {
        Reflector.CallInstanceMethod(Argument.That, methodName, MemberAccessibility.Public, genericArgs, positionalParams, null, null);
        throw new InvalidOperationException();
      }
      catch (TargetInvocationException ex) when (ex.InnerException?.GetType() == expectedException)
      { }
    }

    #endregion
  }
}
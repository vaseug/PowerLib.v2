using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using PowerLib.System.Collections;
using System.Globalization;
using System.Numerics;
using PowerLib.System.Arrays;

namespace PowerLib.System.Validation;

public static class ArgumentExtension
{
  #region Internal methods

  private static Argument Self([NotNull] this Argument argument, [CallerArgumentExpression("argument")] string? argumentParameter = "argument")
    => argument ?? throw new ArgumentNullException(argumentParameter);

  #endregion
  #region Emission

  public static Argument<T?> Of<T>(this Argument argument, T? value, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => new()
    {
      Value = value,
      Name = valueParameter
    };

  public static Argument<object?> ObjectOf<T>(this Argument argument, T? value, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => new()
    {
      Value = value,
      Name = valueParameter
    };

  public static Argument<Delegate?> DelegateOf<T>(this Argument argument, T? value, [CallerArgumentExpression("value")] string? valueParameter = "value")
    where T : Delegate
    => new()
    {
      Value = value,
      Name = valueParameter
    };

  public static Argument<IEnumerable?> EnumerableOf(this Argument argument, IEnumerable? collection, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => new()
    {
      Value = collection,
      Name = collectionParameter
    };

  public static Argument<IEnumerable<T>?> EnumerableOf<T>(this Argument argument, IEnumerable<T>? collection, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => new()
    {
      Value = collection,
      Name = collectionParameter
    };

  public static Argument<IAsyncEnumerable<T>?> AsyncEnumerableOf<T>(this Argument argument, IAsyncEnumerable<T>? collection, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => new()
    {
      Value = collection,
      Name = collectionParameter
    };

  public static Argument<ICollection?> CollectionOf(this Argument argument, ICollection? collection, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => new()
    {
      Value = collection,
      Name = collectionParameter
    };

  public static Argument<ICollection<T>?> CollectionOf<T>(this Argument argument, ICollection<T>? collection, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => new()
    {
      Value = collection,
      Name = collectionParameter
    };

  public static Argument<Array?> ArrayOf(Array? array, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => new()
    {
      Value = array,
      Name = arrayParameter
    };

  public static Argument<T> NotNullOf<T>(this Argument argument, [NotNull] T? value, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => new()
    {
      Value = argument.Self().NotNull(value, valueParameter: valueParameter),
      Name = valueParameter
    };

  public static Argument<object> NotNullObjectOf<T>(this Argument argument, [NotNull] T? value, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => new()
    {
      Value = argument.Self().NotNull(value, valueParameter: valueParameter),
      Name = valueParameter
    };

  public static Argument<Delegate> NotNullDelegateOf<T>(this Argument argument, [NotNull] T? value, [CallerArgumentExpression("value")] string? valueParameter = "value")
    where T : Delegate
    => new()
    {
      Value = argument.Self().NotNull(value, valueParameter: valueParameter),
      Name = valueParameter
    };

  public static Argument<IEnumerable> NotNullEnumerableOf(this Argument argument, [NotNull] IEnumerable? collection, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => new()
    {
      Value = argument.Self().NotNull(collection, valueParameter: collectionParameter),
      Name = collectionParameter
    };

  public static Argument<IEnumerable<T>> NotNullEnumerableOf<T>(this Argument argument, [NotNull] IEnumerable<T>? collection, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => new()
    {
      Value = argument.Self().NotNull(collection, valueParameter: collectionParameter),
      Name = collectionParameter
    };

  public static Argument<IAsyncEnumerable<T>> NotNullAsyncEnumerableOf<T>(this Argument argument, [NotNull] IAsyncEnumerable<T>? collection, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => new()
    {
      Value = argument.Self().NotNull(collection, valueParameter: collectionParameter),
      Name = collectionParameter
    };

  public static Argument<ICollection> NotNullCollectionOf(this Argument argument, [NotNull] ICollection? collection, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => new()
    {
      Value = argument.Self().NotNull(collection),
      Name = collectionParameter
    };

  public static Argument<ICollection<T>> NotNullCollectionOf<T>(this Argument argument, [NotNull] ICollection<T>? collection, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => new()
    {
      Value = argument.Self().NotNull(collection),
      Name = collectionParameter
    };

  public static Argument<Array> NotNullArrayOf(this Argument argument, [NotNull] Array? array, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => new()
    {
      Value = argument.Self().NotNull(array, valueParameter: arrayParameter),
      Name = arrayParameter
    };

  #endregion
  #region Casting

  public static Argument<TSource> For<TSource, TResult>(this Argument argument, Argument<TSource> value, Converter<TSource, TResult> converter, Action<Argument<TResult>> action)
  {
    argument.Self();
    argument.NotNull(action);
    argument.NotNull(converter);

    action(new()
    {
      Value = converter(value.Value),
      Name = value.Name
    });
    return value;
  }

  public static Argument<TResult> For<TSource, TResult>(this Argument argument, Argument<TSource> value, Func<Argument, Argument<TSource>, Argument<TResult>> function)
  {
    argument.Self().NotNull(function);

    return function(argument, value);
  }

  public static Argument<TResult> As<TSource, TResult>(this Argument argument, Argument<TSource> value, Converter<TSource, TResult> converter)
  {
    argument.Self().NotNull(converter);

    return new()
    {
      Value = converter(value.Value),
      Name = value.Name,
    };
  }

  public static Argument<TSource?> AsNullable<TSource>(this Argument argument, Argument<TSource> value)
    => new()
    {
      Value = value.Value,
      Name = value.Name,
    };

  #endregion
  #region Null validation

  public static Argument<T?> NullOr<T>(this Argument argument, Argument<T?> value, Action<Argument, Argument<T>> validator)
  {
    argument.Self();

    if (value.Value is not null)
      argument.NotNull(validator)(argument, new()
      {
        Value = value.Value!,
        Name = value.Name,
      });
    return value;
  }

  public static Argument<T?> NullOr<T>(this Argument argument, Argument<T?> value, Func<Argument, Argument<T>, Argument<T>> validator)
  {
    argument.Self();

    return value.Value is null ? value : new Argument<T?>()
    {
      Value = argument.NotNull(validator)(argument, new()
      {
        Value = value.Value!,
        Name = value.Name,
      }).Value,
      Name = value.Name,
    };
  }

  public static Argument<T> NotNull<T>(this Argument argument, Argument<T?> value, string? message = null)
  {
    argument.Self();

    return new()
    {
      Value = argument.NotNull(value.Value, message, value.Name),
      Name = value.Name,
    };
  }

  public static Argument<T> NotNull<T>(this Argument argument, Argument<T?> value, string? message = null)
    where T : struct
  {
    argument.Self();

    return new()
    {
      Value = argument.NotNull(value.Value, message, value.Name),
      Name = value.Name,
    };
  }

  public static Argument<T> NonNullable<T>(this Argument argument, Argument<T> value, string? message = null)
  {
    argument.Self().NotNull(value.Value, message, value.Name);
    return value;
  }

  public static Argument<object> NotNullOnlyOne(this Argument argument, string? message, params Argument<object?>[] arguments)
  {
    var result = argument.Self().NotEmpty(arguments)
      .Where(arg => arg.Value is not null)
      .Select(arg => new Argument<object> { Value = arg.Value!, Name = arg.Name })
      .Take(2)
      .ToList();
    argument.Self().AreValid(result.Count != 1, (msg, prm) => new ArgumentNullException(prm, msg),
      message, ArgumentMessage.IsNotNullOnlyOneOf, arguments.Select(arg => arg.Name).ToArray());
    return result[0];
  }

  public static Argument<T> NotNullOnlyOne<T>(this Argument argument, string? message, params Argument<T?>[] arguments)
  {
    argument.Self();
    try
    {
      argument.MatchExact(arguments, 1, arg => arg.Value is not null);
    }
    catch (ArgumentException ex) when (ex.ParamName == nameof(argument))
    {
      argument.AreValid(false, (msg, prm) => new ArgumentNullException(prm, msg),
        message, ArgumentMessage.IsNotNullOnlyOneOf, arguments.Select(arg => arg.Name).ToArray());
    }


    var result = argument.Self().NotEmpty(arguments)
      .Where(arg => arg.Value is not null)
      .Select(arg => new Argument<T> { Value = arg.Value!, Name = arg.Name })
      .Take(2)
      .ToList();
    argument.Self().AreValid(result.Count != 1, (msg, prm) => new ArgumentNullException(prm, msg),
      message, ArgumentMessage.IsNotNullOnlyOneOf, arguments.Select(arg => arg.Name).ToArray());
    return result[0];
  }

  public static Argument<object>[] NotNullAtLeastOne(this Argument argument, string? message, params Argument<object?>[] arguments)
  {
    var result = argument.Self().NotEmpty(arguments)
      .Where(arg => arg.Value is not null)
      .Select(arg => new Argument<object> { Value = arg.Value!, Name = arg.Name })
      .ToArray();
    argument.Self().AreValid(result.Length > 0, (msg, prm) => new ArgumentNullException(prm, msg),
      message, ArgumentMessage.IsNotNullAtLeastOneOf, arguments.Select(arg => arg.Name).ToArray());
    return result;
  }

  public static Argument<T>[] NotNullAtLeastOne<T>(this Argument argument, string? message, params Argument<T?>[] arguments)
  {
    var result = argument.Self().NotEmpty(arguments)
      .Where(arg => arg.Value is not null)
      .Select(arg => new Argument<T> { Value = arg.Value!, Name = arg.Name })
      .ToArray();
    argument.Self().AreValid(result.Length > 0, (msg, prm) => new ArgumentNullException(prm, msg),
      message, ArgumentMessage.IsNotNullAtLeastOneOf, arguments.Select(arg => arg.Name).ToArray());
    return result;
  }

  public static Argument<object>[] NotNullEveryone(this Argument argument, string? message, params Argument<object?>[] arguments)
  {
    var result = argument.Self().NotEmpty(arguments)
      .Where(arg => arg.Value is not null)
      .Select(arg => new Argument<object> { Value = arg.Value!, Name = arg.Name })
      .ToArray();
    argument.Self().AreValid(result.Length != arguments.Length, (msg, prm) => new ArgumentNullException(prm, msg),
      message, ArgumentMessage.IsNotNullAtLeastOneOf, arguments.Select(arg => arg.Name).ToArray());
    return result;
  }

  public static Argument<T>[] NotNullEveryone<T>(this Argument argument, string? message, params Argument<T?>[] arguments)
  {
    var result = argument.Self().NotEmpty(arguments)
      .Where(arg => arg.Value is not null)
      .Select(arg => new Argument<T> { Value = arg.Value!, Name = arg.Name })
      .ToArray();
    argument.Self().AreValid(result.Length != arguments.Length, (msg, prm) => new ArgumentNullException(prm, msg),
      message, ArgumentMessage.IsNotNullAtLeastOneOf, arguments.Select(arg => arg.Name).ToArray());
    return result;
  }

  #endregion
  #region Outer validation

  public static Argument<T> IsValid<T>(this Argument argument, Argument<T> value, Predicate<T> predicate, string? message = null)
  {
    argument.Self().NotNull(predicate);
    argument.IsValid(value.Value, predicate, message, value.Name);
    return value;
  }

  public static Argument<T> IsValid<T>(this Argument argument, Argument<T> value, [DoesNotReturnIf(false)] bool valid, string? message = null)
  {
    argument.Self().IsValid(value.Value, valid, message, value.Name);
    return value;
  }

  [DoesNotReturn]
  public static Argument<T> Invalid<T>(this Argument argument, Argument<T> value, string? message = null)
  {
    argument.Self().Invalid(value.Value, message, value.Name);
    return value;
  }

  public static Argument<T> InRange<T>(this Argument argument, Argument<T> value, Predicate<T> predicate, string? message = null)
  {
    argument.Self().InRange(value.Value, predicate, message, value.Name);
    return value;
  }

  public static Argument<T> InRange<T>(this Argument argument, Argument<T> value, [DoesNotReturnIf(false)] bool valid, string? message = null)
  {
    argument.Self().InRange(value.Value, valid, message, value.Name);
    return value;
  }

  [DoesNotReturn]
  public static Argument<T> OutOfRange<T>(this Argument argument, Argument<T> value, string? message = null)
  {
    argument.Self().OutOfRange(value.Value, message, value.Name);
    return value;
  }

  public static void AreConsistent(this Argument argument, [DoesNotReturnIf(false)] bool consistent, string? message, params Argument<object?>[] arguments)
  {
    argument.Self().NotNull(arguments);
    argument.AreConsistent(consistent, message, arguments.Select(arg => arg.Name).ToArray());
  }

  public static void AreConsistent<T>(this Argument argument, [DoesNotReturnIf(false)] bool consistent, string? message, params Argument<T>[] arguments)
  {
    argument.Self().NotNull(arguments);
    argument.AreConsistent(consistent, message, arguments.Select(arg => arg.Name).ToArray());
  }

  #endregion
  #region Type validation

  public static Argument<T> InstanceOf<T>(this Argument argument, Argument<T> value, TypeCode typeCode, string? message = null)
  {
    argument.Self().InstanceOf<T>(value.Value, typeCode, message, value.Name);
    return value;
  }

  public static Argument<object> InstanceOf(this Argument argument, Argument<object> value, Type type, string? message = null)
  {
    argument.Self().InstanceOf(value.Value, type, message, value.Name);
    return value;
  }

  public static Argument<T> InstanceOf<T>(this Argument argument, Argument<object> value, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InstanceOf<T>(value.Value, message, value.Name),
      Name = value.Name,
    };
  }

  public static Argument<T> NotInstanceOf<T>(this Argument argument, Argument<T> value, TypeCode typeCode, string? message = null)
  {
    argument.Self().NotInstanceOf<T>(value.Value, typeCode, message, value.Name);
    return value;
  }

  public static Argument<object> NotInstanceOf(this Argument argument, Argument<object> value, Type type, string? message = null)
  {
    argument.Self().NotInstanceOf(value.Value, type, message, value.Name);
    return value;
  }

  public static Argument<object> NotInstanceOf<T>(this Argument argument, Argument<object> value, string? message = null)
  {
    argument.Self().NotInstanceOf<T>(value.Value, message, value.Name);
    return value;
  }

  public static Argument<object?> OfType(this Argument argument, Argument<object?> value, Type type, string? message = null)
  {
    argument.Self().OfType(value.Value, type, message, value.Name);
    return value;
  }

  public static Argument<T?> OfType<T>(this Argument argument, Argument<object?> value, string? message = null)
  {
    return new()
    {
      Value = argument.Self().OfType<T>(value.Value, message, value.Name),
      Name = value.Name,
    };
  }

  public static Argument<Array> ElementOf(this Argument argument, Argument<Array> value, TypeCode elementTypeCode, string? message = null)
  {
    argument.Self().ElementOf(value.Value, elementTypeCode, message, value.Name);
    return value;
  }

  public static Argument<Array> ElementOf(this Argument argument, Argument<Array> value, Type elementType, string? message = null)
  {
    argument.Self().ElementOf(value.Value, elementType, message, value.Name);
    return value;
  }

  public static Argument<Array> ElementOf<T>(this Argument argument, Argument<Array> value, string? message = null)
  {
    argument.Self().ElementOf<T>(value.Value, message, value.Name);
    return value;
  }

  #endregion
  #region Boolean validation

  public static Argument<bool> False(this Argument argument, Argument<bool> value, string? message = null)
  {
    argument.Self().False(value.Value, message, value.Name);
    return value;
  }

  public static Argument<bool> True(this Argument argument, Argument<bool> value, string? message = null)
  {
    argument.Self().True(value.Value, message, value.Name);
    return value;
  }

  #endregion
  #region Enum validation

  public static Argument<T> EnumFlags<T>(this Argument argument, Argument<T> value, string? message = null)
    where T : struct, Enum
  {
    argument.Self().EnumFlags(value.Value, message, value.Name);
    return value;
  }

  public static Argument<T> NotEmptyFlags<T>(this Argument argument, Argument<T> value, string? message = null)
    where T : struct, Enum
  {
    argument.Self().NotEmptyFlags(value.Value, message, value.Name);
    return value;
  }

  public static Argument<T> EmptyFlags<T>(this Argument argument, Argument<T> value, string? message = null)
    where T : struct, Enum
  {
    argument.Self().EmptyFlags(value.Value, message, value.Name);
    return value;
  }

  public static Argument<T> MatchFlags<T>(this Argument argument, Argument<T> value, Argument<T> other, FlagsMatchResult matchResult, string? message = null)
    where T : struct, Enum
  {
    argument.Self().MatchFlags(value.Value, other.Value, matchResult, message, value.Name, other.Name);
    return value;
  }

  #endregion
  #region String validation

  public static Argument<string> Empty(this Argument argument, Argument<string> value, string? message = null)
  {
    argument.Self().Empty(value.Value, message, value.Name);
    return value;
  }

  public static Argument<string> NotEmpty(this Argument argument, Argument<string> value, string? message = null)
  {
    argument.Self().NotEmpty(value.Value, message, value.Name);
    return value;
  }

  public static Argument<string> Whitespace(this Argument argument, Argument<string> value, string? message = null)
  {
    argument.Self().Whitespace(value.Value, message, value.Name);
    return value;
  }
  public static Argument<string> NotWhitespace(this Argument argument, Argument<string> value, string? message = null)
  {
    argument.Self().NotWhitespace(value.Value, message, value.Name);
    return value;
  }

  public static Argument<string> Contains(this Argument argument, Argument<string> value, Argument<string> substring, CompareInfo compareInfo, CompareOptions compareOptions = CompareOptions.None, string? message = null)
  {
    argument.Self().Contains(value.Value, substring.Value, compareInfo, compareOptions, message, value.Name ?? default, substring.Name ?? default);
    return value;
  }

  public static Argument<string> Contains(this Argument argument, Argument<string> value, Argument<string> substring, CultureInfo? cultureInfo = null, CompareOptions compareOptions = CompareOptions.None, string? message = null)
  {
    argument.Self().Contains(value.Value, substring.Value, cultureInfo, compareOptions, message, value.Name ?? default, substring.Name ?? default);
    return value;
  }

  public static Argument<string> NotContains(this Argument argument, Argument<string> value, Argument<string> substring, CompareInfo compareInfo, CompareOptions compareOptions = CompareOptions.None, string? message = null)
  {
    argument.Self().NotContains(value.Value, substring.Value, compareInfo, compareOptions, message, value.Name ?? default, substring.Name ?? default);
    return value;
  }

  public static Argument<string> NotContains(this Argument argument, Argument<string> value, Argument<string> substring, CultureInfo? cultureInfo = null, CompareOptions compareOptions = CompareOptions.None, string? message = null)
  {
    argument.Self().NotContains(value.Value, substring.Value, cultureInfo, compareOptions, message, value.Name ?? default, substring.Name ?? default);
    return value;
  }

  public static Argument<string> StartsWith(this Argument argument, Argument<string> value, Argument<string> prefix, CompareInfo compareInfo, CompareOptions compareOptions = CompareOptions.None, string? message = null)
  {
    argument.Self().StartsWith(value.Value, prefix.Value, compareInfo, compareOptions, message, value.Name ?? default, prefix.Name ?? default);
    return value;
  }

  public static Argument<string> StartsWith(this Argument argument, Argument<string> value, Argument<string> prefix, CultureInfo? cultureInfo = null, CompareOptions compareOptions = CompareOptions.None, string? message = null)
  {
    argument.Self().StartsWith(value.Value, prefix.Value, cultureInfo, compareOptions, message, value.Name ?? default, prefix.Name ?? default);
    return value;
  }

  public static Argument<string> NotStartsWith(this Argument argument, Argument<string> value, Argument<string> prefix, CompareInfo compareInfo, CompareOptions compareOptions = CompareOptions.None, string? message = null)
  {
    argument.Self().NotStartsWith(value.Value, prefix.Value, compareInfo, compareOptions, message, value.Name ?? default, prefix.Name ?? default);
    return value;
  }

  public static Argument<string> NotStartsWith(this Argument argument, Argument<string> value, Argument<string> prefix, CultureInfo? cultureInfo = null, CompareOptions compareOptions = CompareOptions.None, string? message = null)
  {
    argument.Self().NotStartsWith(value.Value, prefix.Value, cultureInfo, compareOptions, message, value.Name ?? default, prefix.Name ?? default);
    return value;
  }

  public static Argument<string> EndsWith(this Argument argument, Argument<string> value, Argument<string> suffix, CompareInfo compareInfo, CompareOptions compareOptions = CompareOptions.None, string? message = null)
  {
    argument.Self().EndsWith(value.Value, suffix.Value, compareInfo, compareOptions, message, value.Name ?? default, suffix.Name ?? default);
    return value;
  }

  public static Argument<string> EndsWith(this Argument argument, Argument<string> value, Argument<string> suffix, CultureInfo? cultureInfo = null, CompareOptions compareOptions = CompareOptions.None, string? message = null)
  {
    argument.Self().EndsWith(value.Value, suffix.Value, cultureInfo, compareOptions, message, value.Name ?? default, suffix.Name ?? default);
    return value;
  }

  public static Argument<string> NotEndsWith(this Argument argument, Argument<string> value, Argument<string> suffix, CompareInfo compareInfo, CompareOptions compareOptions = CompareOptions.None, string? message = null)
  {
    argument.Self().NotEndsWith(value.Value, suffix.Value, compareInfo, compareOptions, message, value.Name ?? default, suffix.Name ?? default);
    return value;
  }

  public static Argument<string> NotEndsWith(this Argument argument, Argument<string> value, Argument<string> suffix, CultureInfo? cultureInfo = null, CompareOptions compareOptions = CompareOptions.None, string? message = null)
  {
    argument.Self().NotEndsWith(value.Value, suffix.Value, cultureInfo, compareOptions, message, value.Name ?? default, suffix.Name ?? default);
    return value;
  }

#if NETCOREAPP2_1_OR_GREATER

  public static Argument<ReadOnlyMemory<char>> Contains(this Argument argument, Argument<ReadOnlyMemory<char>> value, Argument<ReadOnlyMemory<char>> substring,
    CompareInfo compareInfo, CompareOptions compareOptions = CompareOptions.None, string? message = null)
  {
    argument.Self().Contains(value.Value.Span, substring.Value.Span, compareInfo, compareOptions, message, value.Name ?? default, substring.Name ?? default);
    return value;
  }

  public static Argument<ReadOnlyMemory<char>> Contains(this Argument argument, Argument<ReadOnlyMemory<char>> value, Argument<ReadOnlyMemory<char>> substring,
    CultureInfo? cultureInfo = null, CompareOptions compareOptions = CompareOptions.None, string? message = null)
  {
    argument.Self().Contains(value.Value.Span, substring.Value.Span, cultureInfo, compareOptions, message, value.Name ?? default, substring.Name ?? default);
    return value;
  }

  public static Argument<ReadOnlyMemory<char>> NotContains(this Argument argument, Argument<ReadOnlyMemory<char>> value, Argument<ReadOnlyMemory<char>> substring,
    CompareInfo compareInfo, CompareOptions compareOptions = CompareOptions.None, string? message = null)
  {
    argument.Self().NotContains(value.Value.Span, substring.Value.Span, compareInfo, compareOptions, message, value.Name ?? default, substring.Name ?? default);
    return value;
  }

  public static Argument<ReadOnlyMemory<char>> NotContains(this Argument argument, Argument<ReadOnlyMemory<char>> value, Argument<ReadOnlyMemory<char>> substring,
    CultureInfo? cultureInfo = null, CompareOptions compareOptions = CompareOptions.None, string? message = null)
  {
    argument.Self().NotContains(value.Value.Span, substring.Value.Span, cultureInfo, compareOptions, message, value.Name ?? default, substring.Name ?? default);
    return value;
  }

  public static Argument<ReadOnlyMemory<char>> StartsWith(this Argument argument, Argument<ReadOnlyMemory<char>> value, Argument<ReadOnlyMemory<char>> prefix,
    CompareInfo compareInfo, CompareOptions compareOptions = CompareOptions.None, string? message = null)
  {
    argument.Self().StartsWith(value.Value.Span, prefix.Value.Span, compareInfo, compareOptions, message, value.Name ?? default, prefix.Name ?? default);
    return value;
  }

  public static Argument<ReadOnlyMemory<char>> StartsWith(this Argument argument, Argument<ReadOnlyMemory<char>> value, Argument<ReadOnlyMemory<char>> prefix,
    CultureInfo? cultureInfo = null, CompareOptions compareOptions = CompareOptions.None,
    string? message = null)
  {
    argument.Self().StartsWith(value.Value.Span, prefix.Value.Span, cultureInfo, compareOptions, message, value.Name ?? default, prefix.Name ?? default);
    return value;
  }

  public static Argument<ReadOnlyMemory<char>> NotStartsWith(this Argument argument, Argument<ReadOnlyMemory<char>> value, Argument<ReadOnlyMemory<char>> prefix,
    CompareInfo compareInfo, CompareOptions compareOptions = CompareOptions.None, string? message = null)
  {
    argument.Self().NotStartsWith(value.Value.Span, prefix.Value.Span, compareInfo, compareOptions, message, value.Name ?? default, prefix.Name ?? default);
    return value;
  }

  public static Argument<ReadOnlyMemory<char>> NotStartsWith(this Argument argument, Argument<ReadOnlyMemory<char>> value, Argument<ReadOnlyMemory<char>> prefix,
    CultureInfo? cultureInfo = null, CompareOptions compareOptions = CompareOptions.None, string? message = null)
  {
    argument.Self().NotStartsWith(value.Value.Span, prefix.Value.Span, cultureInfo, compareOptions, message, value.Name ?? default, prefix.Name ?? default);
    return value;
  }

  public static Argument<ReadOnlyMemory<char>> EndsWith(this Argument argument, Argument<ReadOnlyMemory<char>> value, Argument<ReadOnlyMemory<char>> suffix,
    CompareInfo compareInfo, CompareOptions compareOptions = CompareOptions.None, string? message = null)
  {
    argument.Self().EndsWith(value.Value.Span, suffix.Value.Span, compareInfo, compareOptions, message, value.Name ?? default, suffix.Name ?? default);
    return value;
  }

  public static Argument<ReadOnlyMemory<char>> EndsWith(this Argument argument, Argument<ReadOnlyMemory<char>> value, Argument<ReadOnlyMemory<char>> suffix,
    CultureInfo? cultureInfo = null, CompareOptions compareOptions = CompareOptions.None, string? message = null)
  {
    argument.Self().EndsWith(value.Value.Span, suffix.Value.Span, cultureInfo, compareOptions, message, value.Name ?? default, suffix.Name ?? default);
    return value;
  }

  public static Argument<ReadOnlyMemory<char>> NotEndsWith(this Argument argument, Argument<ReadOnlyMemory<char>> value, Argument<ReadOnlyMemory<char>> suffix,
    CompareInfo compareInfo, CompareOptions compareOptions = CompareOptions.None, string? message = null)
  {
    argument.Self().NotEndsWith(value.Value.Span, suffix.Value.Span, compareInfo, compareOptions, message, value.Name ?? default, suffix.Name ?? default);
    return value;
  }

  public static Argument<ReadOnlyMemory<char>> NotEndsWith(this Argument argument, Argument<ReadOnlyMemory<char>> value, Argument<ReadOnlyMemory<char>> suffix,
    CultureInfo? cultureInfo = null, CompareOptions compareOptions = CompareOptions.None, string? message = null)
  {
    argument.Self().NotEndsWith(value.Value.Span, suffix.Value.Span, cultureInfo, compareOptions, message, value.Name ?? default, suffix.Name ?? default);
    return value;
  }

#endif

  public static Argument<string> Match(this Argument argument, Argument<string> value, Argument<string> pattern, RegexOptions options = RegexOptions.None, string? message = null)
  {
    argument.Self().Match(value, pattern, options, message, value.Name, pattern.Name);
    return value;
  }

  public static Argument<string> Match(this Argument argument, Argument<string> value, Argument<Regex> regex, string? message = null)
  {
    argument.Self().Match(value.Value, regex.Value, message, value.Name, regex.Name);
    return value;
  }

  public static Argument<string> NotMatch(this Argument argument, Argument<string> value, Argument<string> pattern, RegexOptions options = RegexOptions.None, string? message = null)
  {
    argument.Self().NotMatch(value, pattern, options, message, value.Name, pattern.Name);
    return value;
  }

  public static Argument<string> NotMatch(this Argument argument, Argument<string> value, Argument<Regex> regex, string? message = null)
  {
    argument.Self().NotMatch(value.Value, regex.Value, message, value.Name, regex.Name);
    return value;
  }

  #endregion
  #region DateTime interval validation

  public static Argument<DateTimeInterval> InRange(this Argument argument, Argument<DateTime> dateTime, Argument<TimeSpan> timeSpan, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InRange(dateTime.Value, timeSpan.Value, message, dateTime.Name, timeSpan.Name),
      Name = null,
    };
  }

  #endregion
  #region Eqality validation

  public static Argument<T> Equals<T>(this Argument argument, Argument<T> value, Argument<T> other, string? message = null)
  {
    argument.Self().Equal(value.Value, other.Value, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> Equals<T>(this Argument argument, Argument<T> value, Argument<T> other, Equality<T>? equality, string? message = null)
  {
    argument.Self().Equals(value.Value, other.Value, equality, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> Equals<T>(this Argument argument, Argument<T> value, Argument<T> other, IEqualityComparer<T>? equalityComparer, string? message = null)
  {
    argument.Self().Equals(value.Value, other.Value, equalityComparer, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> NotEquals<T>(this Argument argument, Argument<T> value, Argument<T> other, string? message = null)
  {
    argument.Self().NotEquals(value.Value, other.Value, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> NotEquals<T>(this Argument argument, Argument<T> value, Argument<T> other, Equality<T>? equality, string? message = null)
  {
    argument.Self().NotEquals(value.Value, other.Value, equality, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> NotEquals<T>(this Argument argument, Argument<T> value, Argument<T> other, IEqualityComparer<T>? equalityComparer, string? message = null)
  {
    argument.Self().NotEquals(value.Value, other.Value, equalityComparer, message, value.Name, other.Name);
    return value;
  }

  #endregion
  #region In validation

  public static Argument<IEnumerable<T>> In<T>(this Argument argument, Argument<T> value, Argument<IEnumerable<T>> collection, string? message = null)
  {
    argument.Self().In(value.Value, collection.Value, message, value.Name, collection.Name);
    return collection;
  }

  public static Argument<T> In<T>(this Argument argument, Argument<T> value, Argument<IEnumerable<T>> collection, Equality<T>? equality, string? message = null)
  {
    argument.Self().In(value.Value, collection.Value, equality, message, value.Name, collection.Name);
    return value;
  }

  public static Argument<T> In<T>(this Argument argument, Argument<T> value, Argument<IEnumerable<T>> collection, IEqualityComparer<T>? equalityComparer, string? message = null)
  {
    argument.Self().In(value.Value, collection.Value, equalityComparer, message, value.Name, collection.Name);
    return value;
  }

  public static Argument<T> NotIn<T>(this Argument argument, Argument<T> value, Argument<IEnumerable<T>> collection, string? message = null)
  {
    argument.Self().NotIn(value.Value, collection.Value, message, value.Name, collection.Name);
    return value;
  }

  public static Argument<T> NotIn<T>(this Argument argument, Argument<T> value, Argument<IEnumerable<T>> collection, Equality<T>? equality, string? message = null)
  {
    argument.Self().NotIn(value.Value, collection.Value, equality, message, value.Name, collection.Name);
    return value;
  }

  public static Argument<T> NotIn<T>(this Argument argument, Argument<T> value, Argument<IEnumerable<T>> collection, IEqualityComparer<T>? equalityComparer, string? message = null)
  {
    argument.Self().NotIn(value.Value, collection.Value, equalityComparer, message, value.Name, collection.Name);
    return value;
  }

  #endregion
  #region Compare validation

  public static Argument<T> Compare<T>(this Argument argument, Argument<T> value, Argument<T> other, ComparisonCriteria criteria, string? message = null)
  {
    argument.Self().Compare(value.Value, other.Value, criteria, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> Compare<T>(this Argument argument, Argument<T> value, Argument<T> other, Comparison<T>? comparison, ComparisonCriteria criteria, string? message = null)
  {
    argument.Self().Compare(value.Value, other.Value, comparison, criteria, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> Compare<T>(this Argument argument, Argument<T> value, Argument<T> other, IComparer<T>? comparer, ComparisonCriteria criteria, string? message = null)
  {
    argument.Self().Compare(value.Value, other.Value, comparer, criteria, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> NotCompare<T>(this Argument argument, Argument<T> value, Argument<T> other, ComparisonCriteria criteria, string? message = null)
  {
    argument.Self().NotCompare(value.Value, other.Value, criteria, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> NotCompare<T>(this Argument argument, Argument<T> value, Argument<T> other, Comparison<T>? comparison, ComparisonCriteria criteria, string? message = null)
  {
    argument.Self().NotCompare(value.Value, other.Value, comparison, criteria, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> NotCompare<T>(this Argument argument, Argument<T> value, Argument<T> other, IComparer<T>? comparer, ComparisonCriteria criteria, string? message = null)
  {
    argument.Self().NotCompare(value.Value, other.Value, comparer, criteria, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> LessThan<T>(this Argument argument, Argument<T> value, Argument<T> other, string? message = null)
  {
    argument.Self().LessThan(value.Value, other.Value, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> LessThan<T>(this Argument argument, Argument<T> value, Argument<T> other, Comparison<T>? comparison, string? message = null)
  {
    argument.Self().LessThan(value.Value, other.Value, comparison, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> LessThan<T>(this Argument argument, Argument<T> value, Argument<T> other, IComparer<T>? comparer, string? message = null)
  {
    argument.Self().LessThan(value.Value, other.Value, comparer, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> LessThanOrEqual<T>(this Argument argument, Argument<T> value, Argument<T> other, string? message = null)
  {
    argument.Self().LessThanOrEqual(value.Value, other.Value, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> LessThanOrEqual<T>(this Argument argument, Argument<T> value, Argument<T> other, Comparison<T>? comparison, string? message = null)
  {
    argument.Self().LessThanOrEqual(value.Value, other.Value, comparison, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> LessThanOrEqual<T>(this Argument argument, Argument<T> value, Argument<T> other, IComparer<T>? comparer, string? message = null)
  {
    argument.Self().LessThanOrEqual(value.Value, other.Value, comparer, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> GreaterThan<T>(this Argument argument, Argument<T> value, Argument<T> other, string? message = null)
  {
    argument.Self().GreaterThan(value.Value, other.Value, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> GreaterThan<T>(this Argument argument, Argument<T> value, Argument<T> other, Comparison<T>? comparison, string? message = null)
  {
    argument.Self().GreaterThan(value.Value, other.Value, comparison, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> GreaterThan<T>(this Argument argument, Argument<T> value, Argument<T> other, IComparer<T>? comparer, string? message = null)
  {
    argument.Self().GreaterThan(value.Value, other.Value, comparer, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> GreaterThanOrEqual<T>(this Argument argument, Argument<T> value, Argument<T> other, string? message = null)
  {
    argument.Self().GreaterThanOrEqual(value.Value, other.Value, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> GreaterThanOrEqual<T>(this Argument argument, Argument<T> value, Argument<T> other, Comparison<T>? comparison, string? message = null)
  {
    argument.Self().GreaterThanOrEqual(value.Value, other.Value, comparison, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> GreaterThanOrEqual<T>(this Argument argument, Argument<T> value, Argument<T> other, IComparer<T>? comparer, string? message = null)
  {
    argument.Self().GreaterThanOrEqual(value.Value, other.Value, comparer, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> Equal<T>(this Argument argument, Argument<T> value, Argument<T> other, string? message = null)
  {
    argument.Self().Equal(value.Value, other.Value, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> Equal<T>(this Argument argument, Argument<T> value, Argument<T> other, Comparison<T>? comparison, string? message = null)
  {
    argument.Self().Equal(value.Value, other.Value, comparison, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> Equal<T>(this Argument argument, Argument<T> value, Argument<T> other, IComparer<T>? comparer, string? message = null)
  {
    argument.Self().Equal(value.Value, other.Value, comparer, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> NotEqual<T>(this Argument argument, Argument<T> value, Argument<T> other, string? message = null)
  {
    argument.Self().NotEqual(value.Value, other.Value, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> NotEqual<T>(this Argument argument, Argument<T> value, Argument<T> other, Comparison<T>? comparison, string? message = null)
  {
    argument.Self().NotEqual(value.Value, other.Value, comparison, message, value.Name, other.Name);
    return value;
  }

  public static Argument<T> NotEqual<T>(this Argument argument, Argument<T> value, Argument<T> other, IComparer<T>? comparer, string? message = null)
  {
    argument.Self().NotEqual(value.Value, other.Value, comparer, message, value.Name, other.Name);
    return value;
  }

  #endregion
  #region Between validation

  public static Argument<T> Between<T>(this Argument argument, Argument<T> value, Argument<T> lowerValue, Argument<T> upperValue,
    BetweenCriteria criteria = BetweenCriteria.IncludeBoth, string? message = null)
  {
    argument.Self().Between(value.Value, lowerValue.Value, upperValue.Value, criteria, message, value.Name, lowerValue.Name, upperValue.Name);
    return value;
  }

  public static Argument<T> Between<T>(this Argument argument, Argument<T> value, Argument<T> lowerValue, Argument<T> upperValue, Comparison<T>? comparison,
    BetweenCriteria criteria = BetweenCriteria.IncludeBoth, string? message = null)
  {
    argument.Self().Between(value.Value, lowerValue.Value, upperValue.Value, comparison, criteria, message, value.Name, lowerValue.Name, upperValue.Name);
    return value;
  }

  public static Argument<T> Between<T>(this Argument argument, Argument<T> value, Argument<T> lowerValue, Argument<T> upperValue, IComparer<T>? comparer,
    BetweenCriteria criteria = BetweenCriteria.IncludeBoth, string? message = null)
  {
    argument.Self().Between(value.Value, lowerValue.Value, upperValue.Value, comparer, criteria, message, value.Name, lowerValue.Name, upperValue.Name);
    return value;
  }

  public static Argument<T> NotBetween<T>(this Argument argument, Argument<T> value, Argument<T> lowerValue, Argument<T> upperValue,
    BetweenCriteria criteria = BetweenCriteria.IncludeBoth, string? message = null)
  {
    argument.Self().NotBetween(value.Value, lowerValue.Value, upperValue.Value, criteria, message, value.Name, lowerValue.Name, upperValue.Name);
    return value;
  }

  public static Argument<T> NotBetween<T>(this Argument argument, Argument<T> value, Argument<T> lowerValue, Argument<T> upperValue, Comparison<T>? comparison,
    BetweenCriteria criteria = BetweenCriteria.IncludeBoth, string? message = null)
  {
    argument.Self().NotBetween(value.Value, lowerValue.Value, upperValue.Value, comparison, criteria, message, value.Name, lowerValue.Name, upperValue.Name);
    return value;
  }

  public static Argument<T> NotBetween<T>(this Argument argument, Argument<T> value, Argument<T> lowerValue, Argument<T> upperValue, IComparer<T>? comparer,
    BetweenCriteria criteria = BetweenCriteria.IncludeBoth, string? message = null)
  {
    argument.Self().NotBetween(value.Value, lowerValue.Value, upperValue.Value, comparer, criteria, message, value.Name, lowerValue.Name, upperValue.Name);
    return value;
  }

  #endregion
  #region Numeric validation
  #region SByte

  public static Argument<sbyte> NonZero(this Argument argument, Argument<sbyte> value, string? message = null)
  {
    argument.Self().NonZero(value.Value, message, value.Name);
    return value;
  }

  public static Argument<sbyte> Positive(this Argument argument, Argument<sbyte> value, string? message = null)
  {
    argument.Self().Positive(value.Value, message, value.Name);
    return value;
  }

  public static Argument<sbyte> NonPositive(this Argument argument, Argument<sbyte> value, string? message = null)
  {
    argument.Self().NonPositive(value.Value, message, value.Name);
    return value;
  }

  public static Argument<sbyte> Negative(this Argument argument, Argument<sbyte> value, string? message = null)
  {
    argument.Self().Negative(value.Value, message, value.Name);
    return value;
  }

  public static Argument<sbyte> NonNegative(this Argument argument, Argument<sbyte> value, string? message = null)
  {
    argument.Self().NonNegative(value.Value, message, value.Name);
    return value;
  }

  #endregion
  #region SInt16

  public static Argument<short> NonZero(this Argument argument, Argument<short> value, string? message = null)
  {
    argument.Self().NonZero(value.Value, message, value.Name);
    return value;
  }

  public static Argument<short> Positive(this Argument argument, Argument<short> value, string? message = null)
  {
    argument.Self().Positive(value.Value, message, value.Name);
    return value;
  }

  public static Argument<short> NonPositive(this Argument argument, Argument<short> value, string? message = null)
  {
    argument.Self().NonPositive(value.Value, message, value.Name);
    return value;
  }

  public static Argument<short> Negative(this Argument argument, Argument<short> value, string? message = null)
  {
    argument.Self().Negative(value.Value, message, value.Name);
    return value;
  }

  public static Argument<short> NonNegative(this Argument argument, Argument<short> value, string? message = null)
  {
    argument.Self().NonNegative(value.Value, message, value.Name);
    return value;
  }

  #endregion
  #region SInt32

  public static Argument<int> NonZero(this Argument argument, Argument<int> value, string? message = null)
  {
    argument.Self().NonZero(value.Value, message, value.Name);
    return value;
  }

  public static Argument<int> Positive(this Argument argument, Argument<int> value, string? message = null)
  {
    argument.Self().Positive(value.Value, message, value.Name);
    return value;
  }

  public static Argument<int> NonPositive(this Argument argument, Argument<int> value, string? message = null)
  {
    argument.Self().NonPositive(value.Value, message, value.Name);
    return value;
  }

  public static Argument<int> Negative(this Argument argument, Argument<int> value, string? message = null)
  {
    argument.Self().Negative(value.Value, message, value.Name);
    return value;
  }

  public static Argument<int> NonNegative(this Argument argument, Argument<int> value, string? message = null)
  {
    argument.Self().NonNegative(value.Value, message, value.Name);
    return value;
  }

  #endregion
  #region SInt64

  public static Argument<long> NonZero(this Argument argument, Argument<long> value, string? message = null)
  {
    argument.Self().NonZero(value.Value, message, value.Name);
    return value;
  }

  public static Argument<long> Positive(this Argument argument, Argument<long> value, string? message = null)
  {
    argument.Self().Positive(value.Value, message, value.Name);
    return value;
  }

  public static Argument<long> NonPositive(this Argument argument, Argument<long> value, string? message = null)
  {
    argument.Self().NonPositive(value.Value, message, value.Name);
    return value;
  }

  public static Argument<long> Negative(this Argument argument, Argument<long> value, string? message = null)
  {
    argument.Self().Negative(value.Value, message, value.Name);
    return value;
  }

  public static Argument<long> NonNegative(this Argument argument, Argument<long> value, string? message = null)
  {
    argument.Self().NonNegative(value.Value, message, value.Name);
    return value;
  }

  #endregion
  #region Half
#if NET5_0_OR_GREATER

  public static Argument<Half> NonZero(this Argument argument, Argument<Half> value, string? message = null)
  {
    argument.Self().NonZero(value.Value, message, value.Name);
    return value;
  }

  public static Argument<Half> Positive(this Argument argument, Argument<Half> value, string? message = null)
  {
    argument.Self().Positive(value.Value, message, value.Name);
    return value;
  }

  public static Argument<Half> NonPositive(this Argument argument, Argument<Half> value, string? message = null)
  {
    argument.Self().NonPositive(value.Value, message, value.Name);
    return value;
  }

  public static Argument<Half> Negative(this Argument argument, Argument<Half> value, string? message = null)
  {
    argument.Self().Negative(value.Value, message, value.Name);
    return value;
  }

  public static Argument<Half> NonNegative(this Argument argument, Argument<Half> value, string? message = null)
  {
    argument.Self().NonNegative(value.Value, message, value.Name);
    return value;
  }

#endif
  #endregion
  #region Single

  public static Argument<float> NonZero(this Argument argument, Argument<float> value, string? message = null)
  {
    argument.Self().NonZero(value.Value, message, value.Name);
    return value;
  }

  public static Argument<float> Positive(this Argument argument, Argument<float> value, string? message = null)
  {
    argument.Self().Positive(value.Value, message, value.Name);
    return value;
  }

  public static Argument<float> NonPositive(this Argument argument, Argument<float> value, string? message = null)
  {
    argument.Self().NonPositive(value.Value, message, value.Name);
    return value;
  }

  public static Argument<float> Negative(this Argument argument, Argument<float> value, string? message = null)
  {
    argument.Self().Negative(value.Value, message, value.Name);
    return value;
  }

  public static Argument<float> NonNegative(this Argument argument, Argument<float> value, string? message = null)
  {
    argument.Self().NonNegative(value.Value, message, value.Name);
    return value;
  }

  #endregion
  #region Double

  public static Argument<double> NonZero(this Argument argument, Argument<double> value, string? message = null)
  {
    argument.Self().NonZero(value.Value, message, value.Name);
    return value;
  }

  public static Argument<double> Positive(this Argument argument, Argument<double> value, string? message = null)
  {
    argument.Self().Positive(value.Value, message, value.Name);
    return value;
  }

  public static Argument<double> NonPositive(this Argument argument, Argument<double> value, string? message = null)
  {
    argument.Self().NonPositive(value.Value, message, value.Name);
    return value;
  }

  public static Argument<double> Negative(this Argument argument, Argument<double> value, string? message = null)
  {
    argument.Self().Negative(value.Value, message, value.Name);
    return value;
  }

  public static Argument<double> NonNegative(this Argument argument, Argument<double> value, string? message = null)
  {
    argument.Self().NonNegative(value.Value, message, value.Name);
    return value;
  }

  #endregion
  #region Decimal

  public static Argument<decimal> NonZero(this Argument argument, Argument<decimal> value, string? message = null)
  {
    argument.Self().NonZero(value.Value, message, value.Name);
    return value;
  }

  public static Argument<decimal> Positive(this Argument argument, Argument<decimal> value, string? message = null)
  {
    argument.Self().Positive(value.Value, message, value.Name);
    return value;
  }

  public static Argument<decimal> NonPositive(this Argument argument, Argument<decimal> value, string? message = null)
  {
    argument.Self().NonPositive(value.Value, message, value.Name);
    return value;
  }

  public static Argument<decimal> Negative(this Argument argument, Argument<decimal> value, string? message = null)
  {
    argument.Self().Negative(value.Value, message, value.Name);
    return value;
  }

  public static Argument<decimal> NonNegative(this Argument argument, Argument<decimal> value, string? message = null)
  {
    argument.Self().NonNegative(value.Value, message, value.Name);
    return value;
  }

  #endregion
  #region BigInteger

  public static Argument<BigInteger> NonZero(this Argument argument, Argument<BigInteger> value, string? message = null)
  {
    argument.Self().NonZero(value.Value, message, value.Name);
    return value;
  }

  public static Argument<BigInteger> Positive(this Argument argument, Argument<BigInteger> value, string? message = null)
  {
    argument.Self().Positive(value.Value, message, value.Name);
    return value;
  }

  public static Argument<BigInteger> NonPositive(this Argument argument, Argument<BigInteger> value, string? message = null)
  {
    argument.Self().NonPositive(value.Value, message, value.Name);
    return value;
  }

  public static Argument<BigInteger> Negative(this Argument argument, Argument<BigInteger> value, string? message = null)
  {
    argument.Self().Negative(value.Value, message, value.Name);
    return value;
  }

  public static Argument<BigInteger> NonNegative(this Argument argument, Argument<BigInteger> value, string? message = null)
  {
    argument.Self().NonNegative(value.Value, message, value.Name);
    return value;
  }

  #endregion
  #region TimeSpan

  public static Argument<TimeSpan> NonZero(this Argument argument, Argument<TimeSpan> value, string? message = null)
  {
    argument.Self().NonZero(value.Value, message, value.Name);
    return value;
  }

  public static Argument<TimeSpan> Positive(this Argument argument, Argument<TimeSpan> value, string? message = null)
  {
    argument.Self().Positive(value.Value, message, value.Name);
    return value;
  }

  public static Argument<TimeSpan> NonPositive(this Argument argument, Argument<TimeSpan> value, string? message = null)
  {
    argument.Self().NonPositive(value.Value, message, value.Name);
    return value;
  }

  public static Argument<TimeSpan> Negative(this Argument argument, Argument<TimeSpan> value, string? message = null)
  {
    argument.Self().Negative(value.Value, message, value.Name);
    return value;
  }

  public static Argument<TimeSpan> NonNegative(this Argument argument, Argument<TimeSpan> value, string? message = null)
  {
    argument.Self().NonNegative(value.Value, message, value.Name);
    return value;
  }

  #endregion
  #endregion
  #region Range validation

  public static Argument<int> InRangeOut(this Argument argument, Argument<int> total, Argument<int> index, int basis = 0, string? message = null)
  {
    argument.Self().InRangeOut(total.Value, index.Value, basis, message, total.Name, index.Name);
    return index;
  }

  public static Argument<int> InRangeIn(this Argument argument, Argument<int> total, Argument<int> index, int basis = 0, string? message = null)
  {
    argument.Self().InRangeIn(total.Value, index.Value, basis, message, total.Name, index.Name);
    return index;
  }

  public static Argument<(int index, int count)> InRangeOut(this Argument argument, Argument<int> total, Argument<int> index, Argument<int> count, int basis = 0, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InRangeOut(total.Value, index.Value, count.Value, basis, message, total.Name, index.Name, count.Name),
      Name = null,
    };
  }

  public static Argument<(int index, int count)> InRangeIn(this Argument argument, Argument<int> total, Argument<int> index, Argument<int> count, int basis = 0, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InRangeIn(total.Value, index.Value, count.Value, basis, message, total.Name, index.Name, count.Name),
      Name = null,
    };
  }

  public static Argument<(int index, int count)> InRangeOut(this Argument argument, Argument<int> total, Argument<(int index, int count)> range, int basis = 0, string? message = null)
  {
    argument.Self().InRangeOut(total.Value, range.Value, basis, message, total.Name, range.Name);
    return range;
  }

  public static Argument<(int index, int count)> InRangeIn(this Argument argument, Argument<int> total, Argument<(int index, int count)> range, int basis = 0, string? message = null)
  {
    argument.Self().InRangeIn(total.Value, range.Value, basis, message, total.Name, range.Name);
    return range;
  }

  public static Argument<int> InLimitsOut(this Argument argument, Argument<int> total, Argument<int> count, int basis = 0, int limit = int.MaxValue, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InLimitsOut(total.Value, count.Value, basis, limit, message, total.Name, count.Name),
      Name = null,
    };
  }

  public static Argument<(int index, int count)> InLimitsIn(this Argument argument, Argument<int> total, Argument<int> index, Argument<int> count, int basis = 0, int limit = int.MaxValue, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InLimitsIn(total.Value, index.Value, count.Value, basis, limit, message, total.Name, index.Name, count.Name),
      Name = null,
    };
  }

  public static Argument<long> InRangeOut(this Argument argument, Argument<long> total, Argument<long> index, long basis = 0L, string? message = null)
  {
    argument.Self().InRangeOut(total.Value, index.Value, basis, message, total.Name, index.Name);
    return index;
  }

  public static Argument<long> InRangeIn(this Argument argument, Argument<long> total, Argument<long> index, long basis = 0L, string? message = null)
  {
    argument.Self().InRangeIn(total.Value, index.Value, basis, message, total.Name, index.Name);
    return index;
  }

  public static Argument<(long index, long count)> InRangeOut(this Argument argument, Argument<long> total, Argument<long> index, Argument<long> count, long basis = 0L, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InRangeOut(total.Value, index.Value, count.Value, basis, message, total.Name, index.Name, count.Name),
      Name = null,
    };
  }

  public static Argument<(long index, long count)> InRangeIn(this Argument argument, Argument<long> total, Argument<long> index, Argument<long> count, long basis = 0L, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InRangeIn(total.Value, index.Value, count.Value, basis, message, total.Name, index.Name, count.Name),
      Name = null,
    };
  }

  public static Argument<(long index, long count)> InRangeOut(this Argument argument, Argument<long> total, Argument<(long index, long count)> range, long basis = 0L, string? message = null)
  {
    argument.Self().InRangeOut(total.Value, range.Value, basis, message, total.Name, range.Name);
    return range;
  }

  public static Argument<(long index, long count)> InRangeIn(this Argument argument, Argument<long> total, Argument<(long index, long count)> range, long basis = 0L, string? message = null)
  {
    argument.Self().InRangeIn(total.Value, range.Value, basis, message, total.Name, range.Name);
    return range;
  }

  public static Argument<long> InLimitsOut(this Argument argument, Argument<long> total, Argument<long> count, long basis = 0L, long limit = long.MaxValue, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InLimitsOut(total.Value, count.Value, basis, limit, message, total.Name, count.Name),
      Name = null,
    };
  }

  public static Argument<(long index, long count)> InLimitsIn(this Argument argument, Argument<long> total, Argument<long> index, Argument<long> count, long basis = 0L, long limit = long.MaxValue, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InLimitsIn(total.Value, index.Value, count.Value, basis, limit, message, total.Name, index.Name, count.Name),
      Name = null,
    };
  }

  public static Argument<int> InRangeOut(this Argument argument, Argument<ICollection> collection, Argument<int> index, string? message = null)
  {
    argument.Self().InRangeOut(collection.Value, index.Value, message, collection.Name, index.Name);
    return index;
  }

  public static Argument<int> InRangeIn(this Argument argument, Argument<ICollection> collection, Argument<int> index, string? message = null)
  {
    argument.Self().InRangeIn(collection.Value, index.Value, message, collection.Name, index.Name);
    return index;
  }

  public static Argument<(int index, int count)> InRangeOut(this Argument argument, Argument<ICollection> collection, Argument<int> index, Argument<int> count, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InRangeOut(collection.Value, index.Value, count.Value, message, collection.Name, index.Name, count.Name),
      Name = null,
    };
  }

  public static Argument<(int index, int count)> InRangeIn(this Argument argument, Argument<ICollection> collection, Argument<int> index, Argument<int> count, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InRangeIn(collection.Value, index.Value, count.Value, message, collection.Name, index.Name, count.Name),
      Name = null,
    };
  }

  public static Argument<(int index, int count)> InRangeOut(this Argument argument, Argument<ICollection> collection, Argument<(int index, int count)> range, string? message = null)
  {
    argument.Self().InRangeOut(collection.Value, range.Value, message, collection.Name, range.Name);
    return range;
  }

  public static Argument<(int index, int count)> InRangeIn(this Argument argument, Argument<ICollection> collection, Argument<(int index, int count)> range, string? message = null)
  {
    argument.Self().InRangeIn(collection.Value, range.Value, message, collection.Name, range.Name);
    return range;
  }

  public static Argument<int> InLimitsOut(this Argument argument, Argument<ICollection> collection, Argument<int> count, int limit = int.MaxValue, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InLimitsOut(collection.Value, count.Value, limit, message, collection.Name, count.Name),
      Name = null,
    };
  }

  public static Argument<(int index, int count)> InLimitsIn(this Argument argument, Argument<ICollection> collection, Argument<int> index, Argument<int> count, int limit = int.MaxValue, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InLimitsIn(collection.Value, index.Value, count.Value, limit, message, collection.Name, index.Name, count.Name),
      Name = null,
    };
  }

  public static Argument<int> InRangeOut<T>(this Argument argument, Argument<ICollection<T>> collection, Argument<int> index, string? message = null)
  {
    argument.Self().InRangeOut(collection.Value, index.Value, message, collection.Name, index.Name);
    return index;
  }

  public static Argument<int> InRangeIn<T>(this Argument argument, Argument<ICollection<T>> collection, Argument<int> index, string? message = null)
  {
    argument.Self().InRangeIn(collection.Value, index.Value, message, collection.Name, index.Name);
    return index;
  }

  public static Argument<(int index, int count)> InRangeOut<T>(this Argument argument, Argument<ICollection<T>> collection, Argument<int> index, Argument<int> count, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InRangeOut(collection.Value, index.Value, count.Value, message, collection.Name, index.Name, count.Name),
      Name = null,
    };
  }

  public static Argument<(int index, int count)> InRangeIn<T>(this Argument argument, Argument<ICollection<T>> collection, Argument<int> index, Argument<int> count, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InRangeIn(collection.Value, index.Value, count.Value, message, collection.Name, index.Name, count.Name),
      Name = null,
    };
  }

  public static Argument<(int index, int count)> InRangeOut<T>(this Argument argument, Argument<ICollection<T>> collection, Argument<(int index, int count)> range, string? message = null)
  {
    argument.Self().InRangeOut(collection.Value, range.Value, message, collection.Name, range.Name);
    return range;
  }

  public static Argument<(int index, int count)> InRangeIn<T>(this Argument argument, Argument<ICollection<T>> collection, Argument<(int index, int count)> range, string? message = null)
  {
    argument.Self().InRangeIn(collection.Value, range.Value, message, collection.Name, range.Name);
    return range;
  }

  public static Argument<int> InLimitsOut<T>(this Argument argument, Argument<ICollection<T>> collection, Argument<int> count, int limit = int.MaxValue, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InLimitsOut(collection.Value, count.Value, limit, message, collection.Name, count.Name),
      Name = null,
    };
  }

  public static Argument<(int index, int count)> InLimitsIn<T>(this Argument argument, Argument<ICollection<T>> collection, Argument<int> index, Argument<int> count, int limit = int.MaxValue, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InLimitsIn(collection.Value, index.Value, count.Value, limit, message, collection.Name, index.Name, count.Name),
      Name = null,
    };
  }

  public static Argument<int> InRangeOut<T>(this Argument argument, Argument<IReadOnlyCollection<T>> collection, Argument<int> index, string? message = null)
  {
    argument.Self().InRangeOut(collection.Value, index.Value, message, collection.Name, index.Name);
    return index;
  }

  public static Argument<int> InRangeIn<T>(this Argument argument, Argument<IReadOnlyCollection<T>> collection, Argument<int> index, string? message = null)
  {
    argument.Self().InRangeIn(collection.Value, index.Value, message, collection.Name, index.Name);
    return index;
  }

  public static Argument<(int index, int count)> InRangeOut<T>(this Argument argument, Argument<IReadOnlyCollection<T>> collection, Argument<int> index, Argument<int> count, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InRangeOut(collection.Value, index.Value, count.Value, message, collection.Name, index.Name, count.Name),
      Name = null,
    };
  }

  public static Argument<(int index, int count)> InRangeIn<T>(this Argument argument, Argument<IReadOnlyCollection<T>> collection, Argument<int> index, Argument<int> count, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InRangeIn(collection.Value, index.Value, count.Value, message, collection.Name, index.Name, count.Name),
      Name = null,
    };
  }

  public static Argument<(int index, int count)> InRangeOut<T>(this Argument argument, Argument<IReadOnlyCollection<T>> collection, Argument<(int index, int count)> range, string? message = null)
  {
    argument.Self().InRangeOut(collection.Value, range.Value, message, collection.Name, range.Name);
    return range;
  }

  public static Argument<(int index, int count)> InRangeIn<T>(this Argument argument, Argument<IReadOnlyCollection<T>> collection, Argument<(int index, int count)> range, string? message = null)
  {
    argument.Self().InRangeIn(collection.Value, range.Value, message, collection.Name, range.Name);
    return range;
  }

  public static Argument<int> InLimitsOut<T>(this Argument argument, Argument<IReadOnlyCollection<T>> collection, Argument<int> count, int limit = int.MaxValue, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InLimitsOut(collection.Value, count.Value, limit, message, collection.Name, count.Name),
      Name = null,
    };
  }

  public static Argument<(int index, int count)> InLimitsIn<T>(this Argument argument, Argument<IReadOnlyCollection<T>> collection, Argument<int> index, Argument<int> count, int limit = int.MaxValue, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InLimitsIn(collection.Value, index.Value, count.Value, limit, message, collection.Name, index.Name, count.Name),
      Name = null,
    };
  }

  public static Argument<int> InRangeOut<T>(this Argument argument, Argument<T[]> array, Argument<int> index, string? message = null)
  {
    argument.Self().InRangeOut(array.Value, index.Value, message, array.Name, index.Name);
    return index;
  }

  public static Argument<int> InRangeIn<T>(this Argument argument, Argument<T[]> array, Argument<int> index, string? message = null)
  {
    argument.Self().InRangeIn(array.Value, index.Value, message, array.Name, index.Name);
    return index;
  }

  public static Argument<(int index, int count)> InRangeOut<T>(this Argument argument, Argument<T[]> array, Argument<int> index, Argument<int> count, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InRangeOut(array.Value, index.Value, count.Value, message, array.Name, index.Name, count.Name),
      Name = null,
    };
  }

  public static Argument<(int index, int count)> InRangeIn<T>(this Argument argument, Argument<T[]> array, Argument<int> index, Argument<int> count, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InRangeIn(array.Value, index.Value, count.Value, message, array.Name, index.Name, count.Name),
      Name = null,
    };
  }

  public static Argument<(int index, int count)> InRangeOut<T>(this Argument argument, Argument<T[]> array, Argument<(int index, int count)> range, string? message = null)
  {
    argument.Self().InRangeOut(array.Value, range.Value, message, array.Name, range.Name);
    return range;
  }

  public static Argument<(int index, int count)> InRangeIn<T>(this Argument argument, Argument<T[]> array, Argument<(int index, int count)> range, string? message = null)
  {
    argument.Self().InRangeIn(array.Value, range.Value, message, array.Name, range.Name);
    return range;
  }

  public static Argument<int[]> InRangeOut(this Argument argument, Argument<Array> array, Argument<int[]> indices, bool zeroBased, string? message = null)
  {
    argument.Self().InRangeOut(array.Value, indices.Value, zeroBased, message, array.Name, indices.Name);
    return indices;
  }

  public static Argument<int[]> InRangeIn(this Argument argument, Argument<Array> array, Argument<int[]> indices, bool zeroBased, string? message = null)
  {
    argument.Self().InRangeIn(array.Value, indices.Value, zeroBased, message, array.Name, indices.Name);
    return indices;
  }

  public static Argument<(int index, int count)[]> InRangeOut(this Argument argument, Argument<Array> array, Argument<int[]> indices, Argument<int[]> counts, bool zeroBased, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InRangeOut(array.Value, indices.Value, counts.Value, zeroBased, message, array.Name, indices.Name, counts.Name),
      Name = null,
    };
  }

  public static Argument<(int index, int count)[]> InRangeIn(this Argument argument, Argument<Array> array, Argument<int[]> indices, Argument<int[]> counts, bool zeroBased, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InRangeIn(array.Value, indices.Value, counts.Value, zeroBased, message, array.Name, indices.Name, counts.Name),
      Name = null,
    };
  }

  public static Argument<(int index, int count)[]> InRangeOut(this Argument argument, Argument<Array> array, Argument<(int index, int count)[]> ranges, bool zeroBased, string? message = null)
  {
    argument.Self().InRangeOut(array.Value, ranges.Value, zeroBased, message, array.Name, ranges.Name);
    return ranges;
  }

  public static Argument<(int index, int count)[]> InRangeIn(this Argument argument, Argument<Array> array, Argument<(int index, int count)[]> ranges, bool zeroBased, string? message = null)
  {
    argument.Self().InRangeIn(array.Value, ranges.Value, zeroBased, message, array.Name, ranges.Name);
    return ranges;
  }

  public static Argument<long[]> InRangeOut(this Argument argument, Argument<Array> array, Argument<long[]> indices, string? message = null)
  {
    argument.Self().InRangeOut(array.Value, indices.Value, message, array.Name, indices.Name);
    return indices;
  }

  public static Argument<long[]> InRangeIn(this Argument argument, Argument<Array> array, Argument<long[]> indices, string? message = null)
  {
    argument.Self().InRangeIn(array.Value, indices.Value, message, array.Name, indices.Name);
    return indices;
  }

  public static Argument<(long index, long count)[]> InRangeOut(this Argument argument, Argument<Array> array, Argument<long[]> indices, Argument<long[]> counts, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InRangeOut(array.Value, indices.Value, counts.Value, message, array.Name, indices.Name, counts.Name),
      Name = null,
    };
  }

  public static Argument<(long index, long count)[]> InRangeIn(this Argument argument, Argument<Array> array, Argument<long[]> indices, Argument<long[]> counts, string? message = null)
  {
    return new()
    {
      Value = argument.Self().InRangeIn(array.Value, indices.Value, counts.Value, message, array.Name, indices.Name, counts.Name),
      Name = null,
    };
  }

  public static Argument<(long index, long count)[]> InRangeOut(this Argument argument, Argument<Array> array, Argument<(long index, long count)[]> ranges, string? message = null)
  {
    argument.Self().InRangeOut(array.Value, ranges.Value, message, array.Name, ranges.Name);
    return ranges;
  }

  public static Argument<(long index, long count)[]> InRangeIn(this Argument argument, Argument<Array> array, Argument<(long index, long count)[]> ranges, string? message = null)
  {
    argument.Self().InRangeIn(array.Value, ranges.Value, message, array.Name, ranges.Name);
    return ranges;
  }

  #endregion
  #region Array validation
  #region Validate array

  public static Argument<Array> ValidateRank(this Argument argument, Argument<Array> array, Action<int> rankValidator, string? rankMessage = null)
  {
    argument.Self().ValidateRank(array, rankValidator, rankMessage, array.Name);
    return array;
  }

  public static Argument<Array> ValidateLength(this Argument argument, Argument<Array> array, Action<int> lengthValidator, string? lengthMessage = null)
  {
    argument.Self().ValidateLength(array, lengthValidator, lengthMessage, array.Name);
    return array;
  }

  public static Argument<Array> ValidateLength(this Argument argument, Argument<Array> array, Action<long> lengthValidator, string? lengthMessage = null)
  {
    argument.Self().ValidateLength(array, lengthValidator, lengthMessage, array.Name);
    return array;
  }

  public static Argument<Array> ValidateDimLengths(this Argument argument, Argument<Array> array, Action<int[]> dimLengthsValidator, string? dimLengthsMessage = null)
  {
    argument.Self().ValidateDimLengths(array, dimLengthsValidator, dimLengthsMessage, array.Name);
    return array;
  }

  public static Argument<Array> ValidateDimLengths(this Argument argument, Argument<Array> array, Action<long[]> dimLengthsValidator, string? dimLengthsMessage = null)
  {
    argument.Self().ValidateDimLengths(array, dimLengthsValidator, dimLengthsMessage, array.Name);
    return array;
  }

  public static Argument<Array> ValidateDimensions(this Argument argument, Argument<Array> array, Action<ArrayDimension[]> dimensionsValidator, string? dimensionsMessage = null)
  {
    argument.Self().ValidateDimensions(array, dimensionsValidator, dimensionsMessage, array.Name);
    return array;
  }

  public static Argument<Array> ValidateElements(this Argument argument, Argument<Array> array, ElementDimAction<object?> itemValidator, string? itemMessage = null)
  {
    argument.Self().ValidateElements(array.Value, itemValidator, itemMessage, array.Name);
    return array;
  }

  public static Argument<Array> ValidateElements<T>(this Argument argument, Argument<Array> array, ElementDimAction<T> itemValidator, string? itemMessage = null)
  {
    argument.Self().ValidateElements(array.Value, itemValidator, itemMessage, array.Name);
    return array;
  }

  public static Argument<Array> ValidateElements(this Argument argument, Argument<Array> array, ElementDimLongAction<object?> itemValidator, string? itemMessage = null)
  {
    argument.Self().ValidateElements(array.Value, itemValidator, itemMessage, array.Name);
    return array;
  }

  public static Argument<Array> ValidateElements<T>(this Argument argument, Argument<Array> array, ElementDimLongAction<T> itemValidator, string? itemMessage = null)
  {
    argument.Self().ValidateElements(array.Value, itemValidator, itemMessage, array.Name);
    return array;
  }

  #endregion
  #region Match array

  public static Argument<Array> MatchRank(this Argument argument, Argument<Array> array, Predicate<int> rankPredicate, string? rankMessage = null)
  {
    argument.Self().MatchRank(array, rankPredicate, rankMessage, array.Name);
    return array;
  }

  public static Argument<Array> MatchLength(this Argument argument, Argument<Array> array, Predicate<int> lengthPredicate, string? lengthMessage = null)
  {
    argument.Self().MatchLength(array, lengthPredicate, lengthMessage, array.Name);
    return array;
  }

  public static Argument<Array> MatchLength(this Argument argument, Argument<Array> array, Predicate<long> lengthPredicate, string? lengthMessage = null)
  {
    argument.Self().MatchLength(array, lengthPredicate, lengthMessage, array.Name);
    return array;
  }

  public static Argument<Array> MatchDimLengths(this Argument argument, Argument<Array> array, Predicate<int[]> dimLengthsPredicate, string? dimLengthsMessage = null)
  {
    argument.Self().MatchDimLengths(array, dimLengthsPredicate, dimLengthsMessage, array.Name);
    return array;
  }

  public static Argument<Array> MatchDimLengths(this Argument argument, Argument<Array> array, Predicate<long[]> dimLengthsPredicate, string? dimLengthsMessage = null)
  {
    argument.Self().MatchDimLengths(array, dimLengthsPredicate, dimLengthsMessage, array.Name);
    return array;
  }

  public static Argument<Array> MatchElements(this Argument argument, Argument<Array> array, ElementDimPredicate<object?> itemPredicate, string? itemMessage = null)
  {
    argument.Self().MatchElements(array.Value, itemPredicate, itemMessage, array.Name);
    return array;
  }

  public static Argument<Array> MatchElements<T>(this Argument argument, Argument<Array> array, ElementDimPredicate<T> itemPredicate, string? itemMessage = null)
  {
    argument.Self().MatchElements(array.Value, itemPredicate, itemMessage, array.Name);
    return array;
  }

  public static Argument<Array> MatchElements(this Argument argument, Argument<Array> array, ElementDimLongPredicate<object?> itemPredicate, string? itemMessage = null)
  {
    argument.Self().MatchElements(array.Value, itemPredicate, itemMessage, array.Name);
    return array;
  }

  public static Argument<Array> MatchElements<T>(this Argument argument, Argument<Array> array, ElementDimLongPredicate<T> itemPredicate, string? itemMessage = null)
  {
    argument.Self().MatchElements(array.Value, itemPredicate, itemMessage, array.Name);
    return array;
  }

  #endregion
  #endregion
  #region Collection restrictions
  #region Match restrictions

  public static Argument<IEnumerable> MatchRestrictions(this Argument argument, Argument<IEnumerable> collection, Predicate<CollectionRestrictions> predicate, string? message = null)
  {
    argument.Self().MatchRestrictions(collection.Value, predicate, message, collection.Name);
    return collection;
  }

  public static Argument<IEnumerable<T>> MatchRestrictions<T>(this Argument argument, Argument<IEnumerable<T>> collection, Predicate<CollectionRestrictions> predicate, string? message = null)
  {
    argument.Self().MatchRestrictions(collection.Value, predicate, message, collection.Name);
    return collection;
  }

  #endregion
  #region Restricted

  public static Argument<IEnumerable> Restricted(this Argument argument, Argument<IEnumerable> collection, CollectionRestrictions restrictions, string? message = null)
  {
    argument.Self().Restricted(collection.Value, restrictions, message, collection.Name);
    return collection;
  }

  public static Argument<IEnumerable<T>> Restricted<T>(this Argument argument, Argument<IEnumerable<T>> collection, CollectionRestrictions restrictions, string? message = null)
  {
    argument.Self().Restricted<T>(collection.Value, restrictions, message, collection.Name);
    return collection;
  }

  #endregion
  #region Not restricted

  public static Argument<IEnumerable> NotRestricted(this Argument argument, Argument<IEnumerable> collection, string? message = null)
  {
    argument.Self().NotRestricted(collection.Value, message, collection.Name);
    return collection;
  }

  public static Argument<IEnumerable<T>> NotRestricted<T>(this Argument argument, Argument<IEnumerable<T>> collection, string? message = null)
  {
    argument.Self().NotRestricted(collection.Value, message, collection.Name);
    return collection;
  }

  #endregion
  #endregion
  #region Collection validation
  #region Validate collection

  public static Argument<IEnumerable> ValidateCount(this Argument argument, Argument<IEnumerable> collection, Action<int> countValidator, string? countMessage = null)
    => new()
    {
      Value = argument.Self().ValidateCount(collection.Value, countValidator, countMessage, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IEnumerable<T>> ValidateCount<T>(this Argument argument, Argument<IEnumerable<T>> collection, Action<int> countValidator, string? countMessage = null)
    => new()
    {
      Value = argument.Self().ValidateCount(collection.Value, countValidator, countMessage, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IAsyncEnumerable<T>> ValidateCountAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection, Action<int> countValidator, string? countMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidateCountAsync(collection.Value, countValidator, countMessage, collection.Name, cancellationToken),
      Name = collection.Name,
    };

  public static Argument<IEnumerable> ValidateElements(this Argument argument, Argument<IEnumerable> collection, Action<object?> itemValidator, bool lazy = false, string? itemMessage = null)
    => new()
    {
      Value = argument.Self().ValidateElements(collection.Value, itemValidator, lazy, itemMessage, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IEnumerable<T>> ValidateElements<T>(this Argument argument, Argument<IEnumerable<T>> collection, Action<T> itemValidator, bool lazy = false, string? itemMessage = null)
    => new()
    {
      Value = argument.Self().ValidateElements(collection.Value, itemValidator, lazy, itemMessage, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IAsyncEnumerable<T>> ValidateElementsAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection, Action<T> itemValidator, string? itemMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidateElementsAsync(collection.Value, itemValidator, itemMessage, collection.Name, cancellationToken),
      Name = collection.Name,
    };

  public static Argument<IEnumerable> ValidateElements(this Argument argument, Argument<IEnumerable> collection, ElementAction<object?> itemValidator, bool lazy = false, string? itemMessage = null)
    => new()
    {
      Value = argument.Self().ValidateElements(collection.Value, itemValidator, lazy, itemMessage, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IEnumerable<T>> ValidateElements<T>(this Argument argument, Argument<IEnumerable<T>> collection, ElementAction<T> itemValidator, bool lazy = false, string? itemMessage = null)
    => new()
    {
      Value = argument.Self().ValidateElements(collection.Value, itemValidator, lazy, itemMessage, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IAsyncEnumerable<T>> ValidateElementsAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection, ElementAction<T> itemValidator, string? itemMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidateElementsAsync(collection.Value, itemValidator, itemMessage, collection.Name, cancellationToken),
      Name = collection.Name,
    };

  public static Argument<IEnumerable> ValidateCollection(this Argument argument, Argument<IEnumerable> collection, Action<int>? countValidator, Action<object?>? itemValidator, bool lazy = false,
    string? countMessage = null, string? itemMessage = null)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().ValidateCollection(collection.Value, countValidator, itemValidator, lazy, countMessage, itemMessage, collection.Name),
    };

  public static Argument<IEnumerable<T>> ValidateCollection<T>(this Argument argument, Argument<IEnumerable<T>> collection, Action<int>? countValidator, Action<T>? itemValidator, bool lazy = false,
    string? countMessage = null, string? itemMessage = null)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().ValidateCollection(collection.Value, countValidator, itemValidator, lazy, countMessage, itemMessage, collection.Name),
    };

  public static Argument<IAsyncEnumerable<T>> ValidateCollectionAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection, Action<int>? countValidator, Action<T>? itemValidator,
    string? countMessage = null, string? itemMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().ValidateCollectionAsync(collection.Value, countValidator, itemValidator, countMessage, itemMessage, collection.Name, cancellationToken),
    };

  public static Argument<IEnumerable> ValidateCollection(this Argument argument, Argument<IEnumerable> collection, Action<int>? countValidator, ElementAction<object?>? itemValidator, bool lazy = false,
    string? countMessage = null, string? itemMessage = null)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().ValidateCollection(collection.Value, countValidator, itemValidator, lazy, countMessage, itemMessage, collection.Name),
    };

  public static Argument<IEnumerable<T>> ValidateCollection<T>(this Argument argument, Argument<IEnumerable<T>> collection, Action<int>? countValidator, ElementAction<T>? itemValidator, bool lazy = false,
    string? countMessage = null, string? itemMessage = null)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().ValidateCollection(collection.Value, countValidator, itemValidator, lazy, countMessage, itemMessage, collection.Name),
    };

  public static Argument<IAsyncEnumerable<T>> ValidateCollectionAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection, Action<int>? countValidator, ElementAction<T>? itemValidator,
    string? countMessage = null, string? itemMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().ValidateCollectionAsync(collection.Value, countValidator, itemValidator, countMessage, itemMessage, collection.Name, cancellationToken),
    };

  #endregion
  #region Match collection

  public static Argument<IEnumerable> MatchCount(this Argument argument, Argument<IEnumerable> collection, Predicate<int> countPredicate, string? countMessage = null)
    => new()
    {
      Value = argument.Self().MatchCount(collection.Value, countPredicate, countMessage, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IEnumerable<T>> MatchCount<T>(this Argument argument, Argument<IEnumerable<T>> collection, Predicate<int> countPredicate, string? countMessage = null)
    => new()
    {
      Value = argument.Self().MatchCount(collection.Value, countPredicate, countMessage, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IAsyncEnumerable<T>> MatchCountAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection, Predicate<int> countPredicate, string? countMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchCountAsync(collection.Value, countPredicate, countMessage, collection.Name, cancellationToken),
      Name = collection.Name,
    };

  public static Argument<IEnumerable> MatchElements(this Argument argument, Argument<IEnumerable> collection, Predicate<object?> itemPredicate, bool lazy = false, string? itemMessage = null)
    => new()
    {
      Value = argument.Self().MatchElements(collection.Value, itemPredicate, lazy, itemMessage, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IEnumerable<T>> MatchElements<T>(this Argument argument, Argument<IEnumerable<T>> collection, Predicate<T> itemPredicate, bool lazy = false, string? itemMessage = null)
    => new()
    {
      Value = argument.Self().MatchElements(collection.Value, itemPredicate, lazy, itemMessage, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IAsyncEnumerable<T>> MatchElementsAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection, Predicate<T> itemPredicate, string? itemMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchElementsAsync(collection.Value, itemPredicate, itemMessage, collection.Name, cancellationToken),
      Name = collection.Name,
    };

  public static Argument<IEnumerable> MatchElements(this Argument argument, Argument<IEnumerable> collection, ElementPredicate<object?> itemPredicate, bool lazy = false, string? itemMessage = null)
    => new()
    {
      Value = argument.Self().MatchElements(collection.Value, itemPredicate, lazy, itemMessage, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IEnumerable<T>> MatchElements<T>(this Argument argument, Argument<IEnumerable<T>> collection, ElementPredicate<T> itemPredicate, bool lazy = false, string? itemMessage = null)
    => new()
    {
      Value = argument.Self().MatchElements(collection.Value, itemPredicate, lazy, itemMessage, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IAsyncEnumerable<T>> MatchElementsAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection, ElementPredicate<T> itemPredicate, string? itemMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchElementsAsync(collection.Value, itemPredicate, itemMessage, collection.Name, cancellationToken),
      Name = collection.Name,
    };

  public static Argument<IEnumerable> MatchCollection(this Argument argument, Argument<IEnumerable> collection, Predicate<int>? countPredicate, Predicate<object?>? itemPredicate, bool lazy = false,
    string? countMessage = null, string? itemMessage = null)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().MatchCollection(collection.Value, countPredicate, itemPredicate, lazy, countMessage, itemMessage, collection.Name),
    };

  public static Argument<IEnumerable<T>> MatchCollection<T>(this Argument argument, Argument<IEnumerable<T>> collection, Predicate<int>? countPredicate, Predicate<T>? itemPredicate, bool lazy = false,
    string? countMessage = null, string? itemMessage = null)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().MatchCollection(collection.Value, countPredicate, itemPredicate, lazy, countMessage, itemMessage, collection.Name),
    };

  public static Argument<IAsyncEnumerable<T>> MatchCollectionAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection, Predicate<int>? countPredicate, Predicate<T>? itemPredicate,
    string? countMessage = null, string? itemMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().MatchCollectionAsync(collection.Value, countPredicate, itemPredicate, countMessage, itemMessage, collection.Name, cancellationToken),
    };

  public static Argument<IEnumerable> MatchCollection(this Argument argument, Argument<IEnumerable> collection, Predicate<int>? countPredicate, ElementPredicate<object?>? itemPredicate, bool lazy = false,
    string? countMessage = null, string? itemMessage = null)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().MatchCollection(collection.Value, countPredicate, itemPredicate, lazy, countMessage, itemMessage, collection.Name),
    };

  public static Argument<IEnumerable<T>> MatchCollection<T>(this Argument argument, Argument<IEnumerable<T>> collection, Predicate<int>? countPredicate, ElementPredicate<T>? itemPredicate, bool lazy = false,
    string? countMessage = null, string? itemMessage = null)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().MatchCollection(collection.Value, countPredicate, itemPredicate, lazy, countMessage, itemMessage, collection.Name),
    };

  public static Argument<IAsyncEnumerable<T>> MatchCollectionAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection, Predicate<int>? countPredicate, ElementPredicate<T>? itemPredicate,
    string? countMessage = null, string? itemMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().MatchCollectionAsync(collection.Value, countPredicate, itemPredicate, countMessage, itemMessage, collection.Name, cancellationToken),
    };

  public static Argument<IEnumerable> MatchCollection(this Argument argument, Argument<IEnumerable> collection, Predicate<int>? countPredicate, Predicate<object?> itemPredicate, Func<int, int, bool> totalPredicate, bool lazy = false,
    string? countMessage = null, string? itemMessage = null)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().MatchCollection(collection.Value, countPredicate, itemPredicate, totalPredicate, lazy, countMessage, itemMessage, collection.Name),
    };

  public static Argument<IEnumerable<T>> MatchCollection<T>(this Argument argument, Argument<IEnumerable<T>> collection, Predicate<int>? countPredicate, Predicate<T> itemPredicate, Func<int, int, bool> totalPredicate, bool lazy = false,
    string? countMessage = null, string? itemMessage = null)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().MatchCollection(collection.Value, countPredicate, itemPredicate, totalPredicate, lazy, countMessage, itemMessage, collection.Name),
    };

  public static Argument<IAsyncEnumerable<T>> MatchCollectionAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection, Predicate<int>? countPredicate, Predicate<T> itemPredicate, Func<int, int, bool> totalPredicate,
    string? countMessage = null, string? itemMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().MatchCollectionAsync(collection.Value, countPredicate, itemPredicate, totalPredicate, countMessage, itemMessage, collection.Name, cancellationToken),
    };

  public static Argument<IEnumerable> MatchCollection(this Argument argument, Argument<IEnumerable> collection, Predicate<int>? countPredicate, ElementPredicate<object?> itemPredicate, Func<int, int, bool> totalPredicate, bool lazy = false,
    string? countMessage = null, string? itemMessage = null)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().MatchCollection(collection.Value, countPredicate, itemPredicate, totalPredicate, lazy, countMessage, itemMessage, collection.Name),
    };

  public static Argument<IEnumerable<T>> MatchCollection<T>(this Argument argument, Argument<IEnumerable<T>> collection, Predicate<int>? countPredicate, ElementPredicate<T> itemPredicate, Func<int, int, bool> totalPredicate, bool lazy = false,
    string? countMessage = null, string? itemMessage = null)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().MatchCollection(collection.Value, countPredicate, itemPredicate, totalPredicate, lazy, countMessage, itemMessage, collection.Name),
    };

  public static Argument<IAsyncEnumerable<T>> MatchCollectionAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection, Predicate<int>? countPredicate, ElementPredicate<T> itemPredicate, Func<int, int, bool> totalPredicate,
    string? countMessage = null, string? itemMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().MatchCollectionAsync(collection.Value, countPredicate, itemPredicate, totalPredicate, countMessage, itemMessage, collection.Name, cancellationToken),
    };

  public static Argument<IEnumerable> MatchCollection(this Argument argument, Argument<IEnumerable> collection, Predicate<int>? countPredicate, Predicate<object?> itemPredicate, Func<object?, int, int, bool> stopPredicate, Func<int, int, bool, bool> totalPredicate, bool lazy = false,
    string? countMessage = null, string? itemMessage = null)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().MatchCollection(collection.Value, countPredicate, itemPredicate, stopPredicate, totalPredicate, lazy, countMessage, itemMessage, collection.Name),
    };

  public static Argument<IEnumerable<T>> MatchCollection<T>(this Argument argument, Argument<IEnumerable<T>> collection, Predicate<int>? countPredicate, Predicate<T> itemPredicate, Func<T, int, int, bool> stopPredicate, Func<int, int, bool, bool> totalPredicate, bool lazy = false,
    string? countMessage = null, string? itemMessage = null)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().MatchCollection(collection.Value, countPredicate, itemPredicate, stopPredicate, totalPredicate, lazy, countMessage, itemMessage, collection.Name),
    };

  public static Argument<IAsyncEnumerable<T>> MatchCollectionAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection, Predicate<int>? countPredicate, Predicate<T> itemPredicate, Func<T, int, int, bool> stopPredicate, Func<int, int, bool, bool> totalPredicate,
    string? countMessage = null, string? itemMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().MatchCollectionAsync(collection.Value, countPredicate, itemPredicate, stopPredicate, totalPredicate, countMessage, itemMessage, collection.Name, cancellationToken),
    };

  public static Argument<IEnumerable> MatchCollection(this Argument argument, Argument<IEnumerable> collection, Predicate<int>? countPredicate, ElementPredicate<object?> itemPredicate, Func<object?, int, int, bool> stopPredicate, Func<int, int, bool, bool> totalPredicate, bool lazy = false,
    string? countMessage = null, string? itemMessage = null)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().MatchCollection(collection.Value, countPredicate, itemPredicate, stopPredicate, totalPredicate, lazy, countMessage, itemMessage, collection.Name),
    };

  public static Argument<IEnumerable<T>> MatchCollection<T>(this Argument argument, Argument<IEnumerable<T>> collection, Predicate<int>? countPredicate, ElementPredicate<T> itemPredicate, Func<T, int, int, bool> stopPredicate, Func<int, int, bool, bool> totalPredicate, bool lazy = false,
    string? countMessage = null, string? itemMessage = null)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().MatchCollection(collection.Value, countPredicate, itemPredicate, stopPredicate, totalPredicate, lazy, countMessage, itemMessage, collection.Name),
    };

  public static Argument<IAsyncEnumerable<T>> MatchCollectionAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection, Predicate<int>? countPredicate, ElementPredicate<T> itemPredicate, Func<T, int, int, bool> stopPredicate, Func<int, int, bool, bool> totalPredicate,
    string? countMessage = null, string? itemMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().MatchCollectionAsync(collection.Value, countPredicate, itemPredicate, stopPredicate, totalPredicate, countMessage, itemMessage, collection.Name, cancellationToken),
    };

  #endregion
  #region Match exact elements

  public static Argument<IEnumerable> MatchExact(this Argument argument, Argument<IEnumerable> collection, int count, Predicate<object?> itemPredicate, string? itemMessage = null)
    => new()
    {
      Value = argument.Self().MatchExact(collection.Value, count, itemPredicate, itemMessage, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IEnumerable<T>> MatchExact<T>(this Argument argument, Argument<IEnumerable<T>> collection, int count, Predicate<T> itemPredicate, string? itemMessage = null)
    => new()
    {
      Value = argument.Self().MatchExact(collection.Value, count, itemPredicate, itemMessage, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IAsyncEnumerable<T>> MatchExactAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection, int count, Predicate<T> itemPredicate, string? itemMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchExactAsync(collection.Value, count, itemPredicate, itemMessage, collection.Name, cancellationToken),
      Name = collection.Name,
    };

  public static Argument<IEnumerable> MatchExact(this Argument argument, Argument<IEnumerable> collection, int count, ElementPredicate<object?> itemPredicate, string? itemMessage = null)
    => new()
    {
      Value = argument.Self().MatchExact(collection.Value, count, itemPredicate, itemMessage, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IEnumerable<T>> MatchExact<T>(this Argument argument, Argument<IEnumerable<T>> collection, int count, ElementPredicate<T> itemPredicate, string? itemMessage = null)
    => new()
    {
      Value = argument.Self().MatchExact(collection.Value, count, itemPredicate, itemMessage, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IAsyncEnumerable<T>> MatchExactAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection, int count, ElementPredicate<T> itemPredicate,
    string? itemMessage = null, CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchExactAsync(collection.Value, count, itemPredicate, itemMessage, collection.Name, cancellationToken),
      Name = collection.Name,
    };

  #endregion
  #region Match any element

  public static Argument<IEnumerable> MatchAny(this Argument argument, Argument<IEnumerable> collection, Predicate<object?> itemPredicate, string? itemMessage = null)
    => new()
    {
      Value = argument.Self().MatchAny(collection.Value, itemPredicate, itemMessage, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IEnumerable<T>> MatchAny<T>(this Argument argument, Argument<IEnumerable<T>> collection, Predicate<T> itemPredicate, string? itemMessage = null)
    => new()
    {
      Value = argument.Self().MatchAny(collection.Value, itemPredicate, itemMessage, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IAsyncEnumerable<T>> MatchAnyAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection, Predicate<T> itemPredicate, string? itemMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchAnyAsync(collection.Value, itemPredicate, itemMessage, collection.Name, cancellationToken),
      Name = collection.Name,
    };

  public static Argument<IEnumerable> MatchAny(this Argument argument, Argument<IEnumerable> collection, ElementPredicate<object?> itemPredicate, string? itemMessage = null)
    => new()
    {
      Value = argument.Self().MatchAny(collection.Value, itemPredicate, itemMessage, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IEnumerable<T>> MatchAny<T>(this Argument argument, Argument<IEnumerable<T>> collection, ElementPredicate<T> itemPredicate, string? itemMessage = null)
    => new()
    {
      Value = argument.Self().MatchAny(collection.Value, itemPredicate, itemMessage, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IAsyncEnumerable<T>> MatchAnyAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection, ElementPredicate<T> itemPredicate, string? itemMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchAnyAsync(collection.Value, itemPredicate, itemMessage, collection.Name, cancellationToken),
      Name = collection.Name,
    };

  #endregion
  #region Match all elements

  public static Argument<IEnumerable> MatchAll(this Argument argument, Argument<IEnumerable> collection, Predicate<object?> itemPredicate, string? itemMessage = null)
    => new()
    {
      Value = argument.Self().MatchAll(collection.Value, itemPredicate, itemMessage, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IEnumerable<T>> MatchAll<T>(this Argument argument, Argument<IEnumerable<T>> collection, Predicate<T> itemPredicate, string? itemMessage = null)
    => new()
    {
      Value = argument.Self().MatchAll(collection.Value, itemPredicate, itemMessage, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IAsyncEnumerable<T>> MatchAllAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection, Predicate<T> itemPredicate, string? itemMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchAllAsync(collection.Value, itemPredicate, itemMessage, collection.Name, cancellationToken),
      Name = collection.Name,
    };

  public static Argument<IEnumerable> MatchAll(this Argument argument, Argument<IEnumerable> collection, ElementPredicate<object?> itemPredicate, string? itemMessage = null)
    => new()
    {
      Value = argument.Self().MatchAll(collection.Value, itemPredicate, itemMessage, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IEnumerable<T>> MatchAll<T>(this Argument argument, Argument<IEnumerable<T>> collection, ElementPredicate<T> itemPredicate, string? itemMessage = null)
    => new()
    {
      Value = argument.Self().MatchAll(collection.Value, itemPredicate, itemMessage, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IAsyncEnumerable<T>> MatchAllAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection, ElementPredicate<T> itemPredicate, string? itemMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchAllAsync(collection.Value, itemPredicate, itemMessage, collection.Name, cancellationToken),
      Name = collection.Name,
    };

  #endregion
  #region Empty

  public static Argument<IEnumerable> Empty(this Argument argument, Argument<IEnumerable> collection, string? message = null)
    => new()
    {
      Value = argument.Self().Empty(collection.Value, message, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IEnumerable<T>> Empty<T>(this Argument argument, Argument<IEnumerable<T>> collection, string? message = null)
    => new()
    {
      Value = argument.Self().Empty(collection.Value, message, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IAsyncEnumerable<T>> EmptyAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection, string? message = null, CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().EmptyAsync(collection.Value, message, collection.Name, cancellationToken),
      Name = collection.Name,
    };

  #endregion
  #region Not empty

  public static Argument<IEnumerable> NotEmpty(this Argument argument, Argument<IEnumerable> collection, string? message = null)
    => new()
    {
      Value = argument.Self().NotEmpty(collection.Value, message, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IEnumerable<T>> NotEmpty<T>(this Argument argument, Argument<IEnumerable<T>> collection, string? message = null)
    => new()
    {
      Value = argument.Self().NotEmpty(collection.Value, message, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IAsyncEnumerable<T>> NotEmptyAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection, string? message = null, CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotEmptyAsync(collection.Value, message, collection.Name, cancellationToken),
      Name = collection.Name,
    };

  #endregion
  #region Exact count

  public static Argument<IEnumerable> ExactCount(this Argument argument, Argument<IEnumerable> collection, int count, string? message = null)
    => new()
    {
      Value = argument.Self().ExactCount(collection.Value, count, message, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IEnumerable<T>> ExactCount<T>(this Argument argument, Argument<IEnumerable<T>> collection, int count, string? message = null)
    => new()
    {
      Value = argument.Self().ExactCount(collection.Value, count, message, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IAsyncEnumerable<T>> ExactCount<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection, int count, string? message = null, CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ExactCountAsync(collection.Value, count, message, collection.Name, cancellationToken),
      Name = collection.Name,
    };

  #endregion
  #region Not exact count

  public static Argument<IEnumerable> NotExactCount(this Argument argument, Argument<IEnumerable> collection, int count, string? message = null)
    => new()
    {
      Value = argument.Self().NotExactCount(collection.Value, count, message, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IEnumerable<T>> NotExactCount<T>(this Argument argument, Argument<IEnumerable<T>> collection, int count, string? message = null)
    => new()
    {
      Value = argument.Self().NotExactCount(collection.Value, count, message, collection.Name),
      Name = collection.Name,
    };

  public static Argument<IAsyncEnumerable<T>> NotExactCount<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection, int count, string? message = null, CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotExactCountAsync(collection.Value, count, message, collection.Name, cancellationToken),
      Name = collection.Name,
    };

  #endregion
  #region Not null elements

  public static Argument<IEnumerable> NotNullElements(this Argument argument, Argument<IEnumerable> collection, bool lazy = false,
    string? message = null)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().NotNullElements(collection.Value, lazy, message, collection.Name),
    };

  public static Argument<IEnumerable<T>> NotNullElements<T>(this Argument argument, Argument<IEnumerable<T?>> collection, bool lazy = false,
    string? message = null)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().NotNullElements(collection.Value, lazy, message, collection.Name),
    };

  public static Argument<IAsyncEnumerable<T>> NotNullElementsAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> collection,
    string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().NotNullElementsAsync(collection.Value, message, collection.Name, cancellationToken),
    };

  public static Argument<IEnumerable<T>> NonNullElements<T>(this Argument argument, Argument<IEnumerable<T>> collection, bool lazy = false,
    string? message = null)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().NonNullElements(collection.Value, lazy, message, collection.Name),
    };

  public static Argument<IAsyncEnumerable<T>> NonNullElementsAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T>> collection,
    string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Name = collection.Name,
      Value = argument.Self().NonNullElementsAsync(collection.Value, message, collection.Name, cancellationToken),
    };

  #endregion
  #region Element fail

  [DoesNotReturn]
  public static void ElementFail<TCollection>(this Argument argument, Argument<TCollection> collection, Argument<int> index, Exception? innerException = null, string? message = null)
    where TCollection : IEnumerable
    => argument.Self().ElementFail(collection.Value, index, innerException, message, collection.Name, index.Name);

  [DoesNotReturn]
  public static void ElementFail<TSource>(this Argument argument, Argument<IEnumerable<TSource>> collection, Argument<int> index, Exception? innerException = null, string? message = null)
    => argument.Self().ElementFail(collection.Value, index, innerException, message, collection.Name, index.Name);

  [DoesNotReturn]
  public static void ElementFail<TSource>(this Argument argument, Argument<TSource[]> collection, Argument<int> index, Exception? innerException = null, string? message = null)
    => argument.Self().ElementFail(collection.Value, index, innerException, message, collection.Name, index.Name);

  [DoesNotReturn]
  public static void ElementFail(this Argument argument, Argument<Array> array, Argument<int> index, Exception? innerException = null, string? message = null)
    => argument.Self().ElementFail(array.Value, index, innerException, message, array.Name, index.Name);

  [DoesNotReturn]
  public static void ElementFail(this Argument argument, Argument<Array> array, Argument<int[]> indices, Exception? innerException = null, string? message = null)
    => argument.Self().ElementFail(array.Value, indices, innerException, message, array.Name, indices.Name);

  #endregion
  #endregion
  #region Collections validation
  #region Validate coupled collections

  public static Argument<IEnumerable<(object? x, object? y)>> ValidateCoupledCount(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Action<int>? countValidator, bool lazy = false,
    string? countMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().ValidateCoupledCount(xCollection.Value, yCollection.Value, countValidator, lazy, countMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TX? x, TY? y)>> ValidateCoupledCount<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Action<int>? countValidator, bool lazy = false,
    string? countMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().ValidateCoupledCount(xCollection.Value, yCollection.Value, countValidator, lazy, countMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> ValidateCoupledCountAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Action<int>? countValidator,
    string? countMessage = null, string? imbalancedMessage = null, CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidateCoupledCountAsync(xCollection.Value, yCollection.Value, countValidator, countMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> ValidateCoupledCountAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    Action<int>? countValidator,
    string? countMessage = null, string? imbalancedMessage = null, CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidateCoupledCountAsync(xCollection.Value, yCollection.Value, countValidator, countMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> ValidateCoupledElements(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Action<(object? x, object? y)> xyItemsValidator, bool lazy = false,
    string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().ValidateCoupledElements(xCollection.Value, yCollection.Value, xyItemsValidator, lazy, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> ValidateCoupledElements(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Action<object?> xItemValidator, Action<object?> yItemValidator, Action<(object? x, object? y)> xyItemsValidator, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().ValidateCoupledElements(xCollection.Value, yCollection.Value, xItemValidator, yItemValidator, xyItemsValidator, lazy,
        xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> ValidateCoupledElements(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    ElementAction<(object? x, object? y)> xyItemsValidator, bool lazy = false,
    string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().ValidateCoupledElements(xCollection.Value, yCollection.Value, xyItemsValidator, lazy, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> ValidateCoupledElements(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    ElementAction<object?> xItemValidator, ElementAction<object?> yItemValidator, ElementAction<(object? x, object? y)> xyItemsValidator, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().ValidateCoupledElements(xCollection.Value, yCollection.Value, xItemValidator, yItemValidator, xyItemsValidator, lazy,
        xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TX? x, TY? y)>> ValidateCoupledElements<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Action<(TX? x, TY? y)> xyItemsValidator, bool lazy = false,
    string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().ValidateCoupledElements(xCollection.Value, yCollection.Value, xyItemsValidator, lazy, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TX? x, TY? y)>> ValidateCoupledElements<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Action<TX?> xItemValidator, Action<TY?> yItemValidator, Action<(TX? x, TY? y)> xyItemsValidator, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().ValidateCoupledElements(xCollection.Value, yCollection.Value, xItemValidator, yItemValidator, xyItemsValidator, lazy,
        xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TX? x, TY? y)>> ValidateCoupledElements<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    ElementAction<(TX? x, TY? y)> xyItemsValidator, bool lazy = false,
    string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().ValidateCoupledElements(xCollection.Value, yCollection.Value, xyItemsValidator, lazy, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TX? x, TY? y)>> ValidateCoupledElements<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    ElementAction<TX?> xItemValidator, ElementAction<TY?> yItemValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().ValidateCoupledElements(xCollection.Value, yCollection.Value, xItemValidator, yItemValidator, xyItemsValidator, lazy,
        xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> ValidateCoupledElementsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Action<(TX? x, TY? y)> xyItemsValidator,
    string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidateCoupledElementsAsync(xCollection.Value, yCollection.Value, xyItemsValidator, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> ValidateCoupledElementsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Action<TX?> xItemValidator, Action<TY?> yItemValidator, Action<(TX? x, TY? y)> xyItemsValidator,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidateCoupledElementsAsync(xCollection.Value, yCollection.Value, xItemValidator, yItemValidator,
        xyItemsValidator, xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> ValidateCoupledElementsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    ElementAction<(TX? x, TY? y)> xyItemsValidator,
    string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidateCoupledElementsAsync(xCollection.Value, yCollection.Value, xyItemsValidator, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> ValidateCoupledElementsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    ElementAction<TX?> xItemValidator, ElementAction<TY?> yItemValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidateCoupledElementsAsync(xCollection.Value, yCollection.Value, xItemValidator, yItemValidator, xyItemsValidator,
        xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> ValidateCoupledElementsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    Action<(TX? x, TY? y)> xyItemsValidator,
    string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidateCoupledElementsAsync(xCollection.Value, yCollection.Value, xyItemsValidator, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> ValidateCoupledElementsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    Action<TX?> xItemValidator, Action<TY?> yItemValidator, Action<(TX? x, TY? y)> xyItemsValidator,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidateCoupledElementsAsync(xCollection.Value, yCollection.Value, xItemValidator, yItemValidator, xyItemsValidator,
        xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> ValidateCoupledElementsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    ElementAction<(TX? x, TY? y)> xyItemsValidator,
    string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidateCoupledElementsAsync(xCollection.Value, yCollection.Value, xyItemsValidator, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> ValidateCoupledElementsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    ElementAction<TX?> xItemValidator, ElementAction<TY?> yItemValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidateCoupledElementsAsync(xCollection.Value, yCollection.Value, xItemValidator, yItemValidator, xyItemsValidator,
        xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> ValidateCoupled(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Action<int> countValidator, Action<(object? x, object? y)> xyItemsValidator, bool lazy = false,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().ValidateCoupled(xCollection.Value, yCollection.Value, countValidator, xyItemsValidator, lazy, countMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> ValidateCoupled(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Action<int> countValidator, Action<object?> xItemValidator, Action<object?> yItemValidator, Action<(object? x, object? y)> xyItemsValidator, bool lazy = false,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().ValidateCoupled(xCollection.Value, yCollection.Value, countValidator, xItemValidator, yItemValidator, xyItemsValidator, lazy,
        countMessage, xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> ValidateCoupled(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Action<int> countValidator, ElementAction<(object? x, object? y)> xyItemsValidator, bool lazy = false,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().ValidateCoupled(xCollection.Value, yCollection.Value, countValidator, xyItemsValidator, lazy, countMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> ValidateCoupled(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Action<int> countValidator, ElementAction<object?> xItemValidator, ElementAction<object?> yItemValidator, ElementAction<(object? x, object? y)> xyItemsValidator, bool lazy = false,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().ValidateCoupled(xCollection.Value, yCollection.Value, countValidator, xItemValidator, yItemValidator, xyItemsValidator, lazy,
        countMessage, xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TX? x, TY? y)>> ValidateCoupled<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Action<int> countValidator, Action<(TX? x, TY? y)> xyItemsValidator, bool lazy = false,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().ValidateCoupled(xCollection.Value, yCollection.Value, countValidator, xyItemsValidator, lazy, countMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TX? x, TY? y)>> ValidateCoupled<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Action<int> countValidator, Action<TX?> xItemValidator, Action<TY?> yItemValidator, Action<(TX? x, TY? y)> xyItemsValidator, bool lazy = false,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().ValidateCoupled(xCollection.Value, yCollection.Value, countValidator, xItemValidator, yItemValidator, xyItemsValidator, lazy,
        countMessage, xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TX? x, TY? y)>> ValidateCoupled<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Action<int> countValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator, bool lazy = false,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().ValidateCoupled(xCollection.Value, yCollection.Value, countValidator, xyItemsValidator, lazy, countMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TX? x, TY? y)>> ValidateCoupled<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Action<int> countValidator, ElementAction<TX?> xItemValidator, ElementAction<TY?> yItemValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator, bool lazy = false,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().ValidateCoupled(xCollection.Value, yCollection.Value, countValidator, xItemValidator, yItemValidator, xyItemsValidator, lazy,
        countMessage, xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> ValidateCoupledAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Action<int> countValidator, Action<(TX? x, TY? y)> xyItemsValidator,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidateCoupledAsync(xCollection.Value, yCollection.Value, countValidator, xyItemsValidator, countMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> ValidateCoupledAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Action<int> countValidator, Action<TX?> xItemValidator, Action<TY?> yItemValidator, Action<(TX? x, TY? y)> xyItemsValidator,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidateCoupledAsync(xCollection.Value, yCollection.Value, countValidator, xItemValidator, yItemValidator, xyItemsValidator,
        countMessage, xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> ValidateCoupledAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Action<int> countValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidateCoupledAsync(xCollection.Value, yCollection.Value, countValidator, xyItemsValidator, countMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> ValidateCoupledAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Action<int> countValidator, ElementAction<TX?> xItemValidator, ElementAction<TY?> yItemValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidateCoupledAsync(xCollection.Value, yCollection.Value, countValidator, xItemValidator, yItemValidator, xyItemsValidator,
        countMessage, xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> ValidateCoupledAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    Action<int> countValidator, Action<(TX? x, TY? y)> xyItemsValidator,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidateCoupledAsync(xCollection.Value, yCollection.Value, countValidator, xyItemsValidator, countMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> ValidateCoupledAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    Action<int> countValidator, Action<TX?> xItemValidator, Action<TY?> yItemValidator, Action<(TX? x, TY? y)> xyItemsValidator,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidateCoupledAsync(xCollection.Value, yCollection.Value, countValidator, xItemValidator, yItemValidator, xyItemsValidator,
        countMessage, xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> ValidateCoupledAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    Action<int> countValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidateCoupledAsync(xCollection.Value, yCollection.Value, countValidator, xyItemsValidator, countMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> ValidateCoupledAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    Action<int> countValidator, ElementAction<TX?> xItemValidator, ElementAction<TY?> yItemValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidateCoupledAsync(xCollection.Value, yCollection.Value, countValidator, xItemValidator, yItemValidator, xyItemsValidator,
        countMessage, xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  #endregion
  #region Match coupled collections

  public static Argument<IEnumerable<(object? x, object? y)>> MatchCoupledCount(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Predicate<int>? countPredicate, bool lazy = false,
    string? countMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().MatchCoupledCount(xCollection.Value, yCollection.Value, countPredicate, lazy,
        countMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TX? x, TY? y)>> MatchCoupledCount<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Predicate<int>? countPredicate, bool lazy = false,
    string? countMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().MatchCoupledCount(xCollection.Value, yCollection.Value, countPredicate, lazy,
        countMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> MatchCoupledCountAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Predicate<int>? countPredicate,
    string? countMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchCoupledCountAsync(xCollection.Value, yCollection.Value, countPredicate,
        countMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> MatchCoupledCountAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    Predicate<int>? countPredicate,
    string? countMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchCoupledCountAsync(xCollection.Value, yCollection.Value, countPredicate,
        countMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> MatchCoupledElements(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Predicate<(object? x, object? y)> xyItemsPredicate, bool lazy = false,
    string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().MatchCoupledElements(xCollection.Value, yCollection.Value, xyItemsPredicate, lazy,
        xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> MatchCoupledElements(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Predicate<object?> xItemPredicate, Predicate<object?> yItemPredicate, Predicate<(object? x, object? y)> xyItemsPredicate, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().MatchCoupledElements(xCollection.Value, yCollection.Value, xItemPredicate, yItemPredicate, xyItemsPredicate, lazy,
        xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> MatchCoupledElements(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    ElementPredicate<(object? x, object? y)> xyItemsPredicate, bool lazy = false,
    string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().MatchCoupledElements(xCollection.Value, yCollection.Value, xyItemsPredicate, lazy,
        xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> MatchCoupledElements(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    ElementPredicate<object?> xItemPredicate, ElementPredicate<object?> yItemPredicate, ElementPredicate<(object? x, object? y)> xyItemsPredicate, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().MatchCoupledElements(xCollection.Value, yCollection.Value, xItemPredicate, yItemPredicate, xyItemsPredicate, lazy,
        xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TX? x, TY? y)>> MatchCoupledElements<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Predicate<(TX? x, TY? y)> xyItemsPredicate, bool lazy = false,
    string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().MatchCoupledElements(xCollection.Value, yCollection.Value, xyItemsPredicate, lazy,
        xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TX? x, TY? y)>> MatchCoupledElements<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Predicate<TX?> xItemPredicate, Predicate<TY?> yItemPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().MatchCoupledElements(xCollection.Value, yCollection.Value, xItemPredicate, yItemPredicate, xyItemsPredicate, lazy,
        xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TX? x, TY? y)>> MatchCoupledElements<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    ElementPredicate<(TX? x, TY? y)> xyItemsPredicate, bool lazy = false,
    string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().MatchCoupledElements(xCollection.Value, yCollection.Value, xyItemsPredicate, lazy,
        xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TX? x, TY? y)>> MatchCoupledElements<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    ElementPredicate<TX?> xItemPredicate, ElementPredicate<TY?> yItemPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().MatchCoupledElements(xCollection.Value, yCollection.Value, xItemPredicate, yItemPredicate, xyItemsPredicate, lazy,
        xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> MatchCoupledElementsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Predicate<(TX? x, TY? y)> xyItemsPredicate,
    string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchCoupledElementsAsync(xCollection.Value, yCollection.Value, xyItemsPredicate,
        xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> MatchCoupledElementsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Predicate<TX?> xItemPredicate, Predicate<TY?> yItemPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchCoupledElementsAsync(xCollection.Value, yCollection.Value, xItemPredicate, yItemPredicate, xyItemsPredicate,
        xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> MatchCoupledElementsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    ElementPredicate<(TX? x, TY? y)> xyItemsPredicate,
    string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchCoupledElementsAsync(xCollection.Value, yCollection.Value, xyItemsPredicate,
        xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> MatchCoupledElementsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    ElementPredicate<TX?> xItemPredicate, ElementPredicate<TY?> yItemPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchCoupledElementsAsync(xCollection.Value, yCollection.Value, xItemPredicate, yItemPredicate, xyItemsPredicate,
        xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> MatchCoupledElementsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    Predicate<(TX? x, TY? y)> xyItemsPredicate,
    string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchCoupledElementsAsync(xCollection.Value, yCollection.Value, xyItemsPredicate,
        xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> MatchCoupledElementsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    Predicate<TX?> xItemPredicate, Predicate<TY?> yItemPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchCoupledElementsAsync(xCollection.Value, yCollection.Value, xItemPredicate, yItemPredicate, xyItemsPredicate,
        xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> MatchCoupledElementsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    ElementPredicate<(TX? x, TY? y)> xyItemsPredicate,
    string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchCoupledElementsAsync(xCollection.Value, yCollection.Value, xyItemsPredicate,
        xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> MatchCoupledElementsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    ElementPredicate<TX?> xItemPredicate, ElementPredicate<TY?> yItemPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchCoupledElementsAsync(xCollection.Value, yCollection.Value, xItemPredicate, yItemPredicate, xyItemsPredicate,
        xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> MatchCoupled(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Predicate<int> countPredicate, Predicate<(object? x, object? y)> xyItemsPredicate, bool lazy = false,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().MatchCoupled(xCollection.Value, yCollection.Value, countPredicate, xyItemsPredicate, lazy,
        countMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> MatchCoupled(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Predicate<int> countPredicate, Predicate<object?> xItemPredicate, Predicate<object?> yItemPredicate, Predicate<(object? x, object? y)> xyItemsPredicate, bool lazy = false,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().MatchCoupled(xCollection.Value, yCollection.Value, countPredicate, xItemPredicate, yItemPredicate, xyItemsPredicate, lazy,
        countMessage, xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> MatchCoupled(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Predicate<int> countPredicate, ElementPredicate<(object? x, object? y)> xyItemsPredicate, bool lazy = false,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().MatchCoupled(xCollection.Value, yCollection.Value, countPredicate, xyItemsPredicate, lazy,
        countMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> MatchCoupled(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Predicate<int> countPredicate, ElementPredicate<object?> xItemPredicate, ElementPredicate<object?> yItemPredicate, ElementPredicate<(object? x, object? y)> xyItemsPredicate, bool lazy = false,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().MatchCoupled(xCollection.Value, yCollection.Value, countPredicate, xItemPredicate, yItemPredicate, xyItemsPredicate, lazy,
        countMessage, xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TX? x, TY? y)>> MatchCoupled<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Predicate<int> countPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate, bool lazy = false,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().MatchCoupled(xCollection.Value, yCollection.Value, countPredicate, xyItemsPredicate, lazy,
        countMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TX? x, TY? y)>> MatchCoupled<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Predicate<int> countPredicate, Predicate<TX?> xItemPredicate, Predicate<TY?> yItemPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate, bool lazy = false,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().MatchCoupled(xCollection.Value, yCollection.Value, countPredicate, xItemPredicate, yItemPredicate, xyItemsPredicate, lazy,
        countMessage, xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TX? x, TY? y)>> MatchCoupled<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Predicate<int> countPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate, bool lazy = false,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().MatchCoupled(xCollection.Value, yCollection.Value, countPredicate, xyItemsPredicate, lazy,
        countMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TX? x, TY? y)>> MatchCoupled<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Predicate<int> countPredicate, ElementPredicate<TX?> xItemPredicate, ElementPredicate<TY?> yItemPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate, bool lazy = false,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null)
    => new()
    {
      Value = argument.Self().MatchCoupled(xCollection.Value, yCollection.Value, countPredicate, xItemPredicate, yItemPredicate, xyItemsPredicate, lazy,
        countMessage, xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> MatchCoupledAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Predicate<int> countPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchCoupledAsync(xCollection.Value, yCollection.Value, countPredicate, xyItemsPredicate,
        countMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> MatchCoupledAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Predicate<int> countPredicate, Predicate<TX?> xItemPredicate, Predicate<TY?> yItemPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchCoupledAsync(xCollection.Value, yCollection.Value, countPredicate, xItemPredicate, yItemPredicate, xyItemsPredicate,
        countMessage, xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> MatchCoupledAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Predicate<int> countPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchCoupledAsync(xCollection.Value, yCollection.Value, countPredicate, xyItemsPredicate,
        countMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> MatchCoupledAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Predicate<int> countPredicate, ElementPredicate<TX?> xItemPredicate, ElementPredicate<TY?> yItemPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchCoupledAsync(xCollection.Value, yCollection.Value, countPredicate, xItemPredicate, yItemPredicate, xyItemsPredicate,
        countMessage, xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> MatchCoupledAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    Predicate<int> countPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchCoupledAsync(xCollection.Value, yCollection.Value, countPredicate, xyItemsPredicate,
        countMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> MatchCoupledAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    Predicate<int> countPredicate, Predicate<TX?> xItemPredicate, Predicate<TY?> yItemPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchCoupledAsync(xCollection.Value, yCollection.Value, countPredicate, xItemPredicate, yItemPredicate, xyItemsPredicate,
        countMessage, xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> MatchCoupledAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    Predicate<int> countPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchCoupledAsync(xCollection.Value, yCollection.Value, countPredicate, xyItemsPredicate,
        countMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TX? x, TY? y)>> MatchCoupledAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    Predicate<int> countPredicate, ElementPredicate<TX?> xItemPredicate, ElementPredicate<TY?> yItemPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchCoupledAsync(xCollection.Value, yCollection.Value, countPredicate, xItemPredicate, yItemPredicate, xyItemsPredicate,
        countMessage, xItemMessage, yItemMessage, xyItemsMessage, imbalancedMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  #endregion
  #region Equal coupled collections

  public static Argument<IEnumerable<(object? x, object? y)>> EqualCoupled(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().EqualCoupled(xCollection.Value, yCollection.Value, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> EqualCoupled(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Equality<object?>? equality, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().EqualCoupled(xCollection.Value, yCollection.Value, equality, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> EqualCoupled(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    IEqualityComparer? equalityComparer, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().EqualCoupled(xCollection.Value, yCollection.Value, equalityComparer, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(T? x, T? y)>> EqualCoupled<T>(this Argument argument, Argument<IEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().EqualCoupled(xCollection.Value, yCollection.Value, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(T? x, T? y)>> EqualCoupled<T>(this Argument argument, Argument<IEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    Equality<T?>? equality, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().EqualCoupled(xCollection.Value, yCollection.Value, equality, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(T? x, T? y)>> EqualCoupled<T>(this Argument argument, Argument<IEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    IEqualityComparer<T>? equalityComparer, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().EqualCoupled(xCollection.Value, yCollection.Value, equalityComparer, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IAsyncEnumerable<(T? x, T? y)>> EqualCoupledAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().EqualCoupledAsync(xCollection.Value, yCollection.Value, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(T? x, T? y)>> EqualCoupledAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    Equality<T?>? equality, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().EqualCoupledAsync(xCollection.Value, yCollection.Value, equality, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(T? x, T? y)>> EqualCoupledAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    IEqualityComparer<T>? equalityComparer, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().EqualCoupledAsync(xCollection.Value, yCollection.Value, equalityComparer, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(T? x, T? y)>> EqualCoupledAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IAsyncEnumerable<T?>> yCollection,
    string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().EqualCoupledAsync(xCollection.Value, yCollection.Value, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(T? x, T? y)>> EqualCoupledAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IAsyncEnumerable<T?>> yCollection,
    Equality<T?>? equality, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().EqualCoupledAsync(xCollection.Value, yCollection.Value, equality, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(T? x, T? y)>> EqualCoupledAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IAsyncEnumerable<T?>> yCollection,
    IEqualityComparer<T>? equalityComparer, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().EqualCoupledAsync(xCollection.Value, yCollection.Value, equalityComparer, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> NotEqualCoupled(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().NotEqualCoupled(xCollection.Value, yCollection.Value, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> NotEqualCoupled(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Equality<object?>? equality, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().NotEqualCoupled(xCollection.Value, yCollection.Value, equality, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> NotEqualCoupled(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    IEqualityComparer? equalityComparer, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().NotEqualCoupled(xCollection.Value, yCollection.Value, equalityComparer, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(T? x, T? y)>> NotEqualCoupled<T>(this Argument argument, Argument<IEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().NotEqualCoupled(xCollection.Value, yCollection.Value, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(T? x, T? y)>> NotEqualCoupled<T>(this Argument argument, Argument<IEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    Equality<T?>? equality, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().NotEqualCoupled(xCollection.Value, yCollection.Value, equality, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(T? x, T? y)>> NotEqualCoupled<T>(this Argument argument, Argument<IEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    IEqualityComparer<T>? equalityComparer, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().NotEqualCoupled(xCollection.Value, yCollection.Value, equalityComparer, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IAsyncEnumerable<(T? x, T? y)>> NotEqualCoupledAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotEqualCoupledAsync(xCollection.Value, yCollection.Value, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(T? x, T? y)>> NotEqualCoupledAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    Equality<T?>? equality, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotEqualCoupledAsync(xCollection.Value, yCollection.Value, equality, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(T? x, T? y)>> NotEqualCoupledAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    IEqualityComparer<T>? equalityComparer, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotEqualCoupledAsync(xCollection.Value, yCollection.Value, equalityComparer, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(T? x, T? y)>> NotEqualCoupledAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IAsyncEnumerable<T?>> yCollection,
    string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotEqualCoupledAsync(xCollection.Value, yCollection.Value, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(T? x, T? y)>> NotEqualCoupledAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IAsyncEnumerable<T?>> yCollection,
    Equality<T?>? equality, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotEqualCoupledAsync(xCollection.Value, yCollection.Value, equality, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(T? x, T? y)>> NotEqualCoupledAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IAsyncEnumerable<T?>> yCollection,
    IEqualityComparer<T>? equalityComparer, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotEqualCoupledAsync(xCollection.Value, yCollection.Value, equalityComparer, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  #endregion
  #region Compare coupled collections

  public static Argument<IEnumerable<(object? x, object? y)>> CompareCoupled(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    ComparisonCriteria criteria, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().CompareCoupled(xCollection.Value, yCollection.Value, criteria, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> CompareCoupled(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Comparison<object?>? comparison, ComparisonCriteria criteria, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().CompareCoupled(xCollection.Value, yCollection.Value, comparison, criteria, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> CompareCoupled(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    IComparer? comparer, ComparisonCriteria criteria, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().CompareCoupled(xCollection.Value, yCollection.Value, comparer, criteria, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(T? x, T? y)>> CompareCoupled<T>(this Argument argument, Argument<IEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    ComparisonCriteria criteria, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().CompareCoupled(xCollection.Value, yCollection.Value, criteria, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(T? x, T? y)>> CompareCoupled<T>(this Argument argument, Argument<IEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    Comparison<T?>? comparison, ComparisonCriteria criteria, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().CompareCoupled(xCollection.Value, yCollection.Value, comparison, criteria, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(T? x, T? y)>> CompareCoupled<T>(this Argument argument, Argument<IEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    IComparer<T>? comparer, ComparisonCriteria criteria, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().CompareCoupled(xCollection.Value, yCollection.Value, comparer, criteria, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IAsyncEnumerable<(T? x, T? y)>> CompareCoupledAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    ComparisonCriteria criteria, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().CompareCoupledAsync(xCollection.Value, yCollection.Value, criteria, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(T? x, T? y)>> CompareCoupledAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    Comparison<T?>? comparison, ComparisonCriteria criteria, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().CompareCoupledAsync(xCollection.Value, yCollection.Value, comparison, criteria, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(T? x, T? y)>> CompareCoupledAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    IComparer<T>? comparer, ComparisonCriteria criteria, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().CompareCoupledAsync(xCollection.Value, yCollection.Value, comparer, criteria, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(T? x, T? y)>> CompareCoupledAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IAsyncEnumerable<T?>> yCollection,
    ComparisonCriteria criteria, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().CompareCoupledAsync(xCollection.Value, yCollection.Value, criteria, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(T? x, T? y)>> CompareCoupledAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IAsyncEnumerable<T?>> yCollection,
    Comparison<T?>? comparison, ComparisonCriteria criteria, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().CompareCoupledAsync(xCollection.Value, yCollection.Value, comparison, criteria, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(T? x, T? y)>> CompareCoupledAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IAsyncEnumerable<T?>> yCollection,
    IComparer<T>? comparer, ComparisonCriteria criteria, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().CompareCoupledAsync(xCollection.Value, yCollection.Value, comparer, criteria, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> NotCompareCoupled(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    ComparisonCriteria criteria, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().NotCompareCoupled(xCollection.Value, yCollection.Value, criteria, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> NotCompareCoupled(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Comparison<object?>? comparison, ComparisonCriteria criteria, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().NotCompareCoupled(xCollection.Value, yCollection.Value, comparison, criteria, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(object? x, object? y)>> NotCompareCoupled(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    IComparer? comparer, ComparisonCriteria criteria, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().NotCompareCoupled(xCollection.Value, yCollection.Value, comparer, criteria, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(T? x, T? y)>> NotCompareCoupled<T>(this Argument argument, Argument<IEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    ComparisonCriteria criteria, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().NotCompareCoupled(xCollection.Value, yCollection.Value, criteria, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(T? x, T? y)>> NotCompareCoupled<T>(this Argument argument, Argument<IEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    Comparison<T?>? comparison, ComparisonCriteria criteria, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().NotCompareCoupled(xCollection.Value, yCollection.Value, comparison, criteria, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(T? x, T? y)>> NotCompareCoupled<T>(this Argument argument, Argument<IEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    IComparer<T>? comparer, ComparisonCriteria criteria, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().NotCompareCoupled(xCollection.Value, yCollection.Value, comparer, criteria, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IAsyncEnumerable<(T? x, T? y)>> NotCompareCoupledAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    ComparisonCriteria criteria, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotCompareCoupledAsync(xCollection.Value, yCollection.Value, criteria, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(T? x, T? y)>> NotCompareCoupledAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    Comparison<T?>? comparison, ComparisonCriteria criteria, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotCompareCoupledAsync(xCollection.Value, yCollection.Value, comparison, criteria, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(T? x, T? y)>> NotCompareCoupledAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    IComparer<T>? comparer, ComparisonCriteria criteria, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotCompareCoupledAsync(xCollection.Value, yCollection.Value, comparer, criteria, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(T? x, T? y)>> NotCompareCoupledAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IAsyncEnumerable<T?>> yCollection,
    ComparisonCriteria criteria, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotCompareCoupledAsync(xCollection.Value, yCollection.Value, criteria, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(T? x, T? y)>> NotCompareCoupledAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IAsyncEnumerable<T?>> yCollection,
    Comparison<T?>? comparison, ComparisonCriteria criteria, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotCompareCoupledAsync(xCollection.Value, yCollection.Value, comparison, criteria, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(T? x, T? y)>> NotCompareCoupledAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IAsyncEnumerable<T?>> yCollection,
    IComparer<T>? comparer, ComparisonCriteria criteria, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotCompareCoupledAsync(xCollection.Value, yCollection.Value, comparer, criteria, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  #endregion
  #region Validate paired collections

  public static Argument<IEnumerable<(TryOut<object> x, TryOut<object> y)>> ValidatePairedCounts(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Action<(int xCount, int yCount)>? countsValidator, bool lazy = false,
    string? countsMessage = null)
    => new()
    {
      Value = argument.Self().ValidatePairedCounts(xCollection.Value, yCollection.Value, countsValidator, lazy,
        countsMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<TX> x, TryOut<TY> y)>> ValidatePairedCounts<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Action<(int xCount, int yCount)>? countsValidator, bool lazy = false,
    string? countsMessage = null)
    => new()
    {
      Value = argument.Self().ValidatePairedCounts(xCollection.Value, yCollection.Value, countsValidator, lazy,
        countsMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)>> ValidatePairedCountsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Action<(int xCount, int yCount)>? countsValidator,
    string? countsMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidatePairedCountsAsync(xCollection.Value, yCollection.Value, countsValidator,
        countsMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)>> ValidatePairedCountsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    Action<(int xCount, int yCount)>? countsValidator,
    string? countsMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidatePairedCountsAsync(xCollection.Value, yCollection.Value, countsValidator,
        countsMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IEnumerable<(TryOut<object> x, TryOut<object> y)>> ValidatePairedElements(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Action<object?> xItemValidator, Action<object?> yItemValidator, Action<(object? x, object? y)> xyItemsValidator, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null)
    => new()
    {
      Value = argument.Self().ValidatePairedElements(xCollection.Value, yCollection.Value, xItemValidator, yItemValidator, xyItemsValidator, lazy,
        xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<object> x, TryOut<object> y)>> ValidatePairedElements(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    ElementAction<object?> xItemValidator, ElementAction<object?> yItemValidator, ElementAction<(object? x, object? y)> xyItemsValidator, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null)
    => new()
    {
      Value = argument.Self().ValidatePairedElements(xCollection.Value, yCollection.Value, xItemValidator, yItemValidator, xyItemsValidator, lazy,
        xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<TX> x, TryOut<TY> y)>> ValidatePairedElements<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Action<TX?> xItemValidator, Action<TY?> yItemValidator, Action<(TX? x, TY? y)> xyItemsValidator, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null)
    => new()
    {
      Value = argument.Self().ValidatePairedElements(xCollection.Value, yCollection.Value, xItemValidator, yItemValidator, xyItemsValidator, lazy,
        xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<TX> x, TryOut<TY> y)>> ValidatePairedElements<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    ElementAction<TX?> xItemValidator, ElementAction<TY?> yItemValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null)
    => new()
    {
      Value = argument.Self().ValidatePairedElements(xCollection.Value, yCollection.Value, xItemValidator, yItemValidator, xyItemsValidator, lazy,
        xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)>> ValidatePairedElementsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Action<TX?> xItemValidator, Action<TY?> yItemValidator, Action<(TX? x, TY? y)> xyItemsValidator,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidatePairedElementsAsync(xCollection.Value, yCollection.Value, xItemValidator, yItemValidator, xyItemsValidator,
        xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)>> ValidatePairedElementsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    ElementAction<TX?> xItemValidator, ElementAction<TY?> yItemValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidatePairedElementsAsync(xCollection.Value, yCollection.Value, xItemValidator, yItemValidator, xyItemsValidator,
        xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)>> ValidatePairedElementsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    Action<TX?> xItemValidator, Action<TY?> yItemValidator, Action<(TX? x, TY? y)> xyItemsValidator,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidatePairedElementsAsync(xCollection.Value, yCollection.Value, xItemValidator, yItemValidator, xyItemsValidator,
        xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)>> ValidatePairedElementsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    ElementAction<TX?> xItemValidator, ElementAction<TY?> yItemValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidatePairedElementsAsync(xCollection.Value, yCollection.Value, xItemValidator, yItemValidator, xyItemsValidator,
        xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IEnumerable<(TryOut<object> x, TryOut<object> y)>> ValidatePaired(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Action<(int xCount, int yCount)> countsValidator, Action<object?> xItemValidator, Action<object?> yItemValidator, Action<(object? x, object? y)> xyItemsValidator, bool lazy = false,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null)
    => new()
    {
      Value = argument.Self().ValidatePaired(xCollection.Value, yCollection.Value, countsValidator, xItemValidator, yItemValidator, xyItemsValidator, lazy,
        countsMessage, xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<object> x, TryOut<object> y)>> ValidatePaired(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Action<(int xCount, int yCount)> countsValidator, ElementAction<object?> xItemValidator, ElementAction<object?> yItemValidator, ElementAction<(object? x, object? y)> xyItemsValidator, bool lazy = false,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null)
    => new()
    {
      Value = argument.Self().ValidatePaired(xCollection.Value, yCollection.Value, countsValidator, xItemValidator, yItemValidator, xyItemsValidator, lazy,
        countsMessage, xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<TX> x, TryOut<TY> y)>> ValidatePaired<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Action<(int xCount, int yCount)> countsValidator, Action<TX?> xItemValidator, Action<TY?> yItemValidator, Action<(TX? x, TY? y)> xyItemsValidator, bool lazy = false,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null)
    => new()
    {
      Value = argument.Self().ValidatePaired(xCollection.Value, yCollection.Value, countsValidator, xItemValidator, yItemValidator, xyItemsValidator, lazy,
        countsMessage, xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<TX> x, TryOut<TY> y)>> ValidatePaired<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Action<(int xCount, int yCount)> countsValidator, ElementAction<TX?> xItemValidator, ElementAction<TY?> yItemValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator, bool lazy = false,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null)
    => new()
    {
      Value = argument.Self().ValidatePaired(xCollection.Value, yCollection.Value, countsValidator, xItemValidator, yItemValidator, xyItemsValidator, lazy,
        countsMessage, xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)>> ValidatePairedAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Action<(int xCount, int yCount)> countsValidator, Action<TX?> xItemValidator, Action<TY?> yItemValidator, Action<(TX? x, TY? y)> xyItemsValidator,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidatePairedAsync(xCollection.Value, yCollection.Value, countsValidator, xItemValidator, yItemValidator, xyItemsValidator,
        countsMessage, xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)>> ValidatePairedAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Action<(int xCount, int yCount)> countsValidator, ElementAction<TX?> xItemValidator, ElementAction<TY?> yItemValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidatePairedAsync(xCollection.Value, yCollection.Value, countsValidator, xItemValidator, yItemValidator, xyItemsValidator,
        countsMessage, xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)>> ValidatePairedAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    Action<(int xCount, int yCount)> countsValidator, Action<TX?> xItemValidator, Action<TY?> yItemValidator, Action<(TX? x, TY? y)> xyItemsValidator,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidatePairedAsync(xCollection.Value, yCollection.Value, countsValidator, xItemValidator, yItemValidator, xyItemsValidator,
        countsMessage, xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)>> ValidatePairedAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    Action<(int xCount, int yCount)> countsValidator, ElementAction<TX?> xItemValidator, ElementAction<TY?> yItemValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ValidatePairedAsync(xCollection.Value, yCollection.Value, countsValidator, xItemValidator, yItemValidator, xyItemsValidator,
        countsMessage, xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  #endregion
  #region Match paired collections

  public static Argument<IEnumerable<(TryOut<object> x, TryOut<object> y)>> MatchPairedCounts(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Predicate<(int xCount, int yCount)> countsPredicate, bool lazy = false,
    string? countsMessage = null)
    => new()
    {
      Value = argument.Self().MatchPairedCounts(xCollection.Value, yCollection.Value, countsPredicate, lazy,
        countsMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<TX> x, TryOut<TY> y)>> MatchPairedCounts<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Predicate<(int xCount, int yCount)> countsPredicate, bool lazy = false,
    string? countsMessage = null)
    => new()
    {
      Value = argument.Self().MatchPairedCounts(xCollection.Value, yCollection.Value, countsPredicate, lazy,
        countsMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)>> MatchPairedCountsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Predicate<(int xCount, int yCount)> countsPredicate,
    string? countsMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchPairedCountsAsync(xCollection.Value, yCollection.Value, countsPredicate,
        countsMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)>> MatchPairedCountsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    Predicate<(int xCount, int yCount)> countsPredicate,
    string? countsMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchPairedCountsAsync(xCollection.Value, yCollection.Value, countsPredicate,
        countsMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IEnumerable<(TryOut<object> x, TryOut<object> y)>> MatchPairedElements(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Predicate<object?> xItemPredicate, Predicate<object?> yItemPredicate, Predicate<(object? x, object? y)> xyItemsPredicate, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null)
    => new()
    {
      Value = argument.Self().MatchPairedElements(xCollection.Value, yCollection.Value, xItemPredicate, yItemPredicate, xyItemsPredicate, lazy,
        xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<object> x, TryOut<object> y)>> MatchPairedElements(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    ElementPredicate<object?> xItemPredicate, ElementPredicate<object?> yItemPredicate, ElementPredicate<(object? x, object? y)> xyItemsPredicate, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null)
    => new()
    {
      Value = argument.Self().MatchPairedElements(xCollection.Value, yCollection.Value, xItemPredicate, yItemPredicate, xyItemsPredicate, lazy,
        xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<TX> x, TryOut<TY> y)>> MatchPairedElements<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Predicate<TX?> xItemPredicate, Predicate<TY?> yItemPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null)
    => new()
    {
      Value = argument.Self().MatchPairedElements(xCollection.Value, yCollection.Value, xItemPredicate, yItemPredicate, xyItemsPredicate, lazy,
        xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<TX> x, TryOut<TY> y)>> MatchPairedElements<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    ElementPredicate<TX?> xItemPredicate, ElementPredicate<TY?> yItemPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null)
    => new()
    {
      Value = argument.Self().MatchPairedElements(xCollection.Value, yCollection.Value, xItemPredicate, yItemPredicate, xyItemsPredicate, lazy,
        xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)>> MatchPairedElementsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Predicate<TX?> xItemPredicate, Predicate<TY?> yItemPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchPairedElementsAsync(xCollection.Value, yCollection.Value, xItemPredicate, yItemPredicate, xyItemsPredicate,
        xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)>> MatchPairedElementsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    ElementPredicate<TX?> xItemPredicate, ElementPredicate<TY?> yItemPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchPairedElementsAsync(xCollection.Value, yCollection.Value, xItemPredicate, yItemPredicate, xyItemsPredicate,
        xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)>> MatchPairedElementsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    Predicate<TX?> xItemPredicate, Predicate<TY?> yItemPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchPairedElementsAsync(xCollection.Value, yCollection.Value, xItemPredicate, yItemPredicate, xyItemsPredicate,
        xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)>> MatchPairedElementsAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    ElementPredicate<TX?> xItemPredicate, ElementPredicate<TY?> yItemPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchPairedElementsAsync(xCollection.Value, yCollection.Value, xItemPredicate, yItemPredicate, xyItemsPredicate,
        xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IEnumerable<(TryOut<object> x, TryOut<object> y)>> MatchPaired(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Predicate<(int xCount, int yCount)> countsPredicate, Predicate<object?> xItemPredicate, Predicate<object?> yItemPredicate, Predicate<(object? x, object? y)> xyItemsPredicate, bool lazy = false,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null)
    => new()
    {
      Value = argument.Self().MatchPaired(xCollection.Value, yCollection.Value, countsPredicate, xItemPredicate, yItemPredicate, xyItemsPredicate, lazy,
        countsMessage, xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<object> x, TryOut<object> y)>> MatchPaired(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Predicate<(int xCount, int yCount)> countsPredicate, ElementPredicate<object?> xItemPredicate, ElementPredicate<object?> yItemPredicate, ElementPredicate<(object? x, object? y)> xyItemsPredicate, bool lazy = false,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null)
    => new()
    {
      Value = argument.Self().MatchPaired(xCollection.Value, yCollection.Value, countsPredicate, xItemPredicate, yItemPredicate, xyItemsPredicate, lazy,
        countsMessage, xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<TX> x, TryOut<TY> y)>> MatchPaired<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Predicate<(int xCount, int yCount)> countsPredicate, Predicate<TX?> xItemPredicate, Predicate<TY?> yItemPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate, bool lazy = false,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null)
    => new()
    {
      Value = argument.Self().MatchPaired(xCollection.Value, yCollection.Value, countsPredicate, xItemPredicate, yItemPredicate, xyItemsPredicate, lazy,
        countsMessage, xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<TX> x, TryOut<TY> y)>> MatchPaired<TX, TY>(this Argument argument, Argument<IEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Predicate<(int xCount, int yCount)> countsPredicate, ElementPredicate<TX?> xItemPredicate, ElementPredicate<TY?> yItemPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate, bool lazy = false,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null)
    => new()
    {
      Value = argument.Self().MatchPaired(xCollection.Value, yCollection.Value, countsPredicate, xItemPredicate, yItemPredicate, xyItemsPredicate, lazy,
        countsMessage, xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name)
    };

  public static Argument<IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)>> MatchPairedAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Predicate<(int xCount, int yCount)> countsPredicate, Predicate<TX?> xItemPredicate, Predicate<TY?> yItemPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchPairedAsync(xCollection.Value, yCollection.Value, countsPredicate, xItemPredicate, yItemPredicate, xyItemsPredicate,
        countsMessage, xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)>> MatchPairedAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IEnumerable<TY?>> yCollection,
    Predicate<(int xCount, int yCount)> countsPredicate, ElementPredicate<TX?> xItemPredicate, ElementPredicate<TY?> yItemPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchPairedAsync(xCollection.Value, yCollection.Value, countsPredicate, xItemPredicate, yItemPredicate, xyItemsPredicate,
        countsMessage, xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)>> MatchPairedAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    Predicate<(int xCount, int yCount)> countsPredicate, Predicate<TX?> xItemPredicate, Predicate<TY?> yItemPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchPairedAsync(xCollection.Value, yCollection.Value, countsPredicate, xItemPredicate, yItemPredicate, xyItemsPredicate,
        countsMessage, xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)>> MatchPairedAsync<TX, TY>(this Argument argument, Argument<IAsyncEnumerable<TX?>> xCollection, Argument<IAsyncEnumerable<TY?>> yCollection,
    Predicate<(int xCount, int yCount)> countsPredicate, ElementPredicate<TX?> xItemPredicate, ElementPredicate<TY?> yItemPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().MatchPairedAsync(xCollection.Value, yCollection.Value, countsPredicate, xItemPredicate, yItemPredicate, xyItemsPredicate,
        countsMessage, xItemMessage, yItemMessage, xyItemsMessage, xCollection.Name, yCollection.Name, cancellationToken)
    };

  #endregion
  #region Equal paired collections

  public static Argument<IEnumerable<(TryOut<object> x, TryOut<object> y)>> EqualPaired(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().EqualPaired(xCollection.Value, yCollection.Value, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<object> x, TryOut<object> y)>> EqualPaired(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Equality<object?>? equality, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().EqualPaired(xCollection.Value, yCollection.Value, equality, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<object> x, TryOut<object> y)>> EqualPaired(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    IEqualityComparer? equalityComparer, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().EqualPaired(xCollection.Value, yCollection.Value, equalityComparer, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<T> x, TryOut<T> y)>> EqualPaired<T>(this Argument argument, Argument<IEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().EqualPaired(xCollection.Value, yCollection.Value, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<T> x, TryOut<T> y)>> EqualPaired<T>(this Argument argument, Argument<IEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    Equality<T?>? equality, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().EqualPaired(xCollection.Value, yCollection.Value, equality, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<T> x, TryOut<T> y)>> EqualPaired<T>(this Argument argument, Argument<IEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    IEqualityComparer<T>? equalityComparer, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().EqualPaired(xCollection.Value, yCollection.Value, equalityComparer, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)>> EqualPairedAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().EqualPairedAsync(xCollection.Value, yCollection.Value, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)>> EqualPairedAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    Equality<T?>? equality, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().EqualPairedAsync(xCollection.Value, yCollection.Value, equality, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)>> EqualPairedAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    IEqualityComparer<T>? equalityComparer, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().EqualPairedAsync(xCollection.Value, yCollection.Value, equalityComparer, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)>> EqualPairedAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IAsyncEnumerable<T?>> yCollection,
    string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().EqualPairedAsync(xCollection.Value, yCollection.Value, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)>> EqualPairedAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IAsyncEnumerable<T?>> yCollection,
    Equality<T?>? equality, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().EqualPairedAsync(xCollection.Value, yCollection.Value, equality, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)>> EqualPairedAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IAsyncEnumerable<T?>> yCollection,
    IEqualityComparer<T>? equalityComparer, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().EqualPairedAsync(xCollection.Value, yCollection.Value, equalityComparer, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IEnumerable<(TryOut<object> x, TryOut<object> y)>> NotEqualPaired(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().NotEqualPaired(xCollection.Value, yCollection.Value, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<object> x, TryOut<object> y)>> NotEqualPaired(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Equality<object?>? equality, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().NotEqualPaired(xCollection.Value, yCollection.Value, equality, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<object> x, TryOut<object> y)>> NotEqualPaired(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    IEqualityComparer? equalityComparer, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().NotEqualPaired(xCollection.Value, yCollection.Value, equalityComparer, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<T> x, TryOut<T> y)>> NotEqualPaired<T>(this Argument argument, Argument<IEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().NotEqualPaired(xCollection.Value, yCollection.Value, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<T> x, TryOut<T> y)>> NotEqualPaired<T>(this Argument argument, Argument<IEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    Equality<T?>? equality, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().NotEqualPaired(xCollection.Value, yCollection.Value, equality, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<T> x, TryOut<T> y)>> NotEqualPaired<T>(this Argument argument, Argument<IEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    IEqualityComparer<T>? equalityComparer, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().NotEqualPaired(xCollection.Value, yCollection.Value, equalityComparer, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)>> NotEqualPairedAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotEqualPairedAsync(xCollection.Value, yCollection.Value, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)>> NotEqualPairedAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    Equality<T?>? equality, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotEqualPairedAsync(xCollection.Value, yCollection.Value, equality, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)>> NotEqualPairedAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    IEqualityComparer<T>? equalityComparer, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotEqualPairedAsync(xCollection.Value, yCollection.Value, equalityComparer, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)>> NotEqualPairedAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IAsyncEnumerable<T?>> yCollection,
    string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotEqualPairedAsync(xCollection.Value, yCollection.Value, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)>> NotEqualPairedAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IAsyncEnumerable<T?>> yCollection,
    Equality<T?>? equality, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotEqualPairedAsync(xCollection.Value, yCollection.Value, equality, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)>> NotEqualPairedAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IAsyncEnumerable<T?>> yCollection,
    IEqualityComparer<T>? equalityComparer, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotEqualPairedAsync(xCollection.Value, yCollection.Value, equalityComparer, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  #endregion
  #region Compare paired collections

  public static Argument<IEnumerable<(TryOut<object> x, TryOut<object> y)>> ComparePaired(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().ComparePaired(xCollection.Value, yCollection.Value, criteria, emptyOrder, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<object> x, TryOut<object> y)>> ComparePaired(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Comparison<object?>? comparison, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().ComparePaired(xCollection.Value, yCollection.Value, comparison, criteria, emptyOrder, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<object> x, TryOut<object> y)>> ComparePaired(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    IComparer? comparer, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().ComparePaired(xCollection.Value, yCollection.Value, comparer, criteria, emptyOrder, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<T> x, TryOut<T> y)>> ComparePaired<T>(this Argument argument, Argument<IEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().ComparePaired(xCollection.Value, yCollection.Value, criteria, emptyOrder, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<T> x, TryOut<T> y)>> ComparePaired<T>(this Argument argument, Argument<IEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    Comparison<T?>? comparison, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().ComparePaired(xCollection.Value, yCollection.Value, comparison, criteria, emptyOrder, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<T> x, TryOut<T> y)>> ComparePaired<T>(this Argument argument, Argument<IEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    IComparer<T>? comparer, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().ComparePaired(xCollection.Value, yCollection.Value, comparer, criteria, emptyOrder, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)>> ComparePairedAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ComparePairedAsync(xCollection.Value, yCollection.Value, criteria, emptyOrder, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)>> ComparePairedAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    Comparison<T?>? comparison, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ComparePairedAsync(xCollection.Value, yCollection.Value, comparison, criteria, emptyOrder, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)>> ComparePairedAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    IComparer<T>? comparer, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ComparePairedAsync(xCollection.Value, yCollection.Value, comparer, criteria, emptyOrder, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)>> ComparePairedAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IAsyncEnumerable<T?>> yCollection,
    ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ComparePairedAsync(xCollection.Value, yCollection.Value, criteria, emptyOrder, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)>> ComparePairedAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IAsyncEnumerable<T?>> yCollection,
    Comparison<T?>? comparison, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ComparePairedAsync(xCollection.Value, yCollection.Value, comparison, criteria, emptyOrder, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)>> ComparePairedAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IAsyncEnumerable<T?>> yCollection,
    IComparer<T>? comparer, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().ComparePairedAsync(xCollection.Value, yCollection.Value, comparer, criteria, emptyOrder, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IEnumerable<(TryOut<object> x, TryOut<object> y)>> NotComparePaired(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().NotComparePaired(xCollection.Value, yCollection.Value, criteria, emptyOrder, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<object> x, TryOut<object> y)>> NotComparePaired(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    Comparison<object?>? comparison, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().NotComparePaired(xCollection.Value, yCollection.Value, comparison, criteria, emptyOrder, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<object> x, TryOut<object> y)>> NotComparePaired(this Argument argument, Argument<IEnumerable> xCollection, Argument<IEnumerable> yCollection,
    IComparer? comparer, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().NotComparePaired(xCollection.Value, yCollection.Value, comparer, criteria, emptyOrder, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<T> x, TryOut<T> y)>> NotComparePaired<T>(this Argument argument, Argument<IEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().NotComparePaired(xCollection.Value, yCollection.Value, criteria, emptyOrder, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<T> x, TryOut<T> y)>> NotComparePaired<T>(this Argument argument, Argument<IEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    Comparison<T?>? comparison, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().NotComparePaired(xCollection.Value, yCollection.Value, comparison, criteria, emptyOrder, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IEnumerable<(TryOut<T> x, TryOut<T> y)>> NotComparePaired<T>(this Argument argument, Argument<IEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    IComparer<T>? comparer, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, bool lazy = false, string? message = null)
    => new()
    {
      Value = argument.Self().NotComparePaired(xCollection.Value, yCollection.Value, comparer, criteria, emptyOrder, lazy, message, xCollection.Name, yCollection.Name)
    };

  public static Argument<IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)>> NotComparePairedAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotComparePairedAsync(xCollection.Value, yCollection.Value, criteria, emptyOrder, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)>> NotComparePairedAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    Comparison<T?>? comparison, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotComparePairedAsync(xCollection.Value, yCollection.Value, comparison, criteria, emptyOrder, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)>> NotComparePairedAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IEnumerable<T?>> yCollection,
    IComparer<T>? comparer, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotComparePairedAsync(xCollection.Value, yCollection.Value, comparer, criteria, emptyOrder, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)>> NotComparePairedAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IAsyncEnumerable<T?>> yCollection,
    ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotComparePairedAsync(xCollection.Value, yCollection.Value, criteria, emptyOrder, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)>> NotComparePairedAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IAsyncEnumerable<T?>> yCollection,
    Comparison<T?>? comparison, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotComparePairedAsync(xCollection.Value, yCollection.Value, comparison, criteria, emptyOrder, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  public static Argument<IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)>> NotComparePairedAsync<T>(this Argument argument, Argument<IAsyncEnumerable<T?>> xCollection, Argument<IAsyncEnumerable<T?>> yCollection,
    IComparer<T>? comparer, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, string? message = null,
    CancellationToken cancellationToken = default)
    => new()
    {
      Value = argument.Self().NotComparePairedAsync(xCollection.Value, yCollection.Value, comparer, criteria, emptyOrder, message, xCollection.Name, yCollection.Name, cancellationToken)
    };

  #endregion
  #endregion
}

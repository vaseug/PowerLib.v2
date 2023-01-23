using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using PowerLib.System.Accessors;
using PowerLib.System.Arrays;
using PowerLib.System.Arrays.Extensions;
using PowerLib.System.Collections;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Collections.NonGeneric.Extensions;
using PowerLib.System.Globalization;
using PowerLib.System.Linq;
using PowerLib.System.Resources;

namespace PowerLib.System.Validation;

public sealed partial class Argument
{
  #region Internal types

  [Flags]
  private enum NavigateCommand
  {
    None = 0,
    Skip = 1,
    Stop = 2,
  }

  #endregion
  #region Internal fields

  private readonly IValueProvider<CultureInfo> _cultureProvider;

  private readonly EnumResourceAccessor<ArgumentMessage>? _resourceAccessor;

  private static readonly EnumResourceAccessor<ArgumentMessage> defaultResourceAccessor = new();

  private static readonly Lazy<Argument> that = new(() => new(CultureProvider.CurrentCulture, typeof(ArgumentMessage)));

  private static readonly Lazy<Argument> thatUI = new(() => new(CultureProvider.CurrentUICulture, typeof(ArgumentMessage)));

  #endregion
  #region Constructors

  private Argument(IValueProvider<CultureInfo> cultureProvider, Type? resourceSource)
  {
    _cultureProvider = cultureProvider ?? throw new ArgumentNullException(nameof(cultureProvider));
    _resourceAccessor = resourceSource is not null && resourceSource != typeof(ArgumentMessage) ? new EnumResourceAccessor<ArgumentMessage>(resourceSource, null) : null;
  }

  #endregion
  #region Internal methods

  private string FormatString(ArgumentMessage argumentMessage, params object?[] args)
    => (_resourceAccessor ?? defaultResourceAccessor).FormatString(Culture, argumentMessage, args);

  private string FormatList<T>(params T[] arguments)
  {
    var cultureInfo = Culture;
    var textInfo = cultureInfo.TextInfo;
    return string.Join(textInfo.ListSeparator, arguments
      .Select(arg => arg is IFormattable formattable ? formattable.ToString(null, cultureInfo) : arg?.ToString())
      .Where(arg => arg is not null));
  }

  #endregion
  #region Public properties

  public CultureInfo Culture
    => _cultureProvider.Value;

  public static Argument That => that.Value;

  public static Argument ThatUI => thatUI.Value;

  #endregion
  #region Public methods

  public static Argument Create(IValueProvider<CultureInfo> cultureProvider, Type? resourceSource = null)
    => new(cultureProvider, resourceSource);

  #endregion
  #region Null validation

  [return: NotNull]
  public T NotNull<T>([NotNull] T? value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => value is not null ? value :
      throw new ArgumentNullException(valueParameter, message ?? FormatString(ArgumentMessage.IsNull, valueParameter));

  [return: NotNull]
  public T NotNull<T>([NotNull] T? value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    where T : struct
    => value is not null ? value.Value :
      throw new ArgumentNullException(valueParameter, message ?? FormatString(ArgumentMessage.IsNull, valueParameter));

  #endregion
  #region Outer validation

  public TSource IsValid<TSource>(TSource value, Predicate<TSource> predicate,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => NotNull(predicate)(value) ? value :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.IsInvalid, valueParameter), valueParameter);

  public TSource IsValid<TSource>(TSource value, [DoesNotReturnIf(false)] bool valid,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => valid ? value :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.IsInvalid, valueParameter), valueParameter);

  [DoesNotReturn]
  [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used indirectly")]
  public dynamic Invalid<TSource>(TSource value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => throw new ArgumentException(message ?? FormatString(ArgumentMessage.IsInvalid, valueParameter), valueParameter);

  [DoesNotReturn]
  [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used indirectly")]
  public TResult? Invalid<TSource, TResult>(TSource value, TResult? result,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => throw new ArgumentException(message ?? FormatString(ArgumentMessage.IsInvalid, valueParameter), valueParameter);

  public TSource InRange<TSource>(TSource value, Predicate<TSource> predicate,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => NotNull(predicate)(value) ? value :
      throw new ArgumentOutOfRangeException(valueParameter, message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter));

  public TSource InRange<TSource>(TSource value, [DoesNotReturnIf(false)] bool valid,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => valid ? value :
      throw new ArgumentOutOfRangeException(valueParameter, message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter));

  [DoesNotReturn]
  [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used indirectly")]
  public TSource OutOfRange<TSource>(TSource value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => throw new ArgumentOutOfRangeException(valueParameter, message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter));

  public void AreConsistent([DoesNotReturnIf(false)] bool condition, string? message, params string?[] parameters)
  {
    NotNull(parameters);

    if (condition)
      return;
    var parametersName = FormatList(parameters);
    throw new ArgumentException(message ?? FormatString(ArgumentMessage.AreInconsistent, parametersName), parametersName);
  }

  public TResult? AreConsistent<TResult>([DoesNotReturnIf(false)] bool condition, TResult? result, string? message = null, params string?[] parameters)
  {
    NotNull(parameters);

    if (condition)
      return result;
    var parametersName = FormatList(parameters);
    throw new ArgumentException(message ?? FormatString(ArgumentMessage.AreInconsistent, parametersName), parametersName);
  }

  public void AreValid<TException>([DoesNotReturnIf(false)] bool condition, Func<string?, string?, TException> exceptionFactory,
    string? message, params string?[] parameters)
    where TException : ArgumentException
  {
    NotNull(exceptionFactory);
    NotNull(parameters);

    if (condition)
      return;
    var parametersName = FormatList(parameters);
    var exception = exceptionFactory(message ?? FormatString(ArgumentMessage.AreInvalid, parametersName), parametersName);
    Operation.That.IsValid(exception is not null);
    throw exception;
  }

  public TResult? AreValid<TException, TResult>([DoesNotReturnIf(false)] bool condition, Func<string?, string?, TException> exceptionFactory, TResult? result,
    string? message = null, params string?[] parameters)
    where TException : ArgumentException
  {
    NotNull(exceptionFactory);
    NotNull(parameters);

    if (condition)
      return result;
    var parametersName = FormatList(parameters);
    var exception = exceptionFactory(message ?? FormatString(ArgumentMessage.AreInvalid, parametersName), parametersName);
    Operation.That.IsValid(exception is not null);
    throw exception;
  }

  internal void AreValid<TException>([DoesNotReturnIf(false)] bool condition, Func<string?, string?, TException> exceptionFactory,
    string? message, ArgumentMessage argumentMessage, params string?[] parameters)
    where TException : ArgumentException
  {
    NotNull(exceptionFactory);
    NotNull(parameters);

    if (condition)
      return;
    var parametersName = FormatList(parameters);
    var exception = exceptionFactory(message ?? FormatString(argumentMessage, parametersName), parametersName);
    Operation.That.IsValid(exception is not null);
    throw exception;
  }

  internal TResult? AreValid<TException, TResult>([DoesNotReturnIf(false)] bool condition, Func<string?, string?, TException> exceptionFactory, TResult? result,
    string? message, ArgumentMessage argumentMessage, params string?[] parameters)
    where TException : ArgumentException
  {
    NotNull(exceptionFactory);
    NotNull(parameters);

    if (condition)
      return result;
    var parametersName = FormatList(parameters);
    var exception = exceptionFactory(message ?? FormatString(argumentMessage, parametersName), parametersName);
    Operation.That.IsValid(exception is not null);
    throw exception;
  }

  #endregion
  #region Type validation

  public T InstanceOf<T>(T value, TypeCode typeCode,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
  {
    NotNull(value, valueParameter: valueParameter);

    return Type.GetTypeCode(value.GetType()) == typeCode ? value :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.IsNotInstanceOfTypeCode, valueParameter, typeCode), valueParameter);
  }

  public object InstanceOf(object value, Type type,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
  {
    NotNull(value, valueParameter: valueParameter);
    NotNull(type);

    return type.IsInstanceOfType(value) ? value :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.IsNotInstanceOfType, valueParameter, type.FullName), valueParameter);
  }

  public T InstanceOf<T>(object value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
  {
    NotNull(value, valueParameter: valueParameter);

    return value is T result ? result :
        throw new ArgumentException(message ?? FormatString(ArgumentMessage.IsNotInstanceOfType, valueParameter, typeof(T).FullName), valueParameter);
  }

  public T NotInstanceOf<T>(T value, TypeCode typeCode,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
  {
    NotNull(value, valueParameter: valueParameter);

    return Type.GetTypeCode(value.GetType()) != typeCode ? value :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.IsInstanceOfTypeCode, valueParameter, typeCode), valueParameter);
  }

  public object NotInstanceOf(object value, Type type,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
  {
    NotNull(value, valueParameter: valueParameter);
    NotNull(type);

    return !type.IsInstanceOfType(value) ? value :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.IsInstanceOfType, valueParameter, type.FullName), valueParameter);
  }

  public object NotInstanceOf<T>(object value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
  {
    NotNull(value, valueParameter: valueParameter);

    return value is not T ? value :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.IsInstanceOfType, valueParameter, typeof(T).FullName), valueParameter);
  }

  public object MadeOf(object value, Type type,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
  {
    NotNull(value, valueParameter: valueParameter);
    NotNull(type);
    IsValid(type, type.IsGenericTypeDefinition);

    return value.GetType().GetGenericTypeDefinition() == type ? value :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.IsNotConstructedFrom, valueParameter, type.FullName), valueParameter);
  }

  public object NotMadeOf(object value, Type type,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
  {
    NotNull(value, valueParameter: valueParameter);
    NotNull(type);
    IsValid(type, type.IsGenericTypeDefinition);

    return value.GetType().GetGenericTypeDefinition() != type ? value :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.IsConstructedFrom, valueParameter, type.FullName), valueParameter);
  }

  public object? OfType(object? value, Type type,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
  {
    NotNull(type);

    return value is null ? type.IsNullAssignable() ? value :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.TypeDoesNotAllowNull, valueParameter, type.FullName), valueParameter) :
      type.IsInstanceOfType(value) ? value :
        throw new ArgumentException(message ?? FormatString(ArgumentMessage.IsNotInstanceOfType, valueParameter, type.FullName), valueParameter);
  }

  public T? OfType<T>(object? value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
  {
    var type = typeof(T);

    return value is null ? type.IsNullAssignable() ? default(T?) :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.TypeDoesNotAllowNull, valueParameter, type.FullName), valueParameter) :
      value is T result ? result :
        throw new ArgumentException(message ?? FormatString(ArgumentMessage.IsNotInstanceOfType, valueParameter, type.FullName), valueParameter);
  }

  public Array ElementOf(Array array, TypeCode elementTypeCode,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "value")
    => Type.GetTypeCode(NotNull(array, valueParameter: arrayParameter).GetType().GetElementType()) == elementTypeCode ? array :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.HasNotElementOfTypeCode, arrayParameter, elementTypeCode), arrayParameter);

  public Array ElementOf(Array array, Type elementType,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "value")
    => NotNull(elementType).Equals(NotNull(array, valueParameter: arrayParameter).GetType().GetElementType()) ? array :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.HasNotElementOfType, arrayParameter, elementType.FullName), arrayParameter);

  public Array ElementOf<T>(Array array,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "value")
    => typeof(T).Equals(NotNull(array, valueParameter: arrayParameter).GetType().GetElementType()) ? array :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.HasNotElementOfType, arrayParameter, typeof(T).FullName), arrayParameter);

  #endregion
  #region Boolean validation

  public bool False(bool value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => !value ? value :
      throw new ArgumentOutOfRangeException(valueParameter, message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter));

  public bool True(bool value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => value ? value :
      throw new ArgumentOutOfRangeException(valueParameter, message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter));

  #endregion
  #region Enum validation

  public object Enum(object value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => NotNull(value).GetType().IsEnum ? value :
    throw new ArgumentException(message ?? FormatString(ArgumentMessage.IsNotEnumType, valueParameter), valueParameter);

  public T EnumFlags<T>(T value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    where T : struct, Enum
    => typeof(T).IsDefined(typeof(FlagsAttribute), false) ? value :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.EnumTypeIsNotFlags, valueParameter, typeof(T).FullName), valueParameter);

  public T NotEmptyFlags<T>(T value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    where T : struct, Enum
    => EnumFlags(value, message).Equals(default(T)) ? value :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.EnumFlagsAreEmpty, valueParameter, typeof(T).FullName), valueParameter);

  public T EmptyFlags<T>(T value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    where T : struct, Enum
    => !EnumFlags(value, message).Equals(default(T)) ? value :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.EnumFlagsAreNotEmpty, valueParameter, typeof(T).FullName), valueParameter);

  public T MatchFlags<T>(T value, T other, FlagsMatchResult matchResult,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    where T : struct, Enum
    => PwrEnum.MatchFlags(value, other) == matchResult ? value :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.EnumFlagsAreNotMatched, valueParameter, otherParameter, matchResult), valueParameter);

  #endregion
  #region String validation
  #region Internal methods

  private static string ContainsCore(string value, string substring, CompareInfo compareInfo, CompareOptions compareOptions, bool negate,
    string? message, string? valueParameter)
    => compareInfo.IndexOf(value, substring, compareOptions) >= 0 ^ negate ? value : throw new ArgumentException(message, valueParameter);

  private static string StartsWithCore(string value, string prefix, CompareInfo compareInfo, CompareOptions compareOptions, bool negate,
    string? message, string? valueParameter)
    => compareInfo.IsPrefix(value, prefix, compareOptions) ^ negate ? value : throw new ArgumentException(message, valueParameter);

  private static string EndsWithCore(string value, string prefix, CompareInfo compareInfo, CompareOptions compareOptions, bool negate,
    string? message, string? valueParameter)
    => compareInfo.IsSuffix(value, prefix, compareOptions) ^ negate ? value : throw new ArgumentException(message, valueParameter);

  private static string MatchCore(string value, string pattern, RegexOptions options, bool negate,
    string? message, string? valueParameter)
    => Regex.IsMatch(value, pattern, options) ^ negate ? value : throw new ArgumentException(message, valueParameter);

  private static string MatchCore(string value, Regex regex, bool negate,
    string? message, string? valueParameter)
    => regex.IsMatch(value) ^ negate ? value : throw new ArgumentException(message, valueParameter);

#if NETCOREAPP2_1_OR_GREATER

  private static ReadOnlySpan<char> ContainsCore(ReadOnlySpan<char> value, ReadOnlySpan<char> substring, CompareInfo compareInfo, CompareOptions compareOptions, bool negate,
    string? message, string? valueParameter)
    => compareInfo.IndexOf(value, substring, compareOptions) >= 0 ^ negate ? value : throw new ArgumentException(message, valueParameter);

  private static ReadOnlySpan<char> StartsWithCore(ReadOnlySpan<char> value, ReadOnlySpan<char> prefix, CompareInfo compareInfo, CompareOptions compareOptions, bool negate,
    string? message, string? valueParameter)
    => compareInfo.IsPrefix(value, prefix, compareOptions) ^ negate ? value : throw new ArgumentException(message, valueParameter);

  private static ReadOnlySpan<char> EndsWithCore(ReadOnlySpan<char> value, ReadOnlySpan<char> prefix, CompareInfo compareInfo, CompareOptions compareOptions, bool negate,
    string? message, string? valueParameter)
    => compareInfo.IsSuffix(value, prefix, compareOptions) ^ negate ? value : throw new ArgumentException(message, valueParameter);

#endif

  #endregion
  #region Public methods

  public string NotNullOrEmpty([NotNull] string? value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => NotNull(value, message, valueParameter).Length > 0 ? value :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.StringIsEmpty, valueParameter), valueParameter);

  public string NotNullOrWhitespace([NotNull] string? value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => NotNull(value, message, valueParameter).Length > 0 && value.Any(ch => !char.IsWhiteSpace(ch)) ? value :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.StringIsEmptyOrWhitespace, valueParameter), valueParameter);

  public string Empty(string value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => NotNull(value, message, valueParameter).Length == 0 ? value :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.StringIsNotEmpty, valueParameter), valueParameter);

  public string NotEmpty(string value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => NotNull(value, message, valueParameter).Length > 0 ? value :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.StringIsEmpty, valueParameter), valueParameter);

  public string Whitespace(string value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => NotNull(value, message, valueParameter).Length >= 0 && value.All(ch => char.IsWhiteSpace(ch)) ? value :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.StringIsEmptyOrWhitespace, valueParameter), valueParameter);

  public string NotWhitespace(string value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => NotNull(value, message, valueParameter).Length > 0 && value.Any(ch => !char.IsWhiteSpace(ch)) ? value :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.StringIsEmptyOrWhitespace, valueParameter), valueParameter);

  public string Contains(string value, string substring, CompareInfo compareInfo, CompareOptions compareOptions = CompareOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("substring")] string? substringParameter = "substring")
    => ContainsCore(NotNull(value, valueParameter: valueParameter), NotNull(substring, valueParameter: substringParameter), NotNull(compareInfo), compareOptions, false,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, substringParameter), valueParameter);

  public string Contains(string value, string substring, CultureInfo? cultureInfo = null, CompareOptions compareOptions = CompareOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("substring")] string? substringParameter = "substring")
    => ContainsCore(NotNull(value, valueParameter: valueParameter), NotNull(substring, valueParameter: substringParameter),
      (cultureInfo ?? Culture).CompareInfo, compareOptions, false,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, substringParameter), valueParameter);

  public string NotContains(string value, string substring, CompareInfo compareInfo, CompareOptions compareOptions = CompareOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("substring")] string? substringParameter = "substring")
    => ContainsCore(NotNull(value, valueParameter: valueParameter), NotNull(substring, valueParameter: substringParameter), NotNull(compareInfo), compareOptions, true,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, substringParameter), valueParameter);

  public string NotContains(string value, string substring, CultureInfo? cultureInfo = null, CompareOptions compareOptions = CompareOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("substring")] string? substringParameter = "substring")
    => ContainsCore(NotNull(value, valueParameter: valueParameter), NotNull(substring, valueParameter: substringParameter), (cultureInfo ?? Culture).CompareInfo, compareOptions, true,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, substringParameter), valueParameter);

  public string StartsWith(string value, string prefix, CompareInfo compareInfo, CompareOptions compareOptions = CompareOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("prefix")] string? prefixParameter = "prefix")
    => StartsWithCore(NotNull(value, valueParameter: valueParameter), NotNull(prefix, valueParameter: prefixParameter), NotNull(compareInfo), compareOptions, false,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, prefixParameter), valueParameter);

  public string StartsWith(string value, string prefix, CultureInfo? cultureInfo = null, CompareOptions compareOptions = CompareOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("prefix")] string? prefixParameter = "prefix")
    => StartsWithCore(NotNull(value, valueParameter: valueParameter), NotNull(prefix, valueParameter: prefixParameter), (cultureInfo ?? Culture).CompareInfo, compareOptions, false,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, prefixParameter), valueParameter);

  public string NotStartsWith(string value, string prefix, CompareInfo compareInfo, CompareOptions compareOptions = CompareOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("prefix")] string? prefixParameter = "prefix")
    => StartsWithCore(NotNull(value, valueParameter: valueParameter), NotNull(prefix, valueParameter: prefixParameter), NotNull(compareInfo), compareOptions, true,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, prefixParameter), valueParameter);

  public string NotStartsWith(string value, string prefix, CultureInfo? cultureInfo = null, CompareOptions compareOptions = CompareOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("prefix")] string? prefixParameter = "prefix")
    => StartsWithCore(NotNull(value, valueParameter: valueParameter), NotNull(prefix, valueParameter: prefixParameter), (cultureInfo ?? Culture).CompareInfo, compareOptions, true,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, prefixParameter), valueParameter);

  public string EndsWith(string value, string prefix, CompareInfo compareInfo, CompareOptions compareOptions = CompareOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("prefix")] string? prefixParameter = "prefix")
    => EndsWithCore(NotNull(value, valueParameter: valueParameter), NotNull(prefix, valueParameter: prefixParameter), NotNull(compareInfo), compareOptions, false,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, prefixParameter), valueParameter);

  public string EndsWith(string value, string prefix, CultureInfo? cultureInfo = null, CompareOptions compareOptions = CompareOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("prefix")] string? prefixParameter = "prefix")
    => EndsWithCore(NotNull(value, valueParameter: valueParameter), NotNull(prefix, valueParameter: prefixParameter), (cultureInfo ?? Culture).CompareInfo, compareOptions, false,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, prefixParameter), valueParameter);

  public string NotEndsWith(string value, string prefix, CompareInfo compareInfo, CompareOptions compareOptions = CompareOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("prefix")] string? prefixParameter = "prefix")
    => EndsWithCore(NotNull(value, valueParameter: valueParameter), NotNull(prefix, valueParameter: prefixParameter), NotNull(compareInfo), compareOptions, true,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, prefixParameter), valueParameter);

  public string NotEndsWith(string value, string prefix, CultureInfo? cultureInfo = null, CompareOptions compareOptions = CompareOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("prefix")] string? prefixParameter = "prefix")
    => EndsWithCore(NotNull(value, valueParameter: valueParameter), NotNull(prefix, valueParameter: prefixParameter), (cultureInfo ?? Culture).CompareInfo, compareOptions, true,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, prefixParameter), valueParameter);

  public string Match(string value, string pattern, RegexOptions options = RegexOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("pattern")] string? patternParameter = "pattern")
    => MatchCore(NotNull(value, valueParameter: valueParameter), NotNull(pattern, valueParameter: patternParameter), options, false,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, patternParameter, options), valueParameter);

  public string Match(string value, Regex regex,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("regex")] string? regexParameter = "regex")
    => MatchCore(NotNull(value, valueParameter: valueParameter), NotNull(regex, valueParameter: regexParameter), false,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, regexParameter), valueParameter);

  public string NotMatch(string value, string pattern, RegexOptions options = RegexOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("pattern")] string? patternParameter = "pattern")
    => MatchCore(NotNull(value, valueParameter: valueParameter), NotNull(pattern, valueParameter: patternParameter), options, true,
      message ?? FormatString(ArgumentMessage.StringIsMatched, valueParameter, patternParameter, options), valueParameter);

  public string NotMatch(string value, Regex regex,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("regex")] string? regexParameter = "regex")
    => MatchCore(NotNull(value, valueParameter: valueParameter), NotNull(regex, valueParameter: regexParameter), true,
      message ?? FormatString(ArgumentMessage.StringIsMatched, valueParameter, regexParameter), valueParameter);

#if NETCOREAPP2_1_OR_GREATER

  public ReadOnlySpan<char> Contains(ReadOnlySpan<char> value, ReadOnlySpan<char> substring, CompareInfo compareInfo, CompareOptions compareOptions = CompareOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("substring")] string? substringParameter = "substring")
    => ContainsCore(value, substring, NotNull(compareInfo), compareOptions, false,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, substringParameter), valueParameter);

  public ReadOnlySpan<char> Contains(ReadOnlySpan<char> value, ReadOnlySpan<char> substring, CultureInfo? cultureInfo = null, CompareOptions compareOptions = CompareOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("substring")] string? substringParameter = "substring")
    => ContainsCore(value, substring, (cultureInfo ?? Culture).CompareInfo, compareOptions, false,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, substringParameter), valueParameter);

  public ReadOnlySpan<char> NotContains(ReadOnlySpan<char> value, ReadOnlySpan<char> substring, CompareInfo compareInfo, CompareOptions compareOptions = CompareOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("substring")] string? substringParameter = "substring")
    => ContainsCore(value, substring, NotNull(compareInfo), compareOptions, true,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, substringParameter), valueParameter);

  public ReadOnlySpan<char> NotContains(ReadOnlySpan<char> value, ReadOnlySpan<char> substring, CultureInfo? cultureInfo = null, CompareOptions compareOptions = CompareOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("substring")] string? substringParameter = "substring")
    => ContainsCore(value, substring, (cultureInfo ?? Culture).CompareInfo, compareOptions, true,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, substringParameter), valueParameter);

  public ReadOnlySpan<char> StartsWith(ReadOnlySpan<char> value, ReadOnlySpan<char> prefix, CompareInfo compareInfo, CompareOptions compareOptions = CompareOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("prefix")] string? prefixParameter = "prefix")
    => StartsWithCore(value, prefix, NotNull(compareInfo), compareOptions, false,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, prefixParameter), valueParameter);

  public ReadOnlySpan<char> StartsWith(ReadOnlySpan<char> value, ReadOnlySpan<char> prefix, CultureInfo? cultureInfo = null, CompareOptions compareOptions = CompareOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("prefix")] string? prefixParameter = "prefix")
    => StartsWithCore(value, prefix, (cultureInfo ?? Culture).CompareInfo, compareOptions, false,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, prefixParameter), valueParameter);

  public ReadOnlySpan<char> NotStartsWith(ReadOnlySpan<char> value, ReadOnlySpan<char> prefix, CompareInfo compareInfo, CompareOptions compareOptions = CompareOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("prefix")] string? prefixParameter = "prefix")
    => StartsWithCore(value, prefix, NotNull(compareInfo), compareOptions, true,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, prefixParameter), valueParameter);

  public ReadOnlySpan<char> NotStartsWith(ReadOnlySpan<char> value, ReadOnlySpan<char> prefix, CultureInfo? cultureInfo = null, CompareOptions compareOptions = CompareOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("prefix")] string? prefixParameter = "prefix")
    => StartsWithCore(value, prefix, (cultureInfo ?? Culture).CompareInfo, compareOptions, true,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, prefixParameter), valueParameter);

  public ReadOnlySpan<char> EndsWith(ReadOnlySpan<char> value, ReadOnlySpan<char> prefix, CompareInfo compareInfo, CompareOptions compareOptions = CompareOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("prefix")] string? prefixParameter = "prefix")
    => EndsWithCore(value, prefix, NotNull(compareInfo), compareOptions, false,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, prefixParameter), valueParameter);

  public ReadOnlySpan<char> EndsWith(ReadOnlySpan<char> value, ReadOnlySpan<char> prefix, CultureInfo? cultureInfo = null, CompareOptions compareOptions = CompareOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("prefix")] string? prefixParameter = "prefix")
    => EndsWithCore(value, prefix, (cultureInfo ?? Culture).CompareInfo, compareOptions, false,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, prefixParameter), valueParameter);

  public ReadOnlySpan<char> NotEndsWith(ReadOnlySpan<char> value, ReadOnlySpan<char> prefix, CompareInfo compareInfo, CompareOptions compareOptions = CompareOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("prefix")] string? prefixParameter = "prefix")
    => EndsWithCore(value, prefix, NotNull(compareInfo), compareOptions, true,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, prefixParameter), valueParameter);

  public ReadOnlySpan<char> NotEndsWith(ReadOnlySpan<char> value, ReadOnlySpan<char> prefix, CultureInfo? cultureInfo = null, CompareOptions compareOptions = CompareOptions.None,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("prefix")] string? prefixParameter = "prefix")
    => EndsWithCore(value, prefix, (cultureInfo ?? Culture).CompareInfo, compareOptions, true,
      message ?? FormatString(ArgumentMessage.StringIsMismatched, valueParameter, prefixParameter), valueParameter);

#endif

  #endregion
  #endregion
  #region DateTime interval validation

  public DateTimeInterval InRange(DateTime dateTime, TimeSpan timeSpan,
    string? message = null, [CallerArgumentExpression("dateTime")] string? dateTimeParameter = "dateTime", [CallerArgumentExpression("timeSpan")] string? timeSpanParameter = "timeSpan")
    => timeSpan >= DateTime.MinValue - dateTime && timeSpan <= DateTime.MaxValue - dateTime ? new DateTimeInterval(dateTime, timeSpan) :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.DateTimeIntervalIsOutOfRange, dateTimeParameter, timeSpanParameter));

  #endregion
  #region Reference equals validation

  public T Same<T>(T value, T other,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    where T : class
    => ReferenceEquals(value, other) ? value : throw new ArgumentException(message ?? FormatString(ArgumentMessage.IsNotSame, valueParameter, otherParameter), valueParameter);

  public T NotSame<T>(T value, T other,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    where T : class
    => !ReferenceEquals(value, other) ? value : throw new ArgumentException(message ?? FormatString(ArgumentMessage.IsSame, valueParameter, otherParameter), valueParameter);

  #endregion
  #region Equality validation

  private static T EqualsCore<T>(T value, T other, Equality<T> equality, bool negate, string? message, string? valueParameter)
    => equality(value, other) ^ negate ? value : throw new ArgumentOutOfRangeException(valueParameter, value, message);

  public T Equals<T>(T value, T other,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => EqualsCore(value, other, EqualityComparer<T>.Default.Equals, false,
      message ?? FormatString(ArgumentMessage.IsNotEqual, valueParameter, otherParameter), valueParameter);

  public T Equals<T>(T value, T other, Equality<T>? equality,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => EqualsCore(value, other, equality ?? EqualityComparer<T>.Default.Equals, false,
      message ?? FormatString(ArgumentMessage.IsNotEqual, valueParameter, otherParameter), valueParameter);

  public T Equals<T>(T value, T other, IEqualityComparer<T>? equalityComparer,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => EqualsCore(value, other, (equalityComparer ?? EqualityComparer<T>.Default).Equals, false,
      message ?? FormatString(ArgumentMessage.IsNotEqual, valueParameter, otherParameter), valueParameter);

  public T NotEquals<T>(T value, T other,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => EqualsCore(value, other, EqualityComparer<T>.Default.Equals, true,
      message ?? FormatString(ArgumentMessage.IsEqual, valueParameter, otherParameter), valueParameter);

  public T NotEquals<T>(T value, T other, Equality<T>? equality,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => EqualsCore(value, other, equality ?? EqualityComparer<T>.Default.Equals, true,
      message ?? FormatString(ArgumentMessage.IsEqual, valueParameter, otherParameter), valueParameter);

  public T NotEquals<T>(T value, T other, IEqualityComparer<T>? equalityComparer,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => EqualsCore(value, other, (equalityComparer ?? EqualityComparer<T>.Default).Equals, true,
      message ?? FormatString(ArgumentMessage.IsEqual, valueParameter, otherParameter), valueParameter);

  #endregion
  #region In validation

  private static T InCore<T>(T value, IEnumerable<T> collection, Equality<T> equality, bool negate, string? message, string? valueParameter)
    => collection.Any(item => equality(item, value)) ^ negate ? value : throw new ArgumentOutOfRangeException(valueParameter, value, message);

  public T In<T>(T value, IEnumerable<T> collection,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => InCore(value, NotNull(collection, valueParameter: collectionParameter), EqualityComparer<T>.Default.Equals, false,
      message ?? FormatString(ArgumentMessage.IsNotExistIn, valueParameter, collectionParameter), valueParameter);

  public T In<T>(T value, IEnumerable<T> collection, Equality<T>? equality,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => InCore(value, NotNull(collection, valueParameter: collectionParameter), equality ?? EqualityComparer<T>.Default.Equals, false,
      message ?? FormatString(ArgumentMessage.IsNotExistIn, valueParameter, collectionParameter), valueParameter);

  public T In<T>(T value, IEnumerable<T> collection, IEqualityComparer<T>? equalityComparer,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => InCore(value, NotNull(collection, valueParameter: collectionParameter), (equalityComparer ?? EqualityComparer<T>.Default).Equals, false,
      message ?? FormatString(ArgumentMessage.IsNotExistIn, valueParameter, collectionParameter), valueParameter);

  public T NotIn<T>(T value, IEnumerable<T> collection,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => InCore(value, NotNull(collection, valueParameter: collectionParameter), EqualityComparer<T>.Default.Equals, true,
      message ?? FormatString(ArgumentMessage.IsExistIn, valueParameter, collectionParameter), valueParameter);

  public T NotIn<T>(T value, IEnumerable<T> collection, Equality<T>? equality,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => InCore(value, NotNull(collection, valueParameter: collectionParameter), equality ?? EqualityComparer<T>.Default.Equals, true,
      message ?? FormatString(ArgumentMessage.IsExistIn, valueParameter, collectionParameter), valueParameter);

  public T NotIn<T>(T value, IEnumerable<T> collection, IEqualityComparer<T>? equalityComparer,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => InCore(value, NotNull(collection, valueParameter: collectionParameter), (equalityComparer ?? EqualityComparer<T>.Default).Equals, true,
      message ?? FormatString(ArgumentMessage.IsExistIn, valueParameter, collectionParameter), valueParameter);

  #endregion
  #region Compare validation

  private static T CompareCore<T>(T value, T other, Comparison<T> comparison, ComparisonCriteria criteria, bool negate, string? message, string? valueParameter)
    => Comparison.Match(comparison(value, other), criteria) ^ negate ? value : throw new ArgumentOutOfRangeException(valueParameter, value, message);

  public T Compare<T>(T value, T other, ComparisonCriteria criteria,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => CompareCore(value, other, Comparer<T>.Default.Compare, criteria, false,
      message ?? FormatString(ArgumentMessage.IsNotCompared, valueParameter, otherParameter), valueParameter);

  public T Compare<T>(T value, T other, Comparison<T>? comparison, ComparisonCriteria criteria,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => CompareCore(value, other, comparison ?? Comparer<T>.Default.Compare, criteria, false,
      message ?? FormatString(ArgumentMessage.IsNotCompared, valueParameter, otherParameter), valueParameter);

  public T Compare<T>(T value, T other, IComparer<T>? comparer, ComparisonCriteria criteria,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => CompareCore(value, other, (comparer ?? Comparer<T>.Default).Compare, criteria, false,
      message ?? FormatString(ArgumentMessage.IsNotCompared, valueParameter, otherParameter), valueParameter);

  public T NotCompare<T>(T value, T other, ComparisonCriteria criteria,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => CompareCore(value, other, Comparer<T>.Default.Compare, criteria, true,
      message ?? FormatString(ArgumentMessage.IsCompared, valueParameter, otherParameter), valueParameter);

  public T NotCompare<T>(T value, T other, Comparison<T>? comparison, ComparisonCriteria criteria,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => CompareCore(value, other, comparison ?? Comparer<T>.Default.Compare, criteria, true,
      message ?? FormatString(ArgumentMessage.IsCompared, valueParameter, otherParameter), valueParameter);

  public T NotCompare<T>(T value, T other, IComparer<T>? comparer, ComparisonCriteria criteria,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => CompareCore(value, other, (comparer ?? Comparer<T>.Default).Compare, criteria, true,
      message ?? FormatString(ArgumentMessage.IsCompared, valueParameter, otherParameter), valueParameter);

  public T LessThan<T>(T value, T other,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => CompareCore(value, other, Comparer<T>.Default.Compare, ComparisonCriteria.LessThan, false,
      message ?? FormatString(ArgumentMessage.IsNotCompared, valueParameter, otherParameter), valueParameter);

  public T LessThan<T>(T value, T other, Comparison<T>? comparison,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => CompareCore(value, other, comparison ?? Comparer<T>.Default.Compare, ComparisonCriteria.LessThan, false,
      message ?? FormatString(ArgumentMessage.IsNotCompared, valueParameter, otherParameter), valueParameter);

  public T LessThan<T>(T value, T other, IComparer<T>? comparer,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => CompareCore(value, other, (comparer ?? Comparer<T>.Default).Compare, ComparisonCriteria.LessThan, false,
      message ?? FormatString(ArgumentMessage.IsNotCompared, valueParameter, otherParameter), valueParameter);

  public T LessThanOrEqual<T>(T value, T other,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => CompareCore(value, other, Comparer<T>.Default.Compare, ComparisonCriteria.LessThanOrEqual, false,
      message ?? FormatString(ArgumentMessage.IsNotCompared, valueParameter, otherParameter), valueParameter);

  public T LessThanOrEqual<T>(T value, T other, Comparison<T>? comparison,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => CompareCore(value, other, comparison ?? Comparer<T>.Default.Compare, ComparisonCriteria.LessThanOrEqual, false,
      message ?? FormatString(ArgumentMessage.IsNotCompared, valueParameter, otherParameter), valueParameter);

  public T LessThanOrEqual<T>(T value, T other, IComparer<T>? comparer,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => CompareCore(value, other, (comparer ?? Comparer<T>.Default).Compare, ComparisonCriteria.LessThanOrEqual, false,
      message ?? FormatString(ArgumentMessage.IsNotCompared, valueParameter, otherParameter), valueParameter);

  public T GreaterThan<T>(T value, T other,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => CompareCore(value, other, Comparer<T>.Default.Compare, ComparisonCriteria.GreaterThan, false,
      message ?? FormatString(ArgumentMessage.IsNotCompared, valueParameter, otherParameter), valueParameter);

  public T GreaterThan<T>(T value, T other, Comparison<T>? comparison,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => CompareCore(value, other, comparison ?? Comparer<T>.Default.Compare, ComparisonCriteria.GreaterThan, false,
      message ?? FormatString(ArgumentMessage.IsNotCompared, valueParameter, otherParameter), valueParameter);

  public T GreaterThan<T>(T value, T other, IComparer<T>? comparer,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => CompareCore(value, other, (comparer ?? Comparer<T>.Default).Compare, ComparisonCriteria.GreaterThan, false,
      message ?? FormatString(ArgumentMessage.IsNotCompared, valueParameter, otherParameter), valueParameter);

  public T GreaterThanOrEqual<T>(T value, T other,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => CompareCore(value, other, Comparer<T>.Default.Compare, ComparisonCriteria.GreaterThanOrEqual, false,
      message ?? FormatString(ArgumentMessage.IsNotCompared, valueParameter, otherParameter), valueParameter);

  public T GreaterThanOrEqual<T>(T value, T other, Comparison<T>? comparison,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => CompareCore(value, other, comparison ?? Comparer<T>.Default.Compare, ComparisonCriteria.GreaterThanOrEqual, false,
      message ?? FormatString(ArgumentMessage.IsNotCompared, valueParameter, otherParameter), valueParameter);

  public T GreaterThanOrEqual<T>(T value, T other, IComparer<T>? comparer,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => CompareCore(value, other, (comparer ?? Comparer<T>.Default).Compare, ComparisonCriteria.GreaterThanOrEqual, false,
      message ?? FormatString(ArgumentMessage.IsNotCompared, valueParameter, otherParameter), valueParameter);

  public T Equal<T>(T value, T other,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => CompareCore(value, other, Comparer<T>.Default.Compare, ComparisonCriteria.Equal, false,
      message ?? FormatString(ArgumentMessage.IsNotCompared, valueParameter, otherParameter), valueParameter);

  public T Equal<T>(T value, T other, Comparison<T>? comparison,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => CompareCore(value, other, comparison ?? Comparer<T>.Default.Compare, ComparisonCriteria.Equal, false,
      message ?? FormatString(ArgumentMessage.IsNotCompared, valueParameter, otherParameter), valueParameter);

  public T Equal<T>(T value, T other, IComparer<T>? comparer,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => CompareCore(value, other, (comparer ?? Comparer<T>.Default).Compare, ComparisonCriteria.Equal, false,
      message ?? FormatString(ArgumentMessage.IsNotCompared, valueParameter, otherParameter), valueParameter);

  public T NotEqual<T>(T value, T other,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => CompareCore(value, other, Comparer<T>.Default.Compare, ComparisonCriteria.NotEqual, false,
      message ?? FormatString(ArgumentMessage.IsNotCompared, valueParameter, otherParameter), valueParameter);

  public T NotEqual<T>(T value, T other, Comparison<T>? comparison,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => CompareCore(value, other, comparison ?? Comparer<T>.Default.Compare, ComparisonCriteria.NotEqual, false,
      message ?? FormatString(ArgumentMessage.IsNotCompared, valueParameter, otherParameter), valueParameter);

  public T NotEqual<T>(T value, T other, IComparer<T>? comparer,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value", [CallerArgumentExpression("other")] string? otherParameter = "other")
    => CompareCore(value, other, (comparer ?? Comparer<T>.Default).Compare, ComparisonCriteria.NotEqual, false,
      message ?? FormatString(ArgumentMessage.IsNotCompared, valueParameter, otherParameter), valueParameter);

  #endregion
  #region Between validation

  private static T BetweenCore<T>(T value, T lowerValue, T upperValue, Comparison<T> comparison, BetweenCriteria criteria, bool negate, string? message, string? valueParameter)
    => Comparison.Match(comparison(value, lowerValue), comparison(value, upperValue), criteria) ^ negate ? value : throw new ArgumentOutOfRangeException(valueParameter, value, message);

  public T Between<T>(T value, T lowerValue, T upperValue, BetweenCriteria criteria = BetweenCriteria.IncludeBoth,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value",
    [CallerArgumentExpression("lowerValue")] string? lowerValueParameter = "lowerValue", [CallerArgumentExpression("upperValue")] string? upperValueParameter = "upperValue")
    => BetweenCore(value, lowerValue, upperValue, Comparer<T>.Default.Compare, criteria, false,
      message ?? FormatString(ArgumentMessage.IsNotBetween, valueParameter, lowerValueParameter, upperValueParameter), valueParameter);

  public T Between<T>(T value, T lowerValue, T upperValue, Comparison<T>? comparison, BetweenCriteria criteria = BetweenCriteria.IncludeBoth,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value",
    [CallerArgumentExpression("lowerValue")] string? lowerValueParameter = "lowerValue", [CallerArgumentExpression("upperValue")] string? upperValueParameter = "upperValue")
    => BetweenCore(value, lowerValue, upperValue, comparison ?? Comparer<T>.Default.Compare, criteria, false,
      message ?? FormatString(ArgumentMessage.IsNotBetween, valueParameter, lowerValueParameter, upperValueParameter), valueParameter);

  public T Between<T>(T value, T lowerValue, T upperValue, IComparer<T>? comparer, BetweenCriteria criteria = BetweenCriteria.IncludeBoth,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value",
    [CallerArgumentExpression("lowerValue")] string? lowerValueParameter = "lowerValue", [CallerArgumentExpression("upperValue")] string? upperValueParameter = "upperValue")
    => BetweenCore(value, lowerValue, upperValue, (comparer ?? Comparer<T>.Default).Compare, criteria, false,
      message ?? FormatString(ArgumentMessage.IsNotBetween, valueParameter, lowerValueParameter, upperValueParameter), valueParameter);

  public T NotBetween<T>(T value, T lowerValue, T upperValue, BetweenCriteria criteria = BetweenCriteria.IncludeBoth,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value",
    [CallerArgumentExpression("lowerValue")] string? lowerValueParameter = "lowerValue", [CallerArgumentExpression("upperValue")] string? upperValueParameter = "upperValue")
    => BetweenCore(value, lowerValue, upperValue, Comparer<T>.Default.Compare, criteria, true,
      message ?? FormatString(ArgumentMessage.IsBetween, valueParameter, lowerValueParameter, upperValueParameter), valueParameter);

  public T NotBetween<T>(T value, T lowerValue, T upperValue, Comparison<T>? comparison, BetweenCriteria criteria = BetweenCriteria.IncludeBoth,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value",
    [CallerArgumentExpression("lowerValue")] string? lowerValueParameter = "lowerValue", [CallerArgumentExpression("upperValue")] string? upperValueParameter = "upperValue")
    => BetweenCore(value, lowerValue, upperValue, comparison ?? Comparer<T>.Default.Compare, criteria, true,
      message ?? FormatString(ArgumentMessage.IsBetween, valueParameter, lowerValueParameter, upperValueParameter), valueParameter);

  public T NotBetween<T>(T value, T lowerValue, T upperValue, IComparer<T>? comparer, BetweenCriteria criteria = BetweenCriteria.IncludeBoth,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value",
    [CallerArgumentExpression("lowerValue")] string? lowerValueParameter = "lowerValue", [CallerArgumentExpression("upperValue")] string? upperValueParameter = "upperValue")
    => BetweenCore(value, lowerValue, upperValue, (comparer ?? Comparer<T>.Default).Compare, criteria, true,
      message ?? FormatString(ArgumentMessage.IsBetween, valueParameter, lowerValueParameter, upperValueParameter), valueParameter);

  #endregion
  #region Numeric validation
  #region SByte

  public sbyte NonZero(sbyte value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, (sbyte)0, Comparer<sbyte>.Default.AsComparison<sbyte>(), ComparisonCriteria.Equal, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public sbyte Positive(sbyte value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, (sbyte)0, Comparer<sbyte>.Default.AsComparison<sbyte>(), ComparisonCriteria.GreaterThan, false,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public sbyte NonPositive(sbyte value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, (sbyte)0, Comparer<sbyte>.Default.AsComparison<sbyte>(), ComparisonCriteria.GreaterThan, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public sbyte Negative(sbyte value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, (sbyte)0, Comparer<sbyte>.Default.AsComparison<sbyte>(), ComparisonCriteria.LessThan, false,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public sbyte NonNegative(sbyte value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, (sbyte)0, Comparer<sbyte>.Default.AsComparison<sbyte>(), ComparisonCriteria.LessThan, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  #endregion
  #region SInt16

  public short NonZero(short value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, (short)0, Comparer<short>.Default.AsComparison<short>(), ComparisonCriteria.Equal, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public short Positive(short value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, (short)0, Comparer<short>.Default.AsComparison<short>(), ComparisonCriteria.GreaterThan, false,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public short NonPositive(short value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, (short)0, Comparer<short>.Default.AsComparison<short>(), ComparisonCriteria.GreaterThan, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public short Negative(short value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, (short)0, Comparer<short>.Default.AsComparison<short>(), ComparisonCriteria.LessThan, false,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public short NonNegative(short value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, (short)0, Comparer<short>.Default.AsComparison<short>(), ComparisonCriteria.LessThan, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  #endregion
  #region SInt32

  public int NonZero(int value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0, Comparer<int>.Default.AsComparison<int>(), ComparisonCriteria.Equal, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public int Positive(int value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0, Comparer<int>.Default.AsComparison<int>(), ComparisonCriteria.GreaterThan, false,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public int NonPositive(int value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0, Comparer<int>.Default.AsComparison<int>(), ComparisonCriteria.GreaterThan, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public int Negative(int value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0, Comparer<int>.Default.AsComparison<int>(), ComparisonCriteria.LessThan, false,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public int NonNegative(int value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0, Comparer<int>.Default.AsComparison<int>(), ComparisonCriteria.LessThan, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  #endregion
  #region SInt64

  public long NonZero(long value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0L, Comparer<long>.Default.AsComparison<long>(), ComparisonCriteria.Equal, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public long Positive(long value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0L, Comparer<long>.Default.AsComparison<long>(), ComparisonCriteria.GreaterThan, false,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public long NonPositive(long value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0L, Comparer<long>.Default.AsComparison<long>(), ComparisonCriteria.GreaterThan, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public long Negative(long value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0L, Comparer<long>.Default.AsComparison<long>(), ComparisonCriteria.LessThan, false,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public long NonNegative(long value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0L, Comparer<long>.Default.AsComparison<long>(), ComparisonCriteria.LessThan, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  #endregion
  #region Half
#if NET5_0_OR_GREATER

  public Half NonZero(Half value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, (Half)0, Comparer<Half>.Default.AsComparison<Half>(), ComparisonCriteria.Equal, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public Half Positive(Half value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, (Half)0, Comparer<Half>.Default.AsComparison<Half>(), ComparisonCriteria.GreaterThan, false,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public Half NonPositive(Half value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, (Half)0, Comparer<Half>.Default.AsComparison<Half>(), ComparisonCriteria.GreaterThan, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public Half Negative(Half value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, (Half)0, Comparer<Half>.Default.AsComparison<Half>(), ComparisonCriteria.LessThan, false,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public Half NonNegative(Half value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, (Half)0, Comparer<Half>.Default.AsComparison<Half>(), ComparisonCriteria.LessThan, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);
#endif
  #endregion
  #region Single

  public float NonZero(float value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0F, Comparer<float>.Default.AsComparison<float>(), ComparisonCriteria.Equal, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public float Positive(float value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0F, Comparer<float>.Default.AsComparison<float>(), ComparisonCriteria.GreaterThan, false,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public float NonPositive(float value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0F, Comparer<float>.Default.AsComparison<float>(), ComparisonCriteria.GreaterThan, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public float Negative(float value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0F, Comparer<float>.Default.AsComparison<float>(), ComparisonCriteria.LessThan, false,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public float NonNegative(float value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0F, Comparer<float>.Default.AsComparison<float>(), ComparisonCriteria.LessThan, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  #endregion
  #region Double

  public double NonZero(double value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0D, Comparer<double>.Default.AsComparison<double>(), ComparisonCriteria.Equal, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public double Positive(double value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0D, Comparer<double>.Default.AsComparison<double>(), ComparisonCriteria.GreaterThan, false,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public double NonPositive(double value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0D, Comparer<double>.Default.AsComparison<double>(), ComparisonCriteria.GreaterThan, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);
  public double Negative(double value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0D, Comparer<double>.Default.AsComparison<double>(), ComparisonCriteria.LessThan, false,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public double NonNegative(double value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0D, Comparer<double>.Default.AsComparison<double>(), ComparisonCriteria.LessThan, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  #endregion
  #region Decimal

  public decimal NonZero(decimal value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0M, Comparer<decimal>.Default.AsComparison<decimal>(), ComparisonCriteria.Equal, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public decimal Positive(decimal value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0M, Comparer<decimal>.Default.AsComparison<decimal>(), ComparisonCriteria.GreaterThan, false,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public decimal NonPositive(decimal value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0M, Comparer<decimal>.Default.AsComparison<decimal>(), ComparisonCriteria.GreaterThan, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public decimal Negative(decimal value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0M, Comparer<decimal>.Default.AsComparison<decimal>(), ComparisonCriteria.LessThan, false,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public decimal NonNegative(decimal value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, 0M, Comparer<decimal>.Default.AsComparison<decimal>(), ComparisonCriteria.LessThan, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  #endregion
  #region BigInteger

  public BigInteger NonZero(BigInteger value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, BigInteger.Zero, Comparer<BigInteger>.Default.AsComparison<BigInteger>(), ComparisonCriteria.Equal, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public BigInteger Positive(BigInteger value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, BigInteger.Zero, Comparer<BigInteger>.Default.AsComparison<BigInteger>(), ComparisonCriteria.GreaterThan, false,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public BigInteger NonPositive(BigInteger value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, BigInteger.Zero, Comparer<BigInteger>.Default.AsComparison<BigInteger>(), ComparisonCriteria.GreaterThan, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public BigInteger Negative(BigInteger value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, BigInteger.Zero, Comparer<BigInteger>.Default.AsComparison<BigInteger>(), ComparisonCriteria.LessThan, false,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public BigInteger NonNegative(BigInteger value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, BigInteger.Zero, Comparer<BigInteger>.Default.AsComparison<BigInteger>(), ComparisonCriteria.LessThan, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  #endregion
  #region TimeSpan

  public TimeSpan NonZero(TimeSpan value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, TimeSpan.Zero, Comparer<TimeSpan>.Default.AsComparison<TimeSpan>(), ComparisonCriteria.Equal, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public TimeSpan Positive(TimeSpan value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, TimeSpan.Zero, Comparer<TimeSpan>.Default.AsComparison<TimeSpan>(), ComparisonCriteria.GreaterThan, false,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public TimeSpan NonPositive(TimeSpan value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, TimeSpan.Zero, Comparer<TimeSpan>.Default.AsComparison<TimeSpan>(), ComparisonCriteria.GreaterThan, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public TimeSpan Negative(TimeSpan value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, TimeSpan.Zero, Comparer<TimeSpan>.Default.AsComparison<TimeSpan>(), ComparisonCriteria.LessThan, false,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  public TimeSpan NonNegative(TimeSpan value,
    string? message = null, [CallerArgumentExpression("value")] string? valueParameter = "value")
    => CompareCore(value, TimeSpan.Zero, Comparer<TimeSpan>.Default.AsComparison<TimeSpan>(), ComparisonCriteria.LessThan, true,
      message ?? FormatString(ArgumentMessage.IsOutOfRange, valueParameter), valueParameter);

  #endregion
  #endregion
  #region Range validation
  #region Total

  public int InRangeOut(int total, int index,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total", [CallerArgumentExpression("index")] string? indexParameter = "index")
  {
    Between(total, 0, int.MaxValue, valueParameter: totalParameter);

    return index >= 0 && index <= total ? index :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, totalParameter, indexParameter));
  }

  public int InRangeIn(int total, int index,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total", [CallerArgumentExpression("index")] string? indexParameter = "index")
  {
    Between(total, 0, int.MaxValue, valueParameter: totalParameter);

    return index >= 0 && index < total ? index :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, totalParameter, indexParameter));
  }

  public int InRangeBasisOut(int total, int index, int basis,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total", [CallerArgumentExpression("index")] string? indexParameter = "index")
  {
    NonNegative(basis);
    Between(total, 0, int.MaxValue - basis, valueParameter: totalParameter);

    return index >= basis && index <= basis + total ? index :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, totalParameter, indexParameter));
  }

  public int InRangeBasisIn(int total, int index, int basis,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total", [CallerArgumentExpression("index")] string? indexParameter = "index")
  {
    NonNegative(basis);
    Between(total, 0, int.MaxValue - basis, valueParameter: totalParameter);

    return index >= basis && index < basis + total ? index :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, totalParameter, indexParameter));
  }

  public (int index, int count) InRangeOut(int total, int index, int count,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    Between(total, 0, int.MaxValue, valueParameter: totalParameter);

    return index >= 0 && index <= total ? count >= 0 && count <= total - index ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, totalParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, totalParameter, indexParameter));
  }

  public (int index, int count) InRangeIn(int total, int index, int count,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    Between(total, 0, int.MaxValue, valueParameter: totalParameter);

    return index >= 0 && index < total ? count >= 0 && count <= total - index ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, totalParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, totalParameter, indexParameter));
  }

  public (int index, int count) InRangeRev(int total, int index, int count,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    Between(total, 0, int.MaxValue, valueParameter: totalParameter);

    return index >= 0 && index < total ? count >= 0 && count <= index + 1 ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, totalParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, totalParameter, indexParameter));
  }

  public (int index, int count) InRangeBasisOut(int total, int index, int count, int basis,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    NonNegative(basis);
    Between(total, 0, int.MaxValue - basis, valueParameter: totalParameter);

    return index >= basis && index <= basis + total ? count >= 0 && count <= basis + total - index ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, totalParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, totalParameter, indexParameter));
  }

  public (int index, int count) InRangeBasisIn(int total, int index, int count, int basis,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    NonNegative(basis);
    Between(total, 0, int.MaxValue - basis, valueParameter: totalParameter);

    return index >= basis && index < basis + total ? count >= 0 && count <= basis + total - index ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, totalParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, totalParameter, indexParameter));
  }

  public (int index, int count) InRangeBasisRev(int total, int index, int count, int basis,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    NonNegative(basis);
    Between(total, 0, int.MaxValue - basis, valueParameter: totalParameter);

    return index >= basis && index < basis + total ? count >= 0 && count <= index + 1 - basis ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, totalParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, totalParameter, indexParameter));
  }

  public (int index, int count) InRangeOut(int total, (int index, int count) range,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total", [CallerArgumentExpression("range")] string? rangeParameter = "range")
  {
    Between(total, 0, int.MaxValue, valueParameter: totalParameter);

    return range.index >= 0 && range.index <= total ? range.count >= 0 && range.count <= total - range.index ? range :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeCountOutOfRange, totalParameter, rangeParameter)) :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeIndexOutOfRange, totalParameter, rangeParameter));
  }

  public (int index, int count) InRangeIn(int total, (int index, int count) range,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total", [CallerArgumentExpression("range")] string? rangeParameter = "range")
  {
    Between(total, 0, int.MaxValue, valueParameter: totalParameter);

    return range.index >= 0 && range.index < total ? range.count >= 0 && range.count <= total - range.index ? range :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeCountOutOfRange, totalParameter, rangeParameter)) :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeIndexOutOfRange, totalParameter, rangeParameter));
  }

  public (int index, int count) InRangeRev(int total, (int index, int count) range,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total", [CallerArgumentExpression("range")] string? rangeParameter = "range")
  {
    Between(total, 0, int.MaxValue, valueParameter: totalParameter);

    return range.index >= 0 && range.index < total ? range.count >= 0 && range.count <= range.index + 1 ? range :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeCountOutOfRange, totalParameter, rangeParameter)) :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeIndexOutOfRange, totalParameter, rangeParameter));
  }

  public (int index, int count) InRangeBasisOut(int total, (int index, int count) range, int basis,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total", [CallerArgumentExpression("range")] string? rangeParameter = "range")
  {
    NonNegative(basis);
    Between(total, 0, int.MaxValue - basis, valueParameter: totalParameter);

    return range.index >= basis && range.index <= basis + total ? range.count >= 0 && range.count <= basis + total - range.index ? range :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeCountOutOfRange, totalParameter, rangeParameter)) :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeIndexOutOfRange, totalParameter, rangeParameter));
  }

  public (int index, int count) InRangeBasisIn(int total, (int index, int count) range, int basis,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total", [CallerArgumentExpression("range")] string? rangeParameter = "range")
  {
    NonNegative(basis);
    Between(total, 0, int.MaxValue - basis, valueParameter: totalParameter);

    return range.index >= basis && range.index < basis + total ? range.count >= 0 && range.count <= basis + total - range.index ? range :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeCountOutOfRange, totalParameter, rangeParameter)) :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeIndexOutOfRange, totalParameter, rangeParameter));
  }

  public (int index, int count) InRangeBasisRev(int total, (int index, int count) range, int basis,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total", [CallerArgumentExpression("range")] string? rangeParameter = "range")
  {
    NonNegative(basis);
    Between(total, 0, int.MaxValue - basis, valueParameter: totalParameter);

    return range.index >= basis && range.index < basis + total ? range.count >= 0 && range.count <= range.index + 1 - basis ? range :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeCountOutOfRange, totalParameter, rangeParameter)) :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeIndexOutOfRange, totalParameter, rangeParameter));
  }

  public int InLimitsOut(int total, int count, int basis = 0, int limit = int.MaxValue,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total",
    [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    NonNegative(basis);
    GreaterThanOrEqual(limit, basis);
    Between(total, 0, limit - basis, valueParameter: totalParameter);

    return count >= 0 && count <= limit - (basis + total) ? count :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, totalParameter, countParameter));
  }

  public (int index, int count) InLimitsIn(int total, int index, int count, int basis = 0, int limit = int.MaxValue,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    NonNegative(basis);
    GreaterThanOrEqual(limit, basis);
    Between(total, 0, limit - basis, valueParameter: totalParameter);

    return index >= basis && index <= basis + total ? count >= 0 && count <= limit - (basis + total) ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, totalParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, totalParameter, indexParameter));
  }

  public long InRangeOut(long total, long index,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total", [CallerArgumentExpression("index")] string? indexParameter = "index")
  {
    Between(total, 0L, long.MaxValue, valueParameter: totalParameter);

    return index >= 0L && index <= total ? index :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, totalParameter, indexParameter));
  }

  public long InRangeIn(long total, long index,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total", [CallerArgumentExpression("index")] string? indexParameter = "index")
  {
    Between(total, 0L, long.MaxValue, valueParameter: totalParameter);

    return index >= 0L && index < total ? index :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, totalParameter, indexParameter));
  }

  public long InRangeBasisOut(long total, long index, long basis,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total", [CallerArgumentExpression("index")] string? indexParameter = "index")
  {
    NonNegative(basis);
    Between(total, 0L, long.MaxValue - basis, valueParameter: totalParameter);

    return index >= basis && index <= basis + total ? index :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, totalParameter, indexParameter));
  }

  public long InRangeBasisIn(long total, long index, long basis,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total", [CallerArgumentExpression("index")] string? indexParameter = "index")
  {
    NonNegative(basis);
    Between(total, 0L, long.MaxValue - basis, valueParameter: totalParameter);

    return index >= basis && index < basis + total ? index :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, totalParameter, indexParameter));
  }

  public (long index, long count) InRangeOut(long total, long index, long count,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    Between(total, 0L, long.MaxValue, valueParameter: totalParameter);

    return index >= 0L && index <= total ? count >= 0L && count <= total - index ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, totalParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, totalParameter, indexParameter));
  }

  public (long index, long count) InRangeIn(long total, long index, long count,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    Between(total, 0L, long.MaxValue, valueParameter: totalParameter);

    return index >= 0L && index < total ? count >= 0L && count <= total - index ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, totalParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, totalParameter, indexParameter));
  }

  public (long index, long count) InRangeRev(long total, long index, long count,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    Between(total, 0L, long.MaxValue, valueParameter: totalParameter);

    return index >= 0L && index < total ? count >= 0L && count <= index + 1 ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, totalParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, totalParameter, indexParameter));
  }

  public (long index, long count) InRangeBasisOut(long total, long index, long count, long basis,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    NonNegative(basis);
    Between(total, 0L, long.MaxValue - basis, valueParameter: totalParameter);

    return index >= basis && index <= basis + total ? count >= 0L && count <= basis + total - index ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, totalParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, totalParameter, indexParameter));
  }

  public (long index, long count) InRangeBasisIn(long total, long index, long count, long basis = 0L,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    NonNegative(basis);
    Between(total, 0L, long.MaxValue - basis, valueParameter: totalParameter);

    return index >= basis && index < basis + total ? count >= 0L && count <= basis + total - index ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, totalParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, totalParameter, indexParameter));
  }

  public (long index, long count) InRangeBasisRev(long total, long index, long count, long basis = 0L,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    NonNegative(basis);
    Between(total, 0L, long.MaxValue - basis, valueParameter: totalParameter);

    return index >= basis && index < basis + total ? count >= 0L && count <= index + 1L - basis ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, totalParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, totalParameter, indexParameter));
  }

  public (long index, long count) InRangeOut(long total, (long index, long count) range,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total", [CallerArgumentExpression("range")] string? rangeParameter = "range")
  {
    Between(total, 0L, int.MaxValue, valueParameter: totalParameter);

    return range.index >= 0L && range.index <= total ? range.count >= 0L && range.count <= total - range.index ? range :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeCountOutOfRange, totalParameter, rangeParameter)) :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeIndexOutOfRange, totalParameter, rangeParameter));
  }

  public (long index, long count) InRangeIn(long total, (long index, long count) range,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total", [CallerArgumentExpression("range")] string? rangeParameter = "range")
  {
    Between(total, 0, int.MaxValue, valueParameter: totalParameter);

    return range.index >= 0L && range.index < total ? range.count >= 0L && range.count <= total - range.index ? range :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeCountOutOfRange, totalParameter, rangeParameter)) :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeIndexOutOfRange, totalParameter, rangeParameter));
  }

  public (long index, long count) InRangeRev(long total, (long index, long count) range,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total", [CallerArgumentExpression("range")] string? rangeParameter = "range")
  {
    Between(total, 0, int.MaxValue, valueParameter: totalParameter);

    return range.index >= 0L && range.index < total ? range.count >= 0L && range.count <= range.index + 1 ? range :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeCountOutOfRange, totalParameter, rangeParameter)) :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeIndexOutOfRange, totalParameter, rangeParameter));
  }

  public (long index, long count) InRangeBasisOut(long total, (long index, long count) range, long basis,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total", [CallerArgumentExpression("range")] string? rangeParameter = "range")
  {
    NonNegative(basis);
    Between(total, 0L, int.MaxValue - basis, valueParameter: totalParameter);

    return range.index >= basis && range.index <= basis + total ? range.count >= 0L && range.count <= basis + total - range.index ? range :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeCountOutOfRange, totalParameter, rangeParameter)) :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeIndexOutOfRange, totalParameter, rangeParameter));
  }

  public (long index, long count) InRangeBasisIn(long total, (long index, long count) range, long basis,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total", [CallerArgumentExpression("range")] string? rangeParameter = "range")
  {
    NonNegative(basis);
    Between(total, 0L, int.MaxValue - basis, valueParameter: totalParameter);

    return range.index >= basis && range.index < basis + total ? range.count >= 0L && range.count <= basis + total - range.index ? range :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeCountOutOfRange, totalParameter, rangeParameter)) :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeIndexOutOfRange, totalParameter, rangeParameter));
  }

  public (long index, long count) InRangeBasisRev(long total, (long index, long count) range, long basis,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total", [CallerArgumentExpression("range")] string? rangeParameter = "range")
  {
    NonNegative(basis);
    Between(total, 0L, int.MaxValue - basis, valueParameter: totalParameter);

    return range.index >= basis && range.index < basis + total ? range.count >= 0L && range.count <= range.index + 1 - basis ? range :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeCountOutOfRange, totalParameter, rangeParameter)) :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeIndexOutOfRange, totalParameter, rangeParameter));
  }

  public long InLimitsOut(long total, long count, long basis = 0L, long limit = long.MaxValue,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total",
    [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    NonNegative(basis);
    GreaterThanOrEqual(limit, basis);
    Between(total, 0, limit - basis, valueParameter: totalParameter);

    return count >= 0L && count <= limit - (basis + total) ? count :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, totalParameter, countParameter));
  }

  public (long index, long count) InLimitsIn(long total, long index, long count, long basis = 0L, long limit = long.MaxValue,
    string? message = null, [CallerArgumentExpression("total")] string? totalParameter = "total",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    NonNegative(basis);
    GreaterThanOrEqual(limit, basis);
    Between(total, 0, limit - basis, valueParameter: totalParameter);

    return index >= basis && index <= basis + total ? count >= 0L && count <= limit - (basis + total) ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, totalParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, totalParameter, indexParameter));
  }

  #endregion
  #region Collection

  public int InRangeOut(ICollection collection, int index,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection", [CallerArgumentExpression("index")] string? indexParameter = "index")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    return index >= 0 && index <= total ? index :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, collectionParameter, indexParameter));
  }

  public int InRangeIn(ICollection collection, int index,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection", [CallerArgumentExpression("index")] string? indexParameter = "index")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    return index >= 0 && index < total ? index :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, collectionParameter, indexParameter));
  }

  public (int index, int count) InRangeOut(ICollection collection, int index, int count,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    return index >= 0 && index <= total ? count >= 0 && count <= total - index ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, collectionParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, collectionParameter, indexParameter));
  }

  public (int index, int count) InRangeIn(ICollection collection, int index, int count,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    return index >= 0 && index < total ? count >= 0 && count <= total - index ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, collectionParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, collectionParameter, indexParameter));
  }

  public (int index, int count) InRangeRev(ICollection collection, int index, int count,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    return index >= 0 && index < total ? count >= 0 && count <= index + 1 ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, collectionParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, collectionParameter, indexParameter));
  }

  public (int index, int count) InRangeOut(ICollection collection, (int index, int count) range,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection", [CallerArgumentExpression("range")] string? rangeParameter = "range")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    return range.index >= 0 && range.index <= total ? range.count >= 0 && range.count <= total - range.index ? range :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeCountOutOfRange, collectionParameter, rangeParameter)) :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeIndexOutOfRange, collectionParameter, rangeParameter));
  }

  public (int index, int count) InRangeIn(ICollection collection, (int index, int count) range,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection", [CallerArgumentExpression("range")] string? rangeParameter = "range")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    return range.index >= 0 && range.index < total ? range.count >= 0 && range.count <= total - range.index ? range :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeCountOutOfRange, collectionParameter, rangeParameter)) :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeIndexOutOfRange, collectionParameter, rangeParameter));
  }

  public (int index, int count) InRangeRev(ICollection collection, (int index, int count) range,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection", [CallerArgumentExpression("range")] string? rangeParameter = "range")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    return range.index >= 0 && range.index < total ? range.count >= 0 && range.count <= range.index + 1 ? range :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeCountOutOfRange, collectionParameter, rangeParameter)) :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeIndexOutOfRange, collectionParameter, rangeParameter));
  }

  public int InLimitsOut(ICollection collection, int count, int limit = int.MaxValue,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    InRange(limit, limit >= total);

    return count >= 0 && count <= limit - total ? count :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, collectionParameter, countParameter));
  }

  public (int index, int count) InLimitsIn(ICollection collection, int index, int count, int limit = int.MaxValue,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    InRange(limit, limit >= total);

    return index >= 0 && index <= total ? count >= 0 && count <= limit - total ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, collectionParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, collectionParameter, indexParameter));
  }

  public int InRangeOut<T>(ICollection<T> collection, int index,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection", [CallerArgumentExpression("index")] string? indexParameter = "index")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    return index >= 0 && index <= total ? index :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, collectionParameter, indexParameter));
  }

  public int InRangeIn<T>(ICollection<T> collection, int index,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection", [CallerArgumentExpression("index")] string? indexParameter = "index")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    return index >= 0 && index < total ? index :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, collectionParameter, indexParameter));
  }

  public (int index, int count) InRangeOut<T>(ICollection<T> collection, int index, int count,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    return index >= 0 && index <= total ? count >= 0 && count <= total - index ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, collectionParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, collectionParameter, indexParameter));
  }

  public (int index, int count) InRangeIn<T>(ICollection<T> collection, int index, int count,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    return index >= 0 && index < total ? count >= 0 && count <= total - index ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, collectionParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, collectionParameter, indexParameter));
  }

  public (int index, int count) InRangeRev<T>(ICollection<T> collection, int index, int count,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    return index >= 0 && index < total ? count >= 0 && count <= index + 1 ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, collectionParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, collectionParameter, indexParameter));
  }

  public (int index, int count) InRangeOut<T>(ICollection<T> collection, (int index, int count) range,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection", [CallerArgumentExpression("range")] string? rangeParameter = "range")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    return range.index >= 0 && range.index <= total ? range.count >= 0 && range.count <= total - range.index ? range :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeCountOutOfRange, collectionParameter, rangeParameter)) :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeIndexOutOfRange, collectionParameter, rangeParameter));
  }

  public (int index, int count) InRangeIn<T>(ICollection<T> collection, (int index, int count) range,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection", [CallerArgumentExpression("range")] string? rangeParameter = "range")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    return range.index >= 0 && range.index < total ? range.count >= 0 && range.count <= total - range.index ? range :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeCountOutOfRange, collectionParameter, rangeParameter)) :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeIndexOutOfRange, collectionParameter, rangeParameter));
  }

  public (int index, int count) InRangeRev<T>(ICollection<T> collection, (int index, int count) range,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection", [CallerArgumentExpression("range")] string? rangeParameter = "range")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    return range.index >= 0 && range.index < total ? range.count >= 0 && range.count <= range.index + 1 ? range :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeCountOutOfRange, collectionParameter, rangeParameter)) :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeIndexOutOfRange, collectionParameter, rangeParameter));
  }

  public int InLimitsOut<T>(ICollection<T> collection, int count, int limit = int.MaxValue,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    InRange(limit, limit >= total);

    return count >= 0 && count <= limit - total ? count :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, collectionParameter, countParameter));
  }

  public (int index, int count) InLimitsIn<T>(ICollection<T> collection, int index, int count, int limit = int.MaxValue,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    InRange(limit, limit >= total);

    return index >= 0 && index <= total ? count >= 0 && count <= limit - total ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, collectionParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, collectionParameter, indexParameter));
  }

  public int InRangeOut<T>(IReadOnlyCollection<T> collection, int index,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection", [CallerArgumentExpression("index")] string? indexParameter = "index")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    return index >= 0 && index <= total ? index :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, collectionParameter, indexParameter));
  }

  public int InRangeIn<T>(IReadOnlyCollection<T> collection, int index,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection", [CallerArgumentExpression("index")] string? indexParameter = "index")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    return index >= 0 && index < total ? index :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, collectionParameter, indexParameter));
  }

  public (int index, int count) InRangeOut<T>(IReadOnlyCollection<T> collection, int index, int count,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    return index >= 0 && index <= total ? count >= 0 && count <= total - index ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, collectionParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, collectionParameter, indexParameter));
  }

  public (int index, int count) InRangeIn<T>(IReadOnlyCollection<T> collection, int index, int count,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    return index >= 0 && index < total ? count >= 0 && count <= total - index ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, collectionParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, collectionParameter, indexParameter));
  }

  public (int index, int count) InRangeRev<T>(IReadOnlyCollection<T> collection, int index, int count,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    return index >= 0 && index < total ? count >= 0 && count <= index + 1 ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, collectionParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, collectionParameter, indexParameter));
  }

  public (int index, int count) InRangeOut<T>(IReadOnlyCollection<T> collection, (int index, int count) range,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection", [CallerArgumentExpression("range")] string? rangeParameter = "range")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    return range.index >= 0 && range.index <= total ? range.count >= 0 && range.count <= total - range.index ? range :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeCountOutOfRange, collectionParameter, rangeParameter)) :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeIndexOutOfRange, collectionParameter, rangeParameter));
  }

  public (int index, int count) InRangeIn<T>(IReadOnlyCollection<T> collection, (int index, int count) range,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection", [CallerArgumentExpression("range")] string? rangeParameter = "range")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    return range.index >= 0 && range.index < total ? range.count >= 0 && range.count <= total - range.index ? range :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeCountOutOfRange, collectionParameter, rangeParameter)) :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeIndexOutOfRange, collectionParameter, rangeParameter));
  }

  public (int index, int count) InRangeRev<T>(IReadOnlyCollection<T> collection, (int index, int count) range,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection", [CallerArgumentExpression("range")] string? rangeParameter = "range")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    return range.index >= 0 && range.index < total ? range.count >= 0 && range.count <= range.index + 1 ? range :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeCountOutOfRange, collectionParameter, rangeParameter)) :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeIndexOutOfRange, collectionParameter, rangeParameter));
  }

  public int InLimitsOut<T>(IReadOnlyCollection<T> collection, int count, int limit = int.MaxValue,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    InRange(limit, limit >= total);

    return count >= 0 && count <= limit - total ? count :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, collectionParameter, countParameter));
  }

  public (int index, int count) InLimitsIn<T>(IReadOnlyCollection<T> collection, int index, int count, int limit = int.MaxValue,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    var total = NotNull(collection, valueParameter: collectionParameter).Count;
    InRange(limit, limit >= total);

    return index >= 0 && index <= total ? count >= 0 && count <= limit - total ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, collectionParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, collectionParameter, indexParameter));
  }

  #endregion
  #region Array

  public int InRangeOut<T>(T[] array, int index,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "array", [CallerArgumentExpression("index")] string? indexParameter = "index")
  {
    var total = NotNull(array, valueParameter: arrayParameter).Length;
    return index >= 0 && index <= total ? index :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, arrayParameter, indexParameter));
  }

  public int InRangeIn<T>(T[] array, int index,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "array", [CallerArgumentExpression("index")] string? indexParameter = "index")
  {
    var total = NotNull(array, valueParameter: arrayParameter).Length;
    return index >= 0 && index < total ? index :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, arrayParameter, indexParameter));
  }

  public (int index, int count) InRangeOut<T>(T[] array, int index, int count,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "array",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    var total = NotNull(array, valueParameter: arrayParameter).Length;
    return index >= 0 && index <= total ? count >= 0 && count <= total - index ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, arrayParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, arrayParameter, indexParameter));
  }

  public (int index, int count) InRangeIn<T>(T[] array, int index, int count,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "array",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    var total = NotNull(array, valueParameter: arrayParameter).Length;
    return index >= 0 && index < total ? count >= 0 && count <= total - index ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, arrayParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, arrayParameter, indexParameter));
  }

  public (int index, int count) InRangeRev<T>(T[] array, int index, int count,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "array",
    [CallerArgumentExpression("index")] string? indexParameter = "index", [CallerArgumentExpression("count")] string? countParameter = "count")
  {
    var total = NotNull(array, valueParameter: arrayParameter).Length;
    return index >= 0 && index < total ? count >= 0 && count <= index + 1 ? (index, count) :
      throw new ArgumentOutOfRangeException(countParameter, message ?? FormatString(ArgumentMessage.CountOutOfRange, arrayParameter, countParameter, indexParameter)) :
      throw new ArgumentOutOfRangeException(indexParameter, message ?? FormatString(ArgumentMessage.IndexOutOfRange, arrayParameter, indexParameter));
  }

  public (int index, int count) InRangeOut<T>(T[] array, (int index, int count) range,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "array", [CallerArgumentExpression("range")] string? rangeParameter = "range")
  {
    var total = NotNull(array, valueParameter: arrayParameter).Length;
    return range.index >= 0 && range.index <= total ? range.count >= 0 && range.count <= total - range.index ? range :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeCountOutOfRange, arrayParameter, rangeParameter)) :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeIndexOutOfRange, arrayParameter, rangeParameter));
  }

  public (int index, int count) InRangeIn<T>(T[] array, (int index, int count) range,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "array", [CallerArgumentExpression("range")] string? rangeParameter = "range")
  {
    var total = NotNull(array, valueParameter: arrayParameter).Length;
    return range.index >= 0 && range.index < total ? range.count >= 0 && range.count <= total - range.index ? range :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeCountOutOfRange, arrayParameter, rangeParameter)) :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeIndexOutOfRange, arrayParameter, rangeParameter));
  }

  public (int index, int count) InRangeRev<T>(T[] array, (int index, int count) range,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "array", [CallerArgumentExpression("range")] string? rangeParameter = "range")
  {
    var total = NotNull(array, valueParameter: arrayParameter).Length;
    return range.index >= 0 && range.index < total ? range.count >= 0 && range.count <= range.index + 1 ? range :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeCountOutOfRange, arrayParameter, rangeParameter)) :
      throw new ArgumentOutOfRangeException(rangeParameter, message ?? FormatString(ArgumentMessage.RangeIndexOutOfRange, arrayParameter, rangeParameter));
  }

  public int[] InRangeOut(Array array, int[] indices, bool zeroBased,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "array", [CallerArgumentExpression("indices")] string? indicesParameter = "indices")
  {
    var dimensions = NotNull(array, valueParameter: arrayParameter).GetRegularArrayDimensions(zeroBased);
    MatchCollection(indices,
      total => total == array.Rank,
      (index, dimension) => index >= (zeroBased ? 0 : dimensions[dimension].LowerBound)
        && index <= (zeroBased ? 0 : dimensions[dimension].LowerBound) + dimensions[dimension].Length,
      false, message, message, indicesParameter);
    return indices;
  }

  public int[] InRangeIn(Array array, int[] indices, bool zeroBased,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "array", [CallerArgumentExpression("indices")] string? indicesParameter = "indices")
  {
    var dimensions = NotNull(array, valueParameter: arrayParameter).GetRegularArrayDimensions(zeroBased);
    MatchCollection(indices,
      total => total == array.Rank,
      (index, dimension) => index >= (zeroBased ? 0 : dimensions[dimension].LowerBound)
        && index < (zeroBased ? 0 : dimensions[dimension].LowerBound) + dimensions[dimension].Length,
      false, message, message, indicesParameter);
    return indices;
  }

  public (int index, int count)[] InRangeOut(Array array, int[] indices, int[] counts, bool zeroBased,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "array",
    [CallerArgumentExpression("indices")] string? indicesParameter = "indices", [CallerArgumentExpression("counts")] string? countsParameter = "counts")
  {
    var dimensions = NotNull(array, valueParameter: arrayParameter).GetRegularArrayDimensions(zeroBased);
    return MatchCoupled(indices, counts,
      count => count == array.Rank,
      (index, dimension) => index >= dimensions[dimension].LowerBound && index <= dimensions[dimension].LowerBound + dimensions[dimension].Length,
      (count, dimension) => count >= 0,
      ((int index, int count) range, int dimension) => range.count <= dimensions[dimension].LowerBound + dimensions[dimension].Length - range.index,
      false, message, message, message, message, message, indicesParameter, countsParameter)
      .Select(range => (range.x, range.y))
      .ToArray();
  }

  public (int index, int count)[] InRangeIn(Array array, int[] indices, int[] counts, bool zeroBased,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "array",
    [CallerArgumentExpression("indices")] string? indicesParameter = "indices", [CallerArgumentExpression("counts")] string? countsParameter = "counts")
  {
    var dimensions = NotNull(array, valueParameter: arrayParameter).GetRegularArrayDimensions(zeroBased);
    return MatchCoupled(indices, counts,
      count => count == array.Rank,
      (index, dimension) => index >= dimensions[dimension].LowerBound && index < dimensions[dimension].LowerBound + dimensions[dimension].Length,
      (count, dimension) => count >= 0,
      ((int index, int count) range, int dimension) => range.count <= dimensions[dimension].LowerBound + dimensions[dimension].Length - range.index,
      false, message, message, message, message, message, indicesParameter, countsParameter)
      .Select(range => (range.x, range.y))
      .ToArray();
  }

  public (int index, int count)[] InRangeOut(Array array, (int index, int count)[] ranges, bool zeroBased,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "array", [CallerArgumentExpression("ranges")] string? rangesParameter = "ranges")
  {
    NotNull(array, valueParameter: arrayParameter);
    var dimensions = array.GetRegularArrayDimensions(zeroBased);
    MatchCollection(ranges,
      total => total == array.Rank,
      (range, dimension) => range.index >= (zeroBased ? 0 : dimensions[dimension].LowerBound)
        && range.index <= (zeroBased ? 0 : dimensions[dimension].LowerBound) + dimensions[dimension].Length
        && range.count >= 0 && range.count <= dimensions[dimension].LowerBound + dimensions[dimension].Length - range.index,
      false, message, message, rangesParameter);
    return ranges;
  }

  public (int index, int count)[] InRangeIn(Array array, (int index, int count)[] ranges, bool zeroBased,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "array", [CallerArgumentExpression("ranges")] string? rangesParameter = "ranges")
  {
    NotNull(array, valueParameter: arrayParameter);
    var dimensions = array.GetRegularArrayDimensions(zeroBased);
    MatchCollection(ranges,
      total => total == array.Rank,
      (range, dimension) => range.index >= (zeroBased ? 0 : dimensions[dimension].LowerBound)
        && range.index < (zeroBased ? 0 : dimensions[dimension].LowerBound) + dimensions[dimension].Length
        && range.count >= 0 && range.count <= dimensions[dimension].LowerBound + dimensions[dimension].Length - range.index,
      false, message, message, rangesParameter);
    return ranges;
  }

  public long[] InRangeOut(Array array, long[] indices,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "array", [CallerArgumentExpression("indices")] string? indicesParameter = "indices")
  {
    var lengths = NotNull(array, valueParameter: arrayParameter).GetRegularArrayLongLengths();
    MatchCollection(indices,
      total => total == array.Rank,
      (index, dimension) => index >= 0 && index <= lengths[dimension],
      false, message, message, indicesParameter);
    return indices;
  }

  public long[] InRangeIn(Array array, long[] indices,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "array", [CallerArgumentExpression("indices")] string? indicesParameter = "indices")
  {
    var lengths = NotNull(array, valueParameter: arrayParameter).GetRegularArrayLongLengths();
    MatchCollection(indices,
      total => total == array.Rank,
      (index, dimension) => index >= 0 && index < lengths[dimension],
      false, message, message, indicesParameter);
    return indices;
  }

  public (long index, long count)[] InRangeOut(Array array, long[] indices, long[] counts,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "array",
    [CallerArgumentExpression("indices")] string? indicesParameter = "indices", [CallerArgumentExpression("counts")] string? countsParameter = "counts")
  {
    NotNull(array, valueParameter: arrayParameter);
    var lengths = array.GetRegularArrayLongLengths();
    return MatchCoupled(indices, counts,
      count => count == array.Rank,
      (index, dimension) => index >= 0L && index <= lengths[dimension],
      (count, dimension) => count >= 0L,
      ((long index, long count) range, int dimension) => range.count <= lengths[dimension] - range.index,
      false, message, message, message, message, message, indicesParameter, countsParameter)
      .Select(range => (range.x, range.y))
      .ToArray();
  }

  public (long index, long count)[] InRangeIn(Array array, long[] indices, long[] counts,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "array",
    [CallerArgumentExpression("indices")] string? indicesParameter = "indices", [CallerArgumentExpression("counts")] string? countsParameter = "counts")
  {
    NotNull(array, valueParameter: arrayParameter);
    var lengths = array.GetRegularArrayLongLengths();
    return MatchCoupled(indices, counts,
      count => count == array.Rank,
      (index, dimension) => index >= 0L && index < lengths[dimension],
      (count, dimension) => count >= 0L,
      ((long index, long count) range, int dimension) => range.count <= lengths[dimension] - range.index,
      false, message, message, message, message, message, indicesParameter, countsParameter)
      .Select(range => (range.x, range.y))
      .ToArray();
  }

  public (long index, long count)[] InRangeOut(Array array, (long index, long count)[] ranges,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "array", [CallerArgumentExpression("ranges")] string? rangesParameter = "ranges")
  {
    NotNull(array, valueParameter: arrayParameter);
    var lengths = array.GetRegularArrayLongLengths();
    MatchCollection(ranges,
      total => total == array.Rank,
      (range, dimension) => range.index >= 0L && range.index <= lengths[dimension] && range.count >= 0L && range.count <= lengths[dimension] - range.index,
      false, message, message, rangesParameter);
    return ranges;
  }

  public (long index, long count)[] InRangeIn(Array array, (long index, long count)[] ranges,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "array", [CallerArgumentExpression("ranges")] string? rangesParameter = "ranges")
  {
    NotNull(array, valueParameter: arrayParameter);
    var lengths = array.GetRegularArrayLongLengths();
    MatchCollection(ranges,
      total => total == array.Rank,
      (range, dimension) => range.index >= 0L && range.index < lengths[dimension] && range.count >= 0L && range.count <= lengths[dimension] - range.index,
      false, message, message, rangesParameter);
    return ranges;
  }

  #endregion
  #endregion
  #region Array validation
  #region Internal methods
  #region Enumeration methods

  private static IEnumerable EnumerateArrayCore(Array array, bool zeroBased,
    Func<object?, int, int[], int, NavigateCommand>? itemNavigator, Action<int, int, bool>? totalValidator)
  {
    int indexIn = 0, indexOut = 0;
    var stopped = false;
    var arrayInfo = new RegularArrayInfo(array.GetRegularArrayDimensions());
    var arrayIndex = new ArrayIndex(arrayInfo, zeroBased);
    var indices = new int[arrayInfo.Rank];
    arrayIndex.FlatIndex = indexIn;
    for (var success = array.Length > 0; !stopped && success; success = !arrayIndex.Inc(), indexIn++)
    {
      var item = arrayIndex.GetValue(array);
      arrayIndex.GetDimIndices(indices);
      var navigateCommand = itemNavigator?.Invoke(item, indexIn, indices, indexOut) ?? NavigateCommand.None;
      yield return item;
      if ((navigateCommand & NavigateCommand.Skip) == 0)
        indexOut++;
      if ((navigateCommand & NavigateCommand.Stop) != 0)
        stopped = true;
    }
    totalValidator?.Invoke(indexIn, indexOut, stopped);
  }

  private static IEnumerable<T> EnumerateArrayCore<T>(Array array, bool zeroBased,
    Func<T, int, int[], int, NavigateCommand>? itemNavigator, Action<int, int, bool>? totalValidator)
  {
    var stopped = false;
    int indexIn = 0, indexOut = 0;
    var arrayInfo = new RegularArrayInfo(array.GetRegularArrayDimensions());
    var arrayIndex = new ArrayIndex(arrayInfo, zeroBased);
    var indices = new int[arrayInfo.Rank];
    arrayIndex.FlatIndex = indexIn;
    for (var success = array.Length > 0; !stopped && success; success = !arrayIndex.Inc(), indexIn++)
    {
      var item = arrayIndex.GetValue<T>(array);
      arrayIndex.GetDimIndices(indices);
      var navigateCommand = itemNavigator?.Invoke(item, indexIn, indices, indexOut) ?? NavigateCommand.None;
      yield return item;
      if ((navigateCommand & NavigateCommand.Skip) == 0)
        indexOut++;
      if ((navigateCommand & NavigateCommand.Stop) != 0)
        stopped = true;
    }
    totalValidator?.Invoke(indexIn, indexOut, stopped);
  }

  private static IEnumerable EnumerateArrayCore(Array array,
    Func<object?, long, long[], long, NavigateCommand>? itemNavigator, Action<long, long, bool>? totalValidator)
  {
    long indexIn = 0L, indexOut = 0L;
    var stopped = false;
    var arrayInfo = new RegularArrayLongInfo(array.GetRegularArrayLongDimensions());
    var arrayIndex = new ArrayLongIndex(arrayInfo);
    var indices = new long[arrayInfo.Rank];
    arrayIndex.FlatIndex = indexIn;
    for (var success = array.LongLength > 0L; !stopped && success; success = !arrayIndex.Inc(), indexIn++)
    {
      var item = arrayIndex.GetValue(array);
      arrayIndex.GetDimIndices(indices);
      var navigateCommand = itemNavigator?.Invoke(item, indexIn, indices, indexOut) ?? NavigateCommand.None;
      yield return item;
      if ((navigateCommand & NavigateCommand.Skip) == 0)
        indexOut++;
      if ((navigateCommand & NavigateCommand.Stop) != 0)
        stopped = true;
    }
    totalValidator?.Invoke(indexIn, indexOut, stopped);
  }

  private static IEnumerable<T> EnumerateArrayCore<T>(Array array,
    Func<T, long, long[], long, NavigateCommand>? itemNavigator, Action<long, long, bool>? totalValidator)
  {
    var stopped = false;
    long indexIn = 0, indexOut = 0;
    var arrayInfo = new RegularArrayLongInfo(array.GetRegularArrayLongDimensions());
    var arrayIndex = new ArrayLongIndex(arrayInfo);
    var indices = new long[arrayInfo.Rank];
    arrayIndex.FlatIndex = indexIn;
    for (var success = array.LongLength > 0L; !stopped && success; success = !arrayIndex.Inc(), indexIn++)
    {
      var item = arrayIndex.GetValue<T>(array);
      arrayIndex.GetDimIndices(indices);
      var navigateCommand = itemNavigator?.Invoke(item, indexIn, indices, indexOut) ?? NavigateCommand.None;
      yield return item;
      if ((navigateCommand & NavigateCommand.Skip) == 0)
        indexOut++;
      if ((navigateCommand & NavigateCommand.Stop) != 0)
        stopped = true;
    }
    totalValidator?.Invoke(indexIn, indexOut, stopped);
  }

  #endregion
  #region Validation methods

  private static Array ValidateArrayCore(Array array,
    Action<int>? rankValidator, Action<int>? lengthValidator, Action<int[]>? dimLengthsValidator,
    Func<object?, int, int[], int, NavigateCommand>? itemValidator, Action<int, int, bool>? totalValidator)
  {
    rankValidator?.Invoke(array.Rank);
    lengthValidator?.Invoke(array.Length);
    dimLengthsValidator?.Invoke(array.GetRegularArrayLengths());
    if (itemValidator is not null || totalValidator is not null)
      EnumerateArrayCore(array, true, itemValidator, totalValidator).OfType<object?>().Enumerate();
    return array;
  }

  private static Array ValidateArrayCore<T>(Array array,
    Action<int>? rankValidator, Action<int>? lengthValidator, Action<int[]>? dimLengthsValidator,
    Func<T, int, int[], int, NavigateCommand>? itemValidator, Action<int, int, bool>? totalValidator)
  {
    rankValidator?.Invoke(array.Rank);
    lengthValidator?.Invoke(array.Length);
    dimLengthsValidator?.Invoke(array.GetRegularArrayLengths());
    if (itemValidator is not null || totalValidator is not null)
      EnumerateArrayCore(array, true, itemValidator, totalValidator).Enumerate();
    return array;
  }

  private static Array ValidateArrayCore(Array array,
    Action<int>? rankValidator, Action<int>? lengthValidator, Action<ArrayDimension[]>? dimensionsValidator,
    Func<object?, int, int[], int, NavigateCommand>? itemValidator, Action<int, int, bool>? totalValidator)
  {
    rankValidator?.Invoke(array.Rank);
    lengthValidator?.Invoke(array.Length);
    dimensionsValidator?.Invoke(array.GetRegularArrayDimensions());
    if (itemValidator is not null || totalValidator is not null)
      EnumerateArrayCore(array, false, itemValidator, totalValidator).OfType<object?>().Enumerate();
    return array;
  }

  private static Array ValidateArrayCore<T>(Array array,
    Action<int>? rankValidator, Action<int>? lengthValidator, Action<ArrayDimension[]>? dimensionsValidator,
    Func<T, int, int[], int, NavigateCommand>? itemValidator, Action<int, int, bool>? totalValidator)
  {
    rankValidator?.Invoke(array.Rank);
    lengthValidator?.Invoke(array.Length);
    dimensionsValidator?.Invoke(array.GetRegularArrayDimensions());
    if (itemValidator is not null || totalValidator is not null)
      EnumerateArrayCore(array, false, itemValidator, totalValidator).Enumerate();
    return array;
  }

  private static Array ValidateArrayCore(Array array,
    Action<int>? rankValidator, Action<long>? lengthValidator, Action<long[]>? dimLengthsValidator,
    Func<object?, long, long[], long, NavigateCommand>? itemValidator, Action<long, long, bool>? totalValidator)
  {
    rankValidator?.Invoke(array.Rank);
    lengthValidator?.Invoke(array.Length);
    dimLengthsValidator?.Invoke(array.GetRegularArrayLongLengths());
    if (itemValidator is not null || totalValidator is not null)
      EnumerateArrayCore(array, itemValidator, totalValidator).OfType<object?>().Enumerate();
    return array;
  }

  private static Array ValidateArrayCore<T>(Array array,
    Action<int>? rankValidator, Action<long>? lengthValidator, Action<long[]>? dimLengthsValidator,
    Func<T, long, long[], long, NavigateCommand>? itemValidator, Action<long, long, bool>? totalValidator)
  {
    rankValidator?.Invoke(array.Rank);
    lengthValidator?.Invoke(array.Length);
    dimLengthsValidator?.Invoke(array.GetRegularArrayLongLengths());
    if (itemValidator is not null || totalValidator is not null)
      EnumerateArrayCore(array, itemValidator, totalValidator).Enumerate();
    return array;
  }

  #endregion
  #region Validation delegates

  private static bool IsOneDimensionalArray(int rank)
    => rank == 1;

  private static bool IsMultiDimensionalArray(int rank)
    => rank > 1;

  private static bool IsEmptyArray(long length)
    => length == 0L;

  private static bool IsNotEmptyArray(long length)
    => length > 0L;

  private static Action<int> ArrayRankValidator(Action<int> rankValidator, string? rankMessage, string? arrayParameter)
    => rank =>
    {
      try
      {
        rankValidator(rank);
      }
      catch (Exception ex)
      {
        throw new ArgumentException(rankMessage, arrayParameter, ex);
      }
    };

  private static Action<int> ArrayRankValidator(Predicate<int> rankPredicate, string? rankMessage, string? arrayParameter)
    => rank =>
    {
      if (!rankPredicate.Invoke(rank))
        throw new ArgumentException(rankMessage, arrayParameter);
    };

  private static Action<int> ArrayLengthValidator(Action<int> lengthValidator, string? lengthMessage, string? arrayParameter)
    => length =>
    {
      try
      {
        lengthValidator(length);
      }
      catch (Exception ex)
      {
        throw new ArgumentException(lengthMessage, arrayParameter, ex);
      }
    };

  private static Action<int> ArrayLengthValidator(Predicate<int> lengthPredicate, string? lengthMessage, string? arrayParameter)
    => length =>
    {
      if (!lengthPredicate.Invoke(length))
        throw new ArgumentException(lengthMessage, arrayParameter);
    };

  private static Action<long> ArrayLengthValidator(Action<long> lengthValidator, string? lengthMessage, string? arrayParameter)
    => length =>
    {
      try
      {
        lengthValidator(length);
      }
      catch (Exception ex)
      {
        throw new ArgumentException(lengthMessage, arrayParameter, ex);
      }
    };

  private static Action<long> ArrayLengthValidator(Predicate<long> lengthPredicate, string? lengthMessage, string? arrayParameter)
    => length =>
    {
      if (!lengthPredicate.Invoke(length))
        throw new ArgumentException(lengthMessage, arrayParameter);
    };

  private static Action<int[]> ArrayDimLengthsValidator(Action<int[]> dimLengthsValidator, string? dimLengthsMessage, string? arrayParameter)
    => dimLengths =>
    {
      try
      {
        dimLengthsValidator(dimLengths);
      }
      catch (Exception ex)
      {
        throw new ArgumentException(dimLengthsMessage, arrayParameter, ex);
      }
    };

  private static Action<int[]> ArrayDimLengthsValidator(Predicate<int[]> dimLengthsPredicate, string? dimLengthsMessage, string? arrayParameter)
    => dimLengths =>
    {
      if (!dimLengthsPredicate.Invoke(dimLengths))
        throw new ArgumentException(dimLengthsMessage, arrayParameter);
    };

  private static Action<ArrayDimension[]> ArrayDimensionsValidator(Action<ArrayDimension[]> dimensionsValidator, string? dimensionsMessage, string? arrayParameter)
    => dimensions =>
    {
      try
      {
        dimensionsValidator(dimensions);
      }
      catch (Exception ex)
      {
        throw new ArgumentException(dimensionsMessage, arrayParameter, ex);
      }
    };

  private static Action<ArrayDimension[]> ArrayDimensionsValidator(Predicate<ArrayDimension[]> dimensionsPredicate, string? dimensionsMessage, string? arrayParameter)
    => dimensions =>
    {
      if (!dimensionsPredicate.Invoke(dimensions))
        throw new ArgumentException(dimensionsMessage, arrayParameter);
    };

  private static Action<long[]> ArrayDimLengthsValidator(Action<long[]> dimLengthsValidator, string? dimLengthsMessage, string? arrayParameter)
    => dimLengths =>
    {
      try
      {
        dimLengthsValidator(dimLengths);
      }
      catch (Exception ex)
      {
        throw new ArgumentException(dimLengthsMessage, arrayParameter, ex);
      }
    };

  private static Action<long[]> ArrayDimLengthsValidator(Predicate<long[]> dimLengthsPredicate, string? dimLengthsMessage, string? arrayParameter)
    => dimLengths =>
    {
      if (!dimLengthsPredicate.Invoke(dimLengths))
        throw new ArgumentException(dimLengthsMessage, arrayParameter);
    };

  private static Func<T, int, int[], int, NavigateCommand> ArrayElementNavigator<T>(ElementDimAction<T> validator, string? message, string? arrayParameter)
    => (item, index, indices, indexOut) =>
    {
      try
      {
        validator(item, index, indices);
        return NavigateCommand.None;
      }
      catch (Exception ex)
      {
        throw new ArgumentRegularArrayElementException(arrayParameter, message, ex, index, indices);
      }
    };

  private static Func<T, int, int[], int, NavigateCommand> ArrayElementNavigator<T>(ElementDimPredicate<T> predicate, string? message, string? arrayParameter)
    => (item, index, indices, indexOut) =>
    {
      if (!predicate(item, index, indices))
        throw new ArgumentRegularArrayElementException(arrayParameter, message, index, indices);
      return NavigateCommand.None;
    };

  private static Func<T, long, long[], long, NavigateCommand> ArrayElementNavigator<T>(ElementDimLongAction<T> validator, string? message, string? arrayParameter)
    => (item, index, indices, indexOut) =>
    {
      try
      {
        validator(item, index, indices);
        return NavigateCommand.None;
      }
      catch (Exception ex)
      {
        throw new ArgumentRegularArrayLongElementException(arrayParameter, message, ex, index, indices);
      }
    };

  private static Func<T, long, long[], long, NavigateCommand> ArrayElementNavigator<T>(ElementDimLongPredicate<T> predicate, string? message, string? arrayParameter)
    => (item, index, indices, indexOut) =>
    {
      if (!predicate(item, index, indices))
        throw new ArgumentRegularArrayLongElementException(arrayParameter, message, index, indices);
      return NavigateCommand.None;
    };

  private static Action<int, int, bool>? ArrayTotalValidator(Action<int, int, bool> validator, string? message, string? arrayParameter)
    => (total, count, stopped) =>
    {
      try
      {
        validator(total, count, stopped);
      }
      catch (Exception ex)
      {
        throw new ArgumentException(message, arrayParameter, ex);
      }
    };

  private static Action<long, long, bool>? ArrayTotalValidator(Action<long, long, bool> validator, string? message, string? arrayParameter)
    => (total, count, stopped) =>
    {
      try
      {
        validator(total, count, stopped);
      }
      catch (Exception ex)
      {
        throw new ArgumentException(message, arrayParameter, ex);
      }
    };

  private static Action<int, int, bool>? ArrayTotalValidator(Func<int, int, bool, bool> predicate, string? message, string? arrayParameter)
    => (total, count, stopped) =>
    {
      if (!predicate(total, count, stopped))
        throw new ArgumentException(message, arrayParameter);
    };

  private static Action<long, long, bool>? ArrayTotalValidator(Func<long, long, bool, bool> predicate, string? message, string? arrayParameter)
    => (total, count, stopped) =>
    {
      if (!predicate(total, count, stopped))
        throw new ArgumentException(message, arrayParameter);
    };

  #endregion
  #endregion
  #region Public methods
  #region Validation methods

  public Array ValidateRank(Array array, Action<int> rankValidator,
    string? rankMessage = null, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => ValidateArrayCore(NotNull(array),
      ArrayRankValidator(NotNull(rankValidator), rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      null, default(Action<int[]>), null, null);

  public Array ValidateLength(Array array, Action<int> lengthValidator,
    string? lengthMessage = null, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => ValidateArrayCore(NotNull(array), null,
      ArrayLengthValidator(NotNull(lengthValidator), lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      default(Action<ArrayDimension[]>), null, null);

  public Array ValidateLength(Array array, Action<long> lengthValidator,
    string? lengthMessage = null, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => ValidateArrayCore(NotNull(array), null,
      ArrayLengthValidator(NotNull(lengthValidator), lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      null, null, null);

  public Array ValidateDimLengths(Array array, Action<int[]> dimLengthsValidator,
    string? dimLengthsMessage = null, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => ValidateArrayCore(NotNull(array), null, null,
      ArrayDimLengthsValidator(NotNull(dimLengthsValidator), dimLengthsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      null, null);

  public Array ValidateDimLengths(Array array, Action<long[]> dimLengthsValidator,
    string? dimLengthsMessage = null, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => ValidateArrayCore(NotNull(array), null, null,
      ArrayDimLengthsValidator(NotNull(dimLengthsValidator), dimLengthsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      null, null);

  public Array ValidateDimensions(Array array, Action<ArrayDimension[]> dimensionsValidator,
    string? dimensionsMessage = null, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => ValidateArrayCore(NotNull(array), null, null,
      ArrayDimensionsValidator(NotNull(dimensionsValidator), dimensionsMessage ?? FormatString(ArgumentMessage.ArrayDimensionsAreMismatched, arrayParameter), arrayParameter),
      null, null);

  public Array ValidateElements(Array array, ElementDimAction<object?> itemValidator,
    string? itemMessage = null, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => ValidateArrayCore(NotNull(array), null, null, default(Action<ArrayDimension[]>),
      ArrayElementNavigator(NotNull(itemValidator), itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      null);

  public Array ValidateElements<T>(Array array, ElementDimAction<T> itemValidator,
    string? itemMessage = null, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => ValidateArrayCore(NotNull(array), null, null, default(Action<ArrayDimension[]>),
      ArrayElementNavigator(NotNull(itemValidator), itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      null);

  public Array ValidateElements(Array array, ElementDimLongAction<object?> itemValidator,
    string? itemMessage = null, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => ValidateArrayCore(NotNull(array), null, null, null,
      ArrayElementNavigator(NotNull(itemValidator), itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      null);

  public Array ValidateElements<T>(Array array, ElementDimLongAction<T> itemValidator,
    string? itemMessage = null, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => ValidateArrayCore(NotNull(array), null, null, null,
      ArrayElementNavigator(NotNull(itemValidator), itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      null);

  public Array ValidateArray(Array array,
    Action<int>? rankValidator, Action<int>? lengthValidator, Action<int[]>? dimLengthsValidator, ElementDimAction<object?>? itemValidator,
    string? rankMessage = null, string? lengthMessage = null, string? dimLengthsMessage = null, string? itemMessage = null,
    [CallerArgumentExpression("array")] string? arrayParameter = "array")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(rankValidator), this.DelegateOf(lengthValidator), this.DelegateOf(dimLengthsValidator), this.DelegateOf(itemValidator));

    return ValidateArrayCore(NotNull(array),
      rankValidator is null ? null : ArrayRankValidator(rankValidator, rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      lengthValidator is null ? null : ArrayLengthValidator(lengthValidator, lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      dimLengthsValidator is null ? null : ArrayDimLengthsValidator(dimLengthsValidator, dimLengthsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      itemValidator is null ? null : ArrayElementNavigator(itemValidator, itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      null);
  }

  public Array ValidateArray(Array array,
    Action<int>? rankValidator, Action<int>? lengthValidator, Action<ArrayDimension[]>? dimensionsValidator, ElementDimAction<object?>? itemValidator,
    string? rankMessage = null, string? lengthMessage = null, string? dimensionsMessage = null, string? itemMessage = null,
    [CallerArgumentExpression("array")] string? arrayParameter = "array")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(rankValidator), this.DelegateOf(lengthValidator), this.DelegateOf(dimensionsValidator), this.DelegateOf(itemValidator));

    return ValidateArrayCore(NotNull(array),
      rankValidator is null ? null : ArrayRankValidator(rankValidator, rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      lengthValidator is null ? null : ArrayLengthValidator(lengthValidator, lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      dimensionsValidator is null ? null : ArrayDimensionsValidator(dimensionsValidator, dimensionsMessage ?? FormatString(ArgumentMessage.ArrayDimensionsAreMismatched, arrayParameter), arrayParameter),
      itemValidator is null ? null : ArrayElementNavigator(itemValidator, itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      null);
  }

  public Array ValidateArray(Array array,
    Action<int>? rankValidator, Action<long>? lengthValidator, Action<long[]>? dimLengthsValidator, ElementDimLongAction<object?>? itemValidator,
    string? rankMessage = null, string? lengthMessage = null, string? dimLengthsMessage = null, string? itemMessage = null,
    [CallerArgumentExpression("array")] string? arrayParameter = "array")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(rankValidator), this.DelegateOf(lengthValidator), this.DelegateOf(dimLengthsValidator), this.DelegateOf(itemValidator));

    return ValidateArrayCore(NotNull(array),
      rankValidator is null ? null : ArrayRankValidator(rankValidator, rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      lengthValidator is null ? null : ArrayLengthValidator(lengthValidator, lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      dimLengthsValidator is null ? null : ArrayDimLengthsValidator(dimLengthsValidator, dimLengthsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      itemValidator is null ? null : ArrayElementNavigator(itemValidator, itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      null);
  }

  public Array ValidateArray(Array array,
    Action<int>? rankValidator, Action<int>? lengthValidator, Action<int[]>? dimLengthsValidator, ElementDimAction<object?>? itemValidator, Action<int, int, bool>? totalValidator,
    string? rankMessage = null, string? lengthMessage = null, string? dimLengthsMessage = null, string? itemMessage = null, string? totalMessage = null,
    [CallerArgumentExpression("array")] string? arrayParameter = "array")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(rankValidator), this.DelegateOf(lengthValidator), this.DelegateOf(dimLengthsValidator), this.DelegateOf(itemValidator), this.DelegateOf(totalValidator));

    return ValidateArrayCore(NotNull(array),
      rankValidator is null ? null : ArrayRankValidator(rankValidator, rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      lengthValidator is null ? null : ArrayLengthValidator(lengthValidator, lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      dimLengthsValidator is null ? null : ArrayDimLengthsValidator(dimLengthsValidator, dimLengthsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      itemValidator is null ? null : ArrayElementNavigator(itemValidator, itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      totalValidator is null ? null : ArrayTotalValidator(totalValidator, totalMessage, arrayParameter));
  }

  public Array ValidateArray(Array array,
    Action<int>? rankValidator, Action<int>? lengthValidator, Action<ArrayDimension[]>? dimensionsValidator, ElementDimAction<object?>? itemValidator, Action<int, int, bool>? totalValidator,
    string? rankMessage = null, string? lengthMessage = null, string? dimensionsMessage = null, string? itemMessage = null, string? totalMessage = null,
    [CallerArgumentExpression("array")] string? arrayParameter = "array")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(rankValidator), this.DelegateOf(lengthValidator), this.DelegateOf(dimensionsValidator), this.DelegateOf(itemValidator), this.DelegateOf(totalValidator));

    return ValidateArrayCore(NotNull(array),
      rankValidator is null ? null : ArrayRankValidator(rankValidator, rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      lengthValidator is null ? null : ArrayLengthValidator(lengthValidator, lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      dimensionsValidator is null ? null : ArrayDimensionsValidator(dimensionsValidator, dimensionsMessage ?? FormatString(ArgumentMessage.ArrayDimensionsAreMismatched, arrayParameter), arrayParameter),
      itemValidator is null ? null : ArrayElementNavigator(itemValidator, itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      totalValidator is null ? null : ArrayTotalValidator(totalValidator, totalMessage, arrayParameter));
  }

  public Array ValidateArray(Array array,
    Action<int>? rankValidator, Action<long>? lengthValidator, Action<long[]>? dimLengthsValidator, ElementDimLongAction<object?>? itemValidator, Action<long, long, bool>? totalValidator,
    string? rankMessage = null, string? lengthMessage = null, string? dimLengthsMessage = null, string? itemMessage = null, string? totalMessage = null,
    [CallerArgumentExpression("array")] string? arrayParameter = "array")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(rankValidator), this.DelegateOf(lengthValidator), this.DelegateOf(dimLengthsValidator), this.DelegateOf(itemValidator), this.DelegateOf(totalValidator));

    return ValidateArrayCore(NotNull(array),
      rankValidator is null ? null : ArrayRankValidator(rankValidator, rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      lengthValidator is null ? null : ArrayLengthValidator(lengthValidator, lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      dimLengthsValidator is null ? null : ArrayDimLengthsValidator(dimLengthsValidator, dimLengthsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      itemValidator is null ? null : ArrayElementNavigator(itemValidator, itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      totalValidator is null ? null : ArrayTotalValidator(totalValidator, totalMessage, arrayParameter));
  }

  public Array ValidateArray<T>(Array array,
    Action<int>? rankValidator, Action<int>? lengthValidator, Action<int[]>? dimLengthsValidator, ElementDimAction<T>? itemValidator,
    string? rankMessage = null, string? lengthMessage = null, string? dimLengthsMessage = null, string? itemMessage = null,
    [CallerArgumentExpression("array")] string? arrayParameter = "array")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(rankValidator), this.DelegateOf(lengthValidator), this.DelegateOf(dimLengthsValidator), this.DelegateOf(itemValidator));

    return ValidateArrayCore(NotNull(array),
      rankValidator is null ? null : ArrayRankValidator(rankValidator, rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      lengthValidator is null ? null : ArrayLengthValidator(lengthValidator, lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      dimLengthsValidator is null ? null : ArrayDimLengthsValidator(dimLengthsValidator, dimLengthsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      itemValidator is null ? null : ArrayElementNavigator(itemValidator, itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      null);
  }

  public Array ValidateArray<T>(Array array,
    Action<int>? rankValidator, Action<int>? lengthValidator, Action<ArrayDimension[]>? dimensionsValidator, ElementDimAction<T>? itemValidator,
    string? rankMessage = null, string? lengthMessage = null, string? dimensionsMessage = null, string? itemMessage = null,
    [CallerArgumentExpression("array")] string? arrayParameter = "array")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(rankValidator), this.DelegateOf(lengthValidator), this.DelegateOf(dimensionsValidator), this.DelegateOf(itemValidator));

    return ValidateArrayCore(NotNull(array),
      rankValidator is null ? null : ArrayRankValidator(rankValidator, rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      lengthValidator is null ? null : ArrayLengthValidator(lengthValidator, lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      dimensionsValidator is null ? null : ArrayDimensionsValidator(dimensionsValidator, dimensionsMessage ?? FormatString(ArgumentMessage.ArrayDimensionsAreMismatched, arrayParameter), arrayParameter),
      itemValidator is null ? null : ArrayElementNavigator(itemValidator, itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      null);
  }

  public Array ValidateArray<T>(Array array,
    Action<int>? rankValidator, Action<long>? lengthValidator, Action<long[]>? dimLengthsValidator, ElementDimLongAction<T>? itemValidator,
    string? rankMessage = null, string? lengthMessage = null, string? dimLengthsMessage = null, string? itemMessage = null,
    [CallerArgumentExpression("array")] string? arrayParameter = "array")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(rankValidator), this.DelegateOf(lengthValidator), this.DelegateOf(dimLengthsValidator), this.DelegateOf(itemValidator));

    return ValidateArrayCore(NotNull(array),
      rankValidator is null ? null : ArrayRankValidator(rankValidator, rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      lengthValidator is null ? null : ArrayLengthValidator(lengthValidator, lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      dimLengthsValidator is null ? null : ArrayDimLengthsValidator(dimLengthsValidator, dimLengthsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      itemValidator is null ? null : ArrayElementNavigator(itemValidator, itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      null);
  }

  public Array ValidateArray<T>(Array array,
    Action<int>? rankValidator, Action<int>? lengthValidator, Action<int[]>? dimLengthsValidator, ElementDimAction<T>? itemValidator, Action<int, int, bool>? totalValidator,
    string? rankMessage = null, string? lengthMessage = null, string? dimLengthsMessage = null, string? itemMessage = null, string? totalMessage = null,
    [CallerArgumentExpression("array")] string? arrayParameter = "array")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(rankValidator), this.DelegateOf(lengthValidator), this.DelegateOf(dimLengthsValidator), this.DelegateOf(itemValidator), this.DelegateOf(totalValidator));

    return ValidateArrayCore(NotNull(array),
      rankValidator is null ? null : ArrayRankValidator(rankValidator, rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      lengthValidator is null ? null : ArrayLengthValidator(lengthValidator, lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      dimLengthsValidator is null ? null : ArrayDimLengthsValidator(dimLengthsValidator, dimLengthsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      itemValidator is null ? null : ArrayElementNavigator(itemValidator, itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      totalValidator is null ? null : ArrayTotalValidator(totalValidator, totalMessage, arrayParameter));
  }

  public Array ValidateArray<T>(Array array,
    Action<int>? rankValidator, Action<int>? lengthValidator, Action<ArrayDimension[]>? dimensionsValidator, ElementDimAction<T>? itemValidator, Action<int, int, bool>? totalValidator,
    string? rankMessage = null, string? lengthMessage = null, string? dimensionsMessage = null, string? itemMessage = null, string? totalMessage = null,
    [CallerArgumentExpression("array")] string? arrayParameter = "array")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(rankValidator), this.DelegateOf(lengthValidator), this.DelegateOf(dimensionsValidator), this.DelegateOf(itemValidator), this.DelegateOf(totalValidator));

    return ValidateArrayCore(NotNull(array),
      rankValidator is null ? null : ArrayRankValidator(rankValidator, rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      lengthValidator is null ? null : ArrayLengthValidator(lengthValidator, lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      dimensionsValidator is null ? null : ArrayDimensionsValidator(dimensionsValidator, dimensionsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      itemValidator is null ? null : ArrayElementNavigator(itemValidator, itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      totalValidator is null ? null : ArrayTotalValidator(totalValidator, totalMessage, arrayParameter));
  }

  public Array ValidateArray<T>(Array array,
    Action<int>? rankValidator, Action<long>? lengthValidator, Action<long[]>? dimLengthsValidator, ElementDimLongAction<T>? itemValidator, Action<long, long, bool>? totalValidator,
    string? rankMessage = null, string? lengthMessage = null, string? dimLengthsMessage = null, string? itemMessage = null, string? totalMessage = null,
    [CallerArgumentExpression("array")] string? arrayParameter = "array")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(rankValidator), this.DelegateOf(lengthValidator), this.DelegateOf(dimLengthsValidator), this.DelegateOf(itemValidator), this.DelegateOf(totalValidator));

    return ValidateArrayCore(NotNull(array),
      rankValidator is null ? null : ArrayRankValidator(rankValidator, rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      lengthValidator is null ? null : ArrayLengthValidator(lengthValidator, lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      dimLengthsValidator is null ? null : ArrayDimLengthsValidator(dimLengthsValidator, dimLengthsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      itemValidator is null ? null : ArrayElementNavigator(itemValidator, itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      totalValidator is null ? null : ArrayTotalValidator(totalValidator, totalMessage, arrayParameter));
  }

  #endregion
  #region Matching methods

  public Array MatchRank(Array array, Predicate<int> rankPredicate,
    string? rankMessage = null, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => ValidateArrayCore(NotNull(array, valueParameter: arrayParameter),
      ArrayRankValidator(NotNull(rankPredicate), rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      null, default(Action<ArrayDimension[]>), null, null);

  public Array MatchLength(Array array, Predicate<int> lengthPredicate,
    string? lengthMessage = null, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => ValidateArrayCore(NotNull(array, valueParameter: arrayParameter), null,
      ArrayLengthValidator(NotNull(lengthPredicate), lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      default(Action<ArrayDimension[]>), null, null);

  public Array MatchLength(Array array, Predicate<long> lengthPredicate,
    string? lengthMessage = null, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => ValidateArrayCore(NotNull(array, valueParameter: arrayParameter), null,
      ArrayLengthValidator(NotNull(lengthPredicate), lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      null, null, null);

  public Array MatchDimLengths(Array array, Predicate<int[]> dimLengthsPredicate,
    string? dimLengthsMessage = null, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => ValidateArrayCore(NotNull(array), null, null,
      ArrayDimLengthsValidator(NotNull(dimLengthsPredicate), dimLengthsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      null, null);

  public Array MatchDimLengths(Array array, Predicate<long[]> dimLengthsPredicate,
    string? dimLengthsMessage = null, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => ValidateArrayCore(NotNull(array), null, null,
      ArrayDimLengthsValidator(NotNull(dimLengthsPredicate), dimLengthsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      null, null);

  public Array MatchDimensions(Array array, Predicate<ArrayDimension[]> dimensionsPredicate,
    string? dimensionsMessage = null, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => ValidateArrayCore(NotNull(array), null, null,
      ArrayDimensionsValidator(NotNull(dimensionsPredicate), dimensionsMessage ?? FormatString(ArgumentMessage.ArrayDimensionsAreMismatched, arrayParameter), arrayParameter),
      null, null);

  public Array MatchElements(Array array, ElementDimPredicate<object?> itemPredicate,
    string? itemMessage = null, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => ValidateArrayCore(NotNull(array), null, null, default(Action<ArrayDimension[]>),
      ArrayElementNavigator(NotNull(itemPredicate), itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      null);

  public Array MatchElements<T>(Array array, ElementDimPredicate<T> itemPredicate,
    string? itemMessage = null, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => ValidateArrayCore(NotNull(array), null, null, default(Action<ArrayDimension[]>),
      ArrayElementNavigator(NotNull(itemPredicate), itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      null);

  public Array MatchElements(Array array, ElementDimLongPredicate<object?> itemPredicate,
    string? itemMessage = null, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => ValidateArrayCore(NotNull(array), null, null, null,
      ArrayElementNavigator(NotNull(itemPredicate), itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      null);

  public Array MatchElements<T>(Array array, ElementDimLongPredicate<T> itemPredicate,
    string? itemMessage = null, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => ValidateArrayCore(NotNull(array), null, null, null,
      ArrayElementNavigator(NotNull(itemPredicate), itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      null);

  public Array MatchArray(Array array,
    Predicate<int>? rankPredicate, Predicate<int>? lengthPredicate, Predicate<int[]>? dimLengthsPredicate, ElementDimPredicate<object?>? itemPredicate,
    string? rankMessage = null, string? lengthMessage = null, string? dimLengthsMessage = null, string? itemMessage = null,
    [CallerArgumentExpression("array")] string? arrayParameter = "array")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(rankPredicate), this.DelegateOf(lengthPredicate), this.DelegateOf(dimLengthsPredicate), this.DelegateOf(itemPredicate));

    return ValidateArrayCore(NotNull(array),
      rankPredicate is null ? null : ArrayRankValidator(rankPredicate, rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      lengthPredicate is null ? null : ArrayLengthValidator(lengthPredicate, lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      dimLengthsPredicate is null ? null : ArrayDimLengthsValidator(dimLengthsPredicate, dimLengthsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      itemPredicate is null ? null : ArrayElementNavigator(itemPredicate, itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      null);
  }

  public Array MatchArray(Array array,
    Predicate<int>? rankPredicate, Predicate<int>? lengthPredicate, Predicate<ArrayDimension[]>? dimensionsPredicate, ElementDimPredicate<object?>? itemValidator,
    string? rankMessage = null, string? lengthMessage = null, string? dimensionsMessage = null, string? itemMessage = null,
    [CallerArgumentExpression("array")] string? arrayParameter = "array")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(rankPredicate), this.DelegateOf(lengthPredicate), this.DelegateOf(dimensionsPredicate), this.DelegateOf(itemValidator));

    return ValidateArrayCore(NotNull(array),
      rankPredicate is null ? null : ArrayRankValidator(rankPredicate, rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      lengthPredicate is null ? null : ArrayLengthValidator(lengthPredicate, lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      dimensionsPredicate is null ? null : ArrayDimensionsValidator(dimensionsPredicate, dimensionsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      itemValidator is null ? null : ArrayElementNavigator(itemValidator, itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      null);
  }

  public Array MatchArray(Array array,
    Predicate<int>? rankPredicate, Predicate<long>? lengthPredicate, Predicate<long[]>? dimLengthsPredicate, ElementDimLongPredicate<object?>? itemPredicate,
    string? rankMessage = null, string? lengthMessage = null, string? dimLengthsMessage = null, string? itemMessage = null,
    [CallerArgumentExpression("array")] string? arrayParameter = "array")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(rankPredicate), this.DelegateOf(lengthPredicate), this.DelegateOf(dimLengthsPredicate), this.DelegateOf(itemPredicate));

    return ValidateArrayCore(NotNull(array),
      rankPredicate is null ? null : ArrayRankValidator(rankPredicate, rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      lengthPredicate is null ? null : ArrayLengthValidator(lengthPredicate, lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      dimLengthsPredicate is null ? null : ArrayDimLengthsValidator(dimLengthsPredicate, dimLengthsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      itemPredicate is null ? null : ArrayElementNavigator(itemPredicate, itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      null);
  }

  public Array MatchArray(Array array,
    Predicate<int>? rankPredicate, Predicate<int>? lengthPredicate, Predicate<int[]>? dimLengthsPredicate, ElementDimPredicate<object?>? itemPredicate, Func<int, int, bool, bool>? totalPredicate,
    string? rankMessage = null, string? lengthMessage = null, string? dimLengthsMessage = null, string? itemMessage = null, string? totalMessage = null,
    [CallerArgumentExpression("array")] string? arrayParameter = "array")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(rankPredicate), this.DelegateOf(lengthPredicate), this.DelegateOf(dimLengthsPredicate), this.DelegateOf(itemPredicate), this.DelegateOf(totalPredicate));

    return ValidateArrayCore(NotNull(array),
      rankPredicate is null ? null : ArrayRankValidator(rankPredicate, rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      lengthPredicate is null ? null : ArrayLengthValidator(lengthPredicate, lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      dimLengthsPredicate is null ? null : ArrayDimLengthsValidator(dimLengthsPredicate, dimLengthsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      itemPredicate is null ? null : ArrayElementNavigator(itemPredicate, itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      totalPredicate is null ? null : ArrayTotalValidator(totalPredicate, totalMessage, arrayParameter));
  }

  public Array MatchArray(Array array,
    Predicate<int>? rankPredicate, Predicate<int>? lengthPredicate, Predicate<ArrayDimension[]>? dimensionsPredicate, ElementDimPredicate<object?>? itemPredicate, Func<int, int, bool, bool>? totalPredicate,
    string? rankMessage = null, string? lengthMessage = null, string? dimensionsMessage = null, string? itemMessage = null, string? totalMessage = null,
    [CallerArgumentExpression("array")] string? arrayParameter = "array")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(rankPredicate), this.DelegateOf(lengthPredicate), this.DelegateOf(dimensionsPredicate), this.DelegateOf(itemPredicate), this.DelegateOf(totalPredicate));

    return ValidateArrayCore(NotNull(array),
      rankPredicate is null ? null : ArrayRankValidator(rankPredicate, rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      lengthPredicate is null ? null : ArrayLengthValidator(lengthPredicate, lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      dimensionsPredicate is null ? null : ArrayDimensionsValidator(dimensionsPredicate, dimensionsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      itemPredicate is null ? null : ArrayElementNavigator(itemPredicate, itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      totalPredicate is null ? null : ArrayTotalValidator(totalPredicate, totalMessage, arrayParameter));
  }

  public Array MatchArray(Array array,
    Predicate<int>? rankPredicate, Predicate<long>? lengthPredicate, Predicate<long[]>? dimLengthsPredicate, ElementDimLongPredicate<object?>? itemPredicate, Func<long, long, bool, bool>? totalPredicate,
    string? rankMessage = null, string? lengthMessage = null, string? dimLengthsMessage = null, string? itemMessage = null, string? totalMessage = null,
    [CallerArgumentExpression("array")] string? arrayParameter = "array")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(rankPredicate), this.DelegateOf(lengthPredicate), this.DelegateOf(dimLengthsPredicate), this.DelegateOf(itemPredicate), this.DelegateOf(totalPredicate));

    return ValidateArrayCore(NotNull(array),
      rankPredicate is null ? null : ArrayRankValidator(rankPredicate, rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      lengthPredicate is null ? null : ArrayLengthValidator(lengthPredicate, lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      dimLengthsPredicate is null ? null : ArrayDimLengthsValidator(dimLengthsPredicate, dimLengthsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      itemPredicate is null ? null : ArrayElementNavigator(itemPredicate, itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      totalPredicate is null ? null : ArrayTotalValidator(totalPredicate, totalMessage, arrayParameter));
  }

  public Array MatchArray<T>(Array array,
    Predicate<int>? rankPredicate, Predicate<int>? lengthPredicate, Predicate<int[]>? dimLengthsPredicate, ElementDimPredicate<T>? itemPredicate,
    string? rankMessage = null, string? lengthMessage = null, string? dimLengthsMessage = null, string? itemMessage = null,
    [CallerArgumentExpression("array")] string? arrayParameter = "array")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(rankPredicate), this.DelegateOf(lengthPredicate), this.DelegateOf(dimLengthsPredicate), this.DelegateOf(itemPredicate));

    return ValidateArrayCore(NotNull(array),
      rankPredicate is null ? null : ArrayRankValidator(rankPredicate, rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      lengthPredicate is null ? null : ArrayLengthValidator(lengthPredicate, lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      dimLengthsPredicate is null ? null : ArrayDimLengthsValidator(dimLengthsPredicate, dimLengthsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      itemPredicate is null ? null : ArrayElementNavigator(itemPredicate, itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      null);
  }

  public Array MatchArray<T>(Array array,
    Predicate<int>? rankPredicate, Predicate<int>? lengthPredicate, Predicate<ArrayDimension[]>? dimensionsPredicate, ElementDimPredicate<T>? itemPredicate,
    string? rankMessage = null, string? lengthMessage = null, string? dimensionsMessage = null, string? itemMessage = null,
    [CallerArgumentExpression("array")] string? arrayParameter = "array")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(rankPredicate), this.DelegateOf(lengthPredicate), this.DelegateOf(dimensionsPredicate), this.DelegateOf(itemPredicate));

    return ValidateArrayCore(NotNull(array),
      rankPredicate is null ? null : ArrayRankValidator(rankPredicate, rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      lengthPredicate is null ? null : ArrayLengthValidator(lengthPredicate, lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      dimensionsPredicate is null ? null : ArrayDimensionsValidator(dimensionsPredicate, dimensionsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      itemPredicate is null ? null : ArrayElementNavigator(itemPredicate, itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      null);
  }

  public Array MatchArray<T>(Array array,
    Predicate<int>? rankPredicate, Predicate<long>? lengthPredicate, Predicate<long[]>? dimLengthsPredicate, ElementDimLongPredicate<T>? itemPredicate,
    string? rankMessage = null, string? lengthMessage = null, string? dimLengthsMessage = null, string? itemMessage = null,
    [CallerArgumentExpression("array")] string? arrayParameter = "array")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(rankPredicate), this.DelegateOf(lengthPredicate), this.DelegateOf(dimLengthsPredicate), this.DelegateOf(itemPredicate));

    return ValidateArrayCore(NotNull(array),
      rankPredicate is null ? null : ArrayRankValidator(rankPredicate, rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      lengthPredicate is null ? null : ArrayLengthValidator(lengthPredicate, lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      dimLengthsPredicate is null ? null : ArrayDimLengthsValidator(dimLengthsPredicate, dimLengthsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      itemPredicate is null ? null : ArrayElementNavigator(itemPredicate, itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      null);
  }

  public Array MatchArray<T>(Array array,
    Predicate<int>? rankPredicate, Predicate<int>? lengthPredicate, Predicate<int[]>? dimLengthsPredicate, ElementDimPredicate<T>? itemPredicate, Func<int, int, bool, bool>? totalPredicate,
    string? rankMessage = null, string? lengthMessage = null, string? dimLengthsMessage = null, string? itemMessage = null, string? totalMessage = null,
    [CallerArgumentExpression("array")] string? arrayParameter = "array")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(rankPredicate), this.DelegateOf(lengthPredicate), this.DelegateOf(dimLengthsPredicate), this.DelegateOf(itemPredicate), this.DelegateOf(totalPredicate));

    return ValidateArrayCore(NotNull(array),
      rankPredicate is null ? null : ArrayRankValidator(rankPredicate, rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      lengthPredicate is null ? null : ArrayLengthValidator(lengthPredicate, lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      dimLengthsPredicate is null ? null : ArrayDimLengthsValidator(dimLengthsPredicate, dimLengthsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      itemPredicate is null ? null : ArrayElementNavigator(itemPredicate, itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      totalPredicate is null ? null : ArrayTotalValidator(totalPredicate, totalMessage, arrayParameter));
  }

  public Array MatchArray<T>(Array array,
    Predicate<int>? rankPredicate, Predicate<int>? lengthPredicate, Predicate<ArrayDimension[]>? dimensionsPredicate, ElementDimPredicate<T>? itemPredicate, Func<int, int, bool, bool>? totalPredicate,
    string? rankMessage = null, string? lengthMessage = null, string? dimensionsMessage = null, string? itemMessage = null, string? totalMessage = null,
    [CallerArgumentExpression("array")] string? arrayParameter = "array")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(rankPredicate), this.DelegateOf(lengthPredicate), this.DelegateOf(dimensionsPredicate), this.DelegateOf(itemPredicate), this.DelegateOf(totalPredicate));

    return ValidateArrayCore(NotNull(array),
      rankPredicate is null ? null : ArrayRankValidator(rankPredicate, rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      lengthPredicate is null ? null : ArrayLengthValidator(lengthPredicate, lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      dimensionsPredicate is null ? null : ArrayDimensionsValidator(dimensionsPredicate, dimensionsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      itemPredicate is null ? null : ArrayElementNavigator(itemPredicate, itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      totalPredicate is null ? null : ArrayTotalValidator(totalPredicate, totalMessage, arrayParameter));
  }

  public Array MatchArray<T>(Array array,
    Predicate<int>? rankPredicate, Predicate<long>? lengthPredicate, Predicate<long[]>? dimLengthsPredicate, ElementDimLongPredicate<T>? itemPredicate, Func<long, long, bool, bool>? totalPredicate,
    string? rankMessage = null, string? lengthMessage = null, string? dimLengthsMessage = null, string? itemMessage = null, string? totalMessage = null,
    [CallerArgumentExpression("array")] string? arrayParameter = "array")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(rankPredicate), this.DelegateOf(lengthPredicate), this.DelegateOf(dimLengthsPredicate), this.DelegateOf(itemPredicate), this.DelegateOf(totalPredicate));

    return ValidateArrayCore(NotNull(array),
      rankPredicate is null ? null : ArrayRankValidator(rankPredicate, rankMessage ?? FormatString(ArgumentMessage.ArrayRankIsMismatched, arrayParameter), arrayParameter),
      lengthPredicate is null ? null : ArrayLengthValidator(lengthPredicate, lengthMessage ?? FormatString(ArgumentMessage.ArrayLengthIsMismatched, arrayParameter), arrayParameter),
      dimLengthsPredicate is null ? null : ArrayDimLengthsValidator(dimLengthsPredicate, dimLengthsMessage ?? FormatString(ArgumentMessage.ArrayDimLengthsAreMismatched, arrayParameter), arrayParameter),
      itemPredicate is null ? null : ArrayElementNavigator(itemPredicate, itemMessage ?? FormatString(ArgumentMessage.ArrayContainsMismatchedElements, arrayParameter), arrayParameter),
      totalPredicate is null ? null : ArrayTotalValidator(totalPredicate, totalMessage, arrayParameter));
  }

  #endregion
  #region Specific methods

  public Array OneDimensionalArray(Array array,
    string? rankMessage = null, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => ValidateArrayCore(NotNull(array, valueParameter: arrayParameter),
      ArrayRankValidator(IsOneDimensionalArray, rankMessage ?? FormatString(ArgumentMessage.ArrayIsMultidimensional, arrayParameter), arrayParameter),
      null, default(Action<int[]>), null, null);

  public Array MultiDimensionalArray(Array array,
    string? rankMessage = null, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => ValidateArrayCore(NotNull(array, valueParameter: arrayParameter),
      ArrayRankValidator(IsMultiDimensionalArray, rankMessage ?? FormatString(ArgumentMessage.ArrayIsOnedimensional, arrayParameter), arrayParameter),
      null, default(Action<int[]>), null, null);

  public Array EmptyArray(Array array,
    string? lengthMessage = null, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => ValidateArrayCore(NotNull(array, valueParameter: arrayParameter), null,
      ArrayLengthValidator((Predicate<long>)IsEmptyArray, lengthMessage ?? FormatString(ArgumentMessage.ArrayIsNotEmpty, arrayParameter), arrayParameter),
      null, null, null);

  public Array NotEmptyArray(Array array,
    string? lengthMessage = null, [CallerArgumentExpression("array")] string? arrayParameter = "array")
    => ValidateArrayCore(NotNull(array, valueParameter: arrayParameter), null,
      ArrayLengthValidator((Predicate<long>)IsNotEmptyArray, lengthMessage ?? FormatString(ArgumentMessage.ArrayIsEmpty, arrayParameter), arrayParameter),
      null, null, null);

  #endregion
  #endregion
  #endregion
  #region Collection restrictions
  #region Internal methods

  private static CollectionRestrictions GetRestrictions(IEnumerable enumerable)
    => enumerable switch
    {
      ICollectionRestricted collectionRestricted => collectionRestricted.Restrictions,
      IDictionary dictionary => dictionary.IsReadOnly ? CollectionRestrictions.ReadOnly : dictionary.IsFixedSize ? CollectionRestrictions.FixedSize : CollectionRestrictions.None,
      IList list => list.IsReadOnly ? CollectionRestrictions.ReadOnly : list.IsFixedSize ? CollectionRestrictions.FixedSize : CollectionRestrictions.None,
      ICollection => CollectionRestrictions.None,
      _ => CollectionRestrictions.ReadOnly,
    };

  private static CollectionRestrictions GetRestrictions<T>(IEnumerable<T> enumerable)
    => enumerable switch
    {
      ICollectionRestricted collectionRestricted => collectionRestricted.Restrictions,
      ICollection<T> collection => collection.IsReadOnly ? CollectionRestrictions.ReadOnly : CollectionRestrictions.None,
      IEnumerable nonGenericEnumerable => GetRestrictions(nonGenericEnumerable),
    };

  private static Predicate<CollectionRestrictions> RestrictionPredicate(CollectionRestrictions expectedRestrictions)
    => expectedRestrictions == CollectionRestrictions.None
      ? restrictions => restrictions == CollectionRestrictions.None
      : restrictions => (restrictions & expectedRestrictions) == expectedRestrictions;

  #endregion
  #region Match restrictions

  public IEnumerable MatchRestrictions(IEnumerable collection, Predicate<CollectionRestrictions> predicate,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
  {
    NotNull(predicate);

    var restrictions = GetRestrictions(collection);
    return predicate(restrictions) ? collection :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.CollectionRestrictionsAreMismatched, collectionParameter), collectionParameter);
  }

  public IEnumerable<T> MatchRestrictions<T>(IEnumerable<T> collection, Predicate<CollectionRestrictions> predicate,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
  {
    NotNull(predicate);

    var restrictions = GetRestrictions(collection);
    return predicate(restrictions) ? collection :
      throw new ArgumentException(message ?? FormatString(ArgumentMessage.CollectionRestrictionsAreMismatched, collectionParameter), collectionParameter);
  }

  #endregion
  #region Restricted

  public IEnumerable Restricted(IEnumerable collection, CollectionRestrictions restrictions,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => MatchRestrictions(collection, RestrictionPredicate(NotEmptyFlags(restrictions, null)),
      message ?? FormatString(ArgumentMessage.CollectionIsNotRestrictedProperly, collectionParameter), collectionParameter);

  public IEnumerable<T> Restricted<T>(IEnumerable<T> collection, CollectionRestrictions restrictions,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => MatchRestrictions(collection, RestrictionPredicate(NotEmptyFlags(restrictions)),
      message ?? FormatString(ArgumentMessage.CollectionIsNotRestrictedProperly, collectionParameter), collectionParameter);

  #endregion
  #region Not restricted

  public IEnumerable NotRestricted(IEnumerable collection,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => MatchRestrictions(collection, RestrictionPredicate(CollectionRestrictions.None),
      message ?? FormatString(ArgumentMessage.CollectionIsRestricted, collectionParameter));

  public IEnumerable<T> NotRestricted<T>(IEnumerable<T> collection,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => MatchRestrictions(collection, RestrictionPredicate(CollectionRestrictions.None),
      message ?? FormatString(ArgumentMessage.CollectionIsRestricted, collectionParameter));

  #endregion
  #endregion
  #region Collection validation
  #region Internal methods
  #region Enumeration methods

  private static IEnumerable EnumerateCollectionCore(IEnumerable collection, Func<object?, int, int, NavigateCommand>? itemNavigator, Action<int, int, bool>? totalValidator)
  {
    if (collection is IList list)
    {
      int indexIn = 0, indexOut = 0;
      var stopped = false;
      for (; !stopped && indexIn < list.Count; indexIn++)
      {
        var item = list[indexIn];
        var navigateCommand = itemNavigator?.Invoke(item, indexIn, indexOut) ?? NavigateCommand.None;
        yield return item;
        if ((navigateCommand & NavigateCommand.Skip) == 0)
          indexOut++;
        if ((navigateCommand & NavigateCommand.Stop) != 0)
          stopped = true;
      }
      totalValidator?.Invoke(indexIn, indexOut, stopped);
    }
    else
    {
      var enumerator = collection.GetEnumerator();
      try
      {
        int indexIn = 0, indexOut = 0;
        var stopped = false;
        for (var success = enumerator.MoveNext(); !stopped && success; indexIn++, success = enumerator.MoveNext())
        {
          var item = enumerator.Current;
          var navigateCommand = itemNavigator?.Invoke(item, indexIn, indexOut) ?? NavigateCommand.None;
          yield return item;
          if ((navigateCommand & NavigateCommand.Skip) == 0)
            indexOut++;
          if ((navigateCommand & NavigateCommand.Stop) != 0)
            stopped = true;
        }
        totalValidator?.Invoke(indexIn, indexOut, stopped);
      }
      finally
      {
        Disposable.BlindDispose(ref enumerator);
      }
    }
  }

  private static IEnumerable<T> EnumerateCollectionCore<T>(IEnumerable<T> collection, Func<T, int, int, NavigateCommand>? itemNavigator, Action<int, int, bool>? totalValidator)
  {
    if (collection is IList<T> list)
    {
      int indexIn = 0, indexOut = 0;
      var stopped = false;
      for (; !stopped && indexIn < list.Count; indexIn++)
      {
        var item = list[indexIn];
        var navigateCommand = itemNavigator?.Invoke(item, indexIn, indexOut) ?? NavigateCommand.None;
        yield return item;
        if ((navigateCommand & NavigateCommand.Skip) == 0)
          indexOut++;
        if ((navigateCommand & NavigateCommand.Stop) != 0)
          stopped = true;
      }
      totalValidator?.Invoke(indexIn, indexOut, stopped);
    }
    else
    {
      using var enumerator = collection.GetEnumerator();
      int indexIn = 0, indexOut = 0;
      var stopped = false;
      for (var success = enumerator.MoveNext(); !stopped && success; indexIn++, success = enumerator.MoveNext())
      {
        var item = enumerator.Current;
        var navigateCommand = itemNavigator?.Invoke(item, indexIn, indexOut) ?? NavigateCommand.None;
        yield return item;
        if ((navigateCommand & NavigateCommand.Skip) == 0)
          indexOut++;
        if ((navigateCommand & NavigateCommand.Stop) != 0)
          stopped = true;
      }
      totalValidator?.Invoke(indexIn, indexOut, stopped);
    }
  }

  private static async IAsyncEnumerable<T> EnumerateCollectionAsyncCore<T>(IAsyncEnumerable<T> collection, Func<T, int, int, NavigateCommand>? itemDelegate, Action<int, int, bool>? totalDelegate,
    [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    var enumerator = collection.GetAsyncEnumerator(cancellationToken);
    try
    {
      int indexIn = 0, indexOut = 0;
      var stopped = false;
      for (var success = await enumerator.MoveNextAsync(cancellationToken); !stopped && success; indexIn++, success = await enumerator.MoveNextAsync(cancellationToken))
      {
        var item = enumerator.Current;
        NavigateCommand navigateCommand = itemDelegate?.Invoke(item, indexIn, indexOut) ?? NavigateCommand.None;
        yield return item;
        if ((navigateCommand & NavigateCommand.Skip) == 0)
          indexOut++;
        if ((navigateCommand & NavigateCommand.Stop) != 0)
          stopped = true;
      }
      totalDelegate?.Invoke(indexIn, indexOut, stopped);
    }
    finally
    {
      await AsyncDisposable.DisposeAsync(ref enumerator);
    }
  }

  #endregion
  #region Validation methods

  private static IEnumerable ValidateCollectionCore(IEnumerable collection,
    Action<int>? countValidator, Func<object?, int, int, NavigateCommand>? itemValidator, Action<int, int, bool>? totalValidator, bool lazy)
  {
    if (countValidator is not null)
    {
      var count = collection.PeekCount();
      if (count >= 0)
        countValidator.Invoke(count);
      else
        totalValidator = TotalValidator(countValidator, totalValidator);
    }
    if (itemValidator is not null || totalValidator is not null)
    {
      var result = EnumerateCollectionCore(collection, itemValidator, totalValidator);
      if (lazy)
        return result;
      result.OfType<object?>().Enumerate();
    }
    return collection;
  }

  private static IEnumerable<T> ValidateCollectionCore<T>(IEnumerable<T> collection,
    Action<int>? countValidator, Func<T, int, int, NavigateCommand>? itemValidator, Action<int, int, bool>? totalValidator, bool lazy)
  {
    if (countValidator is not null)
    {
      var count = collection.PeekCount();
      if (count >= 0)
        countValidator.Invoke(count);
      else
        totalValidator = TotalValidator(countValidator, totalValidator);
    }
    if (itemValidator is not null || totalValidator is not null)
    {
      var result = EnumerateCollectionCore(collection, itemValidator, totalValidator);
      if (lazy)
        return result;
      result.Enumerate();
    }
    return collection;
  }

  private static IAsyncEnumerable<T> ValidateCollectionAsyncCore<T>(IAsyncEnumerable<T> collection,
    Action<int>? countValidator, Func<T, int, int, NavigateCommand>? itemValidator, Action<int, int, bool>? totalValidator,
    CancellationToken cancellationToken)
  {
    if (countValidator is not null)
      totalValidator = TotalValidator(countValidator, totalValidator);
    return itemValidator is null && totalValidator is null ? collection : EnumerateCollectionAsyncCore(collection, itemValidator, totalValidator, cancellationToken);
  }

  #endregion
  #region General validators

  private static bool IsNotEmpty(int count)
    => count > 0;

  private static bool IsEmpty(int count)
    => count == 0;

  private static bool IsNotNull<T>(T item, int index)
    => item is not null;

  private static ElementAction<T> IndexableElementValidator<T>(Action<T> validator)
    => (item, index) => validator(item);

  private static ElementPredicate<T> IndexableElementPredicate<T>(Predicate<T> predicate)
    => (item, index) => predicate(item);

  private static Predicate<int> ExactCountPredicate(int exactCount, bool negate)
    => count => count == exactCount ^ negate;

  private static Predicate<int> CountComparePredicate(int otherCount, ComparisonCriteria criteria, bool negate)
    => count => Comparison.Match(count.CompareTo(otherCount), criteria) ^ negate;

  private static Predicate<int> CountBetweenPredicate(int lowerCount, int upperCount, BetweenCriteria criteria, bool negate)
    => count => Comparison.Match(count.CompareTo(lowerCount), count.CompareTo(upperCount), criteria) ^ negate;

  #endregion
  #region Collection validators

  private static Action<int> CountValidator(Action<int> validator, string? message, string? collectionParameter)
    => count =>
    {
      try
      {
        validator(count);
      }
      catch (Exception ex)
      {
        throw new ArgumentException(message, collectionParameter, ex);
      }
    };

  private static Action<int> CountValidator(Predicate<int> predicate, string? message, string? collectionParameter)
    => count =>
    {
      if (!predicate(count))
        throw new ArgumentException(message, collectionParameter);
    };

  private static Func<T, int, int, NavigateCommand> ElementNavigator<T>(ElementAction<T> validator, string? message, string? collectionParameter)
    => (item, indexIn, indexOut) =>
    {
      try
      {
        validator(item, indexIn);
        return NavigateCommand.None;
      }
      catch (Exception ex)
      {
        throw new ArgumentCollectionElementException(collectionParameter, message, ex, indexIn);
      }
    };

  private static Func<T, int, int, NavigateCommand> ElementNavigator<T>(ElementPredicate<T> predicate, string? message, string? collectionParameter)
    => (item, indexIn, indexOut) =>
    {
      if (!predicate(item, indexIn))
        throw new ArgumentCollectionElementException(collectionParameter, message, indexIn);
      return NavigateCommand.None;
    };

  private static Func<T, int, int, NavigateCommand> ElementNavigator<T>(ElementPredicate<T> predicate, Func<T, int, int, bool> stopPredicate)
    => (item, indexIn, indexOut) =>
      (!predicate(item, indexIn) ? NavigateCommand.Skip : NavigateCommand.None) |
        (stopPredicate(item, indexIn, indexOut) ? NavigateCommand.Stop : NavigateCommand.None);

  private static Func<T, int, int, NavigateCommand> ExactElementNavigator<T>(ElementPredicate<T> predicate, int count)
  => (item, index, matched)
    => !predicate(item, index) ? NavigateCommand.Skip : count > 0 && matched == count ? NavigateCommand.Stop | NavigateCommand.Skip : NavigateCommand.None;

  private static Func<T, int, int, NavigateCommand> AllElementNavigator<T>(ElementPredicate<T> predicate)
    => (item, index, matched)
      => !predicate(item, index) ? NavigateCommand.Stop | NavigateCommand.Skip : NavigateCommand.None;

  private static Func<T, int, int, NavigateCommand> AnyElementNavigator<T>(ElementPredicate<T> predicate, bool earlyOut)
    => (item, index, matched)
      => !predicate(item, index) ? NavigateCommand.Skip : earlyOut ? NavigateCommand.Stop : NavigateCommand.None;

  private static Action<int, int, bool> TotalValidator(Action<int> countValidator, Action<int, int, bool>? totalValidator)
    => (total, matched, stopped) =>
      {
        if (!stopped)
          countValidator.Invoke(total);
        totalValidator?.Invoke(total, matched, stopped);
      };

  private static Action<int, int, bool> TotalValidator(Func<int, int, bool> predicate, string? message, string? collectionParameter)
    => (total, matched, stopped) =>
    {
      if (!predicate(total, matched))
        throw new ArgumentException(message, collectionParameter);
    };

  private static Action<int, int, bool> TotalValidator(Func<int, int, bool, bool> predicate, string? message, string? collectionParameter)
    => (total, matched, stopped) =>
    {
      if (!predicate(total, matched, stopped))
        throw new ArgumentException(message, collectionParameter);
    };

  private static Action<int, int, bool> ExactTotalValidator(int count, string? message, string? collectionParameter)
    => (total, matched, stopped) =>
    {
      if (stopped)
        throw new ArgumentCollectionElementException(collectionParameter, message, total);
      if (matched != count)
        throw new ArgumentException(message, collectionParameter);
    };

  private static Action<int, int, bool> AnyTotalValidator(string? message, string? collectionParameter)
    => (total, matched, stopped) =>
    {
      if (matched == 0 && !stopped)
        throw new ArgumentException(message, collectionParameter);
    };

  private static Action<int, int, bool> AllTotalValidator(string? message, string? collectionParameter)
    => (total, matched, stopped) =>
    {
      if (stopped || total != matched)
        throw new ArgumentCollectionElementException(collectionParameter, message, total);
    };

  #endregion
  #endregion
  #region Public methods
  #region Validate collection

  public IEnumerable ValidateCount(IEnumerable collection, Action<int> countValidator,
    string? countMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      CountValidator(NotNull(countValidator), countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      null, null, false);

  public IEnumerable<T> ValidateCount<T>(IEnumerable<T> collection, Action<int> countValidator,
    string? countMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      CountValidator(NotNull(countValidator), countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      null, null, false);

  public IAsyncEnumerable<T> ValidateCountAsync<T>(IAsyncEnumerable<T> collection, Action<int> countValidator,
    string? countMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter),
      CountValidator(NotNull(countValidator), countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      null, null, cancellationToken);

  public IEnumerable ValidateElements(IEnumerable collection, Action<object?> itemValidator, bool lazy = false,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter), null,
      ElementNavigator(IndexableElementValidator(NotNull(itemValidator)), itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      null, lazy);

  public IEnumerable<T> ValidateElements<T>(IEnumerable<T> collection, Action<T> itemValidator, bool lazy = false,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter), null,
      ElementNavigator(IndexableElementValidator(NotNull(itemValidator)), itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      null, lazy);

  public IAsyncEnumerable<T> ValidateElementsAsync<T>(IAsyncEnumerable<T> collection, Action<T> itemValidator,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter), null,
      ElementNavigator(IndexableElementValidator(NotNull(itemValidator)), itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      null, cancellationToken);

  public IEnumerable ValidateElements(IEnumerable collection, ElementAction<object?> itemValidator, bool lazy = false,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter), null,
      ElementNavigator(NotNull(itemValidator), itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      null, lazy);

  public IEnumerable<T> ValidateElements<T>(IEnumerable<T> collection, ElementAction<T> itemValidator, bool lazy = false,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter), null,
      ElementNavigator(NotNull(itemValidator), itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      null, lazy);

  public IAsyncEnumerable<T> ValidateElementsAsync<T>(IAsyncEnumerable<T> collection, ElementAction<T> itemValidator,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter), null,
      ElementNavigator(NotNull(itemValidator), itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      null, cancellationToken);

  public IEnumerable ValidateCollection(IEnumerable collection, Action<int>? countValidator, Action<object?>? itemValidator, bool lazy = false,
    string? countMessage = null, string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(countValidator), this.DelegateOf(itemValidator));

    return ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      countValidator is null ? null : CountValidator(countValidator, countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      itemValidator is null ? null : ElementNavigator(IndexableElementValidator(itemValidator), itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      null, lazy);
  }

  public IEnumerable<T> ValidateCollection<T>(IEnumerable<T> collection, Action<int>? countValidator, Action<T>? itemValidator, bool lazy = false,
    string? countMessage = null, string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(countValidator), this.DelegateOf(itemValidator));

    return ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      countValidator is null ? null : CountValidator(countValidator, countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      itemValidator is null ? null : ElementNavigator(IndexableElementValidator(itemValidator), itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      null, lazy);
  }

  public IAsyncEnumerable<T> ValidateCollectionAsync<T>(IAsyncEnumerable<T> collection, Action<int>? countValidator, Action<T>? itemValidator,
    string? countMessage = null, string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(countValidator), this.DelegateOf(itemValidator));

    return ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter),
      countValidator is null ? null : CountValidator(countValidator, countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      itemValidator is null ? null : ElementNavigator(IndexableElementValidator(itemValidator), itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      null, cancellationToken);
  }

  public IEnumerable ValidateCollection(IEnumerable collection, Action<int>? countValidator, ElementAction<object?>? itemValidator, bool lazy = false,
    string? countMessage = null, string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(countValidator), this.DelegateOf(itemValidator));

    return ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      countValidator is null ? null : CountValidator(countValidator, countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      itemValidator is null ? null : ElementNavigator(itemValidator, itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      null, lazy);
  }

  public IEnumerable<T> ValidateCollection<T>(IEnumerable<T> collection, Action<int>? countValidator, ElementAction<T>? itemValidator, bool lazy = false,
    string? countMessage = null, string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(countValidator), this.DelegateOf(itemValidator));

    return ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      countValidator is null ? null : CountValidator(countValidator, countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      itemValidator is null ? null : ElementNavigator(itemValidator, itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      null, lazy);
  }

  public IAsyncEnumerable<T> ValidateCollectionAsync<T>(IAsyncEnumerable<T> collection, Action<int>? countValidator, ElementAction<T>? itemValidator,
    string? countMessage = null, string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(countValidator), this.DelegateOf(itemValidator));

    return ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter),
      countValidator is null ? null : CountValidator(countValidator, countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      itemValidator is null ? null : ElementNavigator(itemValidator, itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      null, cancellationToken);
  }

  #endregion
  #region Match collection

  public IEnumerable MatchCount(IEnumerable collection, Predicate<int> countPredicate,
    string? countMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      CountValidator(NotNull(countPredicate), countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      null, null, false);

  public IEnumerable<T> MatchCount<T>(IEnumerable<T> collection, Predicate<int> countPredicate,
    string? countMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      CountValidator(NotNull(countPredicate), countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      null, null, false);

  public IAsyncEnumerable<T> MatchCountAsync<T>(IAsyncEnumerable<T> collection, Predicate<int> countPredicate,
    string? countMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter),
      CountValidator(NotNull(countPredicate), countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      null, null, cancellationToken);

  public IEnumerable MatchElements(IEnumerable collection, Predicate<object?> itemPredicate, bool lazy = false,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter), null,
      ElementNavigator(IndexableElementPredicate(NotNull(itemPredicate)), itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      null, lazy);

  public IEnumerable<T> MatchElements<T>(IEnumerable<T> collection, Predicate<T> itemPredicate, bool lazy = false,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter), null,
      ElementNavigator(IndexableElementPredicate(NotNull(itemPredicate)), itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      null, lazy);

  public IAsyncEnumerable<T> MatchElementsAsync<T>(IAsyncEnumerable<T> collection, Predicate<T> itemPredicate,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter), null,
      ElementNavigator(IndexableElementPredicate(NotNull(itemPredicate)), itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      null, cancellationToken);

  public IEnumerable MatchElements(IEnumerable collection, ElementPredicate<object?> itemPredicate, bool lazy = false,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter), null,
      ElementNavigator(NotNull(itemPredicate), itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      null, lazy);

  public IEnumerable<T> MatchElements<T>(IEnumerable<T> collection, ElementPredicate<T> itemPredicate, bool lazy = false,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter), null,
      ElementNavigator(NotNull(itemPredicate), itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      null, lazy);

  public IAsyncEnumerable<T> MatchElementsAsync<T>(IAsyncEnumerable<T> collection, ElementPredicate<T> itemPredicate,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter), null,
      ElementNavigator(NotNull(itemPredicate), itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      null, cancellationToken);

  public IEnumerable MatchCollection(IEnumerable collection, Predicate<int>? countPredicate, Predicate<object?>? itemPredicate, bool lazy = false,
    string? countMessage = null, string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(countPredicate), this.DelegateOf(itemPredicate));

    return ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      countPredicate is null ? null : CountValidator(countPredicate, countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      itemPredicate is null ? null : ElementNavigator(IndexableElementPredicate(itemPredicate), itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      null, lazy);
  }

  public IEnumerable<T> MatchCollection<T>(IEnumerable<T> collection, Predicate<int>? countPredicate, Predicate<T>? itemPredicate, bool lazy = false,
    string? countMessage = null, string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(countPredicate), this.DelegateOf(itemPredicate));

    return ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      countPredicate is null ? null : CountValidator(countPredicate, countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      itemPredicate is null ? null : ElementNavigator(IndexableElementPredicate(itemPredicate), itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      null, lazy);
  }

  public IAsyncEnumerable<T> MatchCollectionAsync<T>(IAsyncEnumerable<T> collection, Predicate<int>? countPredicate, Predicate<T>? itemPredicate,
    string? countMessage = null, string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(countPredicate), this.DelegateOf(itemPredicate));

    return ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter),
      countPredicate is null ? null : CountValidator(countPredicate, countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      itemPredicate is null ? null : ElementNavigator(IndexableElementPredicate(itemPredicate), itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      null, cancellationToken);
  }

  public IEnumerable MatchCollection(IEnumerable collection, Predicate<int>? countPredicate, ElementPredicate<object?>? itemPredicate, bool lazy = false,
    string? countMessage = null, string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(countPredicate), this.DelegateOf(itemPredicate));

    return ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      countPredicate is null ? null : CountValidator(countPredicate, countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      itemPredicate is null ? null : ElementNavigator(itemPredicate, itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      null, lazy);
  }

  public IEnumerable<T> MatchCollection<T>(IEnumerable<T> collection, Predicate<int>? countPredicate, ElementPredicate<T>? itemPredicate, bool lazy = false,
    string? countMessage = null, string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(countPredicate), this.DelegateOf(itemPredicate));

    return ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      countPredicate is null ? null : CountValidator(countPredicate, countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      itemPredicate is null ? null : ElementNavigator(itemPredicate, itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      null, lazy);
  }

  public IAsyncEnumerable<T> MatchCollectionAsync<T>(IAsyncEnumerable<T> collection, Predicate<int>? countPredicate, ElementPredicate<T>? itemPredicate,
    string? countMessage = null, string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(countPredicate), this.DelegateOf(itemPredicate));

    return ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter),
      countPredicate is null ? null : CountValidator(countPredicate, countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      itemPredicate is null ? null : ElementNavigator(itemPredicate, itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      null, cancellationToken);
  }

  public IEnumerable MatchCollection(IEnumerable collection, Predicate<int>? countPredicate, Predicate<object?> itemPredicate, Func<int, int, bool> totalPredicate, bool lazy = false,
    string? countMessage = null, string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(countPredicate), this.DelegateOf(itemPredicate), this.DelegateOf(totalPredicate));

    return ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      countPredicate is null ? null : CountValidator(countPredicate, countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      itemPredicate is null ? null : ElementNavigator(IndexableElementPredicate(itemPredicate), itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      totalPredicate is null ? null : TotalValidator(totalPredicate, itemMessage, collectionParameter),
      lazy);
  }

  public IEnumerable<T> MatchCollection<T>(IEnumerable<T> collection, Predicate<int>? countPredicate, Predicate<T> itemPredicate, Func<int, int, bool> totalPredicate, bool lazy = false,
    string? countMessage = null, string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(countPredicate), this.DelegateOf(itemPredicate), this.DelegateOf(totalPredicate));

    return ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      countPredicate is null ? null : CountValidator(countPredicate, countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      itemPredicate is null ? null : ElementNavigator(IndexableElementPredicate(itemPredicate), itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      totalPredicate is null ? null : TotalValidator(totalPredicate, itemMessage, collectionParameter),
      lazy);
  }

  public IAsyncEnumerable<T> MatchCollectionAsync<T>(IAsyncEnumerable<T> collection, Predicate<int>? countPredicate, Predicate<T> itemPredicate, Func<int, int, bool> totalPredicate,
    string? countMessage = null, string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(countPredicate), this.DelegateOf(itemPredicate), this.DelegateOf(totalPredicate));

    return ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter),
      countPredicate is null ? null : CountValidator(countPredicate, countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      itemPredicate is null ? null : ElementNavigator(IndexableElementPredicate(itemPredicate), itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      totalPredicate is null ? null : TotalValidator(totalPredicate, itemMessage, collectionParameter),
      cancellationToken);
  }

  public IEnumerable MatchCollection(IEnumerable collection, Predicate<int>? countPredicate, ElementPredicate<object?> itemPredicate, Func<int, int, bool> totalPredicate, bool lazy = false,
    string? countMessage = null, string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(countPredicate), this.DelegateOf(itemPredicate), this.DelegateOf(totalPredicate));

    return ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      countPredicate is null ? null : CountValidator(countPredicate, countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      itemPredicate is null ? null : ElementNavigator(itemPredicate, itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      totalPredicate is null ? null : TotalValidator(totalPredicate, itemMessage, collectionParameter),
      lazy);
  }

  public IEnumerable<T> MatchCollection<T>(IEnumerable<T> collection, Predicate<int>? countPredicate, ElementPredicate<T> itemPredicate, Func<int, int, bool> totalPredicate, bool lazy = false,
    string? countMessage = null, string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(countPredicate), this.DelegateOf(itemPredicate), this.DelegateOf(totalPredicate));

    return ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      countPredicate is null ? null : CountValidator(countPredicate, countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      itemPredicate is null ? null : ElementNavigator(itemPredicate, itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      totalPredicate is null ? null : TotalValidator(totalPredicate, itemMessage, collectionParameter),
      lazy);
  }

  public IAsyncEnumerable<T> MatchCollectionAsync<T>(IAsyncEnumerable<T> collection, Predicate<int>? countPredicate, ElementPredicate<T> itemPredicate, Func<int, int, bool> totalPredicate,
    string? countMessage = null, string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
  {
    this.NotNullAtLeastOne(null, this.DelegateOf(countPredicate), this.DelegateOf(itemPredicate), this.DelegateOf(totalPredicate));

    return ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter),
      countPredicate is null ? null : CountValidator(countPredicate, countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      itemPredicate is null ? null : ElementNavigator(itemPredicate, itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      totalPredicate is null ? null : TotalValidator(totalPredicate, itemMessage, collectionParameter),
      cancellationToken);
  }

  public IEnumerable MatchCollection(IEnumerable collection, Predicate<int>? countPredicate, Predicate<object?> itemPredicate, Func<object?, int, int, bool> stopPredicate, Func<int, int, bool, bool> totalPredicate, bool lazy = false,
    string? countMessage = null, string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
  {
    NotNull(itemPredicate);
    NotNull(stopPredicate);
    NotNull(totalPredicate);

    return ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      countPredicate is null ? null : CountValidator(countPredicate, countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      ElementNavigator(IndexableElementPredicate(itemPredicate), stopPredicate),
      TotalValidator(totalPredicate, itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      lazy);
  }

  public IEnumerable<T> MatchCollection<T>(IEnumerable<T> collection, Predicate<int>? countPredicate, Predicate<T> itemPredicate, Func<T, int, int, bool> stopPredicate, Func<int, int, bool, bool> totalPredicate, bool lazy = false,
    string? countMessage = null, string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
  {
    NotNull(itemPredicate);
    NotNull(stopPredicate);
    NotNull(totalPredicate);

    return ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      countPredicate is null ? null : CountValidator(countPredicate, countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      ElementNavigator(IndexableElementPredicate(itemPredicate), stopPredicate),
      TotalValidator(totalPredicate, itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      lazy);
  }

  public IAsyncEnumerable<T> MatchCollectionAsync<T>(IAsyncEnumerable<T> collection, Predicate<int>? countPredicate, Predicate<T> itemPredicate, Func<T, int, int, bool> stopPredicate, Func<int, int, bool, bool> totalPredicate,
    string? countMessage = null, string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
  {
    NotNull(itemPredicate);
    NotNull(stopPredicate);
    NotNull(totalPredicate);

    return ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter),
      countPredicate is null ? null : CountValidator(countPredicate, countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      ElementNavigator(IndexableElementPredicate(itemPredicate), stopPredicate),
      TotalValidator(totalPredicate, itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      cancellationToken);
  }

  public IEnumerable MatchCollection(IEnumerable collection, Predicate<int>? countPredicate, ElementPredicate<object?> itemPredicate, Func<object?, int, int, bool> stopPredicate, Func<int, int, bool, bool> totalPredicate, bool lazy = false,
    string? countMessage = null, string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
  {
    NotNull(itemPredicate);
    NotNull(stopPredicate);
    NotNull(totalPredicate);

    return ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      countPredicate is null ? null : CountValidator(countPredicate, countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      ElementNavigator(itemPredicate, stopPredicate),
      TotalValidator(totalPredicate, itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      lazy);
  }

  public IEnumerable<T> MatchCollection<T>(IEnumerable<T> collection, Predicate<int>? countPredicate, ElementPredicate<T> itemPredicate, Func<T, int, int, bool> stopPredicate, Func<int, int, bool, bool> totalPredicate, bool lazy = false,
    string? countMessage = null, string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
  {
    NotNull(itemPredicate);
    NotNull(stopPredicate);
    NotNull(totalPredicate);

    return ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      countPredicate is null ? null : CountValidator(countPredicate, countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      ElementNavigator(itemPredicate, stopPredicate),
      TotalValidator(totalPredicate, itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      lazy);
  }

  public IAsyncEnumerable<T> MatchCollectionAsync<T>(IAsyncEnumerable<T> collection, Predicate<int>? countPredicate, ElementPredicate<T> itemPredicate, Func<T, int, int, bool> stopPredicate, Func<int, int, bool, bool> totalPredicate,
    string? countMessage = null, string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
  {
    NotNull(itemPredicate);
    NotNull(stopPredicate);
    NotNull(totalPredicate);

    return ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter),
      countPredicate is null ? null : CountValidator(countPredicate, countMessage ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      ElementNavigator(itemPredicate, stopPredicate),
      TotalValidator(totalPredicate, itemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, collectionParameter), collectionParameter),
      cancellationToken);
  }

  #endregion
  #region Match exact elements

  public IEnumerable MatchExact(IEnumerable collection, int count, Predicate<object?> itemPredicate,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter), null,
      ExactElementNavigator(IndexableElementPredicate(NotNull(itemPredicate)), count),
      ExactTotalValidator(count, itemMessage ?? FormatString(ArgumentMessage.CollectionDoesNotContainRequiredMatchedElements, collectionParameter), collectionParameter),
      false);

  public IEnumerable<T> MatchExact<T>(IEnumerable<T> collection, int count, Predicate<T> itemPredicate,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter), null,
      ExactElementNavigator(IndexableElementPredicate(NotNull(itemPredicate)), count),
      ExactTotalValidator(count, itemMessage ?? FormatString(ArgumentMessage.CollectionDoesNotContainRequiredMatchedElements, collectionParameter), collectionParameter),
      false);

  public IAsyncEnumerable<T> MatchExactAsync<T>(IAsyncEnumerable<T> collection, int count, Predicate<T> itemPredicate,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter), null,
      ExactElementNavigator(IndexableElementPredicate(NotNull(itemPredicate)), count),
      ExactTotalValidator(count, itemMessage ?? FormatString(ArgumentMessage.CollectionDoesNotContainRequiredMatchedElements, collectionParameter), collectionParameter),
      cancellationToken);

  public IEnumerable MatchExact(IEnumerable collection, int count, ElementPredicate<object?> itemPredicate,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter), null,
      ExactElementNavigator(NotNull(itemPredicate), count),
      ExactTotalValidator(count, itemMessage ?? FormatString(ArgumentMessage.CollectionDoesNotContainRequiredMatchedElements, collectionParameter), collectionParameter),
      false);

  public IEnumerable<T> MatchExact<T>(IEnumerable<T> collection, int count, ElementPredicate<T> itemPredicate,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter), null,
      ExactElementNavigator(NotNull(itemPredicate), count),
      ExactTotalValidator(count, itemMessage ?? FormatString(ArgumentMessage.CollectionDoesNotContainRequiredMatchedElements, collectionParameter), collectionParameter),
      false);

  public IAsyncEnumerable<T> MatchExactAsync<T>(IAsyncEnumerable<T> collection, int count, ElementPredicate<T> itemPredicate,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter), null,
      ExactElementNavigator(NotNull(itemPredicate), count),
      ExactTotalValidator(count, itemMessage ?? FormatString(ArgumentMessage.CollectionDoesNotContainRequiredMatchedElements, collectionParameter), collectionParameter),
      cancellationToken);

  #endregion
  #region Match any element

  public IEnumerable MatchAny(IEnumerable collection, Predicate<object?> itemPredicate,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter), null,
      AnyElementNavigator(IndexableElementPredicate(NotNull(itemPredicate)), collection.PeekCount() >= 0),
      AnyTotalValidator(itemMessage ?? FormatString(ArgumentMessage.CollectionDoesNotContainRequiredMatchedElements, collectionParameter), collectionParameter),
      false);

  public IEnumerable<T> MatchAny<T>(IEnumerable<T> collection, Predicate<T> itemPredicate,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter), null,
      AnyElementNavigator(IndexableElementPredicate(NotNull(itemPredicate)), collection.PeekCount() >= 0),
      AnyTotalValidator(itemMessage ?? FormatString(ArgumentMessage.CollectionDoesNotContainRequiredMatchedElements, collectionParameter), collectionParameter),
      false);

  public IAsyncEnumerable<T> MatchAnyAsync<T>(IAsyncEnumerable<T> collection, Predicate<T> itemPredicate,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter), null,
      AnyElementNavigator(IndexableElementPredicate(NotNull(itemPredicate)), false),
      AnyTotalValidator(itemMessage ?? FormatString(ArgumentMessage.CollectionDoesNotContainRequiredMatchedElements, collectionParameter), collectionParameter),
      cancellationToken);

  public IEnumerable MatchAny(IEnumerable collection, ElementPredicate<object?> itemPredicate,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter), null,
      AnyElementNavigator(NotNull(itemPredicate), collection.PeekCount() >= 0),
      AnyTotalValidator(itemMessage ?? FormatString(ArgumentMessage.CollectionDoesNotContainRequiredMatchedElements, collectionParameter), collectionParameter),
      false);

  public IEnumerable<T> MatchAny<T>(IEnumerable<T> collection, ElementPredicate<T> itemPredicate,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter), null,
      AnyElementNavigator(NotNull(itemPredicate), collection.PeekCount() >= 0),
      AnyTotalValidator(itemMessage ?? FormatString(ArgumentMessage.CollectionDoesNotContainRequiredMatchedElements, collectionParameter), collectionParameter),
      false);

  public IAsyncEnumerable<T> MatchAnyAsync<T>(IAsyncEnumerable<T> collection, ElementPredicate<T> itemPredicate,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter), null,
      AnyElementNavigator(NotNull(itemPredicate), false),
      AnyTotalValidator(itemMessage ?? FormatString(ArgumentMessage.CollectionDoesNotContainRequiredMatchedElements, collectionParameter), collectionParameter),
      cancellationToken);

  #endregion
  #region Match all elements

  public IEnumerable MatchAll(IEnumerable collection, Predicate<object?> itemPredicate,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter), null,
      AllElementNavigator(IndexableElementPredicate(NotNull(itemPredicate))),
      AllTotalValidator(itemMessage ?? FormatString(ArgumentMessage.CollectionDoesNotContainRequiredMatchedElements, collectionParameter), collectionParameter),
      false);

  public IEnumerable<T> MatchAll<T>(IEnumerable<T> collection, Predicate<T> itemPredicate,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter), null,
      AllElementNavigator(IndexableElementPredicate(NotNull(itemPredicate))),
      AllTotalValidator(itemMessage ?? FormatString(ArgumentMessage.CollectionDoesNotContainRequiredMatchedElements, collectionParameter), collectionParameter),
      false);

  public IAsyncEnumerable<T> MatchAllAsync<T>(IAsyncEnumerable<T> collection, Predicate<T> itemPredicate,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter), null,
      AllElementNavigator(IndexableElementPredicate(NotNull(itemPredicate))),
      AllTotalValidator(itemMessage ?? FormatString(ArgumentMessage.CollectionDoesNotContainRequiredMatchedElements, collectionParameter), collectionParameter),
      cancellationToken);

  public IEnumerable MatchAll(IEnumerable collection, ElementPredicate<object?> itemPredicate,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter), null,
      AllElementNavigator(NotNull(itemPredicate)),
      AllTotalValidator(itemMessage ?? FormatString(ArgumentMessage.CollectionDoesNotContainRequiredMatchedElements, collectionParameter), collectionParameter),
      false);

  public IEnumerable<T> MatchAll<T>(IEnumerable<T> collection, ElementPredicate<T> itemPredicate,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter), null,
      AllElementNavigator(NotNull(itemPredicate)),
      AllTotalValidator(itemMessage ?? FormatString(ArgumentMessage.CollectionDoesNotContainRequiredMatchedElements, collectionParameter), collectionParameter),
      false);

  public IAsyncEnumerable<T> MatchAllAsync<T>(IAsyncEnumerable<T> collection, ElementPredicate<T> itemPredicate,
    string? itemMessage = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter), null,
      AllElementNavigator(NotNull(itemPredicate)),
      AllTotalValidator(itemMessage ?? FormatString(ArgumentMessage.CollectionDoesNotContainRequiredMatchedElements, collectionParameter), collectionParameter),
      cancellationToken);

  #endregion
  #region Empty

  public IEnumerable Empty(IEnumerable collection,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      CountValidator(IsEmpty, message ?? FormatString(ArgumentMessage.CollectionIsNotEmpty, collectionParameter), collectionParameter),
      null, null, false);

  public IEnumerable<T> Empty<T>(IEnumerable<T> collection,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      CountValidator(IsEmpty, message ?? FormatString(ArgumentMessage.CollectionIsNotEmpty, collectionParameter), collectionParameter),
      null, null, false);

  public IAsyncEnumerable<T> EmptyAsync<T>(IAsyncEnumerable<T> collection,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter),
      CountValidator(IsEmpty, message ?? FormatString(ArgumentMessage.CollectionIsNotEmpty, collectionParameter), collectionParameter),
      null, null, cancellationToken);

  #endregion
  #region Not empty

  public IEnumerable NotEmpty(IEnumerable collection,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      CountValidator(IsNotEmpty, message ?? FormatString(ArgumentMessage.CollectionIsEmpty, collectionParameter), collectionParameter),
      null, null, false);

  public IEnumerable<T> NotEmpty<T>(IEnumerable<T> collection,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      CountValidator(IsNotEmpty, message ?? FormatString(ArgumentMessage.CollectionIsEmpty, collectionParameter), collectionParameter),
      null, null, false);

  public IAsyncEnumerable<T> NotEmptyAsync<T>(IAsyncEnumerable<T> collection,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter),
      CountValidator(IsNotEmpty, message ?? FormatString(ArgumentMessage.CollectionIsEmpty, collectionParameter), collectionParameter),
      null, null, cancellationToken);

  #endregion
  #region Exact count

  public IEnumerable ExactCount(IEnumerable collection, int count,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      CountValidator(ExactCountPredicate(count, false), message ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      null, null, false);

  public IEnumerable<T> ExactCount<T>(IEnumerable<T> collection, int count,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      CountValidator(ExactCountPredicate(count, false), message ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      null, null, false);

  public IAsyncEnumerable<T> ExactCountAsync<T>(IAsyncEnumerable<T> collection, int count,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter),
      CountValidator(ExactCountPredicate(count, false), message ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      null, null, cancellationToken);

  #endregion
  #region Not exact count

  public IEnumerable NotExactCount(IEnumerable collection, int count,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      CountValidator(ExactCountPredicate(count, true), message ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      null, null, false);

  public IEnumerable<T> NotExactCount<T>(IEnumerable<T> collection, int count,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      CountValidator(ExactCountPredicate(count, true), message ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      null, null, false);

  public IAsyncEnumerable<T> NotExactCountAsync<T>(IAsyncEnumerable<T> collection, int count,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter),
      CountValidator(ExactCountPredicate(count, true), message ?? FormatString(ArgumentMessage.CollectionElementsCountIsMismatched, collectionParameter), collectionParameter),
      null, null, cancellationToken);

  #endregion
  #region Not null elements

  public IEnumerable NotNullElements(IEnumerable collection, bool lazy = false,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter), null,
      ElementNavigator<object?>(IsNotNull, message ?? FormatString(ArgumentMessage.CollectionContainsNullElements, collectionParameter), collectionParameter),
      null, lazy);

  public IEnumerable<T> NotNullElements<T>(IEnumerable<T?> collection, bool lazy = false,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => (IEnumerable<T>)ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter), null,
      ElementNavigator<T?>(IsNotNull, message ?? FormatString(ArgumentMessage.CollectionContainsNullElements, collectionParameter), collectionParameter),
      null, lazy);

  public IAsyncEnumerable<T> NotNullElementsAsync<T>(IAsyncEnumerable<T?> collection,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
    => (IAsyncEnumerable<T>)ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter), null,
      ElementNavigator<T?>(IsNotNull, message ?? FormatString(ArgumentMessage.CollectionContainsNullElements, collectionParameter), collectionParameter),
      null, cancellationToken);

  public IEnumerable<T> NonNullElements<T>(IEnumerable<T> collection, bool lazy = false,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
  {
    NotNull(collection, valueParameter: collectionParameter);

    return ValidateCollectionCore(collection, null,
      ElementNavigator<T>(IsNotNull, message ?? FormatString(ArgumentMessage.CollectionContainsNullElements, collectionParameter), collectionParameter),
      null, lazy);
  }

  public IAsyncEnumerable<T> NonNullElementsAsync<T>(IAsyncEnumerable<T> collection,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
  {
    NotNull(collection, valueParameter: collectionParameter);

    return ValidateCollectionAsyncCore(collection, null,
      ElementNavigator<T>(IsNotNull, message ?? FormatString(ArgumentMessage.CollectionContainsNullElements, collectionParameter), collectionParameter),
      null, cancellationToken);
  }

  public IEnumerable NotEmptyNorNullElements(IEnumerable collection, bool lazy = false,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      CountValidator(IsEmpty, message ?? FormatString(ArgumentMessage.CollectionIsEmpty, collectionParameter), collectionParameter),
      ElementNavigator<object?>(IsNotNull, message ?? FormatString(ArgumentMessage.CollectionContainsNullElements, collectionParameter), collectionParameter),
      null, lazy);

  public IEnumerable<T> NotEmptyNorNullElements<T>(IEnumerable<T?> collection, bool lazy = false,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => (IEnumerable<T>)ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      CountValidator(IsEmpty, message ?? FormatString(ArgumentMessage.CollectionIsEmpty, collectionParameter), collectionParameter),
      ElementNavigator<T?>(IsNotNull, message ?? FormatString(ArgumentMessage.CollectionContainsNullElements, collectionParameter), collectionParameter),
      null, lazy);

  public IAsyncEnumerable<T> NotEmptyNorNullElementsAsync<T>(IAsyncEnumerable<T?> collection,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
    => (IAsyncEnumerable<T>)ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter),
      CountValidator(IsEmpty, message ?? FormatString(ArgumentMessage.CollectionIsEmpty, collectionParameter), collectionParameter),
      ElementNavigator<T?>(IsNotNull, message ?? FormatString(ArgumentMessage.CollectionContainsNullElements, collectionParameter), collectionParameter),
      null, cancellationToken);

  public IEnumerable<T> NotEmptyNonNullElements<T>(IEnumerable<T> collection, bool lazy = false,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection")
    => ValidateCollectionCore(NotNull(collection, valueParameter: collectionParameter),
      CountValidator(IsEmpty, message ?? FormatString(ArgumentMessage.CollectionIsEmpty, collectionParameter), collectionParameter),
      ElementNavigator<T>(IsNotNull, message ?? FormatString(ArgumentMessage.CollectionContainsNullElements, collectionParameter), collectionParameter),
      null, lazy);

  public IAsyncEnumerable<T> NotEmptyNonNullElementsAsync<T>(IAsyncEnumerable<T> collection,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionAsyncCore(NotNull(collection, valueParameter: collectionParameter),
      CountValidator(IsEmpty, message ?? FormatString(ArgumentMessage.CollectionIsEmpty, collectionParameter), collectionParameter),
      ElementNavigator<T>(IsNotNull, message ?? FormatString(ArgumentMessage.CollectionContainsNullElements, collectionParameter), collectionParameter),
      null, cancellationToken);

  #endregion
  #region Element fail

  [DoesNotReturn]
  [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used indirectly")]
  public void ElementFail(IEnumerable collection, int index, Exception? innerException = null,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection", [CallerArgumentExpression("index")] string? indexParameter = "index")
    => throw new ArgumentCollectionElementException(collectionParameter, message ?? FormatString(ArgumentMessage.CollectionElementFail, collectionParameter, indexParameter), innerException, index);

  [DoesNotReturn]
  [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used indirectly")]
  public void ElementFail<TSource>(IEnumerable<TSource> collection, int index, Exception? innerException = null,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection", [CallerArgumentExpression("index")] string? indexParameter = "index")
    => throw new ArgumentCollectionElementException(collectionParameter, message ?? FormatString(ArgumentMessage.CollectionElementFail, collectionParameter, indexParameter), innerException, index);

  [DoesNotReturn]
  [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used indirectly")]
  public void ElementFail<TSource>(IAsyncEnumerable<TSource> collection, int index, Exception? innerException = null,
    string? message = null, [CallerArgumentExpression("collection")] string? collectionParameter = "collection", [CallerArgumentExpression("index")] string? indexParameter = "index")
    => throw new ArgumentCollectionElementException(collectionParameter, message ?? FormatString(ArgumentMessage.CollectionElementFail, collectionParameter, indexParameter), innerException, index);

  [DoesNotReturn]
  [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used indirectly")]
  public void ElementFail<TSource>(TSource[] array, int index, Exception? innerException = null,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "array", [CallerArgumentExpression("index")] string? indexParameter = "index")
    => throw new ArgumentRegularArrayElementException(arrayParameter, message ?? FormatString(ArgumentMessage.ArrayElementFail, arrayParameter, indexParameter), innerException, index);

  [DoesNotReturn]
  [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used indirectly")]
  public void ElementFail(Array array, int index, Exception? innerException = null,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "array", [CallerArgumentExpression("index")] string? indexParameter = "index")
    => throw new ArgumentRegularArrayElementException(arrayParameter, message ?? FormatString(ArgumentMessage.ArrayElementFail, arrayParameter, indexParameter), innerException, index);

  [DoesNotReturn]
  [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used indirectly")]
  public void ElementFail(Array array, int[] indices, Exception? innerException = null,
    string? message = null, [CallerArgumentExpression("array")] string? arrayParameter = "array", [CallerArgumentExpression("indices")] string? indicesParameter = "indices")
    => throw new ArgumentRegularArrayElementException(arrayParameter, message ?? FormatString(ArgumentMessage.ArrayElementFail, arrayParameter, indicesParameter), innerException, indices);

  #endregion
  #endregion
  #endregion
  #region Collections validation
  #region Internal methods
  #region Enumeration methods

  private static IEnumerable<(TryOut<object> xItem, TryOut<object> yItem)> EnumerateCollectionsCore(IEnumerable xCollection, IEnumerable yCollection,
    Func<(TryOut<object>, TryOut<object>), int, (int, int), NavigateCommand>? itemDelegate, Action<int, (int, int), bool>? totalDelegate)
  {
    var xEnumerator = xCollection.GetEnumerator();
    try
    {
      var yEnumerator = yCollection.GetEnumerator();
      try
      {
        int index = 0, xCount = 0, yCount = 0;
        bool stopped = false, xSuccess, ySuccess;
        for (xSuccess = xEnumerator.MoveNext(), ySuccess = yEnumerator.MoveNext();
          !stopped && (xSuccess || ySuccess);
          xSuccess &= xEnumerator.MoveNext(), ySuccess &= yEnumerator.MoveNext())
        {
          var xItem = xSuccess ? TryOut.Success<object>(xEnumerator.Current) : TryOut.Failure<object>();
          var yItem = ySuccess ? TryOut.Success<object>(yEnumerator.Current) : TryOut.Failure<object>();
          var navigateCommand = itemDelegate?.Invoke((xItem, yItem), index++, (xCount, yCount)) ?? NavigateCommand.None;
          yield return (xItem, yItem);
          if ((navigateCommand & NavigateCommand.Skip) == 0)
          {
            xCount += xSuccess ? 1 : 0;
            yCount += ySuccess ? 1 : 0;
          }
          if ((navigateCommand & NavigateCommand.Stop) != 0)
            stopped = true;
        }
        totalDelegate?.Invoke(index, (xCount, yCount), stopped);
      }
      finally
      {
        Disposable.BlindDispose(ref yEnumerator);
      }
    }
    finally
    {
      Disposable.BlindDispose(ref xEnumerator);
    }
  }

  private static IEnumerable<(TryOut<TX> xItem, TryOut<TY> yItem)> EnumerateCollectionsCore<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Func<(TryOut<TX>, TryOut<TY>), int, (int, int), NavigateCommand>? itemDelegate, Action<int, (int, int), bool>? totalDelegate)
  {
    using var xEnumerator = xCollection.GetEnumerator();
    using var yEnumerator = yCollection.GetEnumerator();
    int index = 0, xCount = 0, yCount = 0;
    bool stopped = false, xSuccess, ySuccess;
    for (xSuccess = xEnumerator.MoveNext(), ySuccess = yEnumerator.MoveNext();
      !stopped && (xSuccess || ySuccess);
      xSuccess &= xEnumerator.MoveNext(), ySuccess &= yEnumerator.MoveNext())
    {
      var xItem = xSuccess ? TryOut.Success(xEnumerator.Current) : TryOut.Failure<TX>();
      var yItem = ySuccess ? TryOut.Success(yEnumerator.Current) : TryOut.Failure<TY>();
      var navigateCommand = itemDelegate?.Invoke((xItem, yItem), index++, (xCount, yCount)) ?? NavigateCommand.None;
      yield return (xItem, yItem);
      if ((navigateCommand & NavigateCommand.Skip) == 0)
      {
        xCount += xSuccess ? 1 : 0;
        yCount += ySuccess ? 1 : 0;
      }
      if ((navigateCommand & NavigateCommand.Stop) != 0)
        stopped = true;
    }
    totalDelegate?.Invoke(index, (xCount, yCount), stopped);
  }

  private static async IAsyncEnumerable<(TryOut<TX> xItem, TryOut<TY> yItem)> EnumerateCollectionsAsyncCore<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Func<(TryOut<TX>, TryOut<TY>), int, (int, int), NavigateCommand>? itemDelegate, Action<int, (int, int), bool>? totalDelegate,
    [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    var xEnumerator = xCollection.GetAsyncEnumerator(cancellationToken);
    try
    {
      using var yEnumerator = yCollection.GetEnumerator();
      int index = 0, xCount = 0, yCount = 0;
      bool stopped = false, xSuccess, ySuccess;
      for (xSuccess = await xEnumerator.MoveNextAsync(), ySuccess = yEnumerator.MoveNext();
        !stopped && (xSuccess || ySuccess);
        xSuccess &= await xEnumerator.MoveNextAsync(), ySuccess &= yEnumerator.MoveNext())
      {
        var xItem = xSuccess ? TryOut.Success(xEnumerator.Current) : TryOut.Failure<TX>();
        var yItem = ySuccess ? TryOut.Success(yEnumerator.Current) : TryOut.Failure<TY>();
        var navigateCommand = itemDelegate?.Invoke((xItem, yItem), index++, (xCount, yCount)) ?? NavigateCommand.None;
        yield return (xItem, yItem);
        if ((navigateCommand & NavigateCommand.Skip) == 0)
        {
          xCount += xSuccess ? 1 : 0;
          yCount += ySuccess ? 1 : 0;
        }
        if ((navigateCommand & NavigateCommand.Stop) != 0)
          stopped = true;
      }
      totalDelegate?.Invoke(index, (xCount, yCount), stopped);
    }
    finally
    {
      await AsyncDisposable.DisposeAsync(ref xEnumerator);
    }
  }

  private static async IAsyncEnumerable<(TryOut<TX> xItem, TryOut<TY> yItem)> EnumerateCollectionsAsyncCore<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    Func<(TryOut<TX>, TryOut<TY>), int, (int, int), NavigateCommand>? itemDelegate, Action<int, (int, int), bool>? totalDelegate,
    [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    var xEnumerator = xCollection.GetAsyncEnumerator(cancellationToken);
    try
    {
      var yEnumerator = yCollection.GetAsyncEnumerator(cancellationToken);
      try
      {
        int index = 0, xCount = 0, yCount = 0;
        bool stopped = false, xSuccess, ySuccess;
        for (xSuccess = await xEnumerator.MoveNextAsync(), ySuccess = await yEnumerator.MoveNextAsync();
          !stopped && (xSuccess || ySuccess);
          xSuccess &= await xEnumerator.MoveNextAsync(), ySuccess &= await yEnumerator.MoveNextAsync())
        {
          var xItem = xSuccess ? TryOut.Success(xEnumerator.Current) : TryOut.Failure<TX>();
          var yItem = ySuccess ? TryOut.Success(yEnumerator.Current) : TryOut.Failure<TY>();
          var navigateCommand = itemDelegate?.Invoke((xItem, yItem), index++, (xCount, yCount)) ?? NavigateCommand.None;
          yield return (xItem, yItem);
          if ((navigateCommand & NavigateCommand.Skip) == 0)
          {
            xCount += xSuccess ? 1 : 0;
            yCount += ySuccess ? 1 : 0;
          }
          if ((navigateCommand & NavigateCommand.Stop) != 0)
            stopped = true;
        }
        totalDelegate?.Invoke(index, (xCount, yCount), stopped);
      }
      finally
      {
        await AsyncDisposable.DisposeAsync(ref yEnumerator);
      }
    }
    finally
    {
      await AsyncDisposable.DisposeAsync(ref xEnumerator);
    }
  }

  #endregion
  #region Validation methods

  private static IEnumerable<(TryOut<object> xItem, TryOut<object> yItem)> ValidateCollectionsCore(IEnumerable xCollection, IEnumerable yCollection,
    Action<(int, int)>? countsValidator, Func<(TryOut<object>, TryOut<object>), int, (int, int), NavigateCommand>? itemsValidator, Action<int, (int, int), bool>? totalsValidator, bool lazy)
  {
    if (countsValidator is not null)
    {
      var xCount = xCollection.PeekCount();
      var yCount = yCollection.PeekCount();
      if (xCount >= 0 && yCount >= 0)
        countsValidator.Invoke((xCount, yCount));
      else
        totalsValidator = TotalsValidator(countsValidator, totalsValidator);
    }
    var result = EnumerateCollectionsCore(xCollection, yCollection, itemsValidator, totalsValidator);
    if (lazy)
      return result;
    result.Enumerate();
    return EnumerateCollectionsCore(xCollection, yCollection, null, null);
  }

  private static IEnumerable<(TryOut<TX> xItem, TryOut<TY> yItem)> ValidateCollectionsCore<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Action<(int, int)>? countsValidator, Func<(TryOut<TX>, TryOut<TY>), int, (int, int), NavigateCommand>? itemsValidator, Action<int, (int, int), bool>? totalsValidator, bool lazy)
  {
    if (countsValidator is not null)
    {
      var xCount = xCollection.PeekCount();
      var yCount = yCollection.PeekCount();
      if (xCount >= 0 && yCount >= 0)
        countsValidator.Invoke((xCount, yCount));
      else
        totalsValidator = TotalsValidator(countsValidator, totalsValidator);
    }
    var result = EnumerateCollectionsCore(xCollection, yCollection, itemsValidator, totalsValidator);
    if (lazy)
      return result;
    result.Enumerate();
    return EnumerateCollectionsCore(xCollection, yCollection, null, null);
  }

  private static IAsyncEnumerable<(TryOut<TX> xItem, TryOut<TY> yItem)> ValidateCollectionsAsyncCore<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Action<(int, int)>? countsValidator, Func<(TryOut<TX>, TryOut<TY>), int, (int, int), NavigateCommand>? itemsValidator, Action<int, (int, int), bool>? totalsValidator, CancellationToken cancellationToken)
  {
    if (countsValidator is not null)
      totalsValidator = TotalsValidator(countsValidator, totalsValidator);
    return EnumerateCollectionsAsyncCore(xCollection, yCollection, itemsValidator, totalsValidator, cancellationToken);
  }

  private static IAsyncEnumerable<(TryOut<TX> xItem, TryOut<TY> yItem)> ValidateCollectionsAsyncCore<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    Action<(int, int)>? countsValidator, Func<(TryOut<TX>, TryOut<TY>), int, (int, int), NavigateCommand>? itemsValidator, Action<int, (int, int), bool>? totalsValidator, CancellationToken cancellationToken)
  {
    if (countsValidator is not null)
      totalsValidator = TotalsValidator(countsValidator, totalsValidator);
    return EnumerateCollectionsAsyncCore(xCollection, yCollection, itemsValidator, totalsValidator, cancellationToken);
  }

  #endregion
  #region Validation delegates

  private Action<(int xCount, int yCount)> ImbalanceValidator(string? message, string? xCollectionParameter, string? yCollectionParameter)
    => (counts) =>
    {
      if (counts.xCount != counts.yCount)
        throw new ArgumentException(message, FormatList(xCollectionParameter, yCollectionParameter));
    };

  private Action<(int xCount, int yCount)> CoupledCountsValidator(Action<int>? validator,
    string? countMessage, string? imbalancedMessage, string? xCollectionParameter, string? yCollectionParameter)
    => (counts) =>
    {
      if (counts.xCount != counts.yCount)
        throw new ArgumentException(imbalancedMessage, FormatList(xCollectionParameter, yCollectionParameter));
      if (validator is not null)
        try
        {
          validator(counts.xCount);
        }
        catch (Exception ex)
        {
          throw new ArgumentException(countMessage, FormatList(xCollectionParameter, yCollectionParameter), ex);
        }
    };

  private Action<(int xCount, int yCount)> CoupledCountsValidator(Predicate<int>? predicate,
    string? countMessage, string? imbalancedMessage, string? xCollectionParameter, string? yCollectionParameter)
    => (counts) =>
    {
      if (counts.xCount != counts.yCount)
        throw new ArgumentException(imbalancedMessage, FormatList(xCollectionParameter, yCollectionParameter));
      if (!predicate?.Invoke(counts.xCount) ?? false)
        throw new ArgumentException(countMessage, FormatList(xCollectionParameter, yCollectionParameter));
    };

  private Action<(int xCount, int yCount)> PairedCountsValidator(Action<(int xCount, int yCount)>? validator,
    string? countsMessage, string? xCollectionParameter, string? yCollectionParameter)
    => (counts) =>
    {
      if (validator is not null)
        try
        {
          validator(counts);
        }
        catch (Exception ex)
        {
          throw new ArgumentException(countsMessage, FormatList(xCollectionParameter, yCollectionParameter), ex);
        }
    };

  private Action<(int xCount, int yCount)> PairedCountsValidator(Predicate<(int xCount, int yCount)>? predicate,
    string? countsMessage, string? xCollectionParameter, string? yCollectionParameter)
    => (counts) =>
    {
      if (!predicate?.Invoke(counts) ?? false)
        throw new ArgumentException(countsMessage, FormatList(xCollectionParameter, yCollectionParameter));
    };

  private Func<(TryOut<TX> xItem, TryOut<TY> yItem), int, (int, int), NavigateCommand> CoupledElementsValidator<TX, TY>(
    ElementAction<TX?>? xItemValidator, ElementAction<TY?>? yItemValidator, ElementAction<(TX?, TY?)>? xyItemsValidator,
    string? xItemMessage, string? yItemMessage, string? xyItemsMessage, string? imbalancedMessage,
    string? xCollectionParameter, string? yCollectionParameter)
    => (couple, index, counts) =>
    {
      if (couple.xItem.Success ^ couple.yItem.Success)
        throw new ArgumentCollectionElementException(FormatList(xCollectionParameter, yCollectionParameter), imbalancedMessage, index);
      if (xItemValidator is not null)
      {
        try
        {
          xItemValidator(couple.xItem.Value, index);
        }
        catch (Exception ex)
        {
          throw new ArgumentCollectionElementException(xCollectionParameter, xItemMessage, ex, index);
        }
      }
      if (yItemValidator is not null)
      {
        try
        {
          yItemValidator(couple.yItem.Value, index);
        }
        catch (Exception ex)
        {
          throw new ArgumentCollectionElementException(yCollectionParameter, yItemMessage, ex, index);
        }
      }
      if (xyItemsValidator is not null)
      {
        try
        {
          xyItemsValidator((couple.xItem.Value, couple.yItem.Value), index);
        }
        catch (Exception ex)
        {
          throw new ArgumentCollectionElementException(FormatList(xCollectionParameter, yCollectionParameter), xyItemsMessage, ex, index);
        }
      }
      return NavigateCommand.None;
    };

  private Func<(TryOut<TX> xItem, TryOut<TY> yItem), int, (int, int), NavigateCommand> CoupledElementsValidator<TX, TY>(
    ElementPredicate<TX?>? xItemPredicate, ElementPredicate<TY?>? yItemPredicate, ElementPredicate<(TX?, TY?)>? xyItemsPredicate,
    string? xItemMessage, string? yItemMessage, string? xyItemsMessage, string? imbalancedMessage,
    string? xCollectionParameter, string? yCollectionParameter)
    => (couple, index, counts) =>
    {
      if (couple.xItem.Success ^ couple.yItem.Success)
        throw new ArgumentCollectionElementException(FormatList(xCollectionParameter, yCollectionParameter), imbalancedMessage, index);
      if (!xItemPredicate?.Invoke(couple.xItem.Value, index) ?? false)
        throw new ArgumentCollectionElementException(xCollectionParameter, xItemMessage, index);
      if (!yItemPredicate?.Invoke(couple.yItem.Value, index) ?? false)
        throw new ArgumentCollectionElementException(yCollectionParameter, yItemMessage, index);
      if (!xyItemsPredicate?.Invoke((couple.xItem.Value, couple.yItem.Value), index) ?? false)
        throw new ArgumentCollectionElementException(FormatList(xCollectionParameter, yCollectionParameter), xyItemsMessage, index);
      return NavigateCommand.None;
    };

  private Func<(TryOut<TX> xItem, TryOut<TY> yItem), int, (int, int), NavigateCommand> PairedElementsValidator<TX, TY>(
    ElementAction<TX?>? xItemValidator, ElementAction<TY?>? yItemValidator, ElementAction<(TX?, TY?)>? xyItemsValidator,
    string? xItemMessage, string? yItemMessage, string? xyItemsMessage,
    string? xCollectionParameter, string? yCollectionParameter)
    => (pair, index, counts) =>
    {
      if (pair.xItem.Success && xItemValidator is not null)
      {
        try
        {
          xItemValidator(pair.xItem.Value, index);
        }
        catch (Exception ex)
        {
          throw new ArgumentCollectionElementException(xCollectionParameter, xItemMessage, ex, index);
        }
      }
      if (pair.yItem.Success && yItemValidator is not null)
      {
        try
        {
          yItemValidator(pair.yItem.Value, index);
        }
        catch (Exception ex)
        {
          throw new ArgumentCollectionElementException(yCollectionParameter, yItemMessage, ex, index);
        }
      }
      if (pair.xItem.Success && pair.yItem.Success && xyItemsValidator is not null)
      {
        try
        {
          xyItemsValidator((pair.xItem.Value, pair.yItem.Value), index);
        }
        catch (Exception ex)
        {
          throw new ArgumentCollectionElementException(FormatList(xCollectionParameter, yCollectionParameter), xyItemsMessage, ex, index);
        }
      }
      return NavigateCommand.None;
    };

  private Func<(TryOut<TX> xItem, TryOut<TY> yItem), int, (int, int), NavigateCommand> PairedElementsValidator<TX, TY>(
    ElementPredicate<TX?>? xItemPredicate, ElementPredicate<TY?>? yItemPredicate, ElementPredicate<(TX?, TY?)>? xyItemsPredicate,
    string? xItemMessage, string? yItemMessage, string? xyItemsMessage,
    string? xCollectionParameter, string? yCollectionParameter)
    => (pair, index, counts) =>
    {
      if (pair.xItem.Success && (!xItemPredicate?.Invoke(pair.xItem.Value, index) ?? false))
        throw new ArgumentCollectionElementException(xCollectionParameter, xItemMessage, index);
      if (pair.yItem.Success && (!yItemPredicate?.Invoke(pair.yItem.Value, index) ?? false))
        throw new ArgumentCollectionElementException(yCollectionParameter, yItemMessage, index);
      if (pair.xItem.Success && pair.yItem.Success && (!xyItemsPredicate?.Invoke((pair.xItem.Value, pair.yItem.Value), index) ?? false))
        throw new ArgumentCollectionElementException(FormatList(xCollectionParameter, yCollectionParameter), xyItemsMessage, index);
      return NavigateCommand.None;
    };

  private Func<(TryOut<T> xItem, TryOut<T> yItem), int, (int xCount, int yCount), NavigateCommand> CoupledElementsEqualityNavigator<T>(Equality<T?> equality, bool negate,
    string? message, string? xCollectionParameter, string? yCollectionParameter)
    => (couple, index, counts) =>
    {
      if (couple.xItem.Success ^ couple.yItem.Success)
        throw new ArgumentException(message, FormatList(xCollectionParameter, yCollectionParameter));

      var equal = equality(couple.xItem.Value, couple.yItem.Value);
      return equal ? NavigateCommand.None : (NavigateCommand.Skip | (!negate ? NavigateCommand.Stop : NavigateCommand.None));
    };

  private Func<(TryOut<T> xItem, TryOut<T> yItem), int, (int xCount, int yCount), NavigateCommand> CoupledElementsComparisonNavigator<T>(Comparison<T?> comparison, ComparisonCriteria criteria, bool negate,
    string? message, string? xCollectionParameter, string? yCollectionParameter)
  => (couple, index, counts) =>
  {
    if (couple.xItem.Success ^ couple.yItem.Success)
      throw new ArgumentException(message, FormatList(xCollectionParameter, yCollectionParameter));

    var result = comparison(couple.xItem.Value, couple.yItem.Value);
    var matched = Comparison.Match(result, criteria) ^ negate;
    return result == 0 ? NavigateCommand.None : (NavigateCommand.Skip | (!matched && (index == counts.xCount || index == counts.yCount) ? NavigateCommand.Stop : NavigateCommand.None));
  };

  private Func<(TryOut<T> xItem, TryOut<T> yItem), int, (int xCount, int yCount), NavigateCommand> PairedElementsEqualityNavigator<T>(Equality<T?> equality, bool negate)
    => (couple, index, counts) =>
    {
      var equal = couple.xItem.Success && couple.yItem.Success && equality(couple.xItem.Value, couple.yItem.Value);
      return equal ? NavigateCommand.None : (NavigateCommand.Skip | (!negate ? NavigateCommand.Stop : NavigateCommand.None));
    };

  private Func<(TryOut<T> xItem, TryOut<T> yItem), int, (int xCount, int yCount), NavigateCommand> PairedElementsComparisonNavigator<T>(Comparison<T?> comparison, ComparisonCriteria criteria, RelativeOrder emptyOrder, bool negate)
    => (couple, index, counts) =>
    {
      var result = couple.xItem.Success ? couple.yItem.Success ? comparison(couple.xItem.Value, couple.yItem.Value) :
        emptyOrder switch { RelativeOrder.Lower => 1, RelativeOrder.Upper => -1, _ => Invalid(emptyOrder) } :
        couple.yItem.Success ? emptyOrder switch { RelativeOrder.Lower => -1, RelativeOrder.Upper => 1, _ => Invalid(emptyOrder) } :
        Operation.That.Failed();
      var matched = Comparison.Match(result, criteria) ^ negate;
      return result == 0 ? NavigateCommand.None : (NavigateCommand.Skip | (!matched && (index == counts.xCount || index == counts.yCount) ? NavigateCommand.Stop : NavigateCommand.None));
    };

  private static Action<int, (int, int), bool> TotalsValidator(Action<(int, int)> countsValidator, Action<int, (int, int), bool>? totalsValidator)
    => (total, counts, stopped) =>
      {
        if (!stopped)
          countsValidator.Invoke(counts);
        totalsValidator?.Invoke(total, counts, stopped);
      };

  private Action<int, (int xCount, int yCount), bool> EqualityTotalValidator(bool negate,
    string? message, string? xCollectionParameter, string? yCollectionParameter)
    => (total, matched, stopped) =>
    {
      if (!negate && stopped && (total != matched.xCount || total != matched.yCount) || negate && !stopped && total == matched.xCount && total == matched.yCount)
        throw new ArgumentCollectionElementException(FormatList(xCollectionParameter, yCollectionParameter), message, total);
    };

  private Action<int, (int xCount, int yCount), bool> ComparisonTotalValidator(ComparisonCriteria criteria, bool negate,
    string? message, string? xCollectionParameter, string? yCollectionParameter)
    => (total, matched, stopped) =>
    {
      if (stopped && (total != matched.xCount && total != matched.yCount) || !stopped && total == matched.xCount && total == matched.yCount && (Comparison.Match(0, criteria) ^ !negate))
        throw new ArgumentCollectionElementException(FormatList(xCollectionParameter, yCollectionParameter), message, total);
    };

  #endregion
  #endregion
  #region Public methods
  #region Validators

  #endregion
  #region Validate coupled collections

  public IEnumerable<(object? x, object? y)> ValidateCoupledCount(IEnumerable xCollection, IEnumerable yCollection,
    Action<int>? countValidator, bool lazy = false,
    string? countMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(countValidator,
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(TX? x, TY? y)> ValidateCoupledCount<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Action<int>? countValidator, bool lazy = false,
    string? countMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(countValidator,
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> ValidateCoupledCountAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Action<int>? countValidator,
    string? countMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(countValidator,
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> ValidateCoupledCountAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    Action<int>? countValidator,
    string? countMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(countValidator,
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> ValidateCoupledElements(IEnumerable xCollection, IEnumerable yCollection,
    Action<(object? x, object? y)> xyItemsValidator, bool lazy = false,
    string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, IndexableElementValidator(NotNull(xyItemsValidator)), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> ValidateCoupledElements(IEnumerable xCollection, IEnumerable yCollection,
    Action<object?> xItemValidator, Action<object?> yItemValidator, Action<(object? x, object? y)> xyItemsValidator, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(IndexableElementValidator(NotNull(xItemValidator)), IndexableElementValidator(NotNull(yItemValidator)), IndexableElementValidator(NotNull(xyItemsValidator)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> ValidateCoupledElements(IEnumerable xCollection, IEnumerable yCollection,
    ElementAction<(object? x, object? y)> xyItemsValidator, bool lazy = false,
    string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, NotNull(xyItemsValidator), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> ValidateCoupledElements(IEnumerable xCollection, IEnumerable yCollection,
    ElementAction<object?> xItemValidator, ElementAction<object?> yItemValidator, ElementAction<(object? x, object? y)> xyItemsValidator, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(NotNull(xItemValidator), NotNull(yItemValidator), NotNull(xyItemsValidator),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(TX? x, TY? y)> ValidateCoupledElements<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Action<(TX? x, TY? y)> xyItemsValidator, bool lazy = false,
    string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, IndexableElementValidator(NotNull(xyItemsValidator)), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(TX? x, TY? y)> ValidateCoupledElements<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Action<TX?> xItemValidator, Action<TY?> yItemValidator, Action<(TX? x, TY? y)> xyItemsValidator, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(IndexableElementValidator(NotNull(xItemValidator)), IndexableElementValidator(NotNull(yItemValidator)), IndexableElementValidator(NotNull(xyItemsValidator)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(TX? x, TY? y)> ValidateCoupledElements<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    ElementAction<(TX? x, TY? y)> xyItemsValidator, bool lazy = false,
    string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, NotNull(xyItemsValidator), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(TX? x, TY? y)> ValidateCoupledElements<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    ElementAction<TX?> xItemValidator, ElementAction<TY?> yItemValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(NotNull(xItemValidator), NotNull(yItemValidator), NotNull(xyItemsValidator),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> ValidateCoupledElementsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Action<(TX? x, TY? y)> xyItemsValidator,
    string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, IndexableElementValidator(NotNull(xyItemsValidator)), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> ValidateCoupledElementsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Action<TX?> xItemValidator, Action<TY?> yItemValidator, Action<(TX? x, TY? y)> xyItemsValidator,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(IndexableElementValidator(NotNull(xItemValidator)), IndexableElementValidator(NotNull(yItemValidator)), IndexableElementValidator(NotNull(xyItemsValidator)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> ValidateCoupledElementsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    ElementAction<(TX? x, TY? y)> xyItemsValidator,
    string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, NotNull(xyItemsValidator), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> ValidateCoupledElementsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    ElementAction<TX?> xItemValidator, ElementAction<TY?> yItemValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(NotNull(xItemValidator), NotNull(yItemValidator), NotNull(xyItemsValidator),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> ValidateCoupledElementsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    Action<(TX? x, TY? y)> xyItemsValidator,
    string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, IndexableElementValidator(NotNull(xyItemsValidator)), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> ValidateCoupledElementsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    Action<TX?> xItemValidator, Action<TY?> yItemValidator, Action<(TX? x, TY? y)> xyItemsValidator,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(IndexableElementValidator(NotNull(xItemValidator)), IndexableElementValidator(NotNull(yItemValidator)), IndexableElementValidator(NotNull(xyItemsValidator)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> ValidateCoupledElementsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    ElementAction<(TX? x, TY? y)> xyItemsValidator,
    string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, NotNull(xyItemsValidator), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> ValidateCoupledElementsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    ElementAction<TX?> xItemValidator, ElementAction<TY?> yItemValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(NotNull(xItemValidator), NotNull(yItemValidator), NotNull(xyItemsValidator),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> ValidateCoupled(IEnumerable xCollection, IEnumerable yCollection,
    Action<int> countValidator, Action<(object? x, object? y)> xyItemsValidator, bool lazy = false,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countValidator),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, IndexableElementValidator(NotNull(xyItemsValidator)), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> ValidateCoupled(IEnumerable xCollection, IEnumerable yCollection,
    Action<int> countValidator, Action<object?> xItemValidator, Action<object?> yItemValidator, Action<(object? x, object? y)> xyItemsValidator, bool lazy = false,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countValidator),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(IndexableElementValidator(NotNull(xItemValidator)), IndexableElementValidator(NotNull(yItemValidator)), IndexableElementValidator(NotNull(xyItemsValidator)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> ValidateCoupled(IEnumerable xCollection, IEnumerable yCollection,
    Action<int> countValidator, ElementAction<(object? x, object? y)> xyItemsValidator, bool lazy = false,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countValidator),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, NotNull(xyItemsValidator), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> ValidateCoupled(IEnumerable xCollection, IEnumerable yCollection,
    Action<int> countValidator, ElementAction<object?> xItemValidator, ElementAction<object?> yItemValidator, ElementAction<(object? x, object? y)> xyItemsValidator, bool lazy = false,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countValidator),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(NotNull(xItemValidator), NotNull(yItemValidator), NotNull(xyItemsValidator),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(TX? x, TY? y)> ValidateCoupled<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Action<int> countValidator, Action<(TX? x, TY? y)> xyItemsValidator, bool lazy = false,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countValidator),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, IndexableElementValidator(NotNull(xyItemsValidator)), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(TX? x, TY? y)> ValidateCoupled<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Action<int> countValidator, Action<TX?> xItemValidator, Action<TY?> yItemValidator, Action<(TX? x, TY? y)> xyItemsValidator, bool lazy = false,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countValidator),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(IndexableElementValidator(NotNull(xItemValidator)), IndexableElementValidator(NotNull(yItemValidator)), IndexableElementValidator(NotNull(xyItemsValidator)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(TX? x, TY? y)> ValidateCoupled<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Action<int> countValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator, bool lazy = false,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countValidator),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, NotNull(xyItemsValidator), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(TX? x, TY? y)> ValidateCoupled<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Action<int> countValidator, ElementAction<TX?> xItemValidator, ElementAction<TY?> yItemValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator, bool lazy = false,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countValidator),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(NotNull(xItemValidator), NotNull(yItemValidator), NotNull(xyItemsValidator),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> ValidateCoupledAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Action<int> countValidator, Action<(TX? x, TY? y)> xyItemsValidator,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countValidator),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, IndexableElementValidator(NotNull(xyItemsValidator)), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> ValidateCoupledAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Action<int> countValidator, Action<TX?> xItemValidator, Action<TY?> yItemValidator, Action<(TX? x, TY? y)> xyItemsValidator,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countValidator),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(IndexableElementValidator(NotNull(xItemValidator)), IndexableElementValidator(NotNull(yItemValidator)), IndexableElementValidator(NotNull(xyItemsValidator)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> ValidateCoupledAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Action<int> countValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countValidator),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, NotNull(xyItemsValidator), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> ValidateCoupledAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Action<int> countValidator, ElementAction<TX?> xItemValidator, ElementAction<TY?> yItemValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countValidator),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(NotNull(xItemValidator), NotNull(yItemValidator), NotNull(xyItemsValidator),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> ValidateCoupledAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    Action<int> countValidator, Action<(TX? x, TY? y)> xyItemsValidator,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countValidator),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, IndexableElementValidator(NotNull(xyItemsValidator)), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> ValidateCoupledAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    Action<int> countValidator, Action<TX?> xItemValidator, Action<TY?> yItemValidator, Action<(TX? x, TY? y)> xyItemsValidator,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countValidator),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(IndexableElementValidator(NotNull(xItemValidator)), IndexableElementValidator(NotNull(yItemValidator)), IndexableElementValidator(NotNull(xyItemsValidator)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> ValidateCoupledAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    Action<int> countValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countValidator),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, NotNull(xyItemsValidator), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> ValidateCoupledAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    Action<int> countValidator, ElementAction<TX?> xItemValidator, ElementAction<TY?> yItemValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countValidator),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(NotNull(xItemValidator), NotNull(yItemValidator), NotNull(xyItemsValidator),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  #endregion
  #region Match coupled collections

  public IEnumerable<(object? x, object? y)> MatchCoupledCount(IEnumerable xCollection, IEnumerable yCollection,
    Predicate<int>? countPredicate, bool lazy = false,
    string? countMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(countPredicate,
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(TX? x, TY? y)> MatchCoupledCount<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Predicate<int>? countPredicate, bool lazy = false,
    string? countMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(countPredicate,
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> MatchCoupledCountAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Predicate<int>? countPredicate,
    string? countMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(countPredicate,
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> MatchCoupledCountAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    Predicate<int>? countPredicate,
    string? countMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(countPredicate,
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> MatchCoupledElements(IEnumerable xCollection, IEnumerable yCollection,
    Predicate<(object? x, object? y)> xyItemsPredicate, bool lazy = false,
    string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, IndexableElementPredicate(NotNull(xyItemsPredicate)), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> MatchCoupledElements(IEnumerable xCollection, IEnumerable yCollection,
    Predicate<object?> xItemPredicate, Predicate<object?> yItemPredicate, Predicate<(object? x, object? y)> xyItemsPredicate, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(IndexableElementPredicate(NotNull(xItemPredicate)), IndexableElementPredicate(NotNull(yItemPredicate)), IndexableElementPredicate(NotNull(xyItemsPredicate)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> MatchCoupledElements(IEnumerable xCollection, IEnumerable yCollection,
    ElementPredicate<(object? x, object? y)> xyItemsPredicate, bool lazy = false,
    string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, NotNull(xyItemsPredicate), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> MatchCoupledElements(IEnumerable xCollection, IEnumerable yCollection,
    ElementPredicate<object?> xItemPredicate, ElementPredicate<object?> yItemPredicate, ElementPredicate<(object? x, object? y)> xyItemsPredicate, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(NotNull(xItemPredicate), NotNull(yItemPredicate), NotNull(xyItemsPredicate),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(TX? x, TY? y)> MatchCoupledElements<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Predicate<(TX? x, TY? y)> xyItemsPredicate, bool lazy = false,
    string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, IndexableElementPredicate(NotNull(xyItemsPredicate)), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(TX? x, TY? y)> MatchCoupledElements<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Predicate<TX?> xItemPredicate, Predicate<TY?> yItemPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(IndexableElementPredicate(NotNull(xItemPredicate)), IndexableElementPredicate(NotNull(yItemPredicate)), IndexableElementPredicate(NotNull(xyItemsPredicate)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(TX? x, TY? y)> MatchCoupledElements<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    ElementPredicate<(TX? x, TY? y)> xyItemsPredicate, bool lazy = false,
    string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, NotNull(xyItemsPredicate), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(TX? x, TY? y)> MatchCoupledElements<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    ElementPredicate<TX?> xItemPredicate, ElementPredicate<TY?> yItemPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(NotNull(xItemPredicate), NotNull(yItemPredicate), NotNull(xyItemsPredicate),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> MatchCoupledElementsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Predicate<(TX? x, TY? y)> xyItemsPredicate,
    string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, IndexableElementPredicate(NotNull(xyItemsPredicate)), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> MatchCoupledElementsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Predicate<TX?> xItemPredicate, Predicate<TY?> yItemPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(IndexableElementPredicate(NotNull(xItemPredicate)), IndexableElementPredicate(NotNull(yItemPredicate)), IndexableElementPredicate(NotNull(xyItemsPredicate)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> MatchCoupledElementsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    ElementPredicate<(TX? x, TY? y)> xyItemsPredicate,
    string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, NotNull(xyItemsPredicate), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> MatchCoupledElementsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    ElementPredicate<TX?> xItemPredicate, ElementPredicate<TY?> yItemPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(NotNull(xItemPredicate), NotNull(yItemPredicate), NotNull(xyItemsPredicate),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> MatchCoupledElementsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    Predicate<(TX? x, TY? y)> xyItemsPredicate,
    string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, IndexableElementPredicate(NotNull(xyItemsPredicate)), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> MatchCoupledElementsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    Predicate<TX?> xItemPredicate, Predicate<TY?> yItemPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(IndexableElementPredicate(NotNull(xItemPredicate)), IndexableElementPredicate(NotNull(yItemPredicate)), IndexableElementPredicate(NotNull(xyItemsPredicate)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> MatchCoupledElementsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    ElementPredicate<(TX? x, TY? y)> xyItemsPredicate,
    string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, NotNull(xyItemsPredicate), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> MatchCoupledElementsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    ElementPredicate<TX?> xItemPredicate, ElementPredicate<TY?> yItemPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(null, null,
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(NotNull(xItemPredicate), NotNull(yItemPredicate), NotNull(xyItemsPredicate),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> MatchCoupled(IEnumerable xCollection, IEnumerable yCollection,
    Predicate<int> countPredicate, Predicate<(object? x, object? y)> xyItemsPredicate, bool lazy = false,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countPredicate),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, IndexableElementPredicate(NotNull(xyItemsPredicate)), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> MatchCoupled(IEnumerable xCollection, IEnumerable yCollection,
    Predicate<int> countPredicate, Predicate<object?> xItemPredicate, Predicate<object?> yItemPredicate, Predicate<(object? x, object? y)> xyItemsPredicate, bool lazy = false,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countPredicate),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(IndexableElementPredicate(NotNull(xItemPredicate)), IndexableElementPredicate(NotNull(yItemPredicate)), IndexableElementPredicate(NotNull(xyItemsPredicate)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> MatchCoupled(IEnumerable xCollection, IEnumerable yCollection,
    Predicate<int> countPredicate, ElementPredicate<(object? x, object? y)> xyItemsPredicate, bool lazy = false,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countPredicate),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, NotNull(xyItemsPredicate), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> MatchCoupled(IEnumerable xCollection, IEnumerable yCollection,
    Predicate<int> countPredicate, ElementPredicate<object?> xItemPredicate, ElementPredicate<object?> yItemPredicate, ElementPredicate<(object? x, object? y)> xyItemsPredicate, bool lazy = false,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countPredicate),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(NotNull(xItemPredicate), NotNull(yItemPredicate), NotNull(xyItemsPredicate),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(TX? x, TY? y)> MatchCoupled<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Predicate<int> countPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate, bool lazy = false,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countPredicate),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, IndexableElementPredicate(NotNull(xyItemsPredicate)), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(TX? x, TY? y)> MatchCoupled<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Predicate<int> countPredicate, Predicate<TX?> xItemPredicate, Predicate<TY?> yItemPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate, bool lazy = false,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countPredicate),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(IndexableElementPredicate(NotNull(xItemPredicate)), IndexableElementPredicate(NotNull(yItemPredicate)), IndexableElementPredicate(NotNull(xyItemsPredicate)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(TX? x, TY? y)> MatchCoupled<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Predicate<int> countPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate, bool lazy = false,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countPredicate),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, NotNull(xyItemsPredicate), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(TX? x, TY? y)> MatchCoupled<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Predicate<int> countPredicate, ElementPredicate<TX?> xItemPredicate, ElementPredicate<TY?> yItemPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate, bool lazy = false,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countPredicate),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(NotNull(xItemPredicate), NotNull(yItemPredicate), NotNull(xyItemsPredicate),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> MatchCoupledAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Predicate<int> countPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countPredicate),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, IndexableElementPredicate(NotNull(xyItemsPredicate)), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> MatchCoupledAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Predicate<int> countPredicate, Predicate<TX?> xItemPredicate, Predicate<TY?> yItemPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countPredicate),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(IndexableElementPredicate(NotNull(xItemPredicate)), IndexableElementPredicate(NotNull(yItemPredicate)), IndexableElementPredicate(NotNull(xyItemsPredicate)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> MatchCoupledAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Predicate<int> countPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countPredicate),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, NotNull(xyItemsPredicate), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> MatchCoupledAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Predicate<int> countPredicate, ElementPredicate<TX?> xItemPredicate, ElementPredicate<TY?> yItemPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countPredicate),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(NotNull(xItemPredicate), NotNull(yItemPredicate), NotNull(xyItemsPredicate),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> MatchCoupledAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    Predicate<int> countPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countPredicate),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, IndexableElementPredicate(NotNull(xyItemsPredicate)), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> MatchCoupledAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    Predicate<int> countPredicate, Predicate<TX?> xItemPredicate, Predicate<TY?> yItemPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countPredicate),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(IndexableElementPredicate(NotNull(xItemPredicate)), IndexableElementPredicate(NotNull(yItemPredicate)), IndexableElementPredicate(NotNull(xyItemsPredicate)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> MatchCoupledAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    Predicate<int> countPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate,
    string? countMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countPredicate),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(null, null, NotNull(xyItemsPredicate), null, null,
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(TX? x, TY? y)> MatchCoupledAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    Predicate<int> countPredicate, ElementPredicate<TX?> xItemPredicate, ElementPredicate<TY?> yItemPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate,
    string? countMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null, string? imbalancedMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      CoupledCountsValidator(NotNull(countPredicate),
        countMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      CoupledElementsValidator(NotNull(xItemPredicate), NotNull(yItemPredicate), NotNull(xyItemsPredicate),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        imbalancedMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  #endregion
  #region Equal coupled collections

  public IEnumerable<(object? x, object? y)> EqualCoupled(IEnumerable xCollection, IEnumerable yCollection,
    bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsEqualityNavigator(EqualityComparer<object>.Default.AsEquality<object>(), false, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      EqualityTotalValidator(false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> EqualCoupled(IEnumerable xCollection, IEnumerable yCollection,
    Equality<object?>? equality, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsEqualityNavigator(equality ?? EqualityComparer<object>.Default.AsEquality<object>(), false, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      EqualityTotalValidator(false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> EqualCoupled(IEnumerable xCollection, IEnumerable yCollection,
    IEqualityComparer? equalityComparer, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsEqualityNavigator((equalityComparer ?? EqualityComparer<object>.Default).AsEquality(), false, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      EqualityTotalValidator(false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(T? x, T? y)> EqualCoupled<T>(IEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsEqualityNavigator(EqualityComparer<T>.Default.AsEquality<T>(), false, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      EqualityTotalValidator(false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(T? x, T? y)> EqualCoupled<T>(IEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    Equality<T?>? equality, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsEqualityNavigator(equality ?? EqualityComparer<T>.Default.AsEquality<T>(), false, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      EqualityTotalValidator(false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(T? x, T? y)> EqualCoupled<T>(IEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    IEqualityComparer<T>? equalityComparer, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsEqualityNavigator((equalityComparer ?? EqualityComparer<T>.Default).AsEquality(), false, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      EqualityTotalValidator(false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(T? x, T? y)> EqualCoupledAsync<T>(IAsyncEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsEqualityNavigator(EqualityComparer<T>.Default.AsEquality<T>(), false, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      EqualityTotalValidator(false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(T? x, T? y)> EqualCoupledAsync<T>(IAsyncEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    Equality<T?>? equality, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsEqualityNavigator(equality ?? EqualityComparer<T>.Default.AsEquality<T>(), false, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      EqualityTotalValidator(false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(T? x, T? y)> EqualCoupledAsync<T>(IAsyncEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    IEqualityComparer<T>? equalityComparer, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsEqualityNavigator((equalityComparer ?? EqualityComparer<T>.Default).AsEquality(), false, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      EqualityTotalValidator(false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(T? x, T? y)> EqualCoupledAsync<T>(IAsyncEnumerable<T?> xCollection, IAsyncEnumerable<T?> yCollection,
    string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsEqualityNavigator(EqualityComparer<T>.Default.AsEquality<T>(), false, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      EqualityTotalValidator(false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(T? x, T? y)> EqualCoupledAsync<T>(IAsyncEnumerable<T?> xCollection, IAsyncEnumerable<T?> yCollection,
    Equality<T?>? equality, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsEqualityNavigator(equality ?? EqualityComparer<T>.Default.AsEquality<T>(), false, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      EqualityTotalValidator(false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(T? x, T? y)> EqualCoupledAsync<T>(IAsyncEnumerable<T?> xCollection, IAsyncEnumerable<T?> yCollection,
    IEqualityComparer<T>? equalityComparer, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsEqualityNavigator((equalityComparer ?? EqualityComparer<T>.Default).AsEquality(), false, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      EqualityTotalValidator(false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> NotEqualCoupled(IEnumerable xCollection, IEnumerable yCollection,
    bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsEqualityNavigator(EqualityComparer<object>.Default.AsEquality<object>(), true, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      EqualityTotalValidator(true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> NotEqualCoupled(IEnumerable xCollection, IEnumerable yCollection,
    Equality<object?>? equality, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsEqualityNavigator(equality ?? EqualityComparer<object>.Default.AsEquality<object>(), true, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      EqualityTotalValidator(true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> NotEqualCoupled(IEnumerable xCollection, IEnumerable yCollection,
    IEqualityComparer? equalityComparer, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsEqualityNavigator((equalityComparer ?? EqualityComparer<object>.Default).AsEquality(), true, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      EqualityTotalValidator(true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(T? x, T? y)> NotEqualCoupled<T>(IEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsEqualityNavigator(EqualityComparer<T>.Default.AsEquality<T>(), true, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      EqualityTotalValidator(true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(T? x, T? y)> NotEqualCoupled<T>(IEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    Equality<T?>? equality, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsEqualityNavigator(equality ?? EqualityComparer<T>.Default.AsEquality<T>(), true, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      EqualityTotalValidator(true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(T? x, T? y)> NotEqualCoupled<T>(IEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    IEqualityComparer<T>? equalityComparer, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsEqualityNavigator((equalityComparer ?? EqualityComparer<T>.Default).AsEquality(), true, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      EqualityTotalValidator(true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(T? x, T? y)> NotEqualCoupledAsync<T>(IAsyncEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsEqualityNavigator(EqualityComparer<T>.Default.AsEquality<T>(), true, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      EqualityTotalValidator(true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(T? x, T? y)> NotEqualCoupledAsync<T>(IAsyncEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    Equality<T?>? equality, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsEqualityNavigator(equality ?? EqualityComparer<T>.Default.AsEquality<T>(), true, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      EqualityTotalValidator(true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(T? x, T? y)> NotEqualCoupledAsync<T>(IAsyncEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    IEqualityComparer<T>? equalityComparer, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsEqualityNavigator((equalityComparer ?? EqualityComparer<T>.Default).AsEquality(), true, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      EqualityTotalValidator(true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(T? x, T? y)> NotEqualCoupledAsync<T>(IAsyncEnumerable<T?> xCollection, IAsyncEnumerable<T?> yCollection,
    string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsEqualityNavigator(EqualityComparer<T>.Default.AsEquality<T>(), true, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      EqualityTotalValidator(true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(T? x, T? y)> NotEqualCoupledAsync<T>(IAsyncEnumerable<T?> xCollection, IAsyncEnumerable<T?> yCollection,
    Equality<T?>? equality, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsEqualityNavigator(equality ?? EqualityComparer<T>.Default.AsEquality<T>(), true, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      EqualityTotalValidator(true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(T? x, T? y)> NotEqualCoupledAsync<T>(IAsyncEnumerable<T?> xCollection, IAsyncEnumerable<T?> yCollection,
    IEqualityComparer<T>? equalityComparer, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsEqualityNavigator((equalityComparer ?? EqualityComparer<T>.Default).AsEquality(), true, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      EqualityTotalValidator(true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  #endregion
  #region Compare coupled collections

  public IEnumerable<(object? x, object? y)> CompareCoupled(IEnumerable xCollection, IEnumerable yCollection,
    ComparisonCriteria criteria, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsComparisonNavigator(Comparer.Default.AsComparison(), criteria, false, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      ComparisonTotalValidator(criteria, false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> CompareCoupled(IEnumerable xCollection, IEnumerable yCollection,
    Comparison<object?>? comparison, ComparisonCriteria criteria, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsComparisonNavigator(comparison ?? Comparer.Default.Compare, criteria, false, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      ComparisonTotalValidator(criteria, false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> CompareCoupled(IEnumerable xCollection, IEnumerable yCollection,
    IComparer? comparer, ComparisonCriteria criteria, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsComparisonNavigator((comparer ?? Comparer.Default).AsComparison(), criteria, false, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      ComparisonTotalValidator(criteria, false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(T? x, T? y)> CompareCoupled<T>(IEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    ComparisonCriteria criteria, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsComparisonNavigator(Comparer<T>.Default.AsComparison<T>(), criteria, false, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      ComparisonTotalValidator(criteria, false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(T? x, T? y)> CompareCoupled<T>(IEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    Comparison<T?>? comparison, ComparisonCriteria criteria, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsComparisonNavigator(comparison ?? Comparer<T>.Default.AsComparison<T>(), criteria, false, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      ComparisonTotalValidator(criteria, false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(T? x, T? y)> CompareCoupled<T>(IEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    IComparer<T>? comparer, ComparisonCriteria criteria, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsComparisonNavigator((comparer ?? Comparer<T>.Default).AsComparison(), criteria, false, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      ComparisonTotalValidator(criteria, false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(T? x, T? y)> CompareCoupledAsync<T>(IAsyncEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    ComparisonCriteria criteria, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsComparisonNavigator(Comparer<T>.Default.AsComparison<T>(), criteria, false, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      ComparisonTotalValidator(criteria, false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(T? x, T? y)> CompareCoupledAsync<T>(IAsyncEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    Comparison<T?>? comparison, ComparisonCriteria criteria, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsComparisonNavigator(comparison ?? Comparer<T>.Default.AsComparison<T>(), criteria, false, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      ComparisonTotalValidator(criteria, false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(T? x, T? y)> CompareCoupledAsync<T>(IAsyncEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    IComparer<T>? comparer, ComparisonCriteria criteria, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsComparisonNavigator((comparer ?? Comparer<T>.Default).AsComparison(), criteria, false, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      ComparisonTotalValidator(criteria, false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(T? x, T? y)> CompareCoupledAsync<T>(IAsyncEnumerable<T?> xCollection, IAsyncEnumerable<T?> yCollection,
    ComparisonCriteria criteria, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsComparisonNavigator(Comparer<T>.Default.AsComparison<T>(), criteria, false, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      ComparisonTotalValidator(criteria, false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(T? x, T? y)> CompareCoupledAsync<T>(IAsyncEnumerable<T?> xCollection, IAsyncEnumerable<T?> yCollection,
    Comparison<T?>? comparison, ComparisonCriteria criteria, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsComparisonNavigator(comparison ?? Comparer<T>.Default.AsComparison<T>(), criteria, false, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      ComparisonTotalValidator(criteria, false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(T? x, T? y)> CompareCoupledAsync<T>(IAsyncEnumerable<T?> xCollection, IAsyncEnumerable<T?> yCollection,
    IComparer<T>? comparer, ComparisonCriteria criteria, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsComparisonNavigator((comparer ?? Comparer<T>.Default).AsComparison(), criteria, false, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      ComparisonTotalValidator(criteria, false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> NotCompareCoupled(IEnumerable xCollection, IEnumerable yCollection,
    ComparisonCriteria criteria, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsComparisonNavigator(Comparer.Default.AsComparison(), criteria, true, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      ComparisonTotalValidator(criteria, true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> NotCompareCoupled(IEnumerable xCollection, IEnumerable yCollection,
    Comparison<object?>? comparison, ComparisonCriteria criteria, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsComparisonNavigator(comparison ?? Comparer.Default.Compare, criteria, true, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      ComparisonTotalValidator(criteria, true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(object? x, object? y)> NotCompareCoupled(IEnumerable xCollection, IEnumerable yCollection,
    IComparer? comparer, ComparisonCriteria criteria, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsComparisonNavigator((comparer ?? Comparer.Default).AsComparison(), criteria, true, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      ComparisonTotalValidator(criteria, true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(T? x, T? y)> NotCompareCoupled<T>(IEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    ComparisonCriteria criteria, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsComparisonNavigator(Comparer<T>.Default.AsComparison<T>(), criteria, true, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      ComparisonTotalValidator(criteria, true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(T? x, T? y)> NotCompareCoupled<T>(IEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    Comparison<T?>? comparison, ComparisonCriteria criteria, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsComparisonNavigator(comparison ?? Comparer<T>.Default.AsComparison<T>(), criteria, true, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      ComparisonTotalValidator(criteria, true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IEnumerable<(T? x, T? y)> NotCompareCoupled<T>(IEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    IComparer<T>? comparer, ComparisonCriteria criteria, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsComparisonNavigator((comparer ?? Comparer<T>.Default).AsComparison(), criteria, true, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      ComparisonTotalValidator(criteria, true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(T? x, T? y)> NotCompareCoupledAsync<T>(IAsyncEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    ComparisonCriteria criteria, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsComparisonNavigator(Comparer<T>.Default.AsComparison<T>(), criteria, true, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      ComparisonTotalValidator(criteria, true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(T? x, T? y)> NotCompareCoupledAsync<T>(IAsyncEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    Comparison<T?>? comparison, ComparisonCriteria criteria, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsComparisonNavigator(comparison ?? Comparer<T>.Default.AsComparison<T>(), criteria, true, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      ComparisonTotalValidator(criteria, true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(T? x, T? y)> NotCompareCoupledAsync<T>(IAsyncEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    IComparer<T>? comparer, ComparisonCriteria criteria, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsComparisonNavigator((comparer ?? Comparer<T>.Default).AsComparison(), criteria, true, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      ComparisonTotalValidator(criteria, true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(T? x, T? y)> NotCompareCoupledAsync<T>(IAsyncEnumerable<T?> xCollection, IAsyncEnumerable<T?> yCollection,
    ComparisonCriteria criteria, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsComparisonNavigator(Comparer<T>.Default.AsComparison<T>(), criteria, true, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      ComparisonTotalValidator(criteria, true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(T? x, T? y)> NotCompareCoupledAsync<T>(IAsyncEnumerable<T?> xCollection, IAsyncEnumerable<T?> yCollection,
    Comparison<T?>? comparison, ComparisonCriteria criteria, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsComparisonNavigator(comparison ?? Comparer<T>.Default.AsComparison<T>(), criteria, true, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      ComparisonTotalValidator(criteria, true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  public IAsyncEnumerable<(T? x, T? y)> NotCompareCoupledAsync<T>(IAsyncEnumerable<T?> xCollection, IAsyncEnumerable<T?> yCollection,
    IComparer<T>? comparer, ComparisonCriteria criteria, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      CoupledElementsComparisonNavigator((comparer ?? Comparer<T>.Default).AsComparison(), criteria, true, message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      ComparisonTotalValidator(criteria, true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken)
      .Select(couple => (couple.xItem.Value, couple.yItem.Value));

  #endregion
  #region Validate paired collections

  public IEnumerable<(TryOut<object> x, TryOut<object> y)> ValidatePairedCounts(IEnumerable xCollection, IEnumerable yCollection,
    Action<(int xCount, int yCount)>? countsValidator, bool lazy = false,
    string? countsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      PairedCountsValidator(countsValidator,
        countsMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, null, lazy);

  public IEnumerable<(TryOut<TX> x, TryOut<TY> y)> ValidatePairedCounts<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Action<(int xCount, int yCount)>? countsValidator, bool lazy = false,
    string? countsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      PairedCountsValidator(countsValidator,
        countsMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, null, lazy);

  public IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)> ValidatePairedCountsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Action<(int xCount, int yCount)>? countsValidator,
    string? countsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      PairedCountsValidator(countsValidator,
        countsMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, null, cancellationToken);

  public IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)> ValidatePairedCountsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    Action<(int xCount, int yCount)>? countsValidator,
    string? countsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      PairedCountsValidator(countsValidator,
        countsMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, null, cancellationToken);

  public IEnumerable<(TryOut<object> x, TryOut<object> y)> ValidatePairedElements(IEnumerable xCollection, IEnumerable yCollection,
    Action<object?> xItemValidator, Action<object?> yItemValidator, Action<(object? x, object? y)> xyItemsValidator, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsValidator(IndexableElementValidator(NotNull(xItemValidator)), IndexableElementValidator(NotNull(yItemValidator)), IndexableElementValidator(NotNull(xyItemsValidator)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy);

  public IEnumerable<(TryOut<object> x, TryOut<object> y)> ValidatePairedElements(IEnumerable xCollection, IEnumerable yCollection,
    ElementAction<object?> xItemValidator, ElementAction<object?> yItemValidator, ElementAction<(object? x, object? y)> xyItemsValidator, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsValidator(NotNull(xItemValidator), NotNull(yItemValidator), NotNull(xyItemsValidator),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy);

  public IEnumerable<(TryOut<TX> x, TryOut<TY> y)> ValidatePairedElements<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Action<TX?> xItemValidator, Action<TY?> yItemValidator, Action<(TX? x, TY? y)> xyItemsValidator, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsValidator(IndexableElementValidator(NotNull(xItemValidator)), IndexableElementValidator(NotNull(yItemValidator)), IndexableElementValidator(NotNull(xyItemsValidator)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy);

  public IEnumerable<(TryOut<TX> x, TryOut<TY> y)> ValidatePairedElements<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    ElementAction<TX?> xItemValidator, ElementAction<TY?> yItemValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsValidator(NotNull(xItemValidator), NotNull(yItemValidator), NotNull(xyItemsValidator),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy);

  public IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)> ValidatePairedElementsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Action<TX?> xItemValidator, Action<TY?> yItemValidator, Action<(TX? x, TY? y)> xyItemsValidator,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsValidator(IndexableElementValidator(NotNull(xItemValidator)), IndexableElementValidator(NotNull(yItemValidator)), IndexableElementValidator(NotNull(xyItemsValidator)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken);

  public IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)> ValidatePairedElementsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    ElementAction<TX?> xItemValidator, ElementAction<TY?> yItemValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsValidator(NotNull(xItemValidator), NotNull(yItemValidator), NotNull(xyItemsValidator),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken);

  public IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)> ValidatePairedElementsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    Action<TX?> xItemValidator, Action<TY?> yItemValidator, Action<(TX? x, TY? y)> xyItemsValidator,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsValidator(IndexableElementValidator(NotNull(xItemValidator)), IndexableElementValidator(NotNull(yItemValidator)), IndexableElementValidator(NotNull(xyItemsValidator)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken);

  public IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)> ValidatePairedElementsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    ElementAction<TX?> xItemValidator, ElementAction<TY?> yItemValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsValidator(NotNull(xItemValidator), NotNull(yItemValidator), NotNull(xyItemsValidator),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken);

  public IEnumerable<(TryOut<object> x, TryOut<object> y)> ValidatePaired(IEnumerable xCollection, IEnumerable yCollection,
    Action<(int xCount, int yCount)> countsValidator, Action<object?> xItemValidator, Action<object?> yItemValidator, Action<(object? x, object? y)> xyItemsValidator, bool lazy = false,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      PairedCountsValidator(NotNull(countsValidator),
        countsMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      PairedElementsValidator(IndexableElementValidator(NotNull(xItemValidator)), IndexableElementValidator(NotNull(yItemValidator)), IndexableElementValidator(NotNull(xyItemsValidator)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy);

  public IEnumerable<(TryOut<object> x, TryOut<object> y)> ValidatePaired(IEnumerable xCollection, IEnumerable yCollection,
    Action<(int xCount, int yCount)> countsValidator, ElementAction<object?> xItemValidator, ElementAction<object?> yItemValidator, ElementAction<(object? x, object? y)> xyItemsValidator, bool lazy = false,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      PairedCountsValidator(NotNull(countsValidator),
        countsMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      PairedElementsValidator(NotNull(xItemValidator), NotNull(yItemValidator), NotNull(xyItemsValidator),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy);

  public IEnumerable<(TryOut<TX> x, TryOut<TY> y)> ValidatePaired<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Action<(int xCount, int yCount)> countsValidator, Action<TX?> xItemValidator, Action<TY?> yItemValidator, Action<(TX? x, TY? y)> xyItemsValidator, bool lazy = false,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      PairedCountsValidator(NotNull(countsValidator),
        countsMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      PairedElementsValidator(IndexableElementValidator(NotNull(xItemValidator)), IndexableElementValidator(NotNull(yItemValidator)), IndexableElementValidator(NotNull(xyItemsValidator)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy);

  public IEnumerable<(TryOut<TX> x, TryOut<TY> y)> ValidatePaired<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Action<(int xCount, int yCount)> countsValidator, ElementAction<TX?> xItemValidator, ElementAction<TY?> yItemValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator, bool lazy = false,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      PairedCountsValidator(NotNull(countsValidator),
        countsMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      PairedElementsValidator(NotNull(xItemValidator), NotNull(yItemValidator), NotNull(xyItemsValidator),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy);

  public IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)> ValidatePairedAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Action<(int xCount, int yCount)> countsValidator, Action<TX?> xItemValidator, Action<TY?> yItemValidator, Action<(TX? x, TY? y)> xyItemsValidator,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      PairedCountsValidator(NotNull(countsValidator),
        countsMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      PairedElementsValidator(IndexableElementValidator(NotNull(xItemValidator)), IndexableElementValidator(NotNull(yItemValidator)), IndexableElementValidator(NotNull(xyItemsValidator)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken);

  public IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)> ValidatePairedAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Action<(int xCount, int yCount)> countsValidator, ElementAction<TX?> xItemValidator, ElementAction<TY?> yItemValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      PairedCountsValidator(NotNull(countsValidator),
        countsMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      PairedElementsValidator(NotNull(xItemValidator), NotNull(yItemValidator), NotNull(xyItemsValidator),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken);

  public IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)> ValidatePairedAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    Action<(int xCount, int yCount)> countsValidator, Action<TX?> xItemValidator, Action<TY?> yItemValidator, Action<(TX? x, TY? y)> xyItemsValidator,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      PairedCountsValidator(NotNull(countsValidator),
        countsMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      PairedElementsValidator(IndexableElementValidator(NotNull(xItemValidator)), IndexableElementValidator(NotNull(yItemValidator)), IndexableElementValidator(NotNull(xyItemsValidator)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken);

  public IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)> ValidatePairedAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    Action<(int xCount, int yCount)> countsValidator, ElementAction<TX?> xItemValidator, ElementAction<TY?> yItemValidator, ElementAction<(TX? x, TY? y)> xyItemsValidator,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      PairedCountsValidator(NotNull(countsValidator),
        countsMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      PairedElementsValidator(NotNull(xItemValidator), NotNull(yItemValidator), NotNull(xyItemsValidator),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken);

  #endregion
  #region Match paired collections

  public IEnumerable<(TryOut<object> x, TryOut<object> y)> MatchPairedCounts(IEnumerable xCollection, IEnumerable yCollection,
    Predicate<(int xCount, int yCount)> countsPredicate, bool lazy = false,
    string? countsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      PairedCountsValidator(NotNull(countsPredicate),
        countsMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, null, lazy);

  public IEnumerable<(TryOut<TX> x, TryOut<TY> y)> MatchPairedCounts<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Predicate<(int xCount, int yCount)> countsPredicate, bool lazy = false,
    string? countsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      PairedCountsValidator(NotNull(countsPredicate),
        countsMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, null, lazy);

  public IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)> MatchPairedCountsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Predicate<(int xCount, int yCount)> countsPredicate,
    string? countsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      PairedCountsValidator(NotNull(countsPredicate),
        countsMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, null, cancellationToken);

  public IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)> MatchPairedCountsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    Predicate<(int xCount, int yCount)> countsPredicate,
    string? countsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      PairedCountsValidator(NotNull(countsPredicate),
        countsMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, null, cancellationToken);

  public IEnumerable<(TryOut<object> x, TryOut<object> y)> MatchPairedElements(IEnumerable xCollection, IEnumerable yCollection,
    Predicate<object?> xItemPredicate, Predicate<object?> yItemPredicate, Predicate<(object? x, object? y)> xyItemsPredicate, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsValidator(IndexableElementPredicate(NotNull(xItemPredicate)), IndexableElementPredicate(NotNull(yItemPredicate)), IndexableElementPredicate(NotNull(xyItemsPredicate)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy);

  public IEnumerable<(TryOut<object> x, TryOut<object> y)> MatchPairedElements(IEnumerable xCollection, IEnumerable yCollection,
    ElementPredicate<object?> xItemPredicate, ElementPredicate<object?> yItemPredicate, ElementPredicate<(object? x, object? y)> xyItemsPredicate, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsValidator(NotNull(xItemPredicate), NotNull(yItemPredicate), NotNull(xyItemsPredicate),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy);

  public IEnumerable<(TryOut<TX> x, TryOut<TY> y)> MatchPairedElements<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Predicate<TX?> xItemPredicate, Predicate<TY?> yItemPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsValidator(IndexableElementPredicate(NotNull(xItemPredicate)), IndexableElementPredicate(NotNull(yItemPredicate)), IndexableElementPredicate(NotNull(xyItemsPredicate)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy);

  public IEnumerable<(TryOut<TX> x, TryOut<TY> y)> MatchPairedElements<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    ElementPredicate<TX?> xItemPredicate, ElementPredicate<TY?> yItemPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate, bool lazy = false,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsValidator(NotNull(xItemPredicate), NotNull(yItemPredicate), NotNull(xyItemsPredicate),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy);

  public IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)> MatchPairedElementsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Predicate<TX?> xItemPredicate, Predicate<TY?> yItemPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsValidator(IndexableElementPredicate(NotNull(xItemPredicate)), IndexableElementPredicate(NotNull(yItemPredicate)), IndexableElementPredicate(NotNull(xyItemsPredicate)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken);

  public IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)> MatchPairedElementsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    ElementPredicate<TX?> xItemPredicate, ElementPredicate<TY?> yItemPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsValidator(NotNull(xItemPredicate), NotNull(yItemPredicate), NotNull(xyItemsPredicate),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken);

  public IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)> MatchPairedElementsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    Predicate<TX?> xItemPredicate, Predicate<TY?> yItemPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsValidator(IndexableElementPredicate(NotNull(xItemPredicate)), IndexableElementPredicate(NotNull(yItemPredicate)), IndexableElementPredicate(NotNull(xyItemsPredicate)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken);

  public IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)> MatchPairedElementsAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    ElementPredicate<TX?> xItemPredicate, ElementPredicate<TY?> yItemPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate,
    string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsValidator(NotNull(xItemPredicate), NotNull(yItemPredicate), NotNull(xyItemsPredicate),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken);

  public IEnumerable<(TryOut<object> x, TryOut<object> y)> MatchPaired(IEnumerable xCollection, IEnumerable yCollection,
    Predicate<(int xCount, int yCount)> countsPredicate, Predicate<object?> xItemPredicate, Predicate<object?> yItemPredicate, Predicate<(object? x, object? y)> xyItemsPredicate, bool lazy = false,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      PairedCountsValidator(NotNull(countsPredicate),
        countsMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      PairedElementsValidator(IndexableElementPredicate(NotNull(xItemPredicate)), IndexableElementPredicate(NotNull(yItemPredicate)), IndexableElementPredicate(NotNull(xyItemsPredicate)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy);

  public IEnumerable<(TryOut<object> x, TryOut<object> y)> MatchPaired(IEnumerable xCollection, IEnumerable yCollection,
    Predicate<(int xCount, int yCount)> countsPredicate, ElementPredicate<object?> xItemPredicate, ElementPredicate<object?> yItemPredicate, ElementPredicate<(object? x, object? y)> xyItemsPredicate, bool lazy = false,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      PairedCountsValidator(NotNull(countsPredicate),
        countsMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      PairedElementsValidator(NotNull(xItemPredicate), NotNull(yItemPredicate), NotNull(xyItemsPredicate),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy);

  public IEnumerable<(TryOut<TX> x, TryOut<TY> y)> MatchPaired<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Predicate<(int xCount, int yCount)> countsPredicate, Predicate<TX?> xItemPredicate, Predicate<TY?> yItemPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate, bool lazy = false,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      PairedCountsValidator(NotNull(countsPredicate),
        countsMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      PairedElementsValidator(IndexableElementPredicate(NotNull(xItemPredicate)), IndexableElementPredicate(NotNull(yItemPredicate)), IndexableElementPredicate(NotNull(xyItemsPredicate)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy);

  public IEnumerable<(TryOut<TX> x, TryOut<TY> y)> MatchPaired<TX, TY>(IEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Predicate<(int xCount, int yCount)> countsPredicate, ElementPredicate<TX?> xItemPredicate, ElementPredicate<TY?> yItemPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate, bool lazy = false,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      PairedCountsValidator(NotNull(countsPredicate),
        countsMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      PairedElementsValidator(NotNull(xItemPredicate), NotNull(yItemPredicate), NotNull(xyItemsPredicate),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, lazy);

  public IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)> MatchPairedAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Predicate<(int xCount, int yCount)> countsPredicate, Predicate<TX?> xItemPredicate, Predicate<TY?> yItemPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      PairedCountsValidator(NotNull(countsPredicate),
        countsMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      PairedElementsValidator(IndexableElementPredicate(NotNull(xItemPredicate)), IndexableElementPredicate(NotNull(yItemPredicate)), IndexableElementPredicate(NotNull(xyItemsPredicate)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken);

  public IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)> MatchPairedAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IEnumerable<TY?> yCollection,
    Predicate<(int xCount, int yCount)> countsPredicate, ElementPredicate<TX?> xItemPredicate, ElementPredicate<TY?> yItemPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      PairedCountsValidator(NotNull(countsPredicate),
        countsMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      PairedElementsValidator(NotNull(xItemPredicate), NotNull(yItemPredicate), NotNull(xyItemsPredicate),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken);

  public IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)> MatchPairedAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    Predicate<(int xCount, int yCount)> countsPredicate, Predicate<TX?> xItemPredicate, Predicate<TY?> yItemPredicate, Predicate<(TX? x, TY? y)> xyItemsPredicate,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      PairedCountsValidator(NotNull(countsPredicate),
        countsMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      PairedElementsValidator(IndexableElementPredicate(NotNull(xItemPredicate)), IndexableElementPredicate(NotNull(yItemPredicate)), IndexableElementPredicate(NotNull(xyItemsPredicate)),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken);

  public IAsyncEnumerable<(TryOut<TX> x, TryOut<TY> y)> MatchPairedAsync<TX, TY>(IAsyncEnumerable<TX?> xCollection, IAsyncEnumerable<TY?> yCollection,
    Predicate<(int xCount, int yCount)> countsPredicate, ElementPredicate<TX?> xItemPredicate, ElementPredicate<TY?> yItemPredicate, ElementPredicate<(TX? x, TY? y)> xyItemsPredicate,
    string? countsMessage = null, string? xItemMessage = null, string? yItemMessage = null, string? xyItemsMessage = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      PairedCountsValidator(NotNull(countsPredicate),
        countsMessage ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      PairedElementsValidator(NotNull(xItemPredicate), NotNull(yItemPredicate), NotNull(xyItemsPredicate),
        xItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, xCollectionParameter),
        yItemMessage ?? FormatString(ArgumentMessage.CollectionContainsMismatchedElements, yCollectionParameter),
        xyItemsMessage ?? FormatString(ArgumentMessage.CollectionsAreElementwiseMismatched, xCollectionParameter, yCollectionParameter),
        xCollectionParameter, yCollectionParameter),
      null, cancellationToken);

  #endregion
  #region Equal paired collections

  public IEnumerable<(TryOut<object> x, TryOut<object> y)> EqualPaired(IEnumerable xCollection, IEnumerable yCollection,
    bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      PairedElementsEqualityNavigator(EqualityComparer<object>.Default.AsEquality<object>(), false),
      EqualityTotalValidator(false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy);

  public IEnumerable<(TryOut<object> x, TryOut<object> y)> EqualPaired(IEnumerable xCollection, IEnumerable yCollection,
    Equality<object?>? equality, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      PairedElementsEqualityNavigator(equality ?? EqualityComparer<object>.Default.AsEquality<object>(), false),
      EqualityTotalValidator(false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy);

  public IEnumerable<(TryOut<object> x, TryOut<object> y)> EqualPaired(IEnumerable xCollection, IEnumerable yCollection,
    IEqualityComparer? equalityComparer, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      PairedElementsEqualityNavigator((equalityComparer ?? EqualityComparer<object>.Default).AsEquality(), false),
      EqualityTotalValidator(false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy);

  public IEnumerable<(TryOut<T> x, TryOut<T> y)> EqualPaired<T>(IEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      PairedElementsEqualityNavigator(EqualityComparer<T>.Default.AsEquality<T>(), false),
      EqualityTotalValidator(false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy);

  public IEnumerable<(TryOut<T> x, TryOut<T> y)> EqualPaired<T>(IEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    Equality<T?>? equality, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      PairedElementsEqualityNavigator(equality ?? EqualityComparer<T>.Default.AsEquality<T>(), false),
      EqualityTotalValidator(false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy);

  public IEnumerable<(TryOut<T> x, TryOut<T> y)> EqualPaired<T>(IEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    IEqualityComparer<T>? equalityComparer, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      PairedElementsEqualityNavigator((equalityComparer ?? EqualityComparer<T>.Default).AsEquality(), false),
      EqualityTotalValidator(false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy);

  public IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)> EqualPairedAsync<T>(IAsyncEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      PairedElementsEqualityNavigator(EqualityComparer<T>.Default.AsEquality<T>(), false),
      EqualityTotalValidator(false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken);

  public IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)> EqualPairedAsync<T>(IAsyncEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    Equality<T?>? equality, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      PairedElementsEqualityNavigator(equality ?? EqualityComparer<T>.Default.AsEquality<T>(), false),
      EqualityTotalValidator(false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken);

  public IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)> EqualPairedAsync<T>(IAsyncEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    IEqualityComparer<T>? equalityComparer, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      PairedElementsEqualityNavigator((equalityComparer ?? EqualityComparer<T>.Default).AsEquality(), false),
      EqualityTotalValidator(false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken);

  public IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)> EqualPairedAsync<T>(IAsyncEnumerable<T?> xCollection, IAsyncEnumerable<T?> yCollection,
    string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      PairedElementsEqualityNavigator(EqualityComparer<T>.Default.AsEquality<T>(), false),
      EqualityTotalValidator(false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken);

  public IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)> EqualPairedAsync<T>(IAsyncEnumerable<T?> xCollection, IAsyncEnumerable<T?> yCollection,
    Equality<T?>? equality, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      PairedElementsEqualityNavigator(equality ?? EqualityComparer<T>.Default.AsEquality<T>(), false),
      EqualityTotalValidator(false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken);

  public IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)> EqualPairedAsync<T>(IAsyncEnumerable<T?> xCollection, IAsyncEnumerable<T?> yCollection,
    IEqualityComparer<T>? equalityComparer, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      ImbalanceValidator(message ?? FormatString(ArgumentMessage.CollectionsElementsCountsAreNotEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      PairedElementsEqualityNavigator((equalityComparer ?? EqualityComparer<T>.Default).AsEquality(), false),
      EqualityTotalValidator(false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken);

  public IEnumerable<(TryOut<object> x, TryOut<object> y)> NotEqualPaired(IEnumerable xCollection, IEnumerable yCollection,
    bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsEqualityNavigator(EqualityComparer<object>.Default.AsEquality<object>(), true),
      EqualityTotalValidator(true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy);

  public IEnumerable<(TryOut<object> x, TryOut<object> y)> NotEqualPaired(IEnumerable xCollection, IEnumerable yCollection,
    Equality<object?>? equality, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsEqualityNavigator(equality ?? EqualityComparer<object>.Default.AsEquality<object>(), true),
      EqualityTotalValidator(true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy);

  public IEnumerable<(TryOut<object> x, TryOut<object> y)> NotEqualPaired(IEnumerable xCollection, IEnumerable yCollection,
    IEqualityComparer? equalityComparer, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsEqualityNavigator((equalityComparer ?? EqualityComparer<object>.Default).AsEquality(), true),
      EqualityTotalValidator(true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy);

  public IEnumerable<(TryOut<T> x, TryOut<T> y)> NotEqualPaired<T>(IEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsEqualityNavigator(EqualityComparer<T>.Default.AsEquality<T>(), true),
      EqualityTotalValidator(true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy);

  public IEnumerable<(TryOut<T> x, TryOut<T> y)> NotEqualPaired<T>(IEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    Equality<T?>? equality, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsEqualityNavigator(equality ?? EqualityComparer<T>.Default.AsEquality<T>(), true),
      EqualityTotalValidator(true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy);

  public IEnumerable<(TryOut<T> x, TryOut<T> y)> NotEqualPaired<T>(IEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    IEqualityComparer<T>? equalityComparer, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsEqualityNavigator((equalityComparer ?? EqualityComparer<T>.Default).AsEquality(), true),
      EqualityTotalValidator(true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy);

  public IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)> NotEqualPairedAsync<T>(IAsyncEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsEqualityNavigator(EqualityComparer<T>.Default.AsEquality<T>(), true),
      EqualityTotalValidator(true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken);

  public IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)> NotEqualPairedAsync<T>(IAsyncEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    Equality<T?>? equality, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsEqualityNavigator(equality ?? EqualityComparer<T>.Default.AsEquality<T>(), true),
      EqualityTotalValidator(true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken);

  public IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)> NotEqualPairedAsync<T>(IAsyncEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    IEqualityComparer<T>? equalityComparer, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsEqualityNavigator((equalityComparer ?? EqualityComparer<T>.Default).AsEquality(), true),
      EqualityTotalValidator(true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken);

  public IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)> NotEqualPairedAsync<T>(IAsyncEnumerable<T?> xCollection, IAsyncEnumerable<T?> yCollection,
    string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsEqualityNavigator(EqualityComparer<T>.Default.AsEquality<T>(), true),
      EqualityTotalValidator(true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken);

  public IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)> NotEqualPairedAsync<T>(IAsyncEnumerable<T?> xCollection, IAsyncEnumerable<T?> yCollection,
    Equality<T?>? equality, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsEqualityNavigator(equality ?? EqualityComparer<T>.Default.AsEquality<T>(), true),
      EqualityTotalValidator(true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken);

  public IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)> NotEqualPairedAsync<T>(IAsyncEnumerable<T?> xCollection, IAsyncEnumerable<T?> yCollection,
    IEqualityComparer<T>? equalityComparer, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsEqualityNavigator((equalityComparer ?? EqualityComparer<T>.Default).AsEquality(), true),
      EqualityTotalValidator(true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseEqual, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken);

  #endregion
  #region Compare paired collections

  public IEnumerable<(TryOut<object> x, TryOut<object> y)> ComparePaired(IEnumerable xCollection, IEnumerable yCollection,
    ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsComparisonNavigator(Comparer.Default.AsComparison(), criteria, emptyOrder, false),
      ComparisonTotalValidator(criteria, false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy);

  public IEnumerable<(TryOut<object> x, TryOut<object> y)> ComparePaired(IEnumerable xCollection, IEnumerable yCollection,
    Comparison<object?>? comparison, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsComparisonNavigator(comparison ?? Comparer.Default.Compare, criteria, emptyOrder, false),
      ComparisonTotalValidator(criteria, false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy);

  public IEnumerable<(TryOut<object> x, TryOut<object> y)> ComparePaired(IEnumerable xCollection, IEnumerable yCollection,
    IComparer? comparer, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsComparisonNavigator((comparer ?? Comparer.Default).AsComparison(), criteria, emptyOrder, false),
      ComparisonTotalValidator(criteria, false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy);

  public IEnumerable<(TryOut<T> x, TryOut<T> y)> ComparePaired<T>(IEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsComparisonNavigator(Comparer<T>.Default.AsComparison<T>(), criteria, emptyOrder, false),
      ComparisonTotalValidator(criteria, false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy);

  public IEnumerable<(TryOut<T> x, TryOut<T> y)> ComparePaired<T>(IEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    Comparison<T?>? comparison, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsComparisonNavigator(comparison ?? Comparer<T>.Default.AsComparison<T>(), criteria, emptyOrder, false),
      ComparisonTotalValidator(criteria, false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy);

  public IEnumerable<(TryOut<T> x, TryOut<T> y)> ComparePaired<T>(IEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    IComparer<T>? comparer, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsComparisonNavigator((comparer ?? Comparer<T>.Default).AsComparison(), criteria, emptyOrder, false),
      ComparisonTotalValidator(criteria, false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy);

  public IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)> ComparePairedAsync<T>(IAsyncEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsComparisonNavigator(Comparer<T>.Default.AsComparison<T>(), criteria, emptyOrder, false),
      ComparisonTotalValidator(criteria, false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken);

  public IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)> ComparePairedAsync<T>(IAsyncEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    Comparison<T?>? comparison, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsComparisonNavigator(comparison ?? Comparer<T>.Default.AsComparison<T>(), criteria, emptyOrder, false),
      ComparisonTotalValidator(criteria, false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken);

  public IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)> ComparePairedAsync<T>(IAsyncEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    IComparer<T>? comparer, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsComparisonNavigator((comparer ?? Comparer<T>.Default).AsComparison(), criteria, emptyOrder, false),
      ComparisonTotalValidator(criteria, false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken);

  public IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)> ComparePairedAsync<T>(IAsyncEnumerable<T?> xCollection, IAsyncEnumerable<T?> yCollection,
    ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsComparisonNavigator(Comparer<T>.Default.AsComparison<T>(), criteria, emptyOrder, false),
      ComparisonTotalValidator(criteria, false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken);

  public IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)> ComparePairedAsync<T>(IAsyncEnumerable<T?> xCollection, IAsyncEnumerable<T?> yCollection,
    Comparison<T?>? comparison, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsComparisonNavigator(comparison ?? Comparer<T>.Default.AsComparison<T>(), criteria, emptyOrder, false),
      ComparisonTotalValidator(criteria, false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken);

  public IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)> ComparePairedAsync<T>(IAsyncEnumerable<T?> xCollection, IAsyncEnumerable<T?> yCollection,
    IComparer<T>? comparer, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsComparisonNavigator((comparer ?? Comparer<T>.Default).AsComparison(), criteria, emptyOrder, false),
      ComparisonTotalValidator(criteria, false, message ?? FormatString(ArgumentMessage.CollectionsAreNotElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken);

  public IEnumerable<(TryOut<object> x, TryOut<object> y)> NotComparePaired(IEnumerable xCollection, IEnumerable yCollection,
    ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsComparisonNavigator(Comparer.Default.AsComparison(), criteria, emptyOrder, true),
      ComparisonTotalValidator(criteria, true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy);

  public IEnumerable<(TryOut<object> x, TryOut<object> y)> NotComparePaired(IEnumerable xCollection, IEnumerable yCollection,
    Comparison<object?>? comparison, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsComparisonNavigator(comparison ?? Comparer.Default.Compare, criteria, emptyOrder, true),
      ComparisonTotalValidator(criteria, true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy);

  public IEnumerable<(TryOut<object> x, TryOut<object> y)> NotComparePaired(IEnumerable xCollection, IEnumerable yCollection,
    IComparer? comparer, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsComparisonNavigator((comparer ?? Comparer.Default).AsComparison(), criteria, emptyOrder, true),
      ComparisonTotalValidator(criteria, true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy);

  public IEnumerable<(TryOut<T> x, TryOut<T> y)> NotComparePaired<T>(IEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsComparisonNavigator(Comparer<T>.Default.AsComparison<T>(), criteria, emptyOrder, true),
      ComparisonTotalValidator(criteria, true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy);

  public IEnumerable<(TryOut<T> x, TryOut<T> y)> NotComparePaired<T>(IEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    Comparison<T?>? comparison, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsComparisonNavigator(comparison ?? Comparer<T>.Default.AsComparison<T>(), criteria, emptyOrder, true),
      ComparisonTotalValidator(criteria, true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy);

  public IEnumerable<(TryOut<T> x, TryOut<T> y)> NotComparePaired<T>(IEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    IComparer<T>? comparer, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, bool lazy = false, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection")
    => ValidateCollectionsCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsComparisonNavigator((comparer ?? Comparer<T>.Default).AsComparison<T>(), criteria, emptyOrder, true),
      ComparisonTotalValidator(criteria, true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      lazy);

  public IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)> NotComparePairedAsync<T>(IAsyncEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsComparisonNavigator(Comparer<T>.Default.AsComparison<T>(), criteria, emptyOrder, true),
      ComparisonTotalValidator(criteria, true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken);

  public IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)> NotComparePairedAsync<T>(IAsyncEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    Comparison<T?>? comparison, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsComparisonNavigator(comparison ?? Comparer<T>.Default.AsComparison<T>(), criteria, emptyOrder, true),
      ComparisonTotalValidator(criteria, true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken);

  public IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)> NotComparePairedAsync<T>(IAsyncEnumerable<T?> xCollection, IEnumerable<T?> yCollection,
    IComparer<T>? comparer, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsComparisonNavigator((comparer ?? Comparer<T>.Default).AsComparison(), criteria, emptyOrder, true),
      ComparisonTotalValidator(criteria, true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken);

  public IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)> NotComparePairedAsync<T>(IAsyncEnumerable<T?> xCollection, IAsyncEnumerable<T?> yCollection,
    ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsComparisonNavigator(Comparer<T>.Default.AsComparison<T>(), criteria, emptyOrder, true),
      ComparisonTotalValidator(criteria, true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken);

  public IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)> NotComparePairedAsync<T>(IAsyncEnumerable<T?> xCollection, IAsyncEnumerable<T?> yCollection,
    Comparison<T?>? comparison, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsComparisonNavigator(comparison ?? Comparer<T>.Default.AsComparison<T>(), criteria, emptyOrder, true),
      ComparisonTotalValidator(criteria, true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken);

  public IAsyncEnumerable<(TryOut<T> x, TryOut<T> y)> NotComparePairedAsync<T>(IAsyncEnumerable<T?> xCollection, IAsyncEnumerable<T?> yCollection,
    IComparer<T>? comparer, ComparisonCriteria criteria, RelativeOrder emptyOrder = RelativeOrder.Lower, string? message = null,
    [CallerArgumentExpression("xCollection")] string? xCollectionParameter = "xCollection", [CallerArgumentExpression("yCollection")] string? yCollectionParameter = "yCollection",
    CancellationToken cancellationToken = default)
    => ValidateCollectionsAsyncCore(NotNull(xCollection, valueParameter: xCollectionParameter), NotNull(yCollection, valueParameter: yCollectionParameter),
      null,
      PairedElementsComparisonNavigator((comparer ?? Comparer<T>.Default).AsComparison(), criteria, emptyOrder, true),
      ComparisonTotalValidator(criteria, true, message ?? FormatString(ArgumentMessage.CollectionsAreElementwiseCompared, xCollectionParameter, yCollectionParameter), xCollectionParameter, yCollectionParameter),
      cancellationToken);

  #endregion
  #endregion
  #endregion
}

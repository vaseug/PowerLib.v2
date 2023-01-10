using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using PowerLib.System.Accessors;
using PowerLib.System.Globalization;
using PowerLib.System.Resources;

namespace PowerLib.System.Validation;

public sealed class Operation
{
  #region Internal fields

  private readonly IValueProvider<CultureInfo> _cultureProvider;

  private readonly EnumResourceAccessor<OperationMessage>? _resourceAccessor;

  private static readonly EnumResourceAccessor<OperationMessage> defaultResourceAccessor = new EnumResourceAccessor<OperationMessage>();

  private static readonly Lazy<Operation> that = new(() => new(CultureProvider.CurrentCulture, typeof(OperationMessage)));

  private static readonly Lazy<Operation> thatUI = new(() => new(CultureProvider.CurrentUICulture, typeof(OperationMessage)));

  #endregion
  #region Constructors

  private Operation(IValueProvider<CultureInfo> cultureProvider, Type? resourceSource)
  {
    _cultureProvider = cultureProvider ?? throw new ArgumentNullException(nameof(cultureProvider));
    _resourceAccessor = resourceSource is not null && resourceSource != typeof(OperationMessage) ? new EnumResourceAccessor<OperationMessage>(resourceSource, null) : null;
  }

  #endregion
  #region Internal methods

  private string FormatString(OperationMessage argumentMessage, params object?[] args)
    => (_resourceAccessor ?? defaultResourceAccessor).FormatString(Culture, argumentMessage, args);

  #endregion
  #region Public properties

  public CultureInfo Culture
    => _cultureProvider.Value;

  public static Operation That => that.Value;

  public static Operation ThatUI => thatUI.Value;

  #endregion
  #region Public methods

  public static Operation Create(IValueProvider<CultureInfo> cultureProvider, Type? resourceSource = null)
    => new Operation(cultureProvider, resourceSource);

  #endregion
  #region Validate methods

  public void IsValid([DoesNotReturnIf(false)] bool isValid, string? message = null)
  {
    if (!isValid)
      throw new InvalidOperationException(message ?? FormatString(OperationMessage.Failed));
  }

  public T IsValid<T>(T value, [DoesNotReturnIf(false)] bool isValid, string? message = null)
    => isValid ? value :
      throw new InvalidOperationException(message ?? FormatString(OperationMessage.Failed));

  public T IsValid<T>(T value, Predicate<T> predicate, string? message = null)
    => Argument.That.NotNull(predicate)(value) ? value :
      throw new InvalidOperationException(message ?? FormatString(OperationMessage.Failed));

  public void IsInRange([DoesNotReturnIf(false)] bool isInRange, string? message = null)
  {
    if (!isInRange)
      throw new InvalidOperationException(message ?? FormatString(OperationMessage.OutOfRange));
  }

  public T IsInRange<T>(T value, [DoesNotReturnIf(false)] bool isInRange, string? message = null)
    => isInRange ? value :
      throw new InvalidOperationException(message ?? FormatString(OperationMessage.OutOfRange));

  public T IsInRange<T>(T value, Predicate<T> predicate, string? message = null)
    => Argument.That.NotNull(predicate)(value) ? value :
      throw new InvalidOperationException(message ?? FormatString(OperationMessage.OutOfRange));

  public void IsSupported([DoesNotReturnIf(false)] bool isSupported, string? message = null)
  {
    if (!isSupported)
      throw new NotSupportedException(message ?? FormatString(OperationMessage.Unsupported));
  }

  public T IsSupported<T>(T value, [DoesNotReturnIf(false)] bool isSupported, string? message = null)
    => isSupported ? value :
      throw new NotSupportedException(message ?? FormatString(OperationMessage.Unsupported));

  public T IsSupported<T>(T value, Predicate<T> predicate, string? message = null)
    => Argument.That.NotNull(predicate)(value) ? value :
      throw new NotSupportedException(message ?? FormatString(OperationMessage.Unsupported));

  public T IsSupported<T>(object value, string? message = null)
    where T : class
    => value is T result ? result :
    throw new NotSupportedException(message ?? FormatString(OperationMessage.Unsupported));

  public void IsImplemented([DoesNotReturnIf(false)] bool isImplemented, string? message = null)
  {
    if (!isImplemented)
      throw new NotSupportedException(message ?? FormatString(OperationMessage.Unimplemented));
  }

  public T IsImplemented<T>(T value, [DoesNotReturnIf(false)] bool isImplemented, string? message = null)
    => isImplemented ? value :
      throw new NotImplementedException(message ?? FormatString(OperationMessage.Unimplemented));

  public T IsImplemented<T>(T value, Predicate<T> predicate, string? message = null)
    => Argument.That.NotNull(predicate)(value) ? value :
      throw new NotImplementedException(message ?? FormatString(OperationMessage.Unimplemented));

  [DoesNotReturn]
  public dynamic Failed(string? message = null, Exception? innerExcption = null)
    => throw new InvalidOperationException(message ?? FormatString(OperationMessage.Failed), innerExcption);

  [DoesNotReturn]
  public T Failed<T>(string? message = null, Exception? innerExcption = null)
    => throw new InvalidOperationException(message ?? FormatString(OperationMessage.Failed), innerExcption);

  [DoesNotReturn]
  public dynamic OutOfRange(string? message = null, Exception? innerExcption = null)
    => throw new InvalidOperationException(message ?? FormatString(OperationMessage.OutOfRange), innerExcption);

  [DoesNotReturn]
  public T OutOfRange<T>(string? message = null, Exception? innerExcption = null)
    => throw new InvalidOperationException(message ?? FormatString(OperationMessage.OutOfRange), innerExcption);

  [DoesNotReturn]
  public dynamic Unsupported(string? message = null, Exception? innerExcption = null)
    => throw new NotSupportedException(message ?? FormatString(OperationMessage.Unsupported), innerExcption);

  [DoesNotReturn]
  public T Unsupported<T>(string? message = null, Exception? innerExcption = null)
    => throw new NotSupportedException(message ?? FormatString(OperationMessage.Unsupported), innerExcption);

  [DoesNotReturn]
  public dynamic Unimplemented(string? message = null, Exception? innerExcption = null)
    => throw new NotSupportedException(message ?? FormatString(OperationMessage.Unimplemented), innerExcption);

  [DoesNotReturn]
  public T Unimplemented<T>(string? message = null, Exception? innerExcption = null)
    => throw new NotImplementedException(message ?? FormatString(OperationMessage.Unimplemented), innerExcption);

  #endregion
}

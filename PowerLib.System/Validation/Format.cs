using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using PowerLib.System.Accessors;
using PowerLib.System.Globalization;
using PowerLib.System.Resources;

namespace PowerLib.System.Validation;

public sealed class Format
{
  #region Internal fields

  private readonly IValueProvider<CultureInfo> _cultureProvider;

  private readonly EnumResourceAccessor<FormatMessage>? _resourceAccessor;

  private static readonly EnumResourceAccessor<FormatMessage> defaultResourceAccessor = new EnumResourceAccessor<FormatMessage>();

  private static readonly Lazy<Format> that = new(() => new(CultureProvider.CurrentCulture, typeof(FormatMessage)));

  private static readonly Lazy<Format> thatUI = new(() => new(CultureProvider.CurrentUICulture, typeof(FormatMessage)));

  #endregion
  #region Constructors

  private Format(IValueProvider<CultureInfo> cultureProvider, Type? resourceSource)
  {
    _cultureProvider = cultureProvider ?? throw new ArgumentNullException(nameof(cultureProvider));
    _resourceAccessor = resourceSource is not null && resourceSource != typeof(FormatMessage) ? new EnumResourceAccessor<FormatMessage>(resourceSource, null) : null;
  }

  #endregion
  #region Internal methods

  private string FormatString(FormatMessage argumentMessage, params object?[] args)
    => (_resourceAccessor ?? defaultResourceAccessor).FormatString(Culture, argumentMessage, args);

  #endregion
  #region Public properties

  public CultureInfo Culture
    => _cultureProvider.Value;

  public static Format That => that.Value;

  public static Format ThatUI => thatUI.Value;

  #endregion
  #region Public methods

  public static Format Create(IValueProvider<CultureInfo> cultureProvider, Type? resourceSource = null)
    => new Format(cultureProvider, resourceSource);

  #endregion
  #region Validate methodss

  public void IsGood([DoesNotReturnIf(false)] bool isGood, string? message = null)
  {
    if (!isGood)
      throw new FormatException(message ?? FormatString(FormatMessage.Bad));
  }

  [DoesNotReturn]
  public void Bad(string? message = null, Exception? innerExcption = null)
    => throw new FormatException(message ?? FormatString(FormatMessage.Bad), innerExcption);

  [DoesNotReturn]
  public T Bad<T>(string? message = null, Exception? innerExcption = null)
    => throw new FormatException(message ?? FormatString(FormatMessage.Bad), innerExcption);

  public void IsGoodUri([DoesNotReturnIf(false)] bool isGood, string? message = null)
  {
    if (!isGood)
      throw new UriFormatException(message ?? FormatString(FormatMessage.BadUri));
  }

  [DoesNotReturn]
  public void BadUri(string? message = null, Exception? innerExcption = null)
    => throw new UriFormatException(message ?? FormatString(FormatMessage.BadUri), innerExcption);

  [DoesNotReturn]
  public T BadUri<T>(string? message = null, Exception? innerExcption = null)
    => throw new UriFormatException(message ?? FormatString(FormatMessage.BadUri), innerExcption);

  #endregion
}

using System;
using System.Globalization;
using System.IO;
using System.Resources;

namespace PowerLib.System.Resources;

public abstract class ResourceAccessor<TKey>
{
  private readonly ResourceManager _resourceManager;

  #region Constructors

  protected ResourceAccessor(ResourceManager resourceManager)
  {
    _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
  }

  #endregion
  #region Internal properties

  protected ResourceManager ResourceManager => _resourceManager;

  protected abstract CultureInfo? DefaultCulture { get; }

  #endregion
  #region Internal methods

  protected abstract string GetName(TKey key);

  protected virtual CultureInfo? GetCulture(IFormatProvider? formatProvider)
    => formatProvider is CultureInfo cultureInfo ? cultureInfo : formatProvider?.GetFormat(typeof(CultureInfo)) as CultureInfo ?? DefaultCulture;

  #endregion
  #region Public methods

  public string FormatString(TKey key, params object?[] args)
    => FormatString(null, key, args);

  public string FormatString(IFormatProvider? formatProvider, TKey key, params object?[] args)
  {
    var format = GetString(key, formatProvider) ?? throw new InvalidOperationException();
    return string.Format(formatProvider, format, args);
  }

  public string? GetString(TKey key)
    => ResourceManager.GetString(GetName(key), DefaultCulture);

  public string? GetString(TKey key, IFormatProvider? formatProvider)
    => ResourceManager.GetString(GetName(key), GetCulture(formatProvider));

  public Stream? GetStream(TKey key)
    => ResourceManager.GetStream(GetName(key), DefaultCulture);

  public Stream? GetStream(TKey key, IFormatProvider? formatProvider)
    => ResourceManager.GetStream(GetName(key), GetCulture(formatProvider));

  public object? GetObject(TKey key)
    => ResourceManager.GetObject(GetName(key), DefaultCulture);

  public object? GetObject(TKey key, IFormatProvider? formatProvider)
    => ResourceManager.GetObject(GetName(key), GetCulture(formatProvider));

  #endregion
}

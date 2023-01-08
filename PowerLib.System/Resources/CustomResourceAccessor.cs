using System;
using System.Globalization;
using System.Resources;
using PowerLib.System.Validation;

namespace PowerLib.System.Resources;

public sealed class CustomResourceAccessor<TKey> : ResourceAccessor<TKey>
{
  private readonly Func<TKey, string> _keySelector;
  private readonly CultureInfo? _defaultCulture;

  #region Constructors

  public CustomResourceAccessor(Func<TKey, string> keySelector)
    : this(new ResourceManager(typeof(TKey)), keySelector, null)
  { }

  public CustomResourceAccessor(Func<TKey, string> keySelector, CultureInfo? cultureInfo)
    : this(new ResourceManager(typeof(TKey)), keySelector, cultureInfo)
  { }

  public CustomResourceAccessor(Type resourceSource, Func<TKey, string> keySelector)
    : this(new ResourceManager(resourceSource), keySelector, null)
  { }

  public CustomResourceAccessor(Type resourceSource, Func<TKey, string> keySelector, CultureInfo? cultureInfo)
    : this(new ResourceManager(resourceSource), keySelector, cultureInfo)
  { }

  public CustomResourceAccessor(ResourceManager resourceManager, Func<TKey, string> keySelector)
    : this(resourceManager, keySelector, null)
  { }

  public CustomResourceAccessor(ResourceManager resourceManager, Func<TKey, string> keySelector, CultureInfo? cultureInfo)
    : base(resourceManager)
  {
    _keySelector = Argument.That.NotNull(keySelector);
    _defaultCulture = cultureInfo;
  }

  #endregion
  #region Internal properties

  protected override CultureInfo? DefaultCulture => _defaultCulture;

  #endregion
  #region Internal methods

  protected override string GetName(TKey key)
  => (_keySelector is not null ? _keySelector(key) : key?.ToString()) ?? throw new InvalidOperationException();

  #endregion
}

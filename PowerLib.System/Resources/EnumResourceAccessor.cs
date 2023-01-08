using System;
using System.Globalization;
using System.Resources;

namespace PowerLib.System.Resources;

public class EnumResourceAccessor<TKey> : ResourceAccessor<TKey>
  where TKey : Enum
{
  private readonly CultureInfo? _defaultCulture;

  #region Constructors

  public EnumResourceAccessor()
    : this(new ResourceManager(typeof(TKey)), null)
  { }

  public EnumResourceAccessor(CultureInfo? cultureInfo)
    : this(new ResourceManager(typeof(TKey)), cultureInfo)
  { }

  public EnumResourceAccessor(Type resourceSource)
    : this(new ResourceManager(resourceSource), null)
  { }

  public EnumResourceAccessor(Type resourceSource, CultureInfo? cultureInfo)
    : this(new ResourceManager(resourceSource), cultureInfo)
  { }

  public EnumResourceAccessor(ResourceManager resourceManager)
    : this(resourceManager, null)
  { }

  public EnumResourceAccessor(ResourceManager resourceManager, CultureInfo? cultureInfo)
    : base(resourceManager)
  {
    _defaultCulture = cultureInfo;
  }

  #endregion
  #region Internal properties

  protected override CultureInfo? DefaultCulture => _defaultCulture;

  #endregion
  #region Internal methods

  protected override string GetName(TKey key)
    => key.ToString();

  #endregion
}

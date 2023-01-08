using System;
using System.Globalization;
using PowerLib.System.Accessors;

namespace PowerLib.System.Globalization;

public static class CultureProvider
{
  private static readonly Lazy<IValueProvider<CultureInfo>> currentCulture = new(() => new CustomValueProvider<CultureInfo>(GetCurrentCulture));

  private static readonly Lazy<IValueProvider<CultureInfo>> currentUICulture = new(() => new CustomValueProvider<CultureInfo>(GetCurrentUICulture));

  private static readonly Lazy<IValueProvider<CultureInfo>> invariantCulture = new(() => new CustomValueProvider<CultureInfo>(GetInvariantCulture));

  private static readonly Lazy<IValueProvider<CultureInfo>> installedUICulture = new(() => new CustomValueProvider<CultureInfo>(GetInstalledUICulture));

  public static IValueProvider<CultureInfo> CurrentCulture
    => currentCulture.Value;

  public static IValueProvider<CultureInfo> CurrentUICulture
    => currentUICulture.Value;

  public static IValueProvider<CultureInfo> InvariantCulture
    => invariantCulture.Value;

  public static IValueProvider<CultureInfo> InstalledUICulture
    => installedUICulture.Value;

  public static IValueProvider<CultureInfo> GetCultureById(int id)
    => new CustomValueProvider<CultureInfo>(() => CultureInfo.GetCultureInfo(id));

  public static IValueProvider<CultureInfo> GetCultureByName(string name)
    => new CustomValueProvider<CultureInfo>(() => CultureInfo.GetCultureInfo(name));

  public static IValueProvider<CultureInfo> GetCultureByName(string name, string altName)
    => new CustomValueProvider<CultureInfo>(() => CultureInfo.GetCultureInfo(name, altName));

  private static CultureInfo GetCurrentCulture()
    => CultureInfo.CurrentCulture;

  private static CultureInfo GetCurrentUICulture()
    => CultureInfo.CurrentUICulture;

  private static CultureInfo GetInvariantCulture()
    => CultureInfo.InvariantCulture;

  private static CultureInfo GetInstalledUICulture()
    => CultureInfo.InstalledUICulture;
}

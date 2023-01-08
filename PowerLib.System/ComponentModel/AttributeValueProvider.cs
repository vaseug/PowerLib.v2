using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Resources;
using PowerLib.System.Validation;

namespace PowerLib.System.ComponentModel;

public static class AttributeValueProvider
{
  public static object? GetObjectFromString(Type type, string text)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNull(text);

    TypeConverter converter = TypeDescriptor.GetConverter(type);
    Operation.That.IsSupported(converter.CanConvertFrom(typeof(string)));
    return converter.ConvertFromString(text);
  }

  public static object? GetObjectByConversion(Type type, object obj)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNull(obj);

    TypeConverter converter = TypeDescriptor.GetConverter(type);
    Argument.That.NotNull(converter);
    Operation.That.IsSupported(converter.CanConvertFrom(obj.GetType()));
    return converter.ConvertFrom(obj);
  }

  public static object? GetObjectByActivation(Type type, params object?[] args)
  {
    Argument.That.NotNull(type);
    return Activator.CreateInstance(type, args);
  }

  public static object? GetObjectFromResource(Type type, string resourceName, string? cultureName)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrEmpty(resourceName);

    var resManager = new ResourceManager(type);
    try
    {
      return resManager.GetObject(resourceName, cultureName is not null ? CultureInfo.GetCultureInfo(cultureName) : null);
    }
    finally
    {
      resManager.ReleaseAllResources();
    }
  }

  public static object? GetObjectFromResource(Assembly assembly, string baseName, Type? usingResourceSet, string resourceName, string? cultureName)
  {
    Argument.That.NotNull(assembly);
    Argument.That.NotNullOrEmpty(baseName);
    Argument.That.NotNullOrEmpty(resourceName);

    var resManager = new ResourceManager(baseName, assembly, usingResourceSet);
    try
    {
      return resManager.GetObject(resourceName, cultureName is not null ? CultureInfo.GetCultureInfo(cultureName) : null);
    }
    finally
    {
      resManager.ReleaseAllResources();
    }
  }

  public static object? GetObjectFromResource(string assemblyString, string baseName, Type? usingResourceSet, string resourceName, string? cultureName)
  {
    Argument.That.NotNullOrEmpty(assemblyString);
    Argument.That.NotNullOrEmpty(baseName);
    Argument.That.NotNullOrEmpty(resourceName);

    var resManager = new ResourceManager(baseName, Assembly.Load(assemblyString), usingResourceSet);
    try
    {
      return resManager.GetObject(resourceName, cultureName is not null ? CultureInfo.GetCultureInfo(cultureName) : null);
    }
    finally
    {
      resManager.ReleaseAllResources();
    }
  }

  public static object? GetObjectFromFileResource(string resourceDir, string baseName, Type? usingResourceSet, string resourceName, string? cultureName)
  {
    Argument.That.NotNullOrEmpty(resourceDir);
    Argument.That.NotNullOrEmpty(baseName);
    Argument.That.NotNullOrEmpty(resourceName);

    var resManager = ResourceManager.CreateFileBasedResourceManager(baseName, resourceDir, usingResourceSet);
    try
    {
      return resManager.GetObject(resourceName, cultureName is not null ? CultureInfo.GetCultureInfo(cultureName) : null);
    }
    finally
    {
      resManager.ReleaseAllResources();
    }
  }

  public static string? GetStringFromResource(Type type, string resourceName, string? cultureName)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrEmpty(resourceName);

    var resManager = new ResourceManager(type);
    try
    {
      return resManager.GetString(resourceName, cultureName is not null ? CultureInfo.GetCultureInfo(cultureName) : null);
    }
    finally
    {
      resManager.ReleaseAllResources();
    }
  }

  public static string? GetStringFromResource(Assembly assembly, string baseName, Type? usingResourceSet, string resourceName, string? cultureName)
  {
    Argument.That.NotNull(assembly);
    Argument.That.NotNullOrEmpty(baseName);
    Argument.That.NotNullOrEmpty(resourceName);

    var resManager = new ResourceManager(baseName, assembly, usingResourceSet);
    try
    {
      return resManager.GetString(resourceName, cultureName is not null ? CultureInfo.GetCultureInfo(cultureName) : null);
    }
    finally
    {
      resManager.ReleaseAllResources();
    }
  }

  public static string? GetStringFromResource(string assemblyString, string baseName, Type? usingResourceSet, string resourceName, string? cultureName)
  {
    Argument.That.NotNullOrEmpty(assemblyString);
    Argument.That.NotNullOrEmpty(baseName);
    Argument.That.NotNullOrEmpty(resourceName);

    var resManager = new ResourceManager(baseName, Assembly.Load(assemblyString), usingResourceSet);
    try
    {
      return resManager.GetString(resourceName, cultureName is not null ? CultureInfo.GetCultureInfo(cultureName) : null);
    }
    finally
    {
      resManager.ReleaseAllResources();
    }
  }

  public static string? GetStringFromFileResource(string resourceDir, string baseName, Type? usingResourceSet, string resourceName, string? cultureName)
  {
    Argument.That.NotNullOrEmpty(resourceDir);
    Argument.That.NotNullOrEmpty(baseName);
    Argument.That.NotNullOrEmpty(resourceName);

    var resManager = ResourceManager.CreateFileBasedResourceManager(baseName, resourceDir, usingResourceSet);
    try
    {
      return resManager.GetString(resourceName, cultureName is not null ? CultureInfo.GetCultureInfo(cultureName) : null);
    }
    finally
    {
      resManager.ReleaseAllResources();
    }
  }
}

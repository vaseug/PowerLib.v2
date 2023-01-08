using System;
using System.Reflection;
using PowerLib.System.Validation;

namespace PowerLib.System.ComponentModel
{
  [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
  public sealed class DisplayStringResourceAttribute : DisplayStringAttribute
  {
    #region Constructors

    public DisplayStringResourceAttribute(Type type, string resourceName)
      : this(type, resourceName, null)
    { }

    public DisplayStringResourceAttribute(Type type, string resourceName, string? cultureName)
      : base(AttributeValueProvider.GetStringFromResource(type, resourceName, cultureName) ?? string.Empty)
    { }

    public DisplayStringResourceAttribute(Type type, string baseName, Type? usingResourceSet, string resourceName)
      : this(type, baseName, usingResourceSet, resourceName, null)
    { }

    public DisplayStringResourceAttribute(Type type, string baseName, Type? usingResourceSet, string resourceName, string? cultureName)
      : base(AttributeValueProvider.GetStringFromResource(Argument.That.NotNull(type).Assembly, baseName, usingResourceSet, resourceName, cultureName) ?? string.Empty)
    { }

    public DisplayStringResourceAttribute(string resourceContainer, bool resourceFile, string baseName, Type? usingResourceSet, string resourceName)
      : this(resourceContainer, resourceFile, baseName, usingResourceSet, resourceName, null)
    { }

    public DisplayStringResourceAttribute(string resourceContainer, bool resourceFile, string baseName, Type? usingResourceSet, string resourceName, string? cultureName)
      : base((resourceFile ?
        AttributeValueProvider.GetStringFromFileResource(resourceContainer, baseName, usingResourceSet, resourceName, cultureName) :
        AttributeValueProvider.GetStringFromResource(resourceContainer, baseName, usingResourceSet, resourceName, cultureName)) ?? string.Empty)
    { }

    #endregion
  }
}

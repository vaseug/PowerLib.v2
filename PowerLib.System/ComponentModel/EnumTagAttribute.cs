using System;

namespace PowerLib.System.ComponentModel
{
  [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
  public sealed class EnumTagAttribute : Attribute
  {
    #region Constructors

    public EnumTagAttribute(object tag)
    {
      Tag = tag;
    }

    #endregion
    #region Properties

    public object Tag { get; }

    #endregion
  }
}

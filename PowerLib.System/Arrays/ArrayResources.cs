using System;
using System.Globalization;
using PowerLib.System.Resources;

namespace PowerLib.System.Arrays;

internal sealed class ArrayResources : EnumResourceAccessor<ArrayMessage>
{
  private static readonly Lazy<ArrayResources> instance =
    new Lazy<ArrayResources>(() => new());

  private ArrayResources()
  { }

  public static ArrayResources Default
    => instance.Value;
}

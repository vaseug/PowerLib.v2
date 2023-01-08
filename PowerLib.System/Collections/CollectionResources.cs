using System;
using PowerLib.System.Resources;

namespace PowerLib.System.Collections;

internal sealed class CollectionResources : EnumResourceAccessor<CollectionMessage>
{
  private static readonly Lazy<CollectionResources> instance =
    new (() => new());

  private CollectionResources()
  { }

  public static CollectionResources Default
    => instance.Value;
}

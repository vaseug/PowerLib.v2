using System.Diagnostics.CodeAnalysis;

namespace PowerLib.System.Collections;

public enum SortingOrder
{
  Ascending,
  Descending
}

[SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "By design")]
public enum SortingOption
{
  None = 0,
  First = 1,
  Last = 2,
  Single = 3,
}

public enum SearchingOption
{
  None = 0,
  First = 1,
  Last = 2,
}

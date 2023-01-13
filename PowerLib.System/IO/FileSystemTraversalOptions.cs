using System;

namespace PowerLib.System.IO;

[Flags]
public enum FileSystemTraversalOptions
{
  None = 0,
  ExcludeStartDirectory = 1,
  ExcludeEmptyDirectory = 2,
  Reverse = 4,
  Refresh = 8,
}
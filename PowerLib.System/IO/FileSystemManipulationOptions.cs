using System;

namespace PowerLib.System.IO;

[Flags]
public enum FileSystemManipulationOptions
{
  None = 0,
  EnsureDirectory = 1,
  CleanupDirectory = 2,
  ClearReadOnly = 4,
  Refresh = 8,
  SkipNotExists = 16,
}

using System;

namespace PowerLib.System.IO;

[Flags]
public enum FileSystemManipulationMarker
{
  None = 0,
  ElementProcessed = 1,
  ElementError = 2,
  DirectoryCreated = 4,
  DirectoryDeleted = 8,
}

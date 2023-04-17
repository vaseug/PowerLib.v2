using System;
using System.ComponentModel;

namespace PowerLib.System.IO;

[Flags]
public enum FileSystemTraversalOptions
{
  None = 0,
  ExcludeStartDirectory = 1,
  ExcludeEmptyDirectory = 2,
  Refresh = 8,
  ShowFileOccurrence = 16,
  ShowDirectoryEnter = 32,
  ShowDirectoryLeave = 64,
  ShowDirectoryOccurrence = ShowDirectoryEnter | ShowDirectoryLeave,
  ShowAllOccurrence = ShowFileOccurrence | ShowDirectoryOccurrence,
  PreorderTraversal = ShowFileOccurrence | ShowDirectoryEnter,
  PostorderTraversal = ShowFileOccurrence | ShowDirectoryLeave,
}
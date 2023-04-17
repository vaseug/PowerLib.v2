using System;
using System.IO;

namespace PowerLib.System.IO;

public static class FileSystemProgressExtension
{
  public static void Report(this FileSystemProgress fileSystemProgress, FileSystemInfo fileSystemInfo)
  {
    if (fileSystemProgress is IProgress<FileSystemCount> progress)
      switch (fileSystemInfo)
      {
        case FileInfo fileInfo:
          progress.Report(new FileSystemCount { Files = 1, Size = fileInfo.Exists ? fileInfo.Length : 0L });
          break;
        case DirectoryInfo:
          progress.Report(new FileSystemCount { Directories = 1 });
          break;
      }
  }
}

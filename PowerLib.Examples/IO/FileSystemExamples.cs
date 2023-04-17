using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using PowerLib.System;
using PowerLib.System.IO;
using PowerLib.System.Linq;
using PowerLib.System.Validation;
using System.Threading;

namespace PowerLib.Examples.IO;

/// <summary>
/// 
/// </summary>
public static class FileSystemExamples
{
  /// <summary>
  /// Performs copying files from the <paramref name="srcDirectoryName"/> directory to the <paramref name="dstDirectoryName"/> directory with the transfer of the directory structure.
  /// </summary>
  /// <param name="srcDirectoryName">Source directory name.</param>
  /// <param name="dstDirectoryName">Destination directory name.</param>
  /// <param name="maxConcurrents">Maximum number of concurrently executing tasks that will be used to copy files</param>
  /// <param name="predicate">Predicate for filtering file system objects.</param>
  /// <returns>Сollection of results.</returns>
  public static IReadOnlyList<TransferResult<FileSystemInfo>> CopyFileSystemInfos(string srcDirectoryName, string dstDirectoryName, int maxConcurrents, Predicate<FileSystemInfo>? predicate = null)
  {
    Argument.That.NotNullOrEmpty(srcDirectoryName);
    Argument.That.NotNullOrEmpty(dstDirectoryName);

    var srcDirectoryInfo = new DirectoryInfo(srcDirectoryName);
    var dstDirectoryInfo = new DirectoryInfo(dstDirectoryName);
    var sync = new object();
    var scanningPeriod = 1000;
    IReadOnlyList<FileSystemInfo> scannedItems;
    FileSystemCount total;
    using (var scanningFilesProgress = new FileSystemProgress())
    {
      scanningFilesProgress.ProgressChanged += delegate (object? sender, FileSystemCount e)
      {
        lock (sync)
        {
          Console.CursorLeft = 0;
          Console.Write($"Scanning... directories: {e.Directories,8:D}, files: {e.Files,8:D}, size: {e.Size,12:D}");
        }
      };
      Console.WriteLine($"Scanning file system objects in source directory: {srcDirectoryInfo.FullName}");
      scanningFilesProgress.StartPeriodic(scanningPeriod);
      scannedItems = srcDirectoryInfo
        .EnumerateFileSystemInfos(FileSystemTraversalOptions.PreorderTraversal, predicate, null)
        .Apply(item => scanningFilesProgress.Report(item))
        .ToList()
        .AsReadOnly();
      scanningFilesProgress.StopPeriodic();
      total = scanningFilesProgress.Value;
      Thread.Sleep(2000);
    }
    Console.WriteLine();
    total = new FileSystemCount { Files = total.Files, Size = total.Size };
    IReadOnlyList<TransferResult<FileSystemInfo>> copiedItems;
    using (var copingProgress = new FileSystemProgress(total, total.Delta(200, 400, 1000)))
    {
      FileSystemCount last = default;
      copingProgress.ProgressChanged += delegate (object? sender, FileSystemCount e)
      {
        lock (sync)
        {
          if (e > last)
          {
            Console.CursorLeft = 0; Console.Write($"Copying... files: {e.Files,8:D} ({e.FilesRatio(total),7:P}), size: {e.Size,12:D} ({e.SizeRatio(total),7:P})");
            last = e;
          }
        }
      };
      copiedItems = scannedItems.CopyFileSystemInfosDefer(FileSystemManipulationOptions.EnsureDirectory | FileSystemManipulationOptions.ClearReadOnly, maxConcurrents,
        fileSystemInfo => new TransferOptions
        {
          DestinationPath = Path.Combine(dstDirectoryInfo.FullName, PwrPath.GetRelativePath(srcDirectoryInfo.FullName, fileSystemInfo.FullName)!),
          Overwrite = false,
          NoProcessing = fileSystemInfo is DirectoryInfo,
        }, (fsi, op, ex) => { Console.CursorLeft = 0; Console.WriteLine($"{fsi.FullName} : {ex.Message}"); return true; })
        .Apply(item =>
        {
          if (item.ManipulationMarker.IsFlagsSet(FileSystemManipulationMarker.DirectoryCreated)
            && item.DestinationInfo is DirectoryInfo dstDirInfo && item.SourceInfo is DirectoryInfo srcDirInfo && dstDirInfo.Attributes != srcDirInfo.Attributes)
            dstDirInfo.Attributes = srcDirInfo.Attributes;
        })
        .Apply(item =>
        {
          if (item.ManipulationMarker.IsFlagsSet(FileSystemManipulationMarker.ElementProcessed))
            copingProgress.Report(item.SourceInfo);
        })
        .ToList()
        .AsReadOnly();
      Thread.Sleep(2000);
    }
    Console.WriteLine();
    return copiedItems;
  }

  /// <summary>
  /// Performs moving files from the <paramref name="srcDirectoryName"/> directory to the <paramref name="dstDirectoryName"/> directory with the transfer of the directory structure.
  /// </summary>
  /// <param name="srcDirectoryName">Source directory name.</param>
  /// <param name="dstDirectoryName">Destination directory name.</param>
  /// <param name="maxConcurrents">Maximum number of concurrently executing tasks that will be used to move files</param>
  /// <param name="predicate">Predicate for filtering file system objects.</param>
  /// <returns>Сollection of results.</returns>
  public static IReadOnlyList<TransferResult<FileSystemInfo>> MoveFileSystemInfos(string srcDirectoryName, string dstDirectoryName, int maxConcurrents, Predicate<FileSystemInfo>? predicate = null)
  {
    Argument.That.NotNullOrEmpty(srcDirectoryName);
    Argument.That.NotNullOrEmpty(dstDirectoryName);

    var srcDirectoryInfo = new DirectoryInfo(srcDirectoryName);
    var dstDirectoryInfo = new DirectoryInfo(dstDirectoryName);
    var sync = new object();
    var scanningPeriod = 1000;
    IReadOnlyList<MarkedInfo<FileSystemInfo>> scannedItems;
    FileSystemCount total;
    using (var scanningFilesProgress = new FileSystemProgress())
    {
      scanningFilesProgress.ProgressChanged += delegate (object? sender, FileSystemCount e)
      {
        lock (sync)
        {
          Console.CursorLeft = 0;
          Console.Write($"Scanning... directories: {e.Directories,8:D}, files: {e.Files,8:D}, size: {e.Size,12:D}");
        }
      };
      Console.WriteLine($"Scanning file system objects in source directory: {srcDirectoryInfo.FullName}");
      scanningFilesProgress.StartPeriodic(scanningPeriod);
      scannedItems = srcDirectoryInfo
        .EnumerateFileSystemInfos(FileSystemTraversalOptions.ShowAllOccurrence, predicate, null, (fileSystemInfo, traversalMarker) => fileSystemInfo.ToMarkedInfo(traversalMarker))
        .Apply(item => scanningFilesProgress.Report(item.Info))
        .ToList()
        .AsReadOnly();
      scanningFilesProgress.StopPeriodic();
      total = scanningFilesProgress.Value;
      Thread.Sleep(2000);
    }
    Console.WriteLine();
    total = new FileSystemCount { Files = total.Files, Size = total.Size };
    IReadOnlyList<TransferResult<FileSystemInfo>> movedItems;
    using (var movingProgress = new FileSystemProgress(total, total.Delta(200, 400, 1000)))
    {
      FileSystemCount last = default;
      movingProgress.ProgressChanged += delegate (object? sender, FileSystemCount e)
      {
        lock (sync)
        {
          if (e > last)
          {
            Console.CursorLeft = 0; Console.Write($"Moving... files: {e.Files,8:D} ({e.FilesRatio(total),7:P}), size: {e.Size,12:D} ({e.SizeRatio(total),7:P})");
            last = e;
          }
        }
      };
      movedItems = scannedItems.MoveFileSystemInfosDefer(FileSystemManipulationOptions.EnsureDirectory | FileSystemManipulationOptions.CleanupDirectory | FileSystemManipulationOptions.ClearReadOnly, maxConcurrents,
        (fileSystemInfo, traversalMarker) => new TransferOptions
        {
          DestinationPath = Path.Combine(dstDirectoryInfo.FullName, PwrPath.GetRelativePath(srcDirectoryInfo.FullName, fileSystemInfo.FullName)!),
          Overwrite = false,
          NoProcessing = fileSystemInfo is DirectoryInfo,
        },
        (fileSystemInfo, traversalMarker, manipulationParams, exception) => { Console.CursorLeft = 0; Console.WriteLine($"{fileSystemInfo.FullName} : {exception.Message}"); return true; })
        .Apply(item =>
        {
          if (item.ManipulationMarker.IsFlagsSet(FileSystemManipulationMarker.DirectoryCreated)
            && item.DestinationInfo is DirectoryInfo dstDirInfo && item.SourceInfo is DirectoryInfo srcDirInfo && dstDirInfo.Attributes != srcDirInfo.Attributes)
            dstDirInfo.Attributes = srcDirInfo.Attributes;
        })
        .Apply(item =>
        {
          if (item.ManipulationMarker.IsFlagsSet(FileSystemManipulationMarker.ElementProcessed))
            movingProgress.Report(item.SourceInfo);
        })
        .ToList()
        .AsReadOnly();
      Thread.Sleep(2000);
    }
    Console.WriteLine();
    return movedItems;
  }

  /// <summary>
  /// Performs replacing files in the <paramref name="dstDirectoryName"/> directory by the files from <paramref name="srcDirectoryName"/> directory and backup files to <paramref name="bakDirectoryName"/> directory.
  /// </summary>
  /// <param name="srcDirectoryName">Source directory name.</param>
  /// <param name="dstDirectoryName">Destination directory name.</param>
  /// <param name="bakDirectoryName">Destination backup directory name.</param>
  /// <param name="maxConcurrents">Maximum number of concurrently executing tasks that will be used to replace files.</param>
  /// <param name="predicate">Predicate for filtering file system objects.</param>
  /// <returns>Сollection of results.</returns>
  public static IReadOnlyList<ReplaceResult<FileSystemInfo>> ReplaceFileSystemInfos(string srcDirectoryName, string dstDirectoryName, string? bakDirectoryName, int maxConcurrents, Predicate<FileSystemInfo>? predicate = null)
  {
    Argument.That.NotNull(srcDirectoryName);
    Argument.That.NotNull(dstDirectoryName);

    var srcDirectoryInfo = new DirectoryInfo(srcDirectoryName);
    var dstDirectoryInfo = new DirectoryInfo(dstDirectoryName);
    var bakDirectoryInfo = bakDirectoryName is null ? null : new DirectoryInfo(bakDirectoryName);
    var sync = new object();
    var scanningPeriod = 1000;
    IReadOnlyList<MarkedInfo<FileSystemInfo>> scannedItems;
    FileSystemCount total;
    using (var scanningProgress = new FileSystemProgress())
    {
      scanningProgress.ProgressChanged += delegate (object? sender, FileSystemCount e)
      {
        lock (sync)
        {
          Console.CursorLeft = 0;
          Console.Write($"Scanning... directories: {e.Directories,8:D}, files: {e.Files,8:D}, size: {e.Size,12:D}");
        }
      };
      Console.WriteLine($"Scanning file system objects in directory: {srcDirectoryInfo.FullName}");
      scanningProgress.StartPeriodic(scanningPeriod);
      scannedItems = srcDirectoryInfo
        .EnumerateFileSystemInfos(FileSystemTraversalOptions.ShowAllOccurrence, predicate, null, (fileSystemInfo, traversalMarker) => fileSystemInfo.ToMarkedInfo(traversalMarker))
        .Apply(markedInfo =>
        {
          if (markedInfo.Info is FileInfo || markedInfo.TraversalMarker == FileSystemTraversalMarker.EnterDirectory)
            scanningProgress.Report(markedInfo.Info);
        })
        .ToList()
        .AsReadOnly();
      scanningProgress.StopPeriodic();
      total = scanningProgress.Value;
      Thread.Sleep(2000);
    }
    Console.WriteLine();
    total = new FileSystemCount { Files = total.Files, Size = total.Size };
    IReadOnlyList<ReplaceResult<FileSystemInfo>> replacedItems;
    using (var replacingProgress = new FileSystemProgress(total, total.Delta(200, 400, 1000)))
    {
      FileSystemCount last = default;
      replacingProgress.ProgressChanged += delegate (object? sender, FileSystemCount e)
      {
        lock (sync)
        {
          if (e > last)
          {
            Console.CursorLeft = 0;
            Console.Write($"Replacing... files: {e.Files,8:D} ({e.FilesRatio(total),7:P}), size: {e.Size,12:D} ({e.SizeRatio(total),7:P})");
            last = e;
          }
        }
      };
      replacedItems = scannedItems.ReplaceFileSystemInfosDefer(FileSystemManipulationOptions.EnsureDirectory | FileSystemManipulationOptions.CleanupDirectory | FileSystemManipulationOptions.ClearReadOnly, maxConcurrents,
        (fileSystemInfo, traversalMarker) =>
        {
          return new ReplaceOptions
          {
            DestinationPath = Path.Combine(dstDirectoryInfo.FullName, PwrPath.GetRelativePath(srcDirectoryInfo.FullName, fileSystemInfo.FullName)!),
            DestinationBackupPath = bakDirectoryInfo is null ? null : Path.Combine(bakDirectoryInfo.FullName, PwrPath.GetRelativePath(srcDirectoryInfo.FullName, fileSystemInfo.FullName)!),
            NoProcessing = fileSystemInfo is DirectoryInfo,
          };
        },
        (fileSystemInfo, traversalMarker, manipulationParams, exception) => { Console.CursorLeft = 0; Console.WriteLine($"{fileSystemInfo.FullName} : {exception.Message}"); return true; })
        .Apply(item =>
        {
          if (item.ManipulationMarker.IsFlagsSet(FileSystemManipulationMarker.DirectoryCreated)
            && item.DestinationBackupInfo is DirectoryInfo bakDirInfo && item.SourceInfo is DirectoryInfo srcDirInfo && bakDirInfo.Attributes != srcDirInfo.Attributes)
            bakDirInfo.Attributes = srcDirInfo.Attributes;
        })
        .Apply(item =>
        {
          if (item.ManipulationMarker == FileSystemManipulationMarker.ElementProcessed)
            replacingProgress.Report(item.SourceInfo);
        })
        .ToList()
        .AsReadOnly();
      Thread.Sleep(2000);
    }
    Console.WriteLine();
    return replacedItems;
  }

  /// <summary>
  /// Performs deleting files and empty subdirectories in the <paramref name="directoryName"/> directory.
  /// </summary>
  /// <param name="directoryName">Directory name.</param>
  /// <param name="maxConcurrents">Maximum number of concurrently executing tasks that will be used to delete files</param>
  /// <param name="predicate">Predicate for filtering file system objects.</param>
  /// <returns>Сollection of results.</returns>
  public static IReadOnlyList<InfoResult<FileSystemInfo>> DeleteFileSystemInfos(string directoryName, int maxConcurrents, Predicate<FileSystemInfo>? predicate)
  {
    Argument.That.NotNull(directoryName);

    var directoryInfo = new DirectoryInfo(directoryName);
    var sync = new object();
    var scanningPeriod = 1000;
    IReadOnlyList<FileSystemInfo> scannedItems;
    FileSystemCount total;
    using (var scanningProgress = new FileSystemProgress())
    {
      scanningProgress.ProgressChanged += delegate (object? sender, FileSystemCount e)
      {
        lock (sync)
        {
          Console.CursorLeft = 0;
          Console.Write($"Scanning... directories: {e.Directories,8:D}, files: {e.Files,8:D}, size: {e.Size,12:D}");
        }
      };
      Console.WriteLine($"Scanning file system objects in directory: {directoryInfo.FullName}");
      scanningProgress.StartPeriodic(scanningPeriod);
      scannedItems = directoryInfo
        .EnumerateFileSystemInfos(FileSystemTraversalOptions.PostorderTraversal, predicate, null)
        .Apply(info => scanningProgress.Report(info))
        .ToList()
        .AsReadOnly();
      scanningProgress.StopPeriodic();
      total = scanningProgress.Value;
      Thread.Sleep(2000);
    }
    Console.WriteLine();
    total = new FileSystemCount { Files = total.Files, Size = total.Size };
    IReadOnlyList<InfoResult<FileSystemInfo>> deletedItems;
    using (var deletingProgress = new FileSystemProgress(total, total.Delta(200, 200, 500)))
    {
      FileSystemCount last = default;
      deletingProgress.ProgressChanged += delegate (object? sender, FileSystemCount e)
      {
        lock (sync)
        {
          if (e > last)
          {
            Console.CursorLeft = 0; Console.Write($"Deleting... remaining files: {total.Files - e.Files,8:D} ({e.FilesRatio(total),7:P})");
            last = e;
          }
        }
      };
      deletedItems = scannedItems
        .DeleteFileSystemInfosDefer(FileSystemManipulationOptions.CleanupDirectory | FileSystemManipulationOptions.ClearReadOnly, maxConcurrents,
          fileSystemInfo => new DeleteOptions
          {
            Recursive = false,
            NoProcessing = fileSystemInfo is DirectoryInfo,
          }, (fileSystemInfo, manipulationParams, exception) => { Console.CursorLeft = 0; Console.WriteLine($"{fileSystemInfo.FullName} : {exception.Message}"); return true; })
        .Apply(item =>
        {
          if (item.ManipulationMarker == FileSystemManipulationMarker.ElementProcessed)
            deletingProgress.Report(item.Info);
        })
        .ToList()
        .AsReadOnly();
      Thread.Sleep(2000);
    }
    Console.WriteLine();
    return deletedItems;
  }
}

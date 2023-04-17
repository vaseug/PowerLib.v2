using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PowerLib.System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Linq;
using PowerLib.System.Validation;

namespace PowerLib.System.IO
{
  /// <summary>
  ///	FileSystemInfoExtension class provides extension methods to work with FileSystemInfo and inherited FileInfo and DirectoryInfo objects.
  /// </summary>
  public static class FileSystemInfoExtension
  {
    private const string DefaultSearchPattern = "*";

    #region Predicate methods

    public static Predicate<FileSystemInfo> Combine(this Predicate<DirectoryInfo> directoryPredicate, Predicate<FileInfo> filePredicate)
    {
      Argument.That.NotNull(directoryPredicate);
      Argument.That.NotNull(filePredicate);

      return fileSystemInfo => fileSystemInfo switch
      {
        DirectoryInfo directoryInfo => directoryPredicate(directoryInfo),
        FileInfo fileInfo => filePredicate(fileInfo),
        _ => false,
      };
    }

    public static Predicate<FileSystemInfo> Combine(this Predicate<FileInfo> filePredicate, Predicate<DirectoryInfo> directoryPredicate)
    {
      Argument.That.NotNull(filePredicate);
      Argument.That.NotNull(directoryPredicate);

      return fileSystemInfo => fileSystemInfo switch
      {
        DirectoryInfo directoryInfo => directoryPredicate(directoryInfo),
        FileInfo fileInfo => filePredicate(fileInfo),
        _ => false,
      };
    }

    #endregion
    #region Is

    public static bool IsFile(this FileSystemInfo fileSystemInfo)
      => Argument.That.NotNull(fileSystemInfo) is FileInfo;

    public static bool IsDirectory(this FileSystemInfo fileSystemInfo)
      => Argument.That.NotNull(fileSystemInfo) is DirectoryInfo;

    public static bool IsFileAndExists(this FileSystemInfo fileSystemInfo)
      => Argument.That.NotNull(fileSystemInfo) is FileInfo fileInfo && fileInfo.Exists;

    public static bool IsDirectoryAndExists(this FileSystemInfo fileSystemInfo)
      => Argument.That.NotNull(fileSystemInfo) is DirectoryInfo directoryInfo && directoryInfo.Exists;

    public static bool IsBaseOf(this DirectoryInfo directoryInfo, FileSystemInfo fileSystemInfo)
      => PwrPath.IsBaseOf(Argument.That.NotNull(directoryInfo).FullName, Argument.That.NotNull(fileSystemInfo).FullName);

    public static bool HasChildren(this DirectoryInfo directoryInfo)
      => Argument.That.NotNull(directoryInfo).EnumerateFileSystemInfos().Any();

    #endregion
    #region Result methods

    public static MarkedInfo<TInfo> ToMarkedInfo<TInfo>(this TInfo info, FileSystemTraversalMarker traversalMarker)
      where TInfo : FileSystemInfo
      => new(info, traversalMarker);

    public static InfoResult<TInfo> ToInfoResult<TInfo>(this TInfo info, FileSystemTraversalMarker traversalMarker, FileSystemManipulationMarker manipulationMarker)
      where TInfo : FileSystemInfo
      => new(info, traversalMarker, manipulationMarker);

    public static TransferResult<TInfo> ToTransferResult<TInfo>(this TInfo sourceInfo, FileSystemTraversalMarker traversalMarker, FileSystemManipulationMarker manipulationMarker, TInfo? destinationInfo)
      where TInfo : FileSystemInfo
      => new(sourceInfo, traversalMarker, manipulationMarker, destinationInfo);

    public static ReplaceResult<TInfo> ToReplaceResult<TInfo>(this TInfo sourceInfo, FileSystemTraversalMarker traversalMarker, FileSystemManipulationMarker manipulationMarker, TInfo? destinationInfo, TInfo? destinationBackupInfo)
      where TInfo : FileSystemInfo
      => new(sourceInfo, traversalMarker, manipulationMarker, destinationInfo, destinationBackupInfo);

    #endregion
    #region Internal methods
    #region Enumeration auxiliary methods

    private static string? DefaultSearchPatternSelector(DirectoryInfo directoryInfo)
      => DefaultSearchPattern;

    private static string? DefaultSearchPatternSelector(IReadOnlyList<DirectoryInfo> directoryInfos)
      => DefaultSearchPattern;

    private static IEnumerable<FileSystemInfo> GetChildrenCore(DirectoryInfo directoryInfo, Func<DirectoryInfo, string?> searchPatternSelector)
    {
      var searchPattern = searchPatternSelector(directoryInfo);
      return searchPattern is null ? Enumerable.Empty<FileSystemInfo>() : directoryInfo.EnumerateFileSystemInfos(searchPattern, SearchOption.TopDirectoryOnly);
    }

    private static IEnumerable<FileSystemInfo> GetChildrenCore(IReadOnlyList<DirectoryInfo> parentalDirectoryInfos, Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector)
    {
      var searchPattern = searchPatternSelector(parentalDirectoryInfos);
      return searchPattern is null ? Enumerable.Empty<FileSystemInfo>() : parentalDirectoryInfos.GetLast().EnumerateFileSystemInfos(searchPattern, SearchOption.TopDirectoryOnly);
    }

    private static IEnumerable<DirectoryInfo> GetChildDirectoriesCore(DirectoryInfo directoryInfo, Func<DirectoryInfo, string?> searchPatternSelector)
    {
      var searchPattern = searchPatternSelector(directoryInfo);
      return searchPattern is null ? Enumerable.Empty<DirectoryInfo>() : directoryInfo.EnumerateDirectories(searchPattern, SearchOption.TopDirectoryOnly);
    }

    private static IEnumerable<DirectoryInfo> GetChildDirectoriesCore(IReadOnlyList<DirectoryInfo> parentalDirectoryInfos, Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector)
    {
      var searchPattern = searchPatternSelector(parentalDirectoryInfos);
      return searchPattern is null ? Enumerable.Empty<DirectoryInfo>() : parentalDirectoryInfos.GetLast().EnumerateDirectories(searchPattern, SearchOption.TopDirectoryOnly);
    }

    private static bool HasChildrenCore(DirectoryInfo directoryInfo, bool refresh)
    {
      if (refresh)
        directoryInfo.Refresh();
      return directoryInfo.Exists && directoryInfo.EnumerateFileSystemInfos().Any();
    }

    #endregion
    #region Enumeration methods

    private static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosCore<TFileSystemInfo>(DirectoryInfo startDirectoryInfo, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, IEnumerable<FileSystemInfo>> getChildren, Predicate<FileSystemInfo> hasChildren,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison,
      Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
    {
      if (maxDepth == 0 || !hasChildren(startDirectoryInfo))
        yield break;
      var excludeEmpty = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeEmptyDirectory);
      var fileOccurrence = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ShowFileOccurrence);
      var directoryEnter = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ShowDirectoryEnter);
      var directoryLeave = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ShowDirectoryLeave);
      var children = getChildren(startDirectoryInfo);
      if (predicate is not null)
        children = children.Where(predicate.Invoke);
      if (comparison is not null)
        children = children.Sort(comparison);
      foreach (FileSystemInfo fileSystemInfo in children)
      {
        if (!hasChildren(fileSystemInfo) || !fileSystemInfo.Exists)
        {
          switch (fileSystemInfo)
          {
            case FileInfo:
              if (fileOccurrence)
                yield return selector(fileSystemInfo, FileSystemTraversalMarker.None);
              break;
            case DirectoryInfo when !excludeEmpty:
              if (directoryEnter)
                yield return selector(fileSystemInfo, FileSystemTraversalMarker.EnterDirectory);
              if (directoryLeave)
                yield return selector(fileSystemInfo, FileSystemTraversalMarker.LeaveDirectory);
              break;
          }
        }
        else
        {
          switch (fileSystemInfo)
          {
            case FileInfo:
              if (fileOccurrence)
                yield return selector(fileSystemInfo, FileSystemTraversalMarker.None);
              break;
            case DirectoryInfo directoryInfo when !excludeEmpty:
              using (var enumerator = EnumerateFileSystemInfosCore(directoryInfo, maxDepth - 1, traversalOptions, getChildren, hasChildren, predicate, comparison, selector)
                .GetEnumerator())
              {
                if (enumerator.MoveNext())
                {
                  if (directoryEnter)
                    yield return selector(fileSystemInfo, FileSystemTraversalMarker.EnterDirectory);
                  if (maxDepth == 1)
                    continue;
                  else
                    yield return enumerator.Current;
                  while (enumerator.MoveNext())
                    yield return enumerator.Current;
                  if (directoryLeave)
                    yield return selector(fileSystemInfo, FileSystemTraversalMarker.LeaveDirectory);
                }
                else if (!excludeEmpty)
                {
                  if (directoryEnter)
                    yield return selector(fileSystemInfo, FileSystemTraversalMarker.EnterDirectory);
                  if (directoryLeave)
                    yield return selector(fileSystemInfo, FileSystemTraversalMarker.LeaveDirectory);
                }
              }
              break;
          }
        }
      }
    }

    private static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosCore<TFileSystemInfo>(DirectoryInfo startDirectoryInfo,
      ParentalDirectoriesContext parentalDirectoriesContext, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<IReadOnlyList<DirectoryInfo>, IEnumerable<FileSystemInfo>> getChildren, Predicate<FileSystemInfo> hasChildren,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
    {
      if (maxDepth == 0 || !hasChildren(startDirectoryInfo))
        yield break;
      var excludeEmpty = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeEmptyDirectory);
      var fileOccurrence = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ShowFileOccurrence);
      var directoryEnter = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ShowDirectoryEnter);
      var directoryLeave = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ShowDirectoryLeave);
      parentalDirectoriesContext.Push(startDirectoryInfo);
      var level = parentalDirectoriesContext.Total;
      var children = getChildren(parentalDirectoriesContext.AtLevel(level));
      if (predicate is not null)
        children = children.Where(fileSystemInfo => predicate((fileSystemInfo, parentalDirectoriesContext.AtLevel(level))));
      if (comparison is not null)
        children = children.Sort(comparison);
      foreach (FileSystemInfo fileSystemInfo in children)
      {
        if (!hasChildren(fileSystemInfo) || !fileSystemInfo.Exists)
        {
          switch (fileSystemInfo)
          {
            case FileInfo:
              if (fileOccurrence)
                yield return selector(fileSystemInfo, parentalDirectoriesContext.AtLevel(level), FileSystemTraversalMarker.None);
              break;
            case DirectoryInfo when !excludeEmpty:
              if (directoryEnter)
                yield return selector(fileSystemInfo, parentalDirectoriesContext.AtLevel(level), FileSystemTraversalMarker.EnterDirectory);
              if (directoryLeave)
                yield return selector(fileSystemInfo, parentalDirectoriesContext.AtLevel(level), FileSystemTraversalMarker.LeaveDirectory);
              break;
          }
        }
        else
        {
          switch (fileSystemInfo)
          {
            case FileInfo:
              if (fileOccurrence)
                yield return selector(fileSystemInfo, parentalDirectoriesContext.AtLevel(level), FileSystemTraversalMarker.None);
              break;
            case DirectoryInfo directoryInfo when !excludeEmpty:
              using (var enumerator = EnumerateFileSystemInfosCore(directoryInfo, parentalDirectoriesContext, maxDepth - 1, traversalOptions, getChildren, hasChildren, predicate, comparison, selector)
                .GetEnumerator())
              {
                if (enumerator.MoveNext())
                {
                  if (directoryEnter)
                    yield return selector(fileSystemInfo, parentalDirectoriesContext.AtLevel(level), FileSystemTraversalMarker.EnterDirectory);
                  if (maxDepth == 1)
                    continue;
                  else
                    yield return enumerator.Current;
                  while (enumerator.MoveNext())
                    yield return enumerator.Current;
                  if (directoryLeave)
                    yield return selector(fileSystemInfo, parentalDirectoriesContext.AtLevel(level), FileSystemTraversalMarker.LeaveDirectory);
                }
                else if (!excludeEmpty)
                {
                  if (directoryEnter)
                    yield return selector(fileSystemInfo, parentalDirectoriesContext.AtLevel(level), FileSystemTraversalMarker.EnterDirectory);
                  if (directoryLeave)
                    yield return selector(fileSystemInfo, parentalDirectoriesContext.AtLevel(level), FileSystemTraversalMarker.LeaveDirectory);
                }
              }
              break;
          }
        }
      }
      parentalDirectoriesContext.Pop();
    }

    private static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosCore<TFileSystemInfo>(DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
    {
      if (!startDirectoryInfo.Exists)
        throw new DirectoryNotFoundException();
      if (traversalOptions.OverlapFlags(FileSystemTraversalOptions.ShowAllOccurrence) == FileSystemTraversalOptions.None)
        traversalOptions = traversalOptions.CombineFlags(FileSystemTraversalOptions.PreorderTraversal);
      var excludeStart = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeStartDirectory);
      var excludeEmpty = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeEmptyDirectory);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var directoryEnter = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ShowDirectoryEnter);
      var directoryLeave = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ShowDirectoryLeave);
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, maxDepth, traversalOptions,
        directoryInfo => GetChildrenCore(directoryInfo, searchPatternSelector),
        fileSystemInfo => fileSystemInfo is DirectoryInfo directoryInfo && HasChildrenCore(directoryInfo, refresh),
        predicate, comparison, selector);
      using var enumerator = children.GetEnumerator();
      if (enumerator.MoveNext())
      {
        if (!excludeStart && directoryEnter)
          yield return selector(startDirectoryInfo, FileSystemTraversalMarker.EnterDirectory);
        do
          yield return enumerator.Current;
        while (enumerator.MoveNext());
        if (!excludeStart && directoryLeave)
          yield return selector(startDirectoryInfo, FileSystemTraversalMarker.LeaveDirectory);
      }
      else if (!excludeEmpty && !excludeStart)
      {
        if (directoryEnter)
          yield return selector(startDirectoryInfo, FileSystemTraversalMarker.EnterDirectory);
        if (directoryLeave)
          yield return selector(startDirectoryInfo, FileSystemTraversalMarker.LeaveDirectory);
      }
    }

    private static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosCore<TFileSystemInfo>(DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
    {
      if (!startDirectoryInfo.Exists)
        throw new DirectoryNotFoundException();
      if (traversalOptions.OverlapFlags(FileSystemTraversalOptions.ShowAllOccurrence) == FileSystemTraversalOptions.None)
        traversalOptions = traversalOptions.CombineFlags(FileSystemTraversalOptions.PreorderTraversal);
      var excludeStart = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeStartDirectory);
      var excludeEmpty = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeEmptyDirectory);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var directoryEnter = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ShowDirectoryEnter);
      var directoryLeave = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ShowDirectoryLeave);
      var parentalDirectoriesContext = new ParentalDirectoriesContext();
      var level = parentalDirectoriesContext.Total;
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, parentalDirectoriesContext, maxDepth, traversalOptions,
        parentalDirectoryInfos => GetChildrenCore(parentalDirectoryInfos, searchPatternSelector),
        fileSystemInfo => fileSystemInfo is DirectoryInfo directoryInfo && HasChildrenCore(directoryInfo, refresh),
        predicate, comparison, selector);
      using var enumerator = children.GetEnumerator();
      if (enumerator.MoveNext())
      {
        if (!excludeStart && directoryEnter)
          yield return selector(startDirectoryInfo, parentalDirectoriesContext.AtLevel(level), FileSystemTraversalMarker.EnterDirectory);
        do
          yield return enumerator.Current;
        while (enumerator.MoveNext());
        if (!excludeStart && directoryLeave)
          yield return selector(startDirectoryInfo, parentalDirectoriesContext.AtLevel(level), FileSystemTraversalMarker.LeaveDirectory);
      }
      else if (!excludeEmpty && !excludeStart)
      {
        if (directoryEnter)
          yield return selector(startDirectoryInfo, parentalDirectoriesContext.AtLevel(level), FileSystemTraversalMarker.EnterDirectory);
        if (directoryLeave)
          yield return selector(startDirectoryInfo, parentalDirectoriesContext.AtLevel(level), FileSystemTraversalMarker.LeaveDirectory);
      }
    }

    private static IEnumerable<TDirectoryInfo> EnumerateDirectoriesCore<TDirectoryInfo>(DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison,
      Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
    {
      if (!startDirectoryInfo.Exists)
        throw new DirectoryNotFoundException();
      traversalOptions = traversalOptions.RemoveFlags(FileSystemTraversalOptions.ShowFileOccurrence);
      if (!traversalOptions.IsFlagsOverlapped(FileSystemTraversalOptions.ShowDirectoryOccurrence))
        traversalOptions = traversalOptions.CombineFlags(FileSystemTraversalOptions.ShowDirectoryEnter);
      var excludeStart = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeStartDirectory);
      var excludeEmpty = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeEmptyDirectory);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var directoryEnter = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ShowDirectoryEnter);
      var directoryLeave = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ShowDirectoryLeave);
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, maxDepth, traversalOptions,
        directoryInfo => GetChildDirectoriesCore(directoryInfo, searchPatternSelector),
        fileSystemInfo => fileSystemInfo is DirectoryInfo directoryInfo && HasChildrenCore(directoryInfo, refresh),
        fileSystemInfo => fileSystemInfo is DirectoryInfo directoryInfo && (predicate?.Invoke(directoryInfo) ?? true),
        comparison is not null ? (x, y) => comparison((DirectoryInfo)x, (DirectoryInfo)y) : default,
        (fileSystemInfo, traversalMarker) => selector((DirectoryInfo)fileSystemInfo, traversalMarker));
      using var enumerator = children.GetEnumerator();
      if (enumerator.MoveNext())
      {
        if (!excludeStart && directoryEnter)
          yield return selector(startDirectoryInfo, FileSystemTraversalMarker.EnterDirectory);
        do
          yield return enumerator.Current;
        while (enumerator.MoveNext());
        if (!excludeStart && directoryLeave)
          yield return selector(startDirectoryInfo, FileSystemTraversalMarker.LeaveDirectory);
      }
      else if (!excludeEmpty && !excludeStart)
      {
        if (directoryEnter)
          yield return selector(startDirectoryInfo, FileSystemTraversalMarker.EnterDirectory);
        if (directoryLeave)
          yield return selector(startDirectoryInfo, FileSystemTraversalMarker.LeaveDirectory);
      }
    }

    private static IEnumerable<TDirectoryInfo> EnumerateDirectoriesCore<TDirectoryInfo>(DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
    {
      if (!startDirectoryInfo.Exists)
        throw new DirectoryNotFoundException();
      traversalOptions = traversalOptions.RemoveFlags(FileSystemTraversalOptions.ShowFileOccurrence);
      if (!traversalOptions.IsFlagsOverlapped(FileSystemTraversalOptions.ShowDirectoryOccurrence))
        traversalOptions = traversalOptions.CombineFlags(FileSystemTraversalOptions.ShowDirectoryEnter);
      var excludeStart = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeStartDirectory);
      var excludeEmpty = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeEmptyDirectory);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var directoryEnter = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ShowDirectoryEnter);
      var directoryLeave = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ShowDirectoryLeave);
      var parentalDirectoriesContext = new ParentalDirectoriesContext();
      var level = parentalDirectoriesContext.Total;
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, parentalDirectoriesContext, maxDepth, traversalOptions,
        parentalDirectoryInfos => GetChildDirectoriesCore(parentalDirectoryInfos, searchPatternSelector),
        fileSystemInfo => fileSystemInfo is DirectoryInfo directoryInfo && HasChildrenCore(directoryInfo, refresh),
        ((FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos) element) =>
          element.fileSystemInfo is DirectoryInfo directoryInfo && (predicate?.Invoke((directoryInfo, element.parentalDirectoryInfos)) ?? true),
        comparison is not null ? (x, y) => comparison((DirectoryInfo)x, (DirectoryInfo)y) : default,
        (FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos, FileSystemTraversalMarker marker)
          => selector((DirectoryInfo)fileSystemInfo, parentalDirectoryInfos, marker));
      using var enumerator = children.GetEnumerator();
      if (enumerator.MoveNext())
      {
        if (!excludeStart && directoryEnter)
          yield return selector(startDirectoryInfo, parentalDirectoriesContext.AtLevel(level), FileSystemTraversalMarker.EnterDirectory);
        do
          yield return enumerator.Current;
        while (enumerator.MoveNext());
        if (!excludeStart && directoryLeave)
          yield return selector(startDirectoryInfo, parentalDirectoriesContext.AtLevel(level), FileSystemTraversalMarker.LeaveDirectory);
      }
      else if (!excludeEmpty && !excludeStart)
      {
        if (directoryEnter)
          yield return selector(startDirectoryInfo, parentalDirectoriesContext.AtLevel(level), FileSystemTraversalMarker.EnterDirectory);
        if (directoryLeave)
          yield return selector(startDirectoryInfo, parentalDirectoriesContext.AtLevel(level), FileSystemTraversalMarker.LeaveDirectory);
      }
    }

    private static IEnumerable<TFileInfo> EnumerateFilesCore<TFileInfo>(DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, FileSystemTraversalMarker, TFileInfo> selector)
    {
      if (!startDirectoryInfo.Exists)
        throw new DirectoryNotFoundException();
      traversalOptions = traversalOptions
        .CombineFlags(FileSystemTraversalOptions.ShowFileOccurrence)
        .RemoveFlags(FileSystemTraversalOptions.ShowDirectoryOccurrence);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, maxDepth, traversalOptions,
        directoryInfo => GetChildrenCore(directoryInfo, searchPatternSelector),
        fileSystemInfo => fileSystemInfo is DirectoryInfo directoryInfo && HasChildrenCore(directoryInfo, refresh),
        fileSystemInfo =>
          fileSystemInfo is DirectoryInfo directoryInfo && (directoryPredicate?.Invoke(directoryInfo) ?? true) ||
          fileSystemInfo is FileInfo fileInfo && (filePredicate?.Invoke(fileInfo) ?? true),
        comparison,
        (fileSystemInfo, traversalMarker) => selector((FileInfo)fileSystemInfo, traversalMarker));
      return children;
    }

    private static IEnumerable<TFileInfo> EnumerateFilesCore<TFileInfo>(DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison, Func<FileInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileInfo> selector)
    {
      if (!startDirectoryInfo.Exists)
        throw new DirectoryNotFoundException();
      traversalOptions = traversalOptions
        .CombineFlags(FileSystemTraversalOptions.ShowFileOccurrence)
        .RemoveFlags(FileSystemTraversalOptions.ShowDirectoryOccurrence);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var parentalDirectoriesContext = new ParentalDirectoriesContext();
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, parentalDirectoriesContext, maxDepth, traversalOptions,
        parentalDirectoryInfos => GetChildrenCore(parentalDirectoryInfos, searchPatternSelector),
        fileSystemInfo => fileSystemInfo is DirectoryInfo directoryInfo && HasChildrenCore(directoryInfo, refresh),
        ((FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos) element) =>
          element.fileSystemInfo is DirectoryInfo directoryInfo && (directoryPredicate?.Invoke((directoryInfo, element.parentalDirectoryInfos)) ?? true) ||
          element.fileSystemInfo is FileInfo fileInfo && (filePredicate?.Invoke((fileInfo, element.parentalDirectoryInfos)) ?? true),
        comparison,
        (fileSystemInfo, parentalDirectoryInfos, traversalMarker) => selector((FileInfo)fileSystemInfo, parentalDirectoryInfos, traversalMarker));
      return children;
    }

    #endregion
    #region Manipulation auxiliary methods

    private static bool CreateIfNotExists(string directoryPath)
    {
      if (Directory.Exists(directoryPath))
        return false;
      Directory.CreateDirectory(directoryPath);
      return true;
    }

    private static bool RemoveIfEmpty(this DirectoryInfo directoryInfo, bool clearReadOnly)
    {
      if (!directoryInfo.Exists || directoryInfo.Parent is null || HasChildrenCore(directoryInfo, false))
        return false;
      if (clearReadOnly)
        directoryInfo.ClearReadOnly();
      directoryInfo.Delete();
      return true;
    }

    private static bool ClearReadOnly(this FileSystemInfo fileSystemInfo)
    {
      if (!fileSystemInfo.Exists || !fileSystemInfo.Attributes.IsFlagsSet(FileAttributes.ReadOnly))
        return false;
      fileSystemInfo.Attributes = fileSystemInfo.Attributes.RemoveFlags(FileAttributes.ReadOnly);
      return true;
    }

    private static IEnumerable<FileSystemInfo> ClearReadOnlyRecursive(this DirectoryInfo directoryInfo)
    {
      if (!directoryInfo.Exists)
        yield break;
      if (directoryInfo.Attributes.IsFlagsSet(FileAttributes.ReadOnly))
      {
        directoryInfo.Attributes = directoryInfo.Attributes.RemoveFlags(FileAttributes.ReadOnly);
        yield return directoryInfo;
      }
      foreach (var fileSystemInfo in directoryInfo.EnumerateFileSystemInfos(DefaultSearchPattern, SearchOption.AllDirectories)
        .Where(fileSystemInfo => fileSystemInfo.Attributes.IsFlagsSet(FileAttributes.ReadOnly))
        .Apply(fileSystemInfo => fileSystemInfo.Attributes = fileSystemInfo.Attributes.RemoveFlags(FileAttributes.ReadOnly)))
        yield return fileSystemInfo;
    }

    private static FileInfo CopyCore(FileInfo fileInfo, TransferOptions copyOptions, bool clearReadOnly)
    {
      if (copyOptions.NoProcessing)
        return new FileInfo(copyOptions.DestinationPath);
      if (copyOptions.Overwrite && clearReadOnly)
        new FileInfo(copyOptions.DestinationPath).ClearReadOnly();
      return fileInfo.CopyTo(copyOptions.DestinationPath, copyOptions.Overwrite);
    }

    private static FileSystemInfo CopyCore(FileSystemInfo fileSystemInfo, TransferOptions copyOptions, bool clearReadOnly)
      => fileSystemInfo switch
      {
        FileInfo fileInfo => CopyCore(fileInfo, copyOptions, clearReadOnly),
        DirectoryInfo => copyOptions.NoProcessing ? new DirectoryInfo(copyOptions.DestinationPath) : Operation.That.Failed(),
        _ => Argument.That.Invalid(fileSystemInfo)
      };

    private static FileInfo MoveCore(FileInfo fileInfo, TransferOptions moveOptions, bool clearReadOnly)
    {
      if (moveOptions.NoProcessing)
        return new FileInfo(moveOptions.DestinationPath);
      if (moveOptions.Overwrite && clearReadOnly)
        new FileInfo(moveOptions.DestinationPath).ClearReadOnly();
      var dstFileInfo = new FileInfo(fileInfo.FullName);
#if NETCOREAPP3_0_OR_GREATER
      dstFileInfo.MoveTo(moveOptions.DestinationPath, moveOptions.Overwrite);
#else
      dstFileInfo.MoveTo(moveOptions.DestinationPath);
#endif
      return dstFileInfo;
    }

    private static DirectoryInfo MoveCore(DirectoryInfo directoryInfo, TransferOptions moveOptions)
    {
      if (moveOptions.NoProcessing)
        return new DirectoryInfo(moveOptions.DestinationPath);
      var dstDirectoryInfo = new DirectoryInfo(directoryInfo.FullName);
      dstDirectoryInfo.MoveTo(moveOptions.DestinationPath);
      return dstDirectoryInfo;
    }

    private static FileSystemInfo MoveCore(FileSystemInfo fileSystemInfo, TransferOptions moveOptions, bool clearReadOnly)
      => fileSystemInfo switch
      {
        FileInfo fileInfo => MoveCore(fileInfo, moveOptions, clearReadOnly),
        DirectoryInfo directoryInfo => MoveCore(directoryInfo, moveOptions),
        _ => Argument.That.Invalid(fileSystemInfo)
      };

    private static (FileInfo dstFileInfo, FileInfo? bakFileInfo) ReplaceCore(FileInfo fileInfo, ReplaceOptions replaceOptions, bool clearReadOnly)
    {
      var bakFileInfo = replaceOptions.DestinationBackupPath is not null ? new FileInfo(replaceOptions.DestinationBackupPath) : null;
      if (replaceOptions.NoProcessing)
        return (new FileInfo(replaceOptions.DestinationPath), bakFileInfo);
      if (clearReadOnly)
      {
        new FileInfo(replaceOptions.DestinationPath).ClearReadOnly();
        bakFileInfo?.ClearReadOnly();
        fileInfo.ClearReadOnly();
      }
      var dstFileInfo = fileInfo.Replace(replaceOptions.DestinationPath, replaceOptions.DestinationBackupPath, replaceOptions.IgnoreMetadataErrors);
      return (dstFileInfo, bakFileInfo);
    }

    private static (FileSystemInfo dstFileSystemInfo, FileSystemInfo? bakFileSystemInfo) ReplaceCore(FileSystemInfo fileSystemInfo, ReplaceOptions replaceOptions, bool clearReadOnly)
      => fileSystemInfo switch
      {
        FileInfo fileInfo => ((FileSystemInfo, FileSystemInfo?))ReplaceCore(fileInfo, replaceOptions, clearReadOnly),
        DirectoryInfo => replaceOptions.NoProcessing
          ? ((FileSystemInfo, FileSystemInfo?))(new DirectoryInfo(replaceOptions.DestinationPath), replaceOptions.DestinationBackupPath is not null ? new DirectoryInfo(replaceOptions.DestinationBackupPath) : null)
          : Operation.That.Unsupported(),
        _ => Argument.That.Invalid(fileSystemInfo)
      };

    private static void DeleteCore(FileInfo fileInfo, DeleteOptions deleteParams, bool clearReadOnly)
    {
      if (!deleteParams.NoProcessing)
      {
        if (clearReadOnly)
          fileInfo.ClearReadOnly();
        fileInfo.Delete();
      }
    }

    private static void DeleteCore(DirectoryInfo directoryInfo, DeleteOptions deleteParams, bool clearReadOnly)
    {
      if (!deleteParams.NoProcessing)
      {
        if (clearReadOnly)
          if (deleteParams.Recursive)
            directoryInfo.ClearReadOnlyRecursive();
          else
            directoryInfo.ClearReadOnly();
        directoryInfo.Delete(deleteParams.Recursive);
      }
    }

    private static void DeleteCore(FileSystemInfo fileSystemInfo, DeleteOptions deleteParams, bool clearReadOnly)
    {
      switch (fileSystemInfo)
      {
        case FileInfo fileInfo:
          DeleteCore(fileInfo, deleteParams, clearReadOnly);
          break;
        case DirectoryInfo directoryInfo:
          DeleteCore(directoryInfo, deleteParams, clearReadOnly);
          break;
        default:
          Argument.That.Invalid(fileSystemInfo);
          break;
      };
    }

    #endregion
    #region File system manipulation methods

    private static async IAsyncEnumerable<TManipulationResult> ManipulateFileSystemInfosAsyncCore<TFileSystemItem, TManipulationParams, TManipulationInterim, TManipulationResult>(
      IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> fileSystemInfoSelector,
      Func<TFileSystemItem, TManipulationParams> manipulationParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams, TManipulationInterim?> fileSystemHandler,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TManipulationInterim?, TManipulationResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams?, Exception, bool>? errorHandler,
      [EnumeratorCancellation] CancellationToken cancellationToken)
    {
      foreach (var fileSystemItem in fileSystemItems)
      {
        cancellationToken.ThrowIfCancellationRequested();
        yield return await Task.Run(() => Process(fileSystemItem));
      };

      TManipulationResult Process(TFileSystemItem fileSystemItem)
      {
        var markedFileSystemInfo = fileSystemInfoSelector(fileSystemItem);
        var fileSystemInfo = markedFileSystemInfo.Info;
        var traversalMarker = markedFileSystemInfo.TraversalMarker;
        if (manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.Refresh))
          fileSystemInfo.Refresh();
        if (fileSystemInfo.Exists || !manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.SkipNotExists))
        {
          var manipulationMarker = FileSystemManipulationMarker.None;
          var manipulationParams = manipulationParamsSelector.Invoke(fileSystemItem);
          if (manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.EnsureDirectory) && traversalMarker.IsFlagsSet(FileSystemTraversalMarker.EnterDirectory) && fileSystemInfo is DirectoryInfo enterDirectoryInfo
            && manipulationParams is IEnsuringOptions ensuringOption && ensuringOption.EnsuringDirectories is not null)
            manipulationMarker = manipulationMarker.CombineFlags(Safe.Invoke(
              () => ensuringOption.EnsuringDirectories.Select(path => CreateIfNotExists(path)).Count(created => created) > 0 ? FileSystemManipulationMarker.DirectoryCreated : FileSystemManipulationMarker.None,
              exception => errorHandler?.Invoke(fileSystemInfo, traversalMarker, manipulationParams, exception) ?? false, _ => FileSystemManipulationMarker.None));
          var manipulationInterim = default(TManipulationInterim);
          manipulationMarker = manipulationMarker.CombineFlags(Safe.Invoke(() =>
          {
            manipulationInterim = fileSystemHandler(fileSystemInfo, traversalMarker, manipulationParams);
            return manipulationParams is IProcessingOptions processingOptions && processingOptions.NoProcessing ? FileSystemManipulationMarker.None : FileSystemManipulationMarker.ElementProcessed;
          }, exception => errorHandler?.Invoke(fileSystemInfo, traversalMarker, manipulationParams, exception) ?? false, _ => FileSystemManipulationMarker.ElementError));
          if (manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.CleanupDirectory) && traversalMarker.IsFlagsSet(FileSystemTraversalMarker.LeaveDirectory) && fileSystemInfo is DirectoryInfo leaveDirectoryInfo)
            manipulationMarker = manipulationMarker.CombineFlags(Safe.Invoke(
              () => leaveDirectoryInfo.RemoveIfEmpty(manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly)) ? FileSystemManipulationMarker.DirectoryDeleted : FileSystemManipulationMarker.None,
              exception => errorHandler?.Invoke(fileSystemInfo, traversalMarker, manipulationParams, exception) ?? false, _ => FileSystemManipulationMarker.None));
          return resultSelector(fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim);
        }
        return resultSelector(fileSystemInfo, traversalMarker, FileSystemManipulationMarker.None, default);
      }
    }

    private static IEnumerable<TManipulationResult> ManipulateFileSystemInfosCore<TFileSystemItem, TManipulationParams, TManipulationInterim, TManipulationResult>(
      IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> fileSystemInfoSelector,
      Func<TFileSystemItem, TManipulationParams> manipulationParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams, TManipulationInterim?> fileSystemHandler,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TManipulationInterim?, TManipulationResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams?, Exception, bool>? errorHandler,
      CancellationToken cancellationToken)
    {
      return fileSystemItems
        .Select(fileSystemItem =>
        {
          cancellationToken.ThrowIfCancellationRequested();
          return Process(fileSystemItem);
        });

      TManipulationResult Process(TFileSystemItem fileSystemItem)
      {
        var markedFileSystemInfo = fileSystemInfoSelector(fileSystemItem);
        var fileSystemInfo = markedFileSystemInfo.Info;
        var traversalMarker = markedFileSystemInfo.TraversalMarker;
        if (manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.Refresh))
          fileSystemInfo.Refresh();
        if (fileSystemInfo.Exists || !manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.SkipNotExists))
        {
          var manipulationMarker = FileSystemManipulationMarker.None;
          var manipulationParams = manipulationParamsSelector.Invoke(fileSystemItem);
          if (manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.EnsureDirectory) && traversalMarker.IsFlagsSet(FileSystemTraversalMarker.EnterDirectory) && fileSystemInfo is DirectoryInfo enterDirectoryInfo
            && manipulationParams is IEnsuringOptions ensuringOption && ensuringOption.EnsuringDirectories is not null)
            manipulationMarker = manipulationMarker.CombineFlags(Safe.Invoke(
              () => ensuringOption.EnsuringDirectories.Select(path => CreateIfNotExists(path)).Count(created => created) > 0 ? FileSystemManipulationMarker.DirectoryCreated : FileSystemManipulationMarker.None,
              exception => errorHandler?.Invoke(fileSystemInfo, traversalMarker, manipulationParams, exception) ?? false, _ => FileSystemManipulationMarker.None));
          var manipulationInterim = default(TManipulationInterim);
          manipulationMarker = manipulationMarker.CombineFlags(Safe.Invoke(() =>
          {
            manipulationInterim = fileSystemHandler(fileSystemInfo, traversalMarker, manipulationParams);
            return manipulationParams is IProcessingOptions processingOptions && processingOptions.NoProcessing ? FileSystemManipulationMarker.None : FileSystemManipulationMarker.ElementProcessed;
          }, exception => errorHandler?.Invoke(fileSystemInfo, traversalMarker, manipulationParams, exception) ?? false, _ => FileSystemManipulationMarker.ElementError));
          if (manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.CleanupDirectory) && traversalMarker.IsFlagsSet(FileSystemTraversalMarker.LeaveDirectory) && fileSystemInfo is DirectoryInfo leaveDirectoryInfo)
            manipulationMarker = manipulationMarker.CombineFlags(Safe.Invoke(
              () => leaveDirectoryInfo.RemoveIfEmpty(manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly)) ? FileSystemManipulationMarker.DirectoryDeleted : FileSystemManipulationMarker.None,
              exception => errorHandler?.Invoke(fileSystemInfo, traversalMarker, manipulationParams, exception) ?? false, _ => FileSystemManipulationMarker.None));
          return resultSelector(fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim);
        }
        return resultSelector(fileSystemInfo, traversalMarker, FileSystemManipulationMarker.None, default);
      }
    }

    private static IEnumerable<TManipulationResult> ManipulateFileSystemInfosCore<TFileSystemItem, TManipulationParams, TManipulationInterim, TManipulationResult>(
      IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> fileSystemInfoSelector,
      Func<TFileSystemItem, TManipulationParams> manipulationParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams, TManipulationInterim?> fileSystemHandler,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TManipulationInterim?, TManipulationResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams?, Exception, bool>? errorHandler,
      CancellationToken cancellationToken)
    {
      return fileSystemItems
        .Select(fileSystemItem =>
        {
          cancellationToken.ThrowIfCancellationRequested();
          var item = ProcessSource(fileSystemItem);
          return item.fileSystemInfo is DirectoryInfo && item.traversalMarker == FileSystemTraversalMarker.EnterDirectory ? ProcessDirectoryEnter(item) : item;
        })
        .AsParallel()
        .AsOrdered()
        .WithCancellation(cancellationToken)
        .WithMergeOptions(ParallelMergeOptions.NotBuffered)
        .WithDegreeOfParallelism(maxConcurrents)
        .Select(item =>
        {
          cancellationToken.ThrowIfCancellationRequested();
          return item.fileSystemInfo is FileInfo ? ProcessFile(item) : item;
        })
        .AsSequential()
        .Select(item =>
        {
          cancellationToken.ThrowIfCancellationRequested();
          return ProcessResult(item.fileSystemInfo is DirectoryInfo && item.traversalMarker == FileSystemTraversalMarker.LeaveDirectory ? ProcessDirectoryLeave(item) : item);
        });

      (FileSystemInfo fileSystemInfo, FileSystemTraversalMarker traversalMarker, TManipulationParams? manipulationParams, FileSystemManipulationMarker manipulationMarker, TManipulationInterim? manipulationInterim) ProcessSource(TFileSystemItem fileSystemItem)
      {
        var markedFileSystemInfo = fileSystemInfoSelector(fileSystemItem);
        var fileSystemInfo = markedFileSystemInfo.Info;
        var traversalMarker = markedFileSystemInfo.TraversalMarker;
        if (manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.Refresh))
          fileSystemInfo.Refresh();
        var manipulationParams = fileSystemInfo.Exists || !manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.SkipNotExists) ? manipulationParamsSelector.Invoke(fileSystemItem) : default;
        return (fileSystemInfo, traversalMarker, manipulationParams, FileSystemManipulationMarker.None, default);
      }

      (FileSystemInfo fileSystemInfo, FileSystemTraversalMarker traversalMarker, TManipulationParams? manipulationParams, FileSystemManipulationMarker manipulationMarker, TManipulationInterim? manipulationInterim) ProcessDirectoryEnter(
        (FileSystemInfo fileSystemInfo, FileSystemTraversalMarker traversalMarker, TManipulationParams? manipulationParams, FileSystemManipulationMarker manipulationMarker, TManipulationInterim? manipulationInterim) item)
      {
        if (item.traversalMarker.IsFlagsSet(FileSystemTraversalMarker.EnterDirectory) && item.fileSystemInfo is DirectoryInfo enterDirectoryInfo &&
          (item.fileSystemInfo.Exists || !manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.SkipNotExists)))
        {
          var manipulationMarker = item.manipulationMarker;
          if (manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.EnsureDirectory) && item.manipulationParams is IEnsuringOptions ensuringOption && ensuringOption.EnsuringDirectories is not null)
            manipulationMarker = manipulationMarker.CombineFlags(Safe.Invoke(
              () => ensuringOption.EnsuringDirectories.Select(path => CreateIfNotExists(path)).Count(created => created) > 0 ? FileSystemManipulationMarker.DirectoryCreated : FileSystemManipulationMarker.None,
              exception => errorHandler?.Invoke(item.fileSystemInfo, item.traversalMarker, item.manipulationParams, exception) ?? false, _ => FileSystemManipulationMarker.None));
          var manipulationInterim = default(TManipulationInterim);
          manipulationMarker = manipulationMarker.CombineFlags(Safe.Invoke(() =>
          {
            manipulationInterim = fileSystemHandler(item.fileSystemInfo, item.traversalMarker, item.manipulationParams);
            return item.manipulationParams is IProcessingOptions processingOptions && processingOptions.NoProcessing ? FileSystemManipulationMarker.None : FileSystemManipulationMarker.ElementProcessed;
          }, exception => errorHandler?.Invoke(item.fileSystemInfo, item.traversalMarker, item.manipulationParams, exception) ?? false, _ => FileSystemManipulationMarker.ElementError));
          return (item.fileSystemInfo, item.traversalMarker, item.manipulationParams, manipulationMarker, manipulationInterim);
        }
        return item;
      }

      (FileSystemInfo fileSystemInfo, FileSystemTraversalMarker traversalMarker, TManipulationParams? manipulationParams, FileSystemManipulationMarker manipulationMarker, TManipulationInterim? manipulationInterim) ProcessFile(
        (FileSystemInfo fileSystemInfo, FileSystemTraversalMarker traversalMarker, TManipulationParams? manipulationParams, FileSystemManipulationMarker manipulationMarker, TManipulationInterim? manipulationInterim) item)
      {
        if (item.traversalMarker.IsFlagsSet(FileSystemTraversalMarker.None) && item.fileSystemInfo is FileInfo srcFileInfo &&
          (item.fileSystemInfo.Exists || !manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.SkipNotExists)))
        {
          var manipulationInterim = default(TManipulationInterim);
          var manipulationMarker = item.manipulationMarker.CombineFlags(Safe.Invoke(() =>
          {
            manipulationInterim = fileSystemHandler(item.fileSystemInfo, item.traversalMarker, item.manipulationParams);
            return item.manipulationParams is IProcessingOptions processingOptions && processingOptions.NoProcessing ? FileSystemManipulationMarker.None : FileSystemManipulationMarker.ElementProcessed;
          }, exception => errorHandler?.Invoke(item.fileSystemInfo, item.traversalMarker, item.manipulationParams, exception) ?? false, _ => FileSystemManipulationMarker.ElementError));
          return (item.fileSystemInfo, item.traversalMarker, item.manipulationParams, manipulationMarker, manipulationInterim);
        }
        return item;
      }

      (FileSystemInfo fileSystemInfo, FileSystemTraversalMarker traversalMarker, TManipulationParams? manipulationParams, FileSystemManipulationMarker manipulationMarker, TManipulationInterim? manipulationInterim) ProcessDirectoryLeave(
        (FileSystemInfo fileSystemInfo, FileSystemTraversalMarker traversalMarker, TManipulationParams? manipulationParams, FileSystemManipulationMarker manipulationMarker, TManipulationInterim? manipulationInterim) item)
      {
        if (item.traversalMarker.IsFlagsSet(FileSystemTraversalMarker.LeaveDirectory) && item.fileSystemInfo is DirectoryInfo leaveDirectoryInfo &&
          (item.fileSystemInfo.Exists || !manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.SkipNotExists)))
        {
          var manipulationInterim = default(TManipulationInterim);
          var manipulationMarker = item.manipulationMarker.CombineFlags(Safe.Invoke(() =>
          {
            manipulationInterim = fileSystemHandler(item.fileSystemInfo, item.traversalMarker, item.manipulationParams);
            return item.manipulationParams is IProcessingOptions processingOptions && processingOptions.NoProcessing ? FileSystemManipulationMarker.None : FileSystemManipulationMarker.ElementProcessed;
          }, exception => errorHandler?.Invoke(item.fileSystemInfo, item.traversalMarker, item.manipulationParams, exception) ?? false, _ => FileSystemManipulationMarker.ElementError));
          if (manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.CleanupDirectory))
            manipulationMarker = manipulationMarker.CombineFlags(Safe.Invoke(
              () => leaveDirectoryInfo.RemoveIfEmpty(manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly)) ? FileSystemManipulationMarker.DirectoryDeleted : FileSystemManipulationMarker.None,
              exception => errorHandler?.Invoke(item.fileSystemInfo, item.traversalMarker, item.manipulationParams, exception) ?? false, _ => FileSystemManipulationMarker.None));
          return (item.fileSystemInfo, item.traversalMarker, item.manipulationParams, manipulationMarker, manipulationInterim);
        }
        return item;
      }

      TManipulationResult ProcessResult((FileSystemInfo fileSystemInfo, FileSystemTraversalMarker traversalMarker, TManipulationParams? manipulationParams, FileSystemManipulationMarker manipulationMarker, TManipulationInterim? manipulationInterim) item)
        => resultSelector(item.fileSystemInfo, item.traversalMarker, item.manipulationMarker, item.manipulationInterim);
    }

    #endregion
    #region Directory manipulation methods

    private static IEnumerable<TManipulationResult> ManipulateDirectoriesCore<TDirectoryItem, TManipulationParams, TManipulationInterim, TManipulationResult>(
      IEnumerable<TDirectoryItem> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<TDirectoryItem, MarkedInfo<DirectoryInfo>> markedInfoSelector,
      Func<TDirectoryItem, TManipulationParams> manipulationParamsSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, TManipulationParams, TManipulationInterim?> directoryHandler,
      Func<DirectoryInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TManipulationInterim?, TManipulationResult> resultSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, TManipulationParams?, Exception, bool>? errorHandler,
      CancellationToken cancellationToken)
    {
      return directoryItems
        .Select(directoryItem =>
        {
          cancellationToken.ThrowIfCancellationRequested();
          return Process(directoryItem);
        });

      TManipulationResult Process(TDirectoryItem directoryItem)
      {
        var directoryMarkedInfo = markedInfoSelector(directoryItem);
        var directoryInfo = directoryMarkedInfo.Info;
        var traversalMarker = directoryMarkedInfo.TraversalMarker;
        if (manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.Refresh))
          directoryInfo.Refresh();
        if (directoryInfo.Exists || !manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.SkipNotExists))
        {
          switch (traversalMarker)
          {
            case FileSystemTraversalMarker.EnterDirectory:
              {
                var manipulationInterim = default(TManipulationInterim);
                var manipulationMarker = FileSystemManipulationMarker.None;
                var manipulationParams = manipulationParamsSelector(directoryItem);
                if (manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.EnsureDirectory) && manipulationParams is IEnsuringOptions ensuringOption && ensuringOption.EnsuringDirectories is not null)
                  manipulationMarker = manipulationMarker.CombineFlags(Safe.Invoke(
                    () => ensuringOption.EnsuringDirectories.Select(path => CreateIfNotExists(path)).Count(created => created) > 0 ? FileSystemManipulationMarker.DirectoryCreated : FileSystemManipulationMarker.None,
                    exception => errorHandler?.Invoke(directoryInfo, traversalMarker, manipulationParams, exception) ?? false, _ => FileSystemManipulationMarker.None));
                manipulationMarker = manipulationMarker.CombineFlags(Safe.Invoke(() =>
                {
                  manipulationInterim = directoryHandler(directoryInfo, traversalMarker, manipulationParams);
                  return manipulationParams is IProcessingOptions processingOptions && processingOptions.NoProcessing ? FileSystemManipulationMarker.None : FileSystemManipulationMarker.ElementProcessed;
                }, exception => errorHandler?.Invoke(directoryInfo, traversalMarker, manipulationParams, exception) ?? false, _ => FileSystemManipulationMarker.ElementError));
                return resultSelector(directoryInfo, traversalMarker, manipulationMarker, manipulationInterim);
              }
            case FileSystemTraversalMarker.LeaveDirectory:
              {
                var manipulationInterim = default(TManipulationInterim);
                var manipulationMarker = FileSystemManipulationMarker.None;
                var manipulationParams = manipulationParamsSelector(directoryItem);
                manipulationMarker = manipulationMarker.CombineFlags(Safe.Invoke(() =>
                {
                  manipulationInterim = directoryHandler(directoryInfo, traversalMarker, manipulationParams);
                  return manipulationParams is IProcessingOptions processingOptions && processingOptions.NoProcessing ? FileSystemManipulationMarker.None : FileSystemManipulationMarker.ElementProcessed;
                }, exception => errorHandler?.Invoke(directoryInfo, traversalMarker, manipulationParams, exception) ?? false, _ => FileSystemManipulationMarker.ElementError));
                if (manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.CleanupDirectory))
                  manipulationMarker = manipulationMarker.CombineFlags(Safe.Invoke(
                    () => directoryInfo.RemoveIfEmpty(manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly)) ? FileSystemManipulationMarker.DirectoryDeleted : FileSystemManipulationMarker.None,
                    exception => errorHandler?.Invoke(directoryInfo, traversalMarker, default, exception) ?? false, _ => FileSystemManipulationMarker.None));
                return resultSelector(directoryInfo, traversalMarker, manipulationMarker, manipulationInterim);
              }
          }
        }
        return resultSelector(directoryInfo, traversalMarker, FileSystemManipulationMarker.None, default);
      }
    }

    #endregion
    #region File manipulation methods

    private static async IAsyncEnumerable<TManipulationResult> ManipulateFilesAsyncCore<TFileItem, TManipulationParams, TManipulationInterim, TManipulationResult>(
      IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, TManipulationParams> manipulationParamsSelector,
      Func<FileInfo, TManipulationParams, TManipulationInterim?> fileHandler,
      Func<FileInfo, FileSystemManipulationMarker, TManipulationInterim?, TManipulationResult> resultSelector,
      Func<FileInfo, TManipulationParams?, Exception, bool>? errorHandler,
      [EnumeratorCancellation] CancellationToken cancellationToken)
    {
      foreach (var fileItem in fileItems)
      {
        cancellationToken.ThrowIfCancellationRequested();
        yield return await Task.Run(() => Process(fileItem));
      };

      TManipulationResult Process(TFileItem fileItem)
      {
        var fileInfo = fileInfoSelector(fileItem);
        if (manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.Refresh))
          fileInfo.Refresh();
        if (fileInfo.Exists || !manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.SkipNotExists))
        {
          var manipulationMarker = FileSystemManipulationMarker.None;
          var options = manipulationParamsSelector(fileItem);
          if (manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.EnsureDirectory) && options is IEnsuringOptions ensuringOption && ensuringOption.EnsuringDirectories is not null)
            manipulationMarker = manipulationMarker.CombineFlags(Safe.Invoke(
              () => ensuringOption.EnsuringDirectories.Select(path => CreateIfNotExists(path)).Count(created => created) > 0 ? FileSystemManipulationMarker.DirectoryCreated : FileSystemManipulationMarker.None,
              exception => errorHandler?.Invoke(fileInfo, options, exception) ?? false, _ => FileSystemManipulationMarker.None));
          var manipulationInterim = default(TManipulationInterim);
          manipulationMarker = manipulationMarker.CombineFlags(Safe.Invoke(() =>
          {
            manipulationInterim = fileHandler(fileInfo, options);
            return options is IProcessingOptions processingOptions && processingOptions.NoProcessing ? FileSystemManipulationMarker.None : FileSystemManipulationMarker.ElementProcessed;
          }, exception => errorHandler?.Invoke(fileInfo, options, exception) ?? false, _ => FileSystemManipulationMarker.ElementError));
          if (manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.CleanupDirectory))
            manipulationMarker = manipulationMarker.CombineFlags(Safe.Invoke(
              () => fileInfo.Directory?.RemoveIfEmpty(manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly)) ?? false ? FileSystemManipulationMarker.DirectoryDeleted : FileSystemManipulationMarker.None,
              exception => errorHandler?.Invoke(fileInfo, options, exception) ?? false, _ => FileSystemManipulationMarker.None));
          return resultSelector(fileInfo, manipulationMarker, manipulationInterim);
        }
        return resultSelector(fileInfo, FileSystemManipulationMarker.None, default);
      }
    }

    private static IEnumerable<TManipulationResult> ManipulateFilesCore<TFileItem, TManipulationParams, TManipulationInterim, TManipulationResult>(
      IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, TManipulationParams> manipulationParamsSelector,
      Func<FileInfo, TManipulationParams, TManipulationInterim?> fileHandler,
      Func<FileInfo, FileSystemManipulationMarker, TManipulationInterim?, TManipulationResult> resultSelector,
      Func<FileInfo, TManipulationParams?, Exception, bool>? errorHandler,
      CancellationToken cancellationToken)
    {
      return fileItems
        .Select(fileItem =>
        {
          cancellationToken.ThrowIfCancellationRequested();
          return Process(fileItem);
        });

      TManipulationResult Process(TFileItem fileItem)
      {
        var fileInfo = fileInfoSelector(fileItem);
        if (manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.Refresh))
          fileInfo.Refresh();
        if (fileInfo.Exists || !manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.SkipNotExists))
        {
          var manipulationMarker = FileSystemManipulationMarker.None;
          var options = manipulationParamsSelector(fileItem);
          if (manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.EnsureDirectory) && options is IEnsuringOptions ensuringOption && ensuringOption.EnsuringDirectories is not null)
            manipulationMarker = manipulationMarker.CombineFlags(Safe.Invoke(
              () => ensuringOption.EnsuringDirectories.Select(path => CreateIfNotExists(path)).Count(created => created) > 0 ? FileSystemManipulationMarker.DirectoryCreated : FileSystemManipulationMarker.None,
              exception => errorHandler?.Invoke(fileInfo, options, exception) ?? false, _ => FileSystemManipulationMarker.None));
          var manipulationInterim = default(TManipulationInterim);
          manipulationMarker = manipulationMarker.CombineFlags(Safe.Invoke(() =>
          {
            manipulationInterim = fileHandler(fileInfo, options);
            return options is IProcessingOptions processingOptions && processingOptions.NoProcessing ? FileSystemManipulationMarker.None : FileSystemManipulationMarker.ElementProcessed;
          }, exception => errorHandler?.Invoke(fileInfo, options, exception) ?? false, _ => FileSystemManipulationMarker.ElementError));
          if (manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.CleanupDirectory))
            manipulationMarker = manipulationMarker.CombineFlags(Safe.Invoke(
              () => fileInfo.Directory?.RemoveIfEmpty(manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly)) ?? false ? FileSystemManipulationMarker.DirectoryDeleted : FileSystemManipulationMarker.None,
              exception => errorHandler?.Invoke(fileInfo, options, exception) ?? false, _ => FileSystemManipulationMarker.None));
          return resultSelector(fileInfo, manipulationMarker, manipulationInterim);
        }
        return resultSelector(fileInfo, FileSystemManipulationMarker.None, default);
      }
    }

    private static IEnumerable<TManipulationResult> ManipulateFilesCore<TFileItem, TManipulationParams, TManipulationInterim, TManipulationResult>(
      IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, TManipulationParams> manipulationParamsSelector,
      Func<FileInfo, TManipulationParams, TManipulationInterim?> fileHandler,
      Func<FileInfo, FileSystemManipulationMarker, TManipulationInterim?, TManipulationResult> resultSelector,
      Func<FileInfo, TManipulationParams?, Exception, bool>? errorHandler,
      CancellationToken cancellationToken)
    {
      return fileItems
        .Select(fileItem =>
        {
          cancellationToken.ThrowIfCancellationRequested();
          var item = ProcessSource(fileItem);
          return manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.EnsureDirectory) ? ProcessDirectoryEnsure(item) : item;
        })
        .AsParallel()
        .AsOrdered()
        .WithCancellation(cancellationToken)
        .WithMergeOptions(ParallelMergeOptions.NotBuffered)
        .WithDegreeOfParallelism(maxConcurrents)
        .Select(item =>
        {
          cancellationToken.ThrowIfCancellationRequested();
          return ProcessFile(item);
        })
        .AsSequential()
        .Select(item =>
        {
          cancellationToken.ThrowIfCancellationRequested();
          return ProcessResult(manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.CleanupDirectory) ? ProcessDirectoryCleanup(item) : item);
        });

      (FileInfo fileInfo, TManipulationParams? manipulationParams, FileSystemManipulationMarker manipulationMarker, TManipulationInterim? manipulationInterim) ProcessSource(TFileItem fileItem)
      {
        var fileInfo = fileInfoSelector(fileItem);
        if (manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.Refresh))
          fileInfo.Refresh();
        var manipulationParams = fileInfo.Exists || !manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.SkipNotExists) ? manipulationParamsSelector.Invoke(fileItem) : default;
        return (fileInfo, manipulationParams, FileSystemManipulationMarker.None, default);
      }

      (FileInfo fileInfo, TManipulationParams? manipulationParams, FileSystemManipulationMarker manipulationMarker, TManipulationInterim? manipulationInterim) ProcessDirectoryEnsure(
        (FileInfo fileInfo, TManipulationParams? manipulationParams, FileSystemManipulationMarker manipulationMarker, TManipulationInterim? manipulationInterim) item)
      {
        if ((item.fileInfo.Exists || !manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.SkipNotExists))
          && item.manipulationParams is IEnsuringOptions ensuringOption && ensuringOption.EnsuringDirectories is not null)
        {
          var manipulationMarker = item.manipulationMarker.CombineFlags(Safe.Invoke(
            () => ensuringOption.EnsuringDirectories.Select(path => CreateIfNotExists(path)).Count(created => created) > 0 ? FileSystemManipulationMarker.DirectoryCreated : FileSystemManipulationMarker.None,
            exception => errorHandler?.Invoke(item.fileInfo, item.manipulationParams, exception) ?? false, _ => FileSystemManipulationMarker.None));
          return (item.fileInfo, item.manipulationParams, manipulationMarker, item.manipulationInterim);
        }
        return (item.fileInfo, item.manipulationParams, item.manipulationMarker, item.manipulationInterim);
      }

      (FileInfo fileInfo, TManipulationParams? manipulationParams, FileSystemManipulationMarker manipulationMarker, TManipulationInterim? manipulationInterim) ProcessFile(
        (FileInfo fileInfo, TManipulationParams? manipulationParams, FileSystemManipulationMarker manipulationMarker, TManipulationInterim? manipulationInterim) item)
      {
        if (item.fileInfo.Exists || !manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.SkipNotExists))
        {
          var manipulationInterim = default(TManipulationInterim);
          var manipulationMarker = item.manipulationMarker.CombineFlags(Safe.Invoke(() =>
          {
            manipulationInterim = fileHandler(item.fileInfo, item.manipulationParams);
            return item.manipulationParams is IProcessingOptions processingOptions && processingOptions.NoProcessing ? FileSystemManipulationMarker.None : FileSystemManipulationMarker.ElementProcessed;
          }, exception => errorHandler?.Invoke(item.fileInfo, item.manipulationParams, exception) ?? false, _ => FileSystemManipulationMarker.ElementError));
          return (item.fileInfo, item.manipulationParams, manipulationMarker, manipulationInterim);
        }
        return (item.fileInfo, item.manipulationParams, item.manipulationMarker, item.manipulationInterim);
      }

      (FileInfo fileInfo, TManipulationParams? manipulationParams, FileSystemManipulationMarker manipulationMarker, TManipulationInterim? manipulationInterim) ProcessDirectoryCleanup(
        (FileInfo fileInfo, TManipulationParams? manipulationParams, FileSystemManipulationMarker manipulationMarker, TManipulationInterim? manipulationInterim) item)
      {
        if (item.fileInfo.Exists)
        {
          var manipulationMarker = item.manipulationMarker.CombineFlags(Safe.Invoke(
            () => item.fileInfo.Directory?.RemoveIfEmpty(manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly)) ?? false ? FileSystemManipulationMarker.DirectoryDeleted : FileSystemManipulationMarker.None,
            exception => errorHandler?.Invoke(item.fileInfo, item.manipulationParams, exception) ?? false, _ => FileSystemManipulationMarker.None));
          return (item.fileInfo, item.manipulationParams, manipulationMarker, item.manipulationInterim);
        }
        return (item.fileInfo, item.manipulationParams, item.manipulationMarker, item.manipulationInterim);
      }

      TManipulationResult ProcessResult((FileInfo fileInfo, TManipulationParams? manipulationParams, FileSystemManipulationMarker manipulationMarker, TManipulationInterim? manipulationInterim) item)
      {
        return resultSelector(item.fileInfo, item.manipulationMarker, item.manipulationInterim);
      }
    }

    #endregion
    #endregion
    #region Enumerate file system items

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFileSystemInfos(traversalOptions, null, default(Comparison<FileSystemInfo>));

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchOption, traversalOptions, null, default(Comparison<FileSystemInfo>));

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFileSystemInfos(maxDepth, traversalOptions, null, default(Comparison<FileSystemInfo>));

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, searchOption, traversalOptions, null, default(Comparison<FileSystemInfo>));

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, maxDepth, traversalOptions, null, default(Comparison<FileSystemInfo>));

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, searchOption, traversalOptions);

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, maxDepth, traversalOptions, null, default(Comparison<FileSystemInfo>));

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, predicate, comparison,
        (fileSystemInfo, marker) => fileSystemInfo);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison,
        (fileSystemInfo, marker) => fileSystemInfo);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, predicate, comparison,
        (fileSystemInfo, marker) => fileSystemInfo);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison,
        (fileSystemInfo, marker) => fileSystemInfo);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, predicate, comparison,
        (fileSystemInfo, marker) => fileSystemInfo);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison,
        (fileSystemInfo, marker) => fileSystemInfo);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate, comparison, (fileSystemInfo, marker) => fileSystemInfo);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfos(traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfos(maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    #endregion
    #region Get file system items

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFileSystemInfos(traversalOptions).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchOption, traversalOptions).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFileSystemInfos(maxDepth, traversalOptions).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, searchOption, traversalOptions).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, maxDepth, traversalOptions).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, searchOption, traversalOptions).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, maxDepth, traversalOptions).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFileSystemInfos(traversalOptions, predicate, comparison).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchOption, traversalOptions, predicate, comparison).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFileSystemInfos(maxDepth, traversalOptions, predicate, comparison).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, searchOption, traversalOptions, predicate, comparison).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, maxDepth, traversalOptions, predicate, comparison).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, searchOption, traversalOptions, predicate, comparison).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, maxDepth, traversalOptions, predicate, comparison).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfos(traversalOptions, predicate, comparer).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchOption, traversalOptions, predicate, comparer).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfos(maxDepth, traversalOptions, predicate, comparer).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, searchOption, traversalOptions, predicate, comparer).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, maxDepth, traversalOptions, predicate, comparer).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, searchOption, traversalOptions, predicate, comparer).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, maxDepth, traversalOptions, predicate, comparer).ToArray();

    #endregion
    #region Enumerate custom file system items

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(traversalOptions, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchOption, traversalOptions, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(maxDepth, traversalOptions, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, searchOption, traversalOptions, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, maxDepth, traversalOptions, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, searchOption, traversalOptions, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, maxDepth, traversalOptions, default(Predicate<FileSystemInfo>), null, selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    #endregion
    #region Get custom file system items

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(traversalOptions, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchOption, traversalOptions, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(maxDepth, traversalOptions, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, searchOption, traversalOptions, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, maxDepth, traversalOptions, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, searchOption, traversalOptions, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, maxDepth, traversalOptions, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    #endregion
    #region Enumerate file system items with parental context

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, searchOption, traversalOptions, null, default(Comparison<FileSystemInfo>));

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, maxDepth, traversalOptions, null, default(Comparison<FileSystemInfo>));

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, predicate, comparison,
        (fileSystemInfo, parentalDirectoryInfos, traversalMarker) => fileSystemInfo);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison,
        (fileSystemInfo, parentalDirectoryInfos, traversalMarker) => fileSystemInfo);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, predicate, comparison,
        (fileSystemInfo, parentalDirectoryInfos, traversalMarker) => fileSystemInfo);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison,
        (fileSystemInfo, parentalDirectoryInfos, traversalMarker) => fileSystemInfo);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, predicate, comparison,
        (fileSystemInfo, parentalDirectoryInfos, traversalMarker) => fileSystemInfo);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison,
        (fileSystemInfo, parentalDirectoryInfos, traversalMarker) => fileSystemInfo);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate, comparison,
        (fileSystemInfo, parentalDirectoryInfos, traversalMarker) => fileSystemInfo);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    #endregion
    #region Get file system items with parental context

    public static FileSystemInfo[] GetFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, searchOption, traversalOptions).ToArray();

    public static FileSystemInfo[] GetFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, maxDepth, traversalOptions).ToArray();

    public static FileSystemInfo[] GetFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(traversalOptions, predicate, comparison).ToArray();

    public static FileSystemInfo[] GetFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchOption, traversalOptions, predicate, comparison).ToArray();

    public static FileSystemInfo[] GetFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(maxDepth, traversalOptions, predicate, comparison).ToArray();

    public static FileSystemInfo[] GetFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, searchOption, traversalOptions, predicate, comparison).ToArray();

    public static FileSystemInfo[] GetFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, maxDepth, traversalOptions, predicate, comparison).ToArray();

    public static FileSystemInfo[] GetFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, searchOption, traversalOptions, predicate, comparison).ToArray();

    public static FileSystemInfo[] GetFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, maxDepth, traversalOptions, predicate, comparison).ToArray();

    public static FileSystemInfo[] GetFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(traversalOptions, predicate, comparer).ToArray();

    public static FileSystemInfo[] GetFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchOption, traversalOptions, predicate, comparer).ToArray();

    public static FileSystemInfo[] GetFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(maxDepth, traversalOptions, predicate, comparer).ToArray();

    public static FileSystemInfo[] GetFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, searchOption, traversalOptions, predicate, comparer).ToArray();

    public static FileSystemInfo[] GetFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, maxDepth, traversalOptions, predicate, comparer).ToArray();

    public static FileSystemInfo[] GetFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, searchOption, traversalOptions, predicate, comparer).ToArray();

    public static FileSystemInfo[] GetFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, maxDepth, traversalOptions, predicate, comparer).ToArray();

    #endregion
    #region Enumerate custom file system items with parental context

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(traversalOptions, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchOption, traversalOptions, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(maxDepth, traversalOptions, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, searchOption, traversalOptions, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, maxDepth, traversalOptions, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, searchOption, traversalOptions, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, maxDepth, traversalOptions, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    #endregion
    #region Get custom file system items with parental context

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(traversalOptions, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchOption, traversalOptions, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(maxDepth, traversalOptions, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, searchOption, traversalOptions, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, maxDepth, traversalOptions, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, searchOption, traversalOptions, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, maxDepth, traversalOptions, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    #endregion
    #region Enumerate directories

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateDirectories(traversalOptions, null, default(Comparison<DirectoryInfo>));

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateDirectories(searchOption, traversalOptions, null, default(Comparison<DirectoryInfo>));

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateDirectories(maxDepth, traversalOptions, null, default(Comparison<DirectoryInfo>));

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, searchOption, traversalOptions, null, default(Comparison<DirectoryInfo>));

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, maxDepth, traversalOptions, null, default(Comparison<DirectoryInfo>));

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, searchOption, traversalOptions, null, default(Comparison<DirectoryInfo>));

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, maxDepth, traversalOptions, null, default(Comparison<DirectoryInfo>));

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, predicate, comparison,
        (directoryInfo, traversalMarker) => directoryInfo);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison,
        (directoryInfo, traversalMarker) => directoryInfo);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, predicate, comparison,
        (directoryInfo, traversalMarker) => directoryInfo);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);

      return EnumerateDirectoriesCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison,
        (directoryInfo, traversalMarker) => directoryInfo);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);

      return EnumerateDirectoriesCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, predicate, comparison,
        (directoryInfo, traversalMarker) => directoryInfo);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);

      return EnumerateDirectoriesCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison,
        (directoryInfo, traversalMarker) => directoryInfo);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);

      return EnumerateDirectoriesCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate, comparison,
        (directoryInfo, traversalMarker) => directoryInfo);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectories(traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectories(searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectories(maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    #endregion
    #region Get directories

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateDirectories(traversalOptions).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateDirectories(searchOption, traversalOptions).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateDirectories(maxDepth, traversalOptions).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, searchOption, traversalOptions).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, maxDepth, traversalOptions).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, searchOption, traversalOptions).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, maxDepth, traversalOptions).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
      => startDirectoryInfo.EnumerateDirectories(traversalOptions, predicate, comparison).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
      => startDirectoryInfo.EnumerateDirectories(searchOption, traversalOptions, predicate, comparison).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
      => startDirectoryInfo.EnumerateDirectories(maxDepth, traversalOptions, predicate, comparison).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, searchOption, traversalOptions, predicate, comparison).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, maxDepth, traversalOptions, predicate, comparison).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, searchOption, traversalOptions, predicate, comparison).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, maxDepth, traversalOptions, predicate, comparison).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectories(traversalOptions, predicate, comparer).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectories(searchOption, traversalOptions, predicate, comparer).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectories(maxDepth, traversalOptions, predicate, comparer).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, searchOption, traversalOptions, predicate, comparer).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, maxDepth, traversalOptions, predicate, comparer).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, searchOption, traversalOptions, predicate, comparer).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, maxDepth, traversalOptions, predicate, comparer).ToArray();

    #endregion
    #region Enumerate custom directories

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(traversalOptions, null, default(Comparison<DirectoryInfo>), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchOption, traversalOptions, null, default(Comparison<DirectoryInfo>), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(maxDepth, traversalOptions, null, default(Comparison<DirectoryInfo>), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, searchOption, traversalOptions, null, default(Comparison<DirectoryInfo>), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, maxDepth, traversalOptions, null, default(Comparison<DirectoryInfo>), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, searchOption, traversalOptions, null, default(Comparison<DirectoryInfo>), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, maxDepth, traversalOptions, null, default(Comparison<DirectoryInfo>), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    #endregion
    #region Get custom directories

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(traversalOptions, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchOption, traversalOptions, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(maxDepth, traversalOptions, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, searchOption, traversalOptions, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, maxDepth, traversalOptions, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, searchOption, traversalOptions, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, maxDepth, traversalOptions, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    #endregion
    #region Enumerate directories with parental context

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, searchOption, traversalOptions, null, default(Comparison<DirectoryInfo>));

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, maxDepth, traversalOptions, null, default(Comparison<DirectoryInfo>));

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, predicate, comparison,
        (directoryInfo, parentalDirectoryInfos, traversalMarker) => directoryInfo);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison,
        (directoryInfo, parentalDirectoryInfos, traversalMarker) => directoryInfo);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, predicate, comparison,
        (directoryInfo, parentalDirectoryInfos, traversalMarker) => directoryInfo);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);

      return EnumerateDirectoriesCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison,
        (directoryInfo, parentalDirectoryInfos, traversalMarker) => directoryInfo);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);

      return EnumerateDirectoriesCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, predicate, comparison,
        (directoryInfo, parentalDirectoryInfos, traversalMarker) => directoryInfo);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);

      return EnumerateDirectoriesCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison,
        (directoryInfo, parentalDirectoryInfos, traversalMarker) => directoryInfo);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);

      return EnumerateDirectoriesCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate, comparison,
        (directoryInfo, parentalDirectoryInfos, traversalMarker) => directoryInfo);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectoriesEx(traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectoriesEx(maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    #endregion
    #region Get directories with parental context

    public static DirectoryInfo[] GetDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, searchOption, traversalOptions).ToArray();

    public static DirectoryInfo[] GetDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, maxDepth, traversalOptions).ToArray();

    public static DirectoryInfo[] GetDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison)
      => startDirectoryInfo.EnumerateDirectoriesEx(traversalOptions, predicate, comparison).ToArray();

    public static DirectoryInfo[] GetDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchOption, traversalOptions, predicate, comparison).ToArray();

    public static DirectoryInfo[] GetDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison)
      => startDirectoryInfo.EnumerateDirectoriesEx(maxDepth, traversalOptions, predicate, comparison).ToArray();

    public static DirectoryInfo[] GetDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, searchOption, traversalOptions, predicate, comparison).ToArray();

    public static DirectoryInfo[] GetDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, maxDepth, traversalOptions, predicate, comparison).ToArray();

    public static DirectoryInfo[] GetDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, searchOption, traversalOptions, predicate, comparison).ToArray();

    public static DirectoryInfo[] GetDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, maxDepth, traversalOptions, predicate, comparison).ToArray();

    public static DirectoryInfo[] GetDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectoriesEx(traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    public static DirectoryInfo[] GetDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    public static DirectoryInfo[] GetDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectoriesEx(maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    public static DirectoryInfo[] GetDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    public static DirectoryInfo[] GetDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    public static DirectoryInfo[] GetDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    public static DirectoryInfo[] GetDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    #endregion
    #region Enumerate custom directories with parental context

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(traversalOptions, null, default(Comparison<DirectoryInfo>), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchOption, traversalOptions, null, default(Comparison<DirectoryInfo>), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(maxDepth, traversalOptions, null, default(Comparison<DirectoryInfo>), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, searchOption, traversalOptions, null, default(Comparison<DirectoryInfo>), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, maxDepth, traversalOptions, null, default(Comparison<DirectoryInfo>), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, searchOption, traversalOptions, null, default(Comparison<DirectoryInfo>), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, maxDepth, traversalOptions, null, default(Comparison<DirectoryInfo>), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate,
      IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate,
      IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate,
      IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate,
      IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate,
      IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate,
      IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate,
      IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    #endregion
    #region Get custom directories with parental context

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(traversalOptions, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchOption, traversalOptions, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(maxDepth, traversalOptions, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, searchOption, traversalOptions, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, maxDepth, traversalOptions, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, searchOption, traversalOptions, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, maxDepth, traversalOptions, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, FileSystemTraversalMarker, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    #endregion
    #region Enumerate files

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFiles(traversalOptions, null, null, default(Comparison<FileSystemInfo>));

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFiles(searchOption, traversalOptions, null, null, default(Comparison<FileSystemInfo>));

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFiles(maxDepth, traversalOptions, null, null, default(Comparison<FileSystemInfo>));

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFiles(searchPattern, searchOption, traversalOptions, null, null, default(Comparison<FileSystemInfo>));

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFiles(searchPattern, maxDepth, traversalOptions, null, null, default(Comparison<FileSystemInfo>));

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFiles(searchPatternSelector, searchOption, traversalOptions, null, null, default(Comparison<FileSystemInfo>));

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFiles(searchPatternSelector, maxDepth, traversalOptions, null, null, default(Comparison<FileSystemInfo>));

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, traversalMarker) => fileInfo);
    }

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, traversalMarker) => fileInfo);
    }

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, traversalMarker) => fileInfo);
    }

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);

      return EnumerateFilesCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, traversalMarker) => fileInfo);
    }

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFilesCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, traversalMarker) => fileInfo);
    }

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);

      return EnumerateFilesCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, traversalMarker) => fileInfo);
    }

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFilesCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, traversalMarker) => fileInfo);
    }

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFiles(traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFiles(searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFiles(maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFiles(searchPattern, searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFiles(searchPattern, maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFiles(searchPatternSelector, searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFiles(searchPatternSelector, maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison());

    #endregion
    #region Get files

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFiles(traversalOptions).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFiles(searchOption, traversalOptions).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFiles(maxDepth, traversalOptions).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFiles(searchPattern, searchOption, traversalOptions).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFiles(searchPattern, maxDepth, traversalOptions).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFiles(searchPatternSelector, searchOption, traversalOptions).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFiles(searchPatternSelector, maxDepth, traversalOptions).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFiles(traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFiles(searchOption, traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFiles(maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFiles(searchPattern, searchOption, traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFiles(searchPattern, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFiles(searchPatternSelector, searchOption, traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFiles(searchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFiles(traversalOptions, filePredicate, directoryPredicate, comparer).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFiles(searchOption, traversalOptions, filePredicate, directoryPredicate, comparer).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFiles(maxDepth, traversalOptions, filePredicate, directoryPredicate, comparer).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFiles(searchPattern, searchOption, traversalOptions, filePredicate, directoryPredicate, comparer).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFiles(searchPattern, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparer).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFiles(searchPatternSelector, searchOption, traversalOptions, filePredicate, directoryPredicate, comparer).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFiles(searchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparer).ToArray();

    #endregion
    #region Enumerate custom files

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(traversalOptions, null, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchOption, traversalOptions, null, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(maxDepth, traversalOptions, null, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchPattern, searchOption, traversalOptions, null, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchPattern, maxDepth, traversalOptions, null, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchPatternSelector, searchOption, traversalOptions, null, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchPatternSelector, maxDepth, traversalOptions, null, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, traversalMarker) => selector(fileInfo));
    }

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, traversalMarker) => selector(fileInfo));
    }

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, traversalMarker) => selector(fileInfo));
    }

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, traversalMarker) => selector(fileInfo));
    }

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, traversalMarker) => selector(fileInfo));
    }

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, traversalMarker) => selector(fileInfo));
    }

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, traversalMarker) => selector(fileInfo));
    }

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchPattern, searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchPattern, maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchPatternSelector, searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchPatternSelector, maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison(), selector);

    #endregion
    #region Get custom files

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(traversalOptions, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchOption, traversalOptions, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(maxDepth, traversalOptions, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchPattern, searchOption, traversalOptions, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchPattern, maxDepth, traversalOptions, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchPatternSelector, searchOption, traversalOptions, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchPatternSelector, maxDepth, traversalOptions, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(traversalOptions, filePredicate, directoryPredicate, comparison, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchOption, traversalOptions, filePredicate, directoryPredicate, comparison, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchPattern, searchOption, traversalOptions, filePredicate, directoryPredicate, comparison, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchPattern, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchPatternSelector, searchOption, traversalOptions, filePredicate, directoryPredicate, comparison, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(traversalOptions, filePredicate, directoryPredicate, comparer, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchOption, traversalOptions, filePredicate, directoryPredicate, comparer, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(maxDepth, traversalOptions, filePredicate, directoryPredicate, comparer, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchPattern, searchOption, traversalOptions, filePredicate, directoryPredicate, comparer, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchPattern, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparer, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchPatternSelector, searchOption, traversalOptions, filePredicate, directoryPredicate, comparer, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparer, selector).ToArray();

    #endregion
    #region Enumerate files with parental context

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, searchOption, traversalOptions, null, null, default(Comparison<FileSystemInfo>));

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, maxDepth, traversalOptions, null, null, default(Comparison<FileSystemInfo>));

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, parentalDirectoryInfos, traversalMarker) => fileInfo);
    }

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, parentalDirectoryInfos, traversalMarker) => fileInfo);
    }

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, parentalDirectoryInfos, traversalMarker) => fileInfo);
    }

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);

      return EnumerateFilesCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, parentalDirectoryInfos, traversalMarker) => fileInfo);
    }

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFilesCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, parentalDirectoryInfos, traversalMarker) => fileInfo);
    }

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);

      return EnumerateFilesCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, parentalDirectoryInfos, traversalMarker) => fileInfo);
    }

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFilesCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, parentalDirectoryInfos, traversalMarker) => fileInfo);
    }

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison());

    #endregion
    #region Get files with parental context

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, searchOption, traversalOptions).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, maxDepth, traversalOptions).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFilesEx(traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFilesEx(searchOption, traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFilesEx(maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, searchOption, traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, searchOption, traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    #endregion
    #region Enumerate custom files with parental context

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(traversalOptions, null, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchOption, traversalOptions, null, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(maxDepth, traversalOptions, null, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, searchOption, traversalOptions, null, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, maxDepth, traversalOptions, null, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, searchOption, traversalOptions, null, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, maxDepth, traversalOptions, null, null, default(Comparison<FileSystemInfo>), selector);

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, parentalDirectoryInfos, traversalMarker) => selector(fileInfo, parentalDirectoryInfos));
    }

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, parentalDirectoryInfos, traversalMarker) => selector(fileInfo, parentalDirectoryInfos));
    }

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, parentalDirectoryInfos, traversalMarker) => selector(fileInfo, parentalDirectoryInfos));
    }

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, parentalDirectoryInfos, traversalMarker) => selector(fileInfo, parentalDirectoryInfos));
    }

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, parentalDirectoryInfos, traversalMarker) => selector(fileInfo, parentalDirectoryInfos));
    }

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, parentalDirectoryInfos, traversalMarker) => selector(fileInfo, parentalDirectoryInfos));
    }

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison,
        (fileInfo, parentalDirectoryInfos, traversalMarker) => selector(fileInfo, parentalDirectoryInfos));
    }

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison(), selector);

    #endregion
    #region Get custom files with parental context

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(traversalOptions, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchOption, traversalOptions, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(maxDepth, traversalOptions, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, searchOption, traversalOptions, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, maxDepth, traversalOptions, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, searchOption, traversalOptions, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, maxDepth, traversalOptions, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(traversalOptions, filePredicate, directoryPredicate, comparison, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchOption, traversalOptions, filePredicate, directoryPredicate, comparison, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, searchOption, traversalOptions, filePredicate, directoryPredicate, comparison, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, searchOption, traversalOptions, filePredicate, directoryPredicate, comparison, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(traversalOptions, filePredicate, directoryPredicate, comparer, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchOption, traversalOptions, filePredicate, directoryPredicate, comparer, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(maxDepth, traversalOptions, filePredicate, directoryPredicate, comparer, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, searchOption, traversalOptions, filePredicate, directoryPredicate, comparer, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparer, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, searchOption, traversalOptions, filePredicate, directoryPredicate, comparer, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      IComparer<FileSystemInfo>? comparer, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparer, selector).ToArray();

    #endregion
    #region File System objects manipulation asynchronus methods

    public static IAsyncEnumerable<TManipulationResult> ManipulateFileSystemInfosAsync<TFileSystemItem, TManipulationParams, TManipulationInterim, TManipulationResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, TManipulationParams> manipulationParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams, TManipulationInterim?> fileSystemHandler,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TManipulationInterim?, TManipulationResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams?, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(manipulationParamsSelector);
      Argument.That.NotNull(resultSelector);

      return ManipulateFileSystemInfosAsyncCore(fileSystemItems, manipulationOptions, markedInfoSelector, manipulationParamsSelector, fileSystemHandler, resultSelector, errorHandler, cancellationToken);
    }

    public static IAsyncEnumerable<TManipulationResult> ManipulateFileSystemInfosAsync<TFileSystemItem, TManipulationParams, TManipulationResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, TManipulationParams> manipulationParamsSelector,
      Action<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams> fileSystemHandler,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TManipulationResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams?, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(manipulationParamsSelector);
      Argument.That.NotNull(resultSelector);

      return ManipulateFileSystemInfosAsyncCore(fileSystemItems, manipulationOptions, markedInfoSelector, manipulationParamsSelector,
        (fileSystemInfo, traversalMarker, fileSystemParameters) =>
        {
          fileSystemHandler(fileSystemInfo, traversalMarker, fileSystemParameters);
          return Void.Value;
        },
        (fileSystemInfo, traversalMarker, manipulationMarker, _) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker),
        errorHandler, cancellationToken);
    }

    #endregion
    #region File System objects manipulation deferred methods

    public static IEnumerable<TManipulationResult> ManipulateFileSystemInfosDefer<TFileSystemItem, TManipulationParams, TManipulationInterim, TManipulationResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, TManipulationParams> manipulationParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams, TManipulationInterim?> fileSystemHandler,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TManipulationInterim?, TManipulationResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams?, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(manipulationParamsSelector);
      Argument.That.NotNull(resultSelector);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, markedInfoSelector, manipulationParamsSelector, fileSystemHandler, resultSelector, errorHandler, cancellationToken);
    }

    public static IEnumerable<TManipulationResult> ManipulateFileSystemInfosDefer<TFileSystemItem, TManipulationParams, TManipulationInterim, TManipulationResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, TManipulationParams> manipulationParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams, TManipulationInterim?> fileSystemHandler,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TManipulationInterim?, TManipulationResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams?, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(manipulationParamsSelector);
      Argument.That.NotNull(resultSelector);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, maxConcurrents, markedInfoSelector, manipulationParamsSelector, fileSystemHandler, resultSelector, errorHandler, cancellationToken);
    }

    public static IEnumerable<TManipulationResult> ManipulateFileSystemInfosDefer<TFileSystemItem, TManipulationParams, TManipulationResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, TManipulationParams> manipulationParamsSelector,
      Action<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams> fileSystemHandler,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TManipulationResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams?, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(manipulationParamsSelector);
      Argument.That.NotNull(resultSelector);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, markedInfoSelector, manipulationParamsSelector,
        (fileSystemInfo, traversalMarker, manipulationParams) =>
        {
          fileSystemHandler(fileSystemInfo, traversalMarker, manipulationParams);
          return Void.Value;
        },
        (fileSystemInfo, traversalMarker, manipulationMarker, _) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<TManipulationResult> ManipulateFileSystemInfosDefer<TFileSystemItem, TManipulationParams, TManipulationResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, TManipulationParams> manipulationParamsSelector,
      Action<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams> fileSystemHandler,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TManipulationResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams?, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(manipulationParamsSelector);
      Argument.That.NotNull(resultSelector);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, maxConcurrents, markedInfoSelector, manipulationParamsSelector,
        (fileSystemInfo, traversalMarker, manipulationParams) =>
        {
          fileSystemHandler(fileSystemInfo, traversalMarker, manipulationParams);
          return Void.Value;
        },
        (fileSystemInfo, traversalMarker, manipulationMarker, _) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker),
        errorHandler, cancellationToken);
    }

    #endregion
    #region File System objects manipulation direct methods

    public static TManipulationResult[] ManipulateFileSystemInfos<TFileSystemItem, TManipulationParams, TManipulationInterim, TManipulationResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, TManipulationParams> manipulationParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams, TManipulationInterim?> fileSystemHandler,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TManipulationInterim?, TManipulationResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams?, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(manipulationParamsSelector);
      Argument.That.NotNull(resultSelector);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, markedInfoSelector, manipulationParamsSelector, fileSystemHandler, resultSelector, errorHandler, cancellationToken)
        .ToArray();
    }

    public static TManipulationResult[] ManipulateFileSystemInfos<TFileSystemItem, TManipulationParams, TManipulationInterim, TManipulationResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, TManipulationParams> manipulationParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams, TManipulationInterim?> fileSystemHandler,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TManipulationInterim?, TManipulationResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams?, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(manipulationParamsSelector);
      Argument.That.NotNull(resultSelector);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, maxConcurrents, markedInfoSelector, manipulationParamsSelector, fileSystemHandler, resultSelector, errorHandler, cancellationToken)
        .ToArray();
    }

    public static TManipulationResult[] ManipulateFileSystemInfos<TFileSystemItem, TManipulationParams, TManipulationResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, TManipulationParams> manipulationParamsSelector,
      Action<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams> fileSystemHandler,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TManipulationResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams?, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(manipulationParamsSelector);
      Argument.That.NotNull(resultSelector);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, markedInfoSelector, manipulationParamsSelector,
        (fileSystemInfo, traversalMarker, manipulationParams) =>
        {
          fileSystemHandler(fileSystemInfo, traversalMarker, manipulationParams);
          return Void.Value;
        },
        (fileSystemInfo, traversalMarker, manipulationMarker, _) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static TManipulationResult[] ManipulateFileSystemInfos<TFileSystemItem, TManipulationParams, TManipulationResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, TManipulationParams> manipulationParamsSelector,
      Action<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams> fileSystemHandler,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TManipulationResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TManipulationParams?, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(manipulationParamsSelector);
      Argument.That.NotNull(resultSelector);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, maxConcurrents, markedInfoSelector, manipulationParamsSelector,
        (fileSystemInfo, traversalMarker, manipulationParams) =>
        {
          fileSystemHandler(fileSystemInfo, traversalMarker, manipulationParams);
          return Void.Value;
        },
        (fileSystemInfo, traversalMarker, manipulationMarker, _) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker),
        errorHandler, cancellationToken)
        .ToArray();
    }

    #endregion
    #region File System objects asynchronus copy methods

    public static IAsyncEnumerable<TTransferResult> CopyFileSystemInfosAsync<TFileSystemItem, TTransferResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, TransferOptions> copyParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, FileSystemInfo?, TTransferResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(copyParamsSelector);
      Argument.That.NotNull(resultSelector);
      
      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosAsyncCore(fileSystemItems, manipulationOptions, markedInfoSelector, copyParamsSelector,
        (fileSystemInfo, traversalMarker, copyParams) => CopyCore(fileSystemInfo, copyParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo),
        errorHandler, cancellationToken);
    }

    public static IAsyncEnumerable<TransferResult<FileSystemInfo>> CopyFileSystemInfosAsync(
      this IEnumerable<MarkedInfo<FileSystemInfo>> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions> copyParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(copyParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosAsyncCore(fileSystemItems, manipulationOptions,
        _ => _,
        fileSystemItem => copyParamsSelector(fileSystemItem.Info, fileSystemItem.TraversalMarker),
        (fileSystemInfo, traversalMarker, copyParams) => CopyCore(fileSystemInfo, copyParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => fileSystemInfo.ToTransferResult(traversalMarker, manipulationMarker, dstFileSystemInfo),
        errorHandler, cancellationToken);
    }

    public static IAsyncEnumerable<TransferResult<FileSystemInfo>> CopyFileSystemInfosAsync(
      this IEnumerable<FileSystemInfo> fileSystemInfos, FileSystemManipulationOptions manipulationOptions,
      Func<FileSystemInfo, TransferOptions> copyParamsSelector,
      Func<FileSystemInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemInfos);
      Argument.That.NotNull(copyParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosAsyncCore(fileSystemInfos, manipulationOptions,
        fileSystemInfo => fileSystemInfo.ToMarkedInfo(fileSystemInfo.IsDirectory() ? FileSystemTraversalMarker.EnterDirectory : FileSystemTraversalMarker.None),
        fileSystemInfo => copyParamsSelector(fileSystemInfo),
        (fileSystemInfo, _, copyParams) => CopyCore(fileSystemInfo, copyParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => fileSystemInfo.ToTransferResult(FileSystemTraversalMarker.None, manipulationMarker, dstFileSystemInfo),
        errorHandler is null ? null : (fileSystemInfo, _, copyParams, exception) => errorHandler(fileSystemInfo, copyParams, exception),
        cancellationToken);
    }

    #endregion
    #region File System objects deferred copy methods

    public static IEnumerable<TTransferResult> CopyFileSystemInfosDefer<TFileSystemItem, TTransferResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, TransferOptions> copyParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, FileSystemInfo?, TTransferResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(copyParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, markedInfoSelector, copyParamsSelector,
        (fileSystemInfo, traversalMarker, copyParams) => CopyCore(fileSystemInfo, copyParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<TTransferResult> CopyFileSystemInfosDefer<TFileSystemItem, TTransferResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, TransferOptions> copyParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, FileSystemInfo?, TTransferResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(copyParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, maxConcurrents, markedInfoSelector, copyParamsSelector,
        (fileSystemInfo, traversalMarker, copyParams) => CopyCore(fileSystemInfo, copyParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<TransferResult<FileSystemInfo>> CopyFileSystemInfosDefer(
      this IEnumerable<MarkedInfo<FileSystemInfo>> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions> copyParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(copyParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions,
        _ => _,
        fileSystemItem => copyParamsSelector(fileSystemItem.Info, fileSystemItem.TraversalMarker),
        (fileSystemInfo, traversalMarker, copyOptions) => CopyCore(fileSystemInfo, copyOptions, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => fileSystemInfo.ToTransferResult(traversalMarker, manipulationMarker, dstFileSystemInfo),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<TransferResult<FileSystemInfo>> CopyFileSystemInfosDefer(
      this IEnumerable<MarkedInfo<FileSystemInfo>> fileSystemItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions> copyParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(copyParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, maxConcurrents,
        _ => _,
        fileSystemItem => copyParamsSelector(fileSystemItem.Info, fileSystemItem.TraversalMarker),
        (fileSystemInfo, traversalMarker, copyOptions) => CopyCore(fileSystemInfo, copyOptions, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => fileSystemInfo.ToTransferResult(traversalMarker, manipulationMarker, dstFileSystemInfo),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<TransferResult<FileSystemInfo>> CopyFileSystemInfosDefer(
      this IEnumerable<FileSystemInfo> fileSystemInfos, FileSystemManipulationOptions manipulationOptions,
      Func<FileSystemInfo, TransferOptions> copyParamsSelector,
      Func<FileSystemInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemInfos);
      Argument.That.NotNull(copyParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemInfos, manipulationOptions,
        fileSystemInfo => fileSystemInfo.ToMarkedInfo(fileSystemInfo.IsDirectory() ? FileSystemTraversalMarker.EnterDirectory : FileSystemTraversalMarker.None),
        fileSystemInfo => copyParamsSelector(fileSystemInfo),
        (fileSystemInfo, _, copyParams) => CopyCore(fileSystemInfo, copyParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => fileSystemInfo.ToTransferResult(FileSystemTraversalMarker.None, manipulationMarker, dstFileSystemInfo),
        errorHandler is null ? null : (fileSystemInfo, _, copyParams, exception) => errorHandler(fileSystemInfo, copyParams, exception),
        cancellationToken);
    }

    public static IEnumerable<TransferResult<FileSystemInfo>> CopyFileSystemInfosDefer(
      this IEnumerable<FileSystemInfo> fileSystemInfos, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<FileSystemInfo, TransferOptions> copyParamsSelector,
      Func<FileSystemInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemInfos);
      Argument.That.NotNull(copyParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemInfos, manipulationOptions, maxConcurrents,
        fileSystemInfo => fileSystemInfo.ToMarkedInfo(fileSystemInfo.IsDirectory() ? FileSystemTraversalMarker.EnterDirectory : FileSystemTraversalMarker.None),
        fileSystemInfo => copyParamsSelector(fileSystemInfo),
        (fileSystemInfo, _, copyParams) => CopyCore(fileSystemInfo, copyParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => fileSystemInfo.ToTransferResult(FileSystemTraversalMarker.None, manipulationMarker, dstFileSystemInfo),
        errorHandler is null ? null : (fileSystemInfo, _, copyParams, exception) => errorHandler(fileSystemInfo, copyParams, exception),
        cancellationToken);
    }

    #endregion
    #region File System objects direct copy methods

    public static TTransferResult[] CopyFileSystemInfos<TFileSystemItem, TTransferResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, TransferOptions> copyParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, FileSystemInfo, TTransferResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(copyParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, markedInfoSelector, copyParamsSelector,
        (fileSystemInfo, traversalMarker, copyParams) => CopyCore(fileSystemInfo, copyParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static TTransferResult[] CopyFileSystemInfos<TFileSystemItem, TTransferResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, TransferOptions> copyParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, FileSystemInfo, TTransferResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(copyParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, maxConcurrents, markedInfoSelector, copyParamsSelector,
        (fileSystemInfo, traversalMarker, copyParams) => CopyCore(fileSystemInfo, copyParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static TransferResult<FileSystemInfo>[] CopyFileSystemInfos(
      this IEnumerable<MarkedInfo<FileSystemInfo>> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions> copyParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(copyParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions,
        _ => _,
        fileSystemItem => copyParamsSelector(fileSystemItem.Info, fileSystemItem.TraversalMarker),
        (fileSystemInfo, traversalMarker, copyOptions) => CopyCore(fileSystemInfo, copyOptions, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => fileSystemInfo.ToTransferResult(traversalMarker, manipulationMarker, dstFileSystemInfo),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static TransferResult<FileSystemInfo>[] CopyFileSystemInfos(
      this IEnumerable<MarkedInfo<FileSystemInfo>> fileSystemItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions> copyParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(copyParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, maxConcurrents,
        _ => _,
        fileSystemItem => copyParamsSelector(fileSystemItem.Info, fileSystemItem.TraversalMarker),
        (fileSystemInfo, traversalMarker, copyOptions) => CopyCore(fileSystemInfo, copyOptions, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => fileSystemInfo.ToTransferResult(traversalMarker, manipulationMarker, dstFileSystemInfo),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static TransferResult<FileSystemInfo>[] CopyFileSystemInfos(
      this IEnumerable<FileSystemInfo> fileSystemInfos, FileSystemManipulationOptions manipulationOptions,
      Func<FileSystemInfo, TransferOptions> copyParamsSelector,
      Func<FileSystemInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemInfos);
      Argument.That.NotNull(copyParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemInfos, manipulationOptions,
        fileSystemInfo => fileSystemInfo.ToMarkedInfo(fileSystemInfo.IsDirectory() ? FileSystemTraversalMarker.EnterDirectory : FileSystemTraversalMarker.None),
        fileSystemInfo => copyParamsSelector(fileSystemInfo),
        (fileSystemInfo, _, copyParams) => CopyCore(fileSystemInfo, copyParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => fileSystemInfo.ToTransferResult(FileSystemTraversalMarker.None, manipulationMarker, dstFileSystemInfo),
        errorHandler is null ? null : (fileSystemInfo, _, copyParams, exception) => errorHandler(fileSystemInfo, copyParams, exception),
        cancellationToken)
        .ToArray();
    }

    public static TransferResult<FileSystemInfo>[] CopyFileSystemInfos(
      this IEnumerable<FileSystemInfo> fileSystemInfos, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<FileSystemInfo, TransferOptions> copyParamsSelector,
      Func<FileSystemInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemInfos);
      Argument.That.NotNull(copyParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemInfos, manipulationOptions, maxConcurrents,
        fileSystemInfo => fileSystemInfo.ToMarkedInfo(fileSystemInfo.IsDirectory() ? FileSystemTraversalMarker.EnterDirectory : FileSystemTraversalMarker.None),
        fileSystemInfo => copyParamsSelector(fileSystemInfo),
        (fileSystemInfo, _, copyParams) => CopyCore(fileSystemInfo, copyParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => fileSystemInfo.ToTransferResult(FileSystemTraversalMarker.None, manipulationMarker, dstFileSystemInfo),
        errorHandler is null ? null : (fileSystemInfo, _, copyParams, exception) => errorHandler(fileSystemInfo, copyParams, exception),
        cancellationToken)
        .ToArray();
    }

    #endregion
    #region File System objects asynchronus move methods

    public static IAsyncEnumerable<TTransferResult> MoveFileSystemInfosAsync<TFileSystemItem, TTransferResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, TransferOptions> moveParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, FileSystemInfo?, TTransferResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(moveParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosAsyncCore(fileSystemItems, manipulationOptions, markedInfoSelector, moveParamsSelector,
        (fileSystemInfo, traversalMarker, copyParams) => MoveCore(fileSystemInfo, copyParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo),
        errorHandler, cancellationToken);
    }

    public static IAsyncEnumerable<TransferResult<FileSystemInfo>> MoveFileSystemInfosAsync(
      this IEnumerable<MarkedInfo<FileSystemInfo>> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions> moveParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(moveParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosAsyncCore(fileSystemItems, manipulationOptions,
        _ => _,
        fileSystemItem => moveParamsSelector(fileSystemItem.Info, fileSystemItem.TraversalMarker),
        (fileSystemInfo, traversalMarker, moveParams) => MoveCore(fileSystemInfo, moveParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => fileSystemInfo.ToTransferResult(traversalMarker, manipulationMarker, dstFileSystemInfo),
        errorHandler, cancellationToken);
    }

    public static IAsyncEnumerable<TransferResult<FileSystemInfo>> MoveFileSystemInfosAsync(
      this IEnumerable<FileSystemInfo> fileSystemInfos, FileSystemManipulationOptions manipulationOptions,
      Func<FileSystemInfo, TransferOptions> moveParamsSelector,
      Func<FileSystemInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemInfos);
      Argument.That.NotNull(moveParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosAsyncCore(fileSystemInfos, manipulationOptions,
        fileSystemInfo => fileSystemInfo.ToMarkedInfo(fileSystemInfo.IsDirectory() ? FileSystemTraversalMarker.EnterDirectory : FileSystemTraversalMarker.None),
        fileSystemInfo => moveParamsSelector(fileSystemInfo),
        (fileSystemInfo, _, moveParams) => MoveCore(fileSystemInfo, moveParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => fileSystemInfo.ToTransferResult(FileSystemTraversalMarker.None, manipulationMarker, dstFileSystemInfo),
        errorHandler is null ? null : (fileSystemInfo, _, moveParams, exception) => errorHandler(fileSystemInfo, moveParams, exception),
        cancellationToken);
    }

    #endregion
    #region File System objects deferred move methods

    public static IEnumerable<TTransferResult> MoveFileSystemInfosDefer<TFileSystemItem, TTransferResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, TransferOptions> moveParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, FileSystemInfo?, TTransferResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(moveParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, markedInfoSelector, moveParamsSelector,
        (fileSystemInfo, traversalMarker, moveParams) => MoveCore(fileSystemInfo, moveParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<TTransferResult> MoveFileSystemInfosDefer<TFileSystemItem, TTransferResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, TransferOptions> moveParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, FileSystemInfo, TTransferResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(moveParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, maxConcurrents, markedInfoSelector, moveParamsSelector,
        (fileSystemInfo, traversalMarker, moveParams) => MoveCore(fileSystemInfo, moveParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<TransferResult<FileSystemInfo>> MoveFileSystemInfosDefer(
      this IEnumerable<MarkedInfo<FileSystemInfo>> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions> moveParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(moveParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions,
        _ => _,
        fileSystemItem => moveParamsSelector(fileSystemItem.Info, fileSystemItem.TraversalMarker),
        (fileSystemInfo, traversalMarker, moveParams) => MoveCore(fileSystemInfo, moveParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => fileSystemInfo.ToTransferResult(traversalMarker, manipulationMarker, dstFileSystemInfo),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<TransferResult<FileSystemInfo>> MoveFileSystemInfosDefer(
      this IEnumerable<MarkedInfo<FileSystemInfo>> fileSystemItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions> moveParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(moveParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, maxConcurrents,
        _ => _,
        fileSystemItem => moveParamsSelector(fileSystemItem.Info, fileSystemItem.TraversalMarker),
        (fileSystemInfo, traversalMarker, moveParams) => MoveCore(fileSystemInfo, moveParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => fileSystemInfo.ToTransferResult(traversalMarker, manipulationMarker, dstFileSystemInfo),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<TransferResult<FileSystemInfo>> MoveFileSystemInfosDefer(
      this IEnumerable<FileSystemInfo> fileSystemInfos, FileSystemManipulationOptions manipulationOptions,
      Func<FileSystemInfo, TransferOptions> moveParamsSelector,
      Func<FileSystemInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemInfos);
      Argument.That.NotNull(moveParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemInfos, manipulationOptions,
        fileSystemInfo => fileSystemInfo.ToMarkedInfo(fileSystemInfo.IsDirectory() ? FileSystemTraversalMarker.EnterDirectory : FileSystemTraversalMarker.None),
        fileSystemInfo => moveParamsSelector(fileSystemInfo),
        (fileSystemInfo, _, moveParams) => MoveCore(fileSystemInfo, moveParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => fileSystemInfo.ToTransferResult(FileSystemTraversalMarker.None, manipulationMarker, dstFileSystemInfo),
        errorHandler is null ? null : (fileSystemInfo, _, moveParams, exception) => errorHandler(fileSystemInfo, moveParams, exception),
        cancellationToken);
    }

    public static IEnumerable<TransferResult<FileSystemInfo>> MoveFileSystemInfosDefer(
      this IEnumerable<FileSystemInfo> fileSystemInfos, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<FileSystemInfo, TransferOptions> moveParamsSelector,
      Func<FileSystemInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemInfos);
      Argument.That.NotNull(moveParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemInfos, manipulationOptions, maxConcurrents,
        fileSystemInfo => fileSystemInfo.ToMarkedInfo(fileSystemInfo.IsDirectory() ? FileSystemTraversalMarker.EnterDirectory : FileSystemTraversalMarker.None),
        fileSystemInfo => moveParamsSelector(fileSystemInfo),
        (fileSystemInfo, _, moveParams) => MoveCore(fileSystemInfo, moveParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => fileSystemInfo.ToTransferResult(FileSystemTraversalMarker.None, manipulationMarker, dstFileSystemInfo),
        errorHandler is null ? null : (fileSystemInfo, _, moveParams, exception) => errorHandler(fileSystemInfo, moveParams, exception),
        cancellationToken);
    }

    #endregion
    #region File System objects direct move methods

    public static TTransferResult[] MoveFileSystemInfos<TFileSystemItem, TTransferResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, TransferOptions> moveParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, FileSystemInfo, TTransferResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(moveParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, markedInfoSelector, moveParamsSelector,
        (fileSystemInfo, traversalMarker, moveParams) => MoveCore(fileSystemInfo, moveParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static TTransferResult[] MoveFileSystemInfos<TFileSystemItem, TTransferResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, TransferOptions> moveParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, FileSystemInfo, TTransferResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(moveParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, maxConcurrents, markedInfoSelector, moveParamsSelector,
        (fileSystemInfo, traversalMarker, moveParams) => MoveCore(fileSystemInfo, moveParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static TransferResult<FileSystemInfo>[] MoveFileSystemInfos(
      this IEnumerable<MarkedInfo<FileSystemInfo>> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions> moveParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(moveParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions,
        _ => _,
        fileSystemItem => moveParamsSelector(fileSystemItem.Info, fileSystemItem.TraversalMarker),
        (fileSystemInfo, traversalMarker, moveParams) => MoveCore(fileSystemInfo, moveParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => fileSystemInfo.ToTransferResult(traversalMarker, manipulationMarker, dstFileSystemInfo),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static TransferResult<FileSystemInfo>[] MoveFileSystemInfos(
      this IEnumerable<MarkedInfo<FileSystemInfo>> fileSystemItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions> moveParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(moveParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, maxConcurrents,
        _ => _,
        fileSystemItem => moveParamsSelector(fileSystemItem.Info, fileSystemItem.TraversalMarker),
        (fileSystemInfo, traversalMarker, moveParams) => MoveCore(fileSystemInfo, moveParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => fileSystemInfo.ToTransferResult(traversalMarker, manipulationMarker, dstFileSystemInfo),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static TransferResult<FileSystemInfo>[] MoveFileSystemInfos(
      this IEnumerable<FileSystemInfo> fileSystemInfos, FileSystemManipulationOptions manipulationOptions,
      Func<FileSystemInfo, TransferOptions> moveParamsSelector,
      Func<FileSystemInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemInfos);
      Argument.That.NotNull(moveParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemInfos, manipulationOptions,
        fileSystemInfo => fileSystemInfo.ToMarkedInfo(fileSystemInfo.IsDirectory() ? FileSystemTraversalMarker.EnterDirectory : FileSystemTraversalMarker.None),
        fileSystemInfo => moveParamsSelector(fileSystemInfo),
        (fileSystemInfo, _, moveParams) => MoveCore(fileSystemInfo, moveParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => fileSystemInfo.ToTransferResult(FileSystemTraversalMarker.None, manipulationMarker, dstFileSystemInfo),
        errorHandler is null ? null : (fileSystemInfo, _, copyParams, exception) => errorHandler(fileSystemInfo, copyParams, exception),
        cancellationToken)
        .ToArray();
    }

    public static TransferResult<FileSystemInfo>[] MoveFileSystemInfos(
      this IEnumerable<FileSystemInfo> fileSystemInfos, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<FileSystemInfo, TransferOptions> moveParamsSelector,
      Func<FileSystemInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemInfos);
      Argument.That.NotNull(moveParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemInfos, manipulationOptions, maxConcurrents,
        fileSystemInfo => fileSystemInfo.ToMarkedInfo(fileSystemInfo.IsDirectory() ? FileSystemTraversalMarker.EnterDirectory : FileSystemTraversalMarker.None),
        fileSystemInfo => moveParamsSelector(fileSystemInfo),
        (fileSystemInfo, _, moveParams) => MoveCore(fileSystemInfo, moveParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, dstFileSystemInfo) => fileSystemInfo.ToTransferResult(FileSystemTraversalMarker.None, manipulationMarker, dstFileSystemInfo),
        errorHandler is null ? null : (fileSystemInfo, _, copyParams, exception) => errorHandler(fileSystemInfo, copyParams, exception),
        cancellationToken)
        .ToArray();
    }

    #endregion
    #region File System objects asynchronus replace methods

    public static IAsyncEnumerable<TReplaceResult> ReplaceFileSystemInfosAsync<TFileSystemItem, TReplaceResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, ReplaceOptions> replaceParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, FileSystemInfo, FileSystemInfo?, TReplaceResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(replaceParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosAsyncCore(fileSystemItems, manipulationOptions, markedInfoSelector, replaceParamsSelector,
        (fileSystemInfo, traversalMarker, replaceParams) => ReplaceCore(fileSystemInfo, replaceParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim.dstFileSystemInfo, manipulationInterim.bakFileSystemInfo),
        errorHandler, cancellationToken);
    }

    public static IAsyncEnumerable<ReplaceResult<FileSystemInfo>> ReplaceFileSystemInfosAsync(
      this IEnumerable<MarkedInfo<FileSystemInfo>> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, ReplaceOptions> replaceParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(replaceParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosAsyncCore(fileSystemItems, manipulationOptions,
        _ => _,
        fileSystemItem => replaceParamsSelector(fileSystemItem.Info, fileSystemItem.TraversalMarker),
        (fileSystemInfo, traversalMarker, replaceParams) => ReplaceCore(fileSystemInfo, replaceParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => fileSystemInfo.ToReplaceResult(traversalMarker, manipulationMarker, manipulationInterim.dstFileSystemInfo, manipulationInterim.bakFileSystemInfo),
        errorHandler, cancellationToken);
    }

    public static IAsyncEnumerable<ReplaceResult<FileSystemInfo>> ReplaceFileSystemInfosAsync(
      this IEnumerable<FileSystemInfo> fileSystemInfos, FileSystemManipulationOptions manipulationOptions,
      Func<FileSystemInfo, ReplaceOptions> replaceParamsSelector,
      Func<FileSystemInfo, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemInfos);
      Argument.That.NotNull(replaceParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosAsyncCore(fileSystemInfos, manipulationOptions,
        fileSystemInfo => fileSystemInfo.ToMarkedInfo(fileSystemInfo.IsDirectory() ? FileSystemTraversalMarker.EnterDirectory : FileSystemTraversalMarker.None),
        fileSystemInfo => replaceParamsSelector(fileSystemInfo),
        (fileSystemInfo, _, replaceParams) => ReplaceCore(fileSystemInfo, replaceParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => fileSystemInfo.ToReplaceResult(FileSystemTraversalMarker.None, manipulationMarker, manipulationInterim.dstFileSystemInfo, manipulationInterim.bakFileSystemInfo),
        errorHandler is null ? null : (fileSystemInfo, _, replaceParams, exception) => errorHandler(fileSystemInfo, replaceParams, exception),
        cancellationToken);
    }

    #endregion
    #region File System objects deferred replace methods

    public static IEnumerable<TReplaceResult> ReplaceFileSystemInfosDefer<TFileSystemItem, TReplaceResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, ReplaceOptions> replaceParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, FileSystemInfo, FileSystemInfo?, TReplaceResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(replaceParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, markedInfoSelector, replaceParamsSelector,
        (fileSystemInfo, traversalMarker, replaceParams) => ReplaceCore(fileSystemInfo, replaceParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim.dstFileSystemInfo, manipulationInterim.bakFileSystemInfo),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<TReplaceResult> ReplaceFileSystemInfosDefer<TFileSystemItem, TReplaceResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, ReplaceOptions> replaceParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, FileSystemInfo, FileSystemInfo?, TReplaceResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(replaceParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, maxConcurrents, markedInfoSelector, replaceParamsSelector,
        (fileSystemInfo, traversalMarker, replaceParams) => ReplaceCore(fileSystemInfo, replaceParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim.dstFileSystemInfo, manipulationInterim.bakFileSystemInfo),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<ReplaceResult<FileSystemInfo>> ReplaceFileSystemInfosDefer(
      this IEnumerable<MarkedInfo<FileSystemInfo>> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, ReplaceOptions> replaceParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(replaceParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions,
        _ => _,
        fileSystemItem => replaceParamsSelector(fileSystemItem.Info, fileSystemItem.TraversalMarker),
        (fileSystemInfo, traversalMarker, replaceParams) => ReplaceCore(fileSystemInfo, replaceParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => fileSystemInfo.ToReplaceResult(traversalMarker, manipulationMarker, manipulationInterim.dstFileSystemInfo, manipulationInterim.bakFileSystemInfo),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<ReplaceResult<FileSystemInfo>> ReplaceFileSystemInfosDefer(
      this IEnumerable<MarkedInfo<FileSystemInfo>> fileSystemItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<FileSystemInfo, FileSystemTraversalMarker, ReplaceOptions> replaceParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(replaceParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, maxConcurrents,
        _ => _,
        fileSystemItem => replaceParamsSelector(fileSystemItem.Info, fileSystemItem.TraversalMarker),
        (fileSystemInfo, traversalMarker, replaceParams) => ReplaceCore(fileSystemInfo, replaceParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => fileSystemInfo.ToReplaceResult(traversalMarker, manipulationMarker, manipulationInterim.dstFileSystemInfo, manipulationInterim.bakFileSystemInfo),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<ReplaceResult<FileSystemInfo>> ReplaceFileSystemInfosDefer(
      this IEnumerable<FileSystemInfo> fileSystemInfos, FileSystemManipulationOptions manipulationOptions,
      Func<FileSystemInfo, ReplaceOptions> replaceParamsSelector,
      Func<FileSystemInfo, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemInfos);
      Argument.That.NotNull(replaceParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemInfos, manipulationOptions,
        fileSystemInfo => fileSystemInfo.ToMarkedInfo(fileSystemInfo.IsDirectory() ? FileSystemTraversalMarker.EnterDirectory : FileSystemTraversalMarker.None),
        fileSystemInfo => replaceParamsSelector(fileSystemInfo),
        (fileSystemInfo, _, replaceParams) => ReplaceCore(fileSystemInfo, replaceParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => fileSystemInfo.ToReplaceResult(FileSystemTraversalMarker.None, manipulationMarker, manipulationInterim.dstFileSystemInfo, manipulationInterim.bakFileSystemInfo),
        errorHandler is null ? null : (fileSystemInfo, _, replaceParams, exception) => errorHandler(fileSystemInfo, replaceParams, exception),
        cancellationToken);
    }

    public static IEnumerable<ReplaceResult<FileSystemInfo>> ReplaceFileSystemInfosDefer(
      this IEnumerable<FileSystemInfo> fileSystemInfos, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<FileSystemInfo, ReplaceOptions> replaceParamsSelector,
      Func<FileSystemInfo, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemInfos);
      Argument.That.NotNull(replaceParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemInfos, manipulationOptions, maxConcurrents,
        fileSystemInfo => fileSystemInfo.ToMarkedInfo(fileSystemInfo.IsDirectory() ? FileSystemTraversalMarker.EnterDirectory : FileSystemTraversalMarker.None),
        fileSystemInfo => replaceParamsSelector(fileSystemInfo),
        (fileSystemInfo, _, replaceParams) => ReplaceCore(fileSystemInfo, replaceParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => fileSystemInfo.ToReplaceResult(FileSystemTraversalMarker.None, manipulationMarker, manipulationInterim.dstFileSystemInfo, manipulationInterim.bakFileSystemInfo),
        errorHandler is null ? null : (fileSystemInfo, _, replaceParams, exception) => errorHandler(fileSystemInfo, replaceParams, exception),
        cancellationToken);
    }

    #endregion
    #region File System objects direct replace methods

    public static TReplaceResult[] ReplaceFileSystemInfos<TFileSystemItem, TReplaceResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, ReplaceOptions> replaceParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, FileSystemInfo, FileSystemInfo?, TReplaceResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(replaceParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, markedInfoSelector, replaceParamsSelector,
        (fileSystemInfo, traversalMarker, replaceParams) => ReplaceCore(fileSystemInfo, replaceParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim.dstFileSystemInfo, manipulationInterim.bakFileSystemInfo),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static TReplaceResult[] ReplaceFileSystemInfos<TFileSystemItem, TReplaceResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, ReplaceOptions> replaceParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, FileSystemInfo, FileSystemInfo?, TReplaceResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(replaceParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, maxConcurrents, markedInfoSelector, replaceParamsSelector,
        (fileSystemInfo, traversalMarker, replaceParams) => ReplaceCore(fileSystemInfo, replaceParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim.dstFileSystemInfo, manipulationInterim.bakFileSystemInfo),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static ReplaceResult<FileSystemInfo>[] ReplaceFileSystemInfos(
      this IEnumerable<MarkedInfo<FileSystemInfo>> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, ReplaceOptions> replaceParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(replaceParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions,
        _ => _,
        fileSystemItem => replaceParamsSelector(fileSystemItem.Info, fileSystemItem.TraversalMarker),
        (fileSystemInfo, traversalMarker, replaceParams) => ReplaceCore(fileSystemInfo, replaceParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => fileSystemInfo.ToReplaceResult(traversalMarker, manipulationMarker, manipulationInterim.dstFileSystemInfo, manipulationInterim.bakFileSystemInfo),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static ReplaceResult<FileSystemInfo>[] ReplaceFileSystemInfos(
      this IEnumerable<MarkedInfo<FileSystemInfo>> fileSystemItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<FileSystemInfo, FileSystemTraversalMarker, ReplaceOptions> replaceParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(replaceParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, maxConcurrents,
        _ => _,
        fileSystemItem => replaceParamsSelector(fileSystemItem.Info, fileSystemItem.TraversalMarker),
        (fileSystemInfo, traversalMarker, replaceParams) => ReplaceCore(fileSystemInfo, replaceParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => fileSystemInfo.ToReplaceResult(traversalMarker, manipulationMarker, manipulationInterim.dstFileSystemInfo, manipulationInterim.bakFileSystemInfo),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static ReplaceResult<FileSystemInfo>[] ReplaceFileSystemInfos(
      this IEnumerable<FileSystemInfo> fileSystemInfos, FileSystemManipulationOptions manipulationOptions,
      Func<FileSystemInfo, ReplaceOptions> replaceParamsSelector,
      Func<FileSystemInfo, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemInfos);
      Argument.That.NotNull(replaceParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemInfos, manipulationOptions,
        fileSystemInfo => fileSystemInfo.ToMarkedInfo(fileSystemInfo.IsDirectory() ? FileSystemTraversalMarker.EnterDirectory : FileSystemTraversalMarker.None),
        fileSystemInfo => replaceParamsSelector(fileSystemInfo),
        (fileSystemInfo, _, replaceParams) => ReplaceCore(fileSystemInfo, replaceParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => fileSystemInfo.ToReplaceResult(FileSystemTraversalMarker.None, manipulationMarker, manipulationInterim.dstFileSystemInfo, manipulationInterim.bakFileSystemInfo),
        errorHandler is null ? null : (fileSystemInfo, _, replaceParams, exception) => errorHandler(fileSystemInfo, replaceParams, exception),
        cancellationToken)
        .ToArray();
    }

    public static ReplaceResult<FileSystemInfo>[] ReplaceFileSystemInfos(
      this IEnumerable<FileSystemInfo> fileSystemInfos, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<FileSystemInfo, ReplaceOptions> replaceParamsSelector,
      Func<FileSystemInfo, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemInfos);
      Argument.That.NotNull(replaceParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemInfos, manipulationOptions, maxConcurrents,
        fileSystemInfo => fileSystemInfo.ToMarkedInfo(fileSystemInfo.IsDirectory() ? FileSystemTraversalMarker.EnterDirectory : FileSystemTraversalMarker.None),
        fileSystemInfo => replaceParamsSelector(fileSystemInfo),
        (fileSystemInfo, _, replaceParams) => ReplaceCore(fileSystemInfo, replaceParams, clearReadOnly),
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => fileSystemInfo.ToReplaceResult(FileSystemTraversalMarker.None, manipulationMarker, manipulationInterim.dstFileSystemInfo, manipulationInterim.bakFileSystemInfo),
        errorHandler is null ? null : (fileSystemInfo, _, replaceParams, exception) => errorHandler(fileSystemInfo, replaceParams, exception),
        cancellationToken)
        .ToArray();
    }

    #endregion
    #region File System objects asynchronus delete methods

    public static IAsyncEnumerable<TDeleteResult> DeleteFileSystemInfosAsync<TFileSystemItem, TDeleteResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, DeleteOptions> deleteParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TDeleteResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(deleteParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosAsyncCore(fileSystemItems, manipulationOptions, markedInfoSelector, deleteParamsSelector,
        (fileSystemInfo, traversalMarker, deleteParams) => { DeleteCore(fileSystemInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileSystemInfo, traversalMarker, manipulationMarker, _) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker),
        errorHandler, cancellationToken);
    }

    public static IAsyncEnumerable<InfoResult<FileSystemInfo>> DeleteFileSystemInfosAsync(
      this IEnumerable<MarkedInfo<FileSystemInfo>> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, DeleteOptions> deleteParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(deleteParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosAsyncCore(fileSystemItems, manipulationOptions,
        _ => _,
        fileSystemItem => deleteParamsSelector(fileSystemItem.Info, fileSystemItem.TraversalMarker),
        (fileSystemInfo, traversalMarker, deleteParams) => { DeleteCore(fileSystemInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => fileSystemInfo.ToInfoResult(traversalMarker, manipulationMarker),
        errorHandler, cancellationToken);
    }

    public static IAsyncEnumerable<InfoResult<FileSystemInfo>> DeleteFileSystemInfosAsync(
      this IEnumerable<FileSystemInfo> fileSystemInfos, FileSystemManipulationOptions manipulationOptions,
      Func<FileSystemInfo, DeleteOptions> deleteParamsSelector,
      Func<FileSystemInfo, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemInfos);
      Argument.That.NotNull(deleteParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosAsyncCore(fileSystemInfos, manipulationOptions,
        fileSystemInfo => fileSystemInfo.ToMarkedInfo(fileSystemInfo.IsDirectory() ? FileSystemTraversalMarker.LeaveDirectory : FileSystemTraversalMarker.None),
        fileSystemInfo => deleteParamsSelector(fileSystemInfo),
        (fileSystemInfo, _, deleteParams) => { DeleteCore(fileSystemInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => fileSystemInfo.ToInfoResult(FileSystemTraversalMarker.None, manipulationMarker),
        errorHandler is null ? null : (fileSystemInfo, _, deleteParams, exception) => errorHandler(fileSystemInfo, deleteParams, exception),
        cancellationToken);
    }

    #endregion
    #region File System objects deferred delete methods

    public static IEnumerable<TDeleteResult> DeleteFileSystemInfosDefer<TFileSystemItem, TDeleteResult>(
      this IEnumerable<TFileSystemItem> fileItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, DeleteOptions> deleteParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TDeleteResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(deleteParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileItems, manipulationOptions, markedInfoSelector, deleteParamsSelector,
        (fileSystemInfo, traversalMarker, deleteParams) => { DeleteCore(fileSystemInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileSystemInfo, traversalMarker, manipulationMarker, _) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<TDeleteResult> DeleteFileSystemInfosDefer<TFileSystemItem, TDeleteResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, DeleteOptions> deleteParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TDeleteResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(deleteParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, maxConcurrents, markedInfoSelector, deleteParamsSelector,
        (fileSystemInfo, traversalMarker, deleteParams) => { DeleteCore(fileSystemInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileSystemInfo, traversalMarker, manipulationMarker, _) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<InfoResult<FileSystemInfo>> DeleteFileSystemInfosDefer(
      this IEnumerable<MarkedInfo<FileSystemInfo>> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, DeleteOptions> deleteParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(deleteParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions,
        _ => _,
        fileSystemItem => deleteParamsSelector(fileSystemItem.Info, fileSystemItem.TraversalMarker),
        (fileSystemInfo, traversalMarker, deleteParams) => { DeleteCore(fileSystemInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => fileSystemInfo.ToInfoResult(traversalMarker, manipulationMarker),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<InfoResult<FileSystemInfo>> DeleteFileSystemInfosDefer(
      this IEnumerable<MarkedInfo<FileSystemInfo>> fileSystemItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<FileSystemInfo, FileSystemTraversalMarker, DeleteOptions> deleteParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(deleteParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, maxConcurrents,
        _ => _,
        fileSystemItem => deleteParamsSelector(fileSystemItem.Info, fileSystemItem.TraversalMarker),
        (fileSystemInfo, traversalMarker, deleteParams) => { DeleteCore(fileSystemInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => fileSystemInfo.ToInfoResult(traversalMarker, manipulationMarker),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<InfoResult<FileSystemInfo>> DeleteFileSystemInfosDefer(
      this IEnumerable<FileSystemInfo> fileSystemInfos, FileSystemManipulationOptions manipulationOptions,
      Func<FileSystemInfo, DeleteOptions> deleteParamsSelector,
      Func<FileSystemInfo, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemInfos);
      Argument.That.NotNull(deleteParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemInfos, manipulationOptions,
        fileSystemInfo => fileSystemInfo.ToMarkedInfo(fileSystemInfo.IsDirectory() ? FileSystemTraversalMarker.LeaveDirectory : FileSystemTraversalMarker.None),
        fileSystemInfo => deleteParamsSelector(fileSystemInfo),
        (fileSystemInfo, _, deleteParams) => { DeleteCore(fileSystemInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => fileSystemInfo.ToInfoResult(FileSystemTraversalMarker.None, manipulationMarker),
        errorHandler is null ? null : (fileSystemInfo, _, deleteParams, exception) => errorHandler(fileSystemInfo, deleteParams, exception),
        cancellationToken);
    }

    public static IEnumerable<InfoResult<FileSystemInfo>> DeleteFileSystemInfosDefer(
      this IEnumerable<FileSystemInfo> fileSystemInfos, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<FileSystemInfo, DeleteOptions> deleteParamsSelector,
      Func<FileSystemInfo, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemInfos);
      Argument.That.NotNull(deleteParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemInfos, manipulationOptions, maxConcurrents,
        fileSystemInfo => fileSystemInfo.ToMarkedInfo(fileSystemInfo.IsDirectory() ? FileSystemTraversalMarker.LeaveDirectory : FileSystemTraversalMarker.None),
        fileSystemInfo => deleteParamsSelector(fileSystemInfo),
        (fileSystemInfo, _, deleteParams) => { DeleteCore(fileSystemInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => fileSystemInfo.ToInfoResult(FileSystemTraversalMarker.None, manipulationMarker),
        errorHandler is null ? null : (fileSystemInfo, _, deleteParams, exception) => errorHandler(fileSystemInfo, deleteParams, exception),
        cancellationToken);
    }

    #endregion
    #region File System objects direct delete methods

    public static TDeleteResult[] DeleteFileSystemInfos<TDirectoryItem, TDeleteResult>(
      this IEnumerable<TDirectoryItem> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<TDirectoryItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TDirectoryItem, DeleteOptions> deleteParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TDeleteResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(deleteParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(directoryItems, manipulationOptions, markedInfoSelector, deleteParamsSelector,
        (fileSystemInfo, traversalMarker, deleteParams) => { DeleteCore(fileSystemInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileSystemInfo, traversalMarker, manipulationMarker, _) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static TDeleteResult[] DeleteFileSystemInfos<TFileSystemItem, TDeleteResult>(
      this IEnumerable<TFileSystemItem> fileSystemItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileSystemItem, MarkedInfo<FileSystemInfo>> markedInfoSelector,
      Func<TFileSystemItem, DeleteOptions> deleteParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TDeleteResult> resultSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(deleteParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, maxConcurrents, markedInfoSelector, deleteParamsSelector,
        (fileSystemInfo, traversalMarker, deleteParams) => { DeleteCore(fileSystemInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileSystemInfo, traversalMarker, manipulationMarker, _) => resultSelector(fileSystemInfo, traversalMarker, manipulationMarker),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static InfoResult<FileSystemInfo>[] DeleteFileSystemInfos(
      this IEnumerable<MarkedInfo<FileSystemInfo>> fileSystemItems, FileSystemManipulationOptions manipulationOptions,
      Func<FileSystemInfo, FileSystemTraversalMarker, DeleteOptions> deleteParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(deleteParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions,
        _ => _,
        fileSystemItem => deleteParamsSelector(fileSystemItem.Info, fileSystemItem.TraversalMarker),
        (fileSystemInfo, traversalMarker, deleteParams) => { DeleteCore(fileSystemInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => fileSystemInfo.ToInfoResult(traversalMarker, manipulationMarker),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static InfoResult<FileSystemInfo>[] DeleteFileSystemInfos(
      this IEnumerable<MarkedInfo<FileSystemInfo>> fileSystemItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<FileSystemInfo, FileSystemTraversalMarker, DeleteOptions> deleteParamsSelector,
      Func<FileSystemInfo, FileSystemTraversalMarker, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemItems);
      Argument.That.NotNull(deleteParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemItems, manipulationOptions, maxConcurrents,
        _ => _,
        fileSystemItem => deleteParamsSelector(fileSystemItem.Info, fileSystemItem.TraversalMarker),
        (fileSystemInfo, traversalMarker, deleteParams) => { DeleteCore(fileSystemInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => fileSystemInfo.ToInfoResult(traversalMarker, manipulationMarker),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static InfoResult<FileSystemInfo>[] DeleteFileSystemInfos(
      this IEnumerable<FileSystemInfo> fileSystemInfos, FileSystemManipulationOptions manipulationOptions,
      Func<FileSystemInfo, DeleteOptions> deleteParamsSelector,
      Func<FileSystemInfo, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemInfos);
      Argument.That.NotNull(deleteParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemInfos, manipulationOptions,
        fileSystemInfo => fileSystemInfo.ToMarkedInfo(fileSystemInfo.IsDirectory() ? FileSystemTraversalMarker.LeaveDirectory : FileSystemTraversalMarker.None),
        fileSystemInfo => deleteParamsSelector(fileSystemInfo),
        (fileSystemInfo, _, deleteParams) => { DeleteCore(fileSystemInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => fileSystemInfo.ToInfoResult(FileSystemTraversalMarker.None, manipulationMarker),
        errorHandler is null ? null : (fileSystemInfo, _, deleteParams, exception) => errorHandler(fileSystemInfo, deleteParams, exception),
        cancellationToken)
        .ToArray();
    }

    public static InfoResult<FileSystemInfo>[] DeleteFileSystemInfos(
      this IEnumerable<FileSystemInfo> fileSystemInfos, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<FileSystemInfo, DeleteOptions> deleteParamsSelector,
      Func<FileSystemInfo, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileSystemInfos);
      Argument.That.NotNull(deleteParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFileSystemInfosCore(fileSystemInfos, manipulationOptions, maxConcurrents,
        fileSystemInfo => fileSystemInfo.ToMarkedInfo(fileSystemInfo.IsDirectory() ? FileSystemTraversalMarker.LeaveDirectory : FileSystemTraversalMarker.None),
        fileSystemInfo => deleteParamsSelector(fileSystemInfo),
        (fileSystemInfo, _, deleteParams) => { DeleteCore(fileSystemInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileSystemInfo, traversalMarker, manipulationMarker, manipulationInterim) => fileSystemInfo.ToInfoResult(FileSystemTraversalMarker.None, manipulationMarker),
        errorHandler is null ? null : (fileSystemInfo, _, deleteParams, exception) => errorHandler(fileSystemInfo, deleteParams, exception),
        cancellationToken)
        .ToArray();
    }

    #endregion
    #region Directories manipulation deferred methods

    public static IEnumerable<TManipulationResult> ManipulateDirectoriesDefer<TDirectoryItem, TManipulationParams, TManipulationInterim, TManipulationResult>(
      this IEnumerable<TDirectoryItem> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<TDirectoryItem, MarkedInfo<DirectoryInfo>> markedInfoSelector,
      Func<TDirectoryItem, TManipulationParams> manipulationParamsSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, TManipulationParams, TManipulationInterim?> directoryHandler,
      Func<DirectoryInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TManipulationInterim?, TManipulationResult> resultSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, TManipulationParams?, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(manipulationParamsSelector);
      Argument.That.NotNull(resultSelector);

      return ManipulateDirectoriesCore(directoryItems, manipulationOptions, markedInfoSelector, manipulationParamsSelector, directoryHandler, resultSelector, errorHandler, cancellationToken);
    }

    public static IEnumerable<TManipulationResult> ManipulateDirectoriesDefer<TDirectoryItem, TManipulationParams, TManipulationResult>(
      this IEnumerable<TDirectoryItem> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<TDirectoryItem, MarkedInfo<DirectoryInfo>> markedInfoSelector,
      Func<TDirectoryItem, TManipulationParams> manipulationParamsSelector,
      Action<DirectoryInfo, FileSystemTraversalMarker, TManipulationParams> directoryHandler,
      Func<DirectoryInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TManipulationResult> resultSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, TManipulationParams?, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(manipulationParamsSelector);
      Argument.That.NotNull(resultSelector);

      return ManipulateDirectoriesCore(directoryItems, manipulationOptions, markedInfoSelector, manipulationParamsSelector,
        (directoryInfo, traversalMarker, manipulationParams) => { directoryHandler(directoryInfo, traversalMarker, manipulationParams); return Void.Value; },
        (directoryInfo, traversalMarker, manipulationMarker, _) => resultSelector(directoryInfo, traversalMarker, manipulationMarker),
        errorHandler, cancellationToken);
    }

    #endregion
    #region Directories manipulation direct methods

    public static TManipulationResult[] ManipulateDirectories<TDirectoryItem, TManipulationOptions, TManipulationInterim, TManipulationResult>(
      this IEnumerable<TDirectoryItem> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<TDirectoryItem, MarkedInfo<DirectoryInfo>> markedInfoSelector,
      Func<TDirectoryItem, TManipulationOptions> manipulationParamsSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, TManipulationOptions, TManipulationInterim?> directoryHandler,
      Func<DirectoryInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TManipulationInterim?, TManipulationResult> resultSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, TManipulationOptions?, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(manipulationParamsSelector);
      Argument.That.NotNull(resultSelector);

      return ManipulateDirectoriesCore(directoryItems, manipulationOptions, markedInfoSelector, manipulationParamsSelector, directoryHandler, resultSelector, errorHandler, cancellationToken)
        .ToArray();
    }

    public static TManipulationResult[] ManipulateDirectories<TDirectoryItem, TManipulationOptions, TManipulationResult>(
      this IEnumerable<TDirectoryItem> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<TDirectoryItem, MarkedInfo<DirectoryInfo>> markedInfoSelector,
      Func<TDirectoryItem, TManipulationOptions> manipulationParamsSelector,
      Action<DirectoryInfo, FileSystemTraversalMarker, TManipulationOptions> directoryHandler,
      Func<DirectoryInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TManipulationResult> resultSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, TManipulationOptions?, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(manipulationParamsSelector);
      Argument.That.NotNull(resultSelector);

      return ManipulateDirectoriesCore(directoryItems, manipulationOptions, markedInfoSelector, manipulationParamsSelector,
        (directoryInfo, traversalMarker, manipulationParams) => { directoryHandler(directoryInfo, traversalMarker, manipulationParams); return Void.Value; },
        (directoryInfo, traversalMarker, manipulationMarker, _) => resultSelector(directoryInfo, traversalMarker, manipulationMarker),
        errorHandler, cancellationToken)
        .ToArray();
    }

    #endregion
    #region Directories replication deferred methods

    public static IEnumerable<TReplicationResult> ReplicateDirectoriesDefer<TDirectoryItem, TReplicationResult>(
      this IEnumerable<TDirectoryItem> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<TDirectoryItem, MarkedInfo<DirectoryInfo>> markedInfoSelector,
      Func<TDirectoryItem, ReplicateOptions> replicationParametersSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, DirectoryInfo?, TReplicationResult> resultSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, ReplicateOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(replicationParametersSelector);
      Argument.That.NotNull(resultSelector);

      return ManipulateDirectoriesCore(directoryItems, manipulationOptions.CombineFlags(FileSystemManipulationOptions.EnsureDirectory),
        markedInfoSelector, replicationParametersSelector,
        (directoryInfo, traversalMarker, replicationParameters) => new DirectoryInfo(replicationParameters.DestinationPath),
        resultSelector, errorHandler, cancellationToken);
    }

    public static IEnumerable<TransferResult<DirectoryInfo>> ReplicateDirectoriesDefer(
      this IEnumerable<MarkedInfo<DirectoryInfo>> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<DirectoryInfo, FileSystemTraversalMarker, ReplicateOptions> replicationParametersSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, ReplicateOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);
      Argument.That.NotNull(replicationParametersSelector);

      return ManipulateDirectoriesCore(directoryItems, manipulationOptions.CombineFlags(FileSystemManipulationOptions.EnsureDirectory),
        _ => _,
        directoryItem => replicationParametersSelector(directoryItem.Info, directoryItem.TraversalMarker),
        (directoryInfo, traversalMarker, replicationParameters) => new DirectoryInfo(replicationParameters.DestinationPath),
        (directoryInfo, traversalMarker, manipulationMarker, dstDirectoryInfo) => directoryInfo.ToTransferResult(traversalMarker, manipulationMarker, dstDirectoryInfo),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<TransferResult<DirectoryInfo>> ReplicateDirectoriesDefer(
      this IEnumerable<DirectoryInfo> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<DirectoryInfo, ReplicateOptions> replicationParametersSelector,
      Func<DirectoryInfo, ReplicateOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);
      Argument.That.NotNull(replicationParametersSelector);

      return ManipulateDirectoriesCore(directoryItems, manipulationOptions.CombineFlags(FileSystemManipulationOptions.EnsureDirectory),
        directoryInfo => directoryInfo.ToMarkedInfo(FileSystemTraversalMarker.EnterDirectory),
        directoryInfo => replicationParametersSelector(directoryInfo),
        (directoryInfo, traversalMarker, replicationParameters) => new DirectoryInfo(replicationParameters.DestinationPath),
        (directoryInfo, traversalMarker, manipulationMarker, dstDirectoryInfo) => directoryInfo.ToTransferResult(FileSystemTraversalMarker.None, manipulationMarker, dstDirectoryInfo!),
        errorHandler is null ? null : (directoryInfo, traversalMarker, replicationParameters, exception) => errorHandler(directoryInfo, replicationParameters, exception),
        cancellationToken);
    }

    #endregion
    #region Directories replication direct methods

    public static TReplicationResult[] ReplicateDirectories<TDirectoryItem, TReplicationResult>(
      this IEnumerable<TDirectoryItem> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<TDirectoryItem, MarkedInfo<DirectoryInfo>> markedInfoSelector,
      Func<TDirectoryItem, ReplicateOptions> replicationParametersSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, DirectoryInfo?, TReplicationResult> resultSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, ReplicateOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(replicationParametersSelector);
      Argument.That.NotNull(resultSelector);

      return ManipulateDirectoriesCore(directoryItems, manipulationOptions.CombineFlags(FileSystemManipulationOptions.EnsureDirectory),
        markedInfoSelector, replicationParametersSelector,
        (directoryInfo, traversalMarker, replicationParameters) => new DirectoryInfo(replicationParameters.DestinationPath),
        resultSelector, errorHandler, cancellationToken)
        .ToArray();
    }

    public static TransferResult<DirectoryInfo>[] ReplicateDirectories(
      this IEnumerable<MarkedInfo<DirectoryInfo>> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<DirectoryInfo, FileSystemTraversalMarker, ReplicateOptions> replicationParametersSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, ReplicateOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);
      Argument.That.NotNull(replicationParametersSelector);

      return ManipulateDirectoriesCore(directoryItems, manipulationOptions.CombineFlags(FileSystemManipulationOptions.EnsureDirectory),
        _ => _,
        directoryItem => replicationParametersSelector(directoryItem.Info, directoryItem.TraversalMarker),
        (directoryInfo, traversalMarker, replicationParameters) => new DirectoryInfo(replicationParameters.DestinationPath),
        (directoryInfo, traversalMarker, manipulationMarker, dstDirectoryInfo) => directoryInfo.ToTransferResult(traversalMarker, manipulationMarker, dstDirectoryInfo),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static TransferResult<DirectoryInfo>[] ReplicateDirectories(
      this IEnumerable<DirectoryInfo> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<DirectoryInfo, ReplicateOptions> replicationParametersSelector,
      Func<DirectoryInfo, ReplicateOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);
      Argument.That.NotNull(replicationParametersSelector);

      return ManipulateDirectoriesCore(directoryItems, manipulationOptions.CombineFlags(FileSystemManipulationOptions.EnsureDirectory),
        directoryInfo => directoryInfo.ToMarkedInfo(FileSystemTraversalMarker.EnterDirectory),
        directoryInfo => replicationParametersSelector(directoryInfo),
        (directoryInfo, traversalMarker, replicationParameters) => new DirectoryInfo(replicationParameters.DestinationPath),
        (directoryInfo, traversalMarker, manipulationMarker, dstDirectoryInfo) => directoryInfo.ToTransferResult(FileSystemTraversalMarker.None, manipulationMarker, dstDirectoryInfo!),
        errorHandler is null ? null : (directoryInfo, traversalMarker, replicationParameters, exception) => errorHandler(directoryInfo, replicationParameters, exception),
        cancellationToken)
        .ToArray();
    }

    #endregion
    #region Directories deferred cleanup methods

    public static IEnumerable<TCleanupResult> CleanupDirectoriesDefer<TDirectoryItem, TCleanupResult>(
      this IEnumerable<TDirectoryItem> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<TDirectoryItem, MarkedInfo<DirectoryInfo>> markedInfoSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TCleanupResult> resultSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(resultSelector);

      return ManipulateDirectoriesCore(directoryItems, manipulationOptions.RemoveFlags(FileSystemManipulationOptions.EnsureDirectory).CombineFlags(FileSystemManipulationOptions.CleanupDirectory),
        markedInfoSelector,
        _ => Void.Value,
        (directoryInfo, traversalMarker, _) => Void.Value,
        (directoryInfo, traversalMarker, manipulationMarker, _) => resultSelector(directoryInfo, traversalMarker, manipulationMarker),
        errorHandler is null ? null : (directoryInfo, traversalMarker, _, exception) => errorHandler(directoryInfo, traversalMarker, exception),
        cancellationToken);
    }

    public static IEnumerable<InfoResult<DirectoryInfo>> CleanupDirectoriesDefer(
      this IEnumerable<MarkedInfo<DirectoryInfo>> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<DirectoryInfo, FileSystemTraversalMarker, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);

      return ManipulateDirectoriesCore(directoryItems, manipulationOptions.RemoveFlags(FileSystemManipulationOptions.EnsureDirectory).CombineFlags(FileSystemManipulationOptions.CleanupDirectory),
        _ => _,
        _ => Void.Value,
        (directoryInfo, traversakMarker, _) => Void.Value,
        (directoryInfo, traversalMarker, manipulationMarker, _) => directoryInfo.ToInfoResult(traversalMarker, manipulationMarker),
        errorHandler is null ? null : (directoryInfo, traversalMarker, _, exception) => errorHandler(directoryInfo, traversalMarker, exception),
        cancellationToken);
    }

    public static IEnumerable<InfoResult<DirectoryInfo>> CleanupDirectoriesDefer(
      this IEnumerable<DirectoryInfo> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<DirectoryInfo, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);

      return ManipulateDirectoriesCore(directoryItems, manipulationOptions.RemoveFlags(FileSystemManipulationOptions.EnsureDirectory).CombineFlags(FileSystemManipulationOptions.CleanupDirectory),
        directoryInfo => directoryInfo.ToMarkedInfo(FileSystemTraversalMarker.LeaveDirectory),
        _ => Void.Value,
        (directoryInfo, traversalMarker, _) => Void.Value,
        (directoryInfo, traversalMarker, manipulationMarker, _) => directoryInfo.ToInfoResult(FileSystemTraversalMarker.None, manipulationMarker),
        errorHandler is null ? null : (directoryInfo, traversalMarker, _, exception) => errorHandler(directoryInfo, exception),
        cancellationToken);
    }

    #endregion
    #region Directories direct cleanup methods

    public static TCleanupResult[] CleanupDirectories<TDirectoryItem, TCleanupResult>(
      this IEnumerable<TDirectoryItem> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<TDirectoryItem, MarkedInfo<DirectoryInfo>> markedInfoSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TCleanupResult> resultSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(resultSelector);

      return ManipulateDirectoriesCore(directoryItems, manipulationOptions.RemoveFlags(FileSystemManipulationOptions.EnsureDirectory).CombineFlags(FileSystemManipulationOptions.CleanupDirectory),
        markedInfoSelector, _ => Void.Value,
        (directoryInfo, traversalMarker, _) => Void.Value,
        (directoryInfo, traversalMarker, manipulationMarker, _) => resultSelector(directoryInfo, traversalMarker, manipulationMarker),
        errorHandler is null ? null : (directoryInfo, traversalMarker, _, exception) => errorHandler(directoryInfo, traversalMarker, exception),
        cancellationToken)
        .ToArray();
    }

    public static InfoResult<DirectoryInfo>[] CleanupDirectories(
      this IEnumerable<MarkedInfo<DirectoryInfo>> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<DirectoryInfo, FileSystemTraversalMarker, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);

      return ManipulateDirectoriesCore(directoryItems, manipulationOptions.RemoveFlags(FileSystemManipulationOptions.EnsureDirectory).CombineFlags(FileSystemManipulationOptions.CleanupDirectory),
        _ => _,
        _ => Void.Value,
        (directoryInfo, traversakMarker, _) => Void.Value,
        (directoryInfo, traversalMarker, manipulationMarker, _) => directoryInfo.ToInfoResult(traversalMarker, manipulationMarker),
        errorHandler is null ? null : (directoryInfo, traversalMarker, _, exception) => errorHandler(directoryInfo, traversalMarker, exception),
        cancellationToken)
        .ToArray();
    }

    public static InfoResult<DirectoryInfo>[] CleanupDirectories(
      this IEnumerable<DirectoryInfo> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<DirectoryInfo, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);

      return ManipulateDirectoriesCore(directoryItems, manipulationOptions.RemoveFlags(FileSystemManipulationOptions.EnsureDirectory).CombineFlags(FileSystemManipulationOptions.CleanupDirectory),
        directoryInfo => directoryInfo.ToMarkedInfo(FileSystemTraversalMarker.LeaveDirectory),
        _ => Void.Value,
        (directoryInfo, traversalMarker, _) => Void.Value,
        (directoryInfo, traversalMarker, manipulationMarker, _) => directoryInfo.ToInfoResult(FileSystemTraversalMarker.None, manipulationMarker),
        errorHandler is null ? null : (directoryInfo, traversalMarker, _, exception) => errorHandler(directoryInfo, exception),
        cancellationToken)
        .ToArray();
    }

    #endregion
    #region Directories deferred move methods

    public static IEnumerable<TTransferResult> MoveDirectoriesDefer<TDirectoryItem, TTransferResult>(
      this IEnumerable<TDirectoryItem> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<TDirectoryItem, MarkedInfo<DirectoryInfo>> markedInfoSelector,
      Func<TDirectoryItem, TransferOptions> moveParamsSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, DirectoryInfo, TTransferResult> resultSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(moveParamsSelector);
      Argument.That.NotNull(resultSelector);

      return ManipulateDirectoriesCore(directoryItems, manipulationOptions, markedInfoSelector, moveParamsSelector,
        (directoryInfo, traversalMarker, moveParams) => MoveCore(directoryInfo, moveParams),
        (directoryInfo, traversalMarker, manipulationMarker, dstDirectoryInfo) => resultSelector(directoryInfo, traversalMarker, manipulationMarker, dstDirectoryInfo),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<TransferResult<DirectoryInfo>> MoveDirectoriesDefer(
      this IEnumerable<MarkedInfo<DirectoryInfo>> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<DirectoryInfo, FileSystemTraversalMarker, TransferOptions> moveParamsSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);
      Argument.That.NotNull(moveParamsSelector);

      return ManipulateDirectoriesCore(directoryItems, manipulationOptions,
        _ => _,
        directoryItem => moveParamsSelector(directoryItem.Info, directoryItem.TraversalMarker),
        (directoryInfo, _, moveParams) => MoveCore(directoryInfo, moveParams),
        (directoryInfo, traversalMarker, manipulationMarker, dstDirectoryInfo) => directoryInfo.ToTransferResult(traversalMarker, manipulationMarker, dstDirectoryInfo),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<TransferResult<DirectoryInfo>> MoveDirectoriesDefer(
      this IEnumerable<DirectoryInfo> directoryInfos, FileSystemManipulationOptions manipulationOptions,
      Func<DirectoryInfo, TransferOptions> moveParamsSelector,
      Func<DirectoryInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryInfos);
      Argument.That.NotNull(moveParamsSelector);

      return ManipulateDirectoriesCore(directoryInfos, manipulationOptions,
        directoryInfo => directoryInfo.ToMarkedInfo(FileSystemTraversalMarker.EnterDirectory),
        directoryInfo => moveParamsSelector(directoryInfo),
        (directoryInfo, _, moveParams) => MoveCore(directoryInfo, moveParams),
        (directoryInfo, _, manipulationMarker, dstDirectoryInfo) => directoryInfo.ToTransferResult(FileSystemTraversalMarker.None, manipulationMarker, dstDirectoryInfo),
        errorHandler is null ? null : (directoryInfo, _, moveParams, exception) => errorHandler(directoryInfo, moveParams, exception),
        cancellationToken);
    }

    #endregion
    #region Directories direct move methods

    public static TTransferResult[] MoveDirectories<TDirectoryItem, TTransferResult>(
      this IEnumerable<TDirectoryItem> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<TDirectoryItem, MarkedInfo<DirectoryInfo>> markedInfoSelector,
      Func<TDirectoryItem, TransferOptions> moveParamsSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, DirectoryInfo?, TTransferResult> resultSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(moveParamsSelector);
      Argument.That.NotNull(resultSelector);

      return ManipulateDirectoriesCore(directoryItems, manipulationOptions, markedInfoSelector, moveParamsSelector,
        (directoryInfo, traversalMarker, moveParams) => MoveCore(directoryInfo, moveParams),
        (directoryInfo, traversalMarker, manipulationMarker, dstDirectoryInfo) => resultSelector(directoryInfo, traversalMarker, manipulationMarker, dstDirectoryInfo),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static TransferResult<DirectoryInfo>[] MoveDirectories(
      this IEnumerable<MarkedInfo<DirectoryInfo>> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<DirectoryInfo, FileSystemTraversalMarker, TransferOptions> moveParamsSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);
      Argument.That.NotNull(moveParamsSelector);

      return ManipulateDirectoriesCore(directoryItems, manipulationOptions,
        _ => _,
        directoryItem => moveParamsSelector(directoryItem.Info, directoryItem.TraversalMarker),
        (directoryInfo, _, moveParams) => MoveCore(directoryInfo, moveParams),
        (directoryInfo, traversalMarker, manipulationMarker, dstDirectoryInfo) => directoryInfo.ToTransferResult(traversalMarker, manipulationMarker, dstDirectoryInfo),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static TransferResult<DirectoryInfo>[] MoveDirectories(
      this IEnumerable<DirectoryInfo> directoryInfos, FileSystemManipulationOptions manipulationOptions,
      Func<DirectoryInfo, TransferOptions> moveParamsSelector,
      Func<DirectoryInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryInfos);
      Argument.That.NotNull(moveParamsSelector);

      return ManipulateDirectoriesCore(directoryInfos, manipulationOptions,
        directoryInfo => directoryInfo.ToMarkedInfo(FileSystemTraversalMarker.EnterDirectory),
        directoryInfo => moveParamsSelector(directoryInfo),
        (directoryInfo, _, moveParams) => MoveCore(directoryInfo, moveParams),
        (directoryInfo, _, manipulationMarker, dstDirectoryInfo) => directoryInfo.ToTransferResult(FileSystemTraversalMarker.None, manipulationMarker, dstDirectoryInfo),
        errorHandler is null ? null : (directoryInfo, _, moveParams, exception) => errorHandler(directoryInfo, moveParams, exception),
        cancellationToken)
        .ToArray();
    }

    #endregion
    #region Directories deferred delete methods

    public static IEnumerable<TDeleteResult> DeleteDirectoriesDefer<TDirectoryItem, TDeleteResult>(
      this IEnumerable<TDirectoryItem> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<TDirectoryItem, MarkedInfo<DirectoryInfo>> markedInfoSelector,
      Func<TDirectoryItem, DeleteOptions> deleteParamsSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TDeleteResult> resultSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(deleteParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateDirectoriesCore(directoryItems, manipulationOptions, markedInfoSelector, deleteParamsSelector,
        (directoryInfo, traversalMarker, deleteParams) => { DeleteCore(directoryInfo, deleteParams, clearReadOnly); return Void.Value; },
        (directoryInfo, traversalMarker, manipulationMarker, _) => resultSelector(directoryInfo, traversalMarker, manipulationMarker),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<InfoResult<DirectoryInfo>> DeleteDirectoriesDefer(
      this IEnumerable<MarkedInfo<DirectoryInfo>> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<DirectoryInfo, FileSystemTraversalMarker, DeleteOptions> deleteParamsSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);
      Argument.That.NotNull(deleteParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateDirectoriesCore(directoryItems, manipulationOptions,
        _ => _,
        fileSystemItem => deleteParamsSelector(fileSystemItem.Info, fileSystemItem.TraversalMarker),
        (directoryInfo, traversalMarker, deleteParams) => { DeleteCore(directoryInfo, deleteParams, clearReadOnly); return Void.Value; },
        (directoryInfo, traversalMarker, manipulationMarker, _) => directoryInfo.ToInfoResult(traversalMarker, manipulationMarker),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<InfoResult<DirectoryInfo>> DeleteDirectoriesDefer(
      this IEnumerable<DirectoryInfo> directoryInfos, FileSystemManipulationOptions manipulationOptions,
      Func<DirectoryInfo, DeleteOptions> deleteParamsSelector,
      Func<DirectoryInfo, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryInfos);
      Argument.That.NotNull(deleteParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateDirectoriesCore(directoryInfos, manipulationOptions,
        directoryInfo => directoryInfo.ToMarkedInfo(FileSystemTraversalMarker.LeaveDirectory),
        directoryInfo => deleteParamsSelector(directoryInfo),
        (directoryInfo, traversalMarker, deleteParams) => { DeleteCore(directoryInfo, deleteParams, clearReadOnly); return Void.Value; },
        (directoryInfo, traversalMarker, manipulationMarker, _) => directoryInfo.ToInfoResult(FileSystemTraversalMarker.None, manipulationMarker),
        errorHandler is null ? null : (directoryInfo, _, deleteParams, exception) => errorHandler(directoryInfo, deleteParams, exception),
        cancellationToken);
    }

    #endregion
    #region Directories direct delete methods

    public static TDeleteResult[] DeleteDirectories<TDirectoryItem, TDeleteResult>(
      this IEnumerable<TDirectoryItem> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<TDirectoryItem, MarkedInfo<DirectoryInfo>> markedInfoSelector,
      Func<TDirectoryItem, DeleteOptions> deleteParamsSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, FileSystemManipulationMarker, TDeleteResult> resultSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);
      Argument.That.NotNull(markedInfoSelector);
      Argument.That.NotNull(deleteParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateDirectoriesCore(directoryItems, manipulationOptions, markedInfoSelector, deleteParamsSelector,
        (directoryInfo, traversalMarker, deleteParams) => { DeleteCore(directoryInfo, deleteParams, clearReadOnly); return Void.Value; },
        (directoryInfo, traversalMarker, manipulationMarker, _) => resultSelector(directoryInfo, traversalMarker, manipulationMarker),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static InfoResult<DirectoryInfo>[] DeleteDirectories(
      this IEnumerable<MarkedInfo<DirectoryInfo>> directoryItems, FileSystemManipulationOptions manipulationOptions,
      Func<DirectoryInfo, FileSystemTraversalMarker, DeleteOptions> deleteParamsSelector,
      Func<DirectoryInfo, FileSystemTraversalMarker, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryItems);
      Argument.That.NotNull(deleteParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateDirectoriesCore(directoryItems, manipulationOptions,
        _ => _,
        fileSystemItem => deleteParamsSelector(fileSystemItem.Info, fileSystemItem.TraversalMarker),
        (directoryInfo, traversalMarker, deleteParams) => { DeleteCore(directoryInfo, deleteParams, clearReadOnly); return Void.Value; },
        (directoryInfo, traversalMarker, manipulationMarker, _) => directoryInfo.ToInfoResult(traversalMarker, manipulationMarker),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static InfoResult<DirectoryInfo>[] DeleteDirectories(
      this IEnumerable<DirectoryInfo> directoryInfos, FileSystemManipulationOptions manipulationOptions,
      Func<DirectoryInfo, DeleteOptions> deleteParamsSelector,
      Func<DirectoryInfo, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(directoryInfos);
      Argument.That.NotNull(deleteParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateDirectoriesCore(directoryInfos, manipulationOptions,
        directoryInfo => directoryInfo.ToMarkedInfo(FileSystemTraversalMarker.LeaveDirectory),
        directoryInfo => deleteParamsSelector(directoryInfo),
        (directoryInfo, traversalMarker, deleteParams) => { DeleteCore(directoryInfo, deleteParams, clearReadOnly); return Void.Value; },
        (directoryInfo, traversalMarker, manipulationMarker, _) => directoryInfo.ToInfoResult(FileSystemTraversalMarker.None, manipulationMarker),
        errorHandler is null ? null : (directoryInfo, _, deleteParams, exception) => errorHandler(directoryInfo, deleteParams, exception),
        cancellationToken)
        .ToArray();
    }

    #endregion
    #region Files manipulation asynchronus methods

    public static IAsyncEnumerable<TManipulationResult> ManipulateFilesAsync<TFileItem, TManipulationParams, TManipulationInterim, TManipulationResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, TManipulationParams> manipulationParamsSelector,
      Func<FileInfo, TManipulationParams, TManipulationInterim?> fileHandler,
      Func<FileInfo, FileSystemManipulationMarker, TManipulationInterim?, TManipulationResult> resultSelector,
      Func<FileInfo, TManipulationParams?, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(manipulationParamsSelector);
      Argument.That.NotNull(fileHandler);
      Argument.That.NotNull(resultSelector);

      return ManipulateFilesAsyncCore(fileItems, manipulationOptions, fileInfoSelector, manipulationParamsSelector, fileHandler, resultSelector, errorHandler, cancellationToken);
    }

    public static IAsyncEnumerable<TManipulationResult> ManipulateFilesAsync<TFileItem, TManipulationParams, TManipulationResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, TManipulationParams> manipulationParamsSelector,
      Action<FileInfo, TManipulationParams> fileHandler,
      Func<FileInfo, FileSystemManipulationMarker, TManipulationResult> resultSelector,
      Func<FileInfo, TManipulationParams?, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(manipulationParamsSelector );
      Argument.That.NotNull(fileHandler);
      Argument.That.NotNull(resultSelector);

      return ManipulateFilesAsyncCore(fileItems, manipulationOptions, fileInfoSelector, manipulationParamsSelector ,
        (fileInfo, manipulationParams) => { fileHandler(fileInfo, manipulationParams); return Void.Value; },
        (fileInfo, manipulationMarker, _) => resultSelector(fileInfo, manipulationMarker),
        errorHandler, cancellationToken);
    }

    #endregion
    #region Files manipulation deferred methods

    public static IEnumerable<TManipulationResult> ManipulateFilesDefer<TFileItem, TManipulationParams, TManipulationInterim, TManipulationResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, TManipulationParams> manipulationParamsSelector,
      Func<FileInfo, TManipulationParams, TManipulationInterim?> fileHandler,
      Func<FileInfo, FileSystemManipulationMarker, TManipulationInterim?, TManipulationResult> resultSelector,
      Func<FileInfo, TManipulationParams?, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(manipulationParamsSelector);
      Argument.That.NotNull(fileHandler);
      Argument.That.NotNull(resultSelector);

      return ManipulateFilesCore(fileItems, manipulationOptions, fileInfoSelector, manipulationParamsSelector, fileHandler, resultSelector, errorHandler, cancellationToken);
    }

    public static IEnumerable<TManipulationResult> ManipulateFilesDefer<TFileItem, TManipulationParams, TManipulationInterim, TManipulationResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, TManipulationParams> manipulationParamsSelector,
      Func<FileInfo, TManipulationParams, TManipulationInterim?> fileHandler,
      Func<FileInfo, FileSystemManipulationMarker, TManipulationInterim?, TManipulationResult> resultSelector,
      Func<FileInfo, TManipulationParams?, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(manipulationParamsSelector);
      Argument.That.NotNull(fileHandler);
      Argument.That.NotNull(resultSelector);

      return ManipulateFilesCore(fileItems, manipulationOptions, maxConcurrents, fileInfoSelector, manipulationParamsSelector, fileHandler, resultSelector, errorHandler, cancellationToken);
    }

    public static IEnumerable<TManipulationResult> ManipulateFilesDefer<TFileItem, TManipulationParams, TManipulationInterim, TManipulationResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, TManipulationParams> manipulationParamsSelector,
      Action<FileInfo, TManipulationParams> fileHandler,
      Func<FileInfo, FileSystemManipulationMarker, TManipulationResult> resultSelector,
      Func<FileInfo, TManipulationParams?, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(manipulationParamsSelector);
      Argument.That.NotNull(fileHandler);
      Argument.That.NotNull(resultSelector);

      return ManipulateFilesCore(fileItems, manipulationOptions, fileInfoSelector, manipulationParamsSelector,
        (fileInfo, manipulationParams) => { fileHandler(fileInfo, manipulationParams); return Void.Value; },
        (fileInfo, manipulationMarker, _) => resultSelector(fileInfo, manipulationMarker),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<TManipulationResult> ManipulateFilesDefer<TFileItem, TManipulationParams, TManipulationInterim, TManipulationResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, TManipulationParams> manipulationParamsSelector,
      Action<FileInfo, TManipulationParams> fileHandler,
      Func<FileInfo, FileSystemManipulationMarker, TManipulationResult> resultSelector,
      Func<FileInfo, TManipulationParams?, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(manipulationParamsSelector);
      Argument.That.NotNull(fileHandler);
      Argument.That.NotNull(resultSelector);

      return ManipulateFilesCore(fileItems, manipulationOptions, maxConcurrents, fileInfoSelector, manipulationParamsSelector,
        (fileInfo, manipulationParams) => { fileHandler(fileInfo, manipulationParams); return Void.Value; },
        (fileInfo, manipulationMarker, _) => resultSelector(fileInfo, manipulationMarker),
        errorHandler, cancellationToken);
    }

    #endregion
    #region Files manipulation direct methods

    public static TManipulationResult[] ManipulateFiles<TFileItem, TManipulationParams, TManipulationInterim, TManipulationResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, TManipulationParams> manipulationParamsSelector,
      Func<FileInfo, TManipulationParams, TManipulationInterim?> fileHandler,
      Func<FileInfo, FileSystemManipulationMarker, TManipulationInterim?, TManipulationResult> resultSelector,
      Func<FileInfo, TManipulationParams?, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(manipulationParamsSelector);
      Argument.That.NotNull(fileHandler);
      Argument.That.NotNull(resultSelector);

      return ManipulateFilesCore(fileItems, manipulationOptions, fileInfoSelector, manipulationParamsSelector, fileHandler, resultSelector, errorHandler, cancellationToken)
        .ToArray();
    }

    public static TManipulationResult[] ManipulateFiles<TFileItem, TManipulationParams, TManipulationInterim, TManipulationResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, TManipulationParams> manipulationParamsSelector,
      Func<FileInfo, TManipulationParams, TManipulationInterim?> fileHandler,
      Func<FileInfo, FileSystemManipulationMarker, TManipulationInterim?, TManipulationResult> resultSelector,
      Func<FileInfo, TManipulationParams?, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(manipulationParamsSelector);
      Argument.That.NotNull(fileHandler);
      Argument.That.NotNull(resultSelector);

      return ManipulateFilesCore(fileItems, manipulationOptions, maxConcurrents, fileInfoSelector, manipulationParamsSelector, fileHandler, resultSelector, errorHandler, cancellationToken)
        .ToArray();
    }

    public static TManipulationResult[] ManipulateFiles<TFileItem, TManipulationParams, TManipulationInterim, TManipulationResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, TManipulationParams> manipulationParamsSelector,
      Action<FileInfo, TManipulationParams> fileHandler,
      Func<FileInfo, FileSystemManipulationMarker, TManipulationResult> resultSelector,
      Func<FileInfo, TManipulationParams?, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(manipulationParamsSelector);
      Argument.That.NotNull(fileHandler);
      Argument.That.NotNull(resultSelector);

      return ManipulateFilesCore(fileItems, manipulationOptions, fileInfoSelector, manipulationParamsSelector,
        (fileInfo, manipulationParams) => { fileHandler(fileInfo, manipulationParams); return Void.Value; },
        (fileInfo, manipulationMarker, _) => resultSelector(fileInfo, manipulationMarker),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static TManipulationResult[] ManipulateFiles<TFileItem, TManipulationParams, TManipulationInterim, TManipulationResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, TManipulationParams> manipulationParamsSelector,
      Action<FileInfo, TManipulationParams> fileHandler,
      Func<FileInfo, FileSystemManipulationMarker, TManipulationResult> resultSelector,
      Func<FileInfo, TManipulationParams?, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(manipulationParamsSelector);
      Argument.That.NotNull(fileHandler);
      Argument.That.NotNull(resultSelector);

      return ManipulateFilesCore(fileItems, manipulationOptions, maxConcurrents, fileInfoSelector, manipulationParamsSelector,
        (fileInfo, manipulationParams) => { fileHandler(fileInfo, manipulationParams); return Void.Value; },
        (fileInfo, manipulationMarker, _) => resultSelector(fileInfo, manipulationMarker),
        errorHandler, cancellationToken)
        .ToArray();
    }

    #endregion
    #region Files asynchronus copy methods

    public static IAsyncEnumerable<TTransferResult> CopyFilesAsync<TFileItem, TTransferResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, TransferOptions> copyParamsSelector,
      Func<FileInfo, FileSystemManipulationMarker, FileInfo?, TTransferResult> resultSelector,
      Func<FileInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(copyParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesAsyncCore(fileItems, manipulationOptions, fileInfoSelector, copyParamsSelector,
        (fileInfo, copyParams) => CopyCore(fileInfo, copyParams, clearReadOnly),
        (fileInfo, manipulationMarker, dstFileInfo) => resultSelector(fileInfo, manipulationMarker, dstFileInfo),
        errorHandler, cancellationToken);
    }

    public static IAsyncEnumerable<TransferResult<FileInfo>> CopyFilesAsync(
      this IEnumerable<FileInfo> fileInfos, FileSystemManipulationOptions manipulationOptions,
      Func<FileInfo, TransferOptions> copyParamsSelector,
      Func<FileInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileInfos);
      Argument.That.NotNull(copyParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesAsyncCore(fileInfos, manipulationOptions,
        _ => _,
        fileInfo => copyParamsSelector(fileInfo),
        (fileInfo, copyParams) => CopyCore(fileInfo, copyParams, clearReadOnly),
        (fileInfo, manipulationMarker, dstFileInfo) => fileInfo.ToTransferResult(FileSystemTraversalMarker.None, manipulationMarker, dstFileInfo),
        errorHandler, cancellationToken);
    }

    #endregion
    #region Files deferred copy methods

    public static IEnumerable<TTransferResult> CopyFilesDefer<TFileItem, TTransferResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, TransferOptions> copyParamsSelector,
      Func<FileInfo, FileSystemManipulationMarker, FileInfo?, TTransferResult> resultSelector,
      Func<FileInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(copyParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileItems, manipulationOptions, fileInfoSelector, copyParamsSelector,
        (fileInfo, copyParams) => CopyCore(fileInfo, copyParams, clearReadOnly),
        resultSelector, errorHandler, cancellationToken);
    }

    public static IEnumerable<TTransferResult> CopyFilesDefer<TFileItem, TTransferResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, TransferOptions> copyParamsSelector,
      Func<FileInfo, FileSystemManipulationMarker, FileInfo?, TTransferResult> resultSelector,
      Func<FileInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(copyParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileItems, manipulationOptions, maxConcurrents, fileInfoSelector, copyParamsSelector,
        (fileInfo, copyParams) => CopyCore(fileInfo, copyParams, clearReadOnly),
        resultSelector, errorHandler, cancellationToken);
    }

    public static IEnumerable<TransferResult<FileInfo>> CopyFilesDefer(
      this IEnumerable<FileInfo> fileInfos, FileSystemManipulationOptions manipulationOptions,
      Func<FileInfo, TransferOptions> copyParamsSelector,
      Func<FileInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileInfos);
      Argument.That.NotNull(copyParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileInfos, manipulationOptions,
        _ => _,
        fileInfo => copyParamsSelector(fileInfo),
        (fileInfo, copyParams) => CopyCore(fileInfo, copyParams, clearReadOnly),
        (fileInfo, manipulationMarker, dstFileInfo) => fileInfo.ToTransferResult(FileSystemTraversalMarker.None, manipulationMarker, dstFileInfo),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<TransferResult<FileInfo>> CopyFilesDefer(
      this IEnumerable<FileInfo> fileInfos, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<FileInfo, TransferOptions> copyParamsSelector,
      Func<FileInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileInfos);
      Argument.That.NotNull(copyParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileInfos, manipulationOptions, maxConcurrents,
        _ => _,
        fileInfo => copyParamsSelector(fileInfo),
        (fileInfo, copyParams) => CopyCore(fileInfo, copyParams, clearReadOnly),
        (fileInfo, manipulationMarker, dstFileInfo) => fileInfo.ToTransferResult(FileSystemTraversalMarker.None, manipulationMarker, dstFileInfo),
        errorHandler, cancellationToken);
    }

    #endregion
    #region Files direct copy methods

    public static TTransferResult[] CopyFiles<TFileItem, TTransferResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, TransferOptions> copyParamsSelector,
      Func<FileInfo, FileSystemManipulationMarker, FileInfo?, TTransferResult> resultSelector,
      Func<FileInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(copyParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileItems, manipulationOptions, fileInfoSelector, copyParamsSelector,
        (fileInfo, copyParams) => CopyCore(fileInfo, copyParams, clearReadOnly),
        resultSelector, errorHandler, cancellationToken)
        .ToArray();
    }

    public static TTransferResult[] CopyFiles<TFileItem, TTransferResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, TransferOptions> copyParamsSelector,
      Func<FileInfo, FileSystemManipulationMarker, FileInfo?, TTransferResult> resultSelector,
      Func<FileInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(copyParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileItems, manipulationOptions, maxConcurrents, fileInfoSelector, copyParamsSelector,
        (fileInfo, copyParams) => CopyCore(fileInfo, copyParams, clearReadOnly),
        resultSelector, errorHandler, cancellationToken)
        .ToArray();
    }

    public static TransferResult<FileInfo>[] CopyFiles(
      this IEnumerable<FileInfo> fileInfos, FileSystemManipulationOptions manipulationOptions,
      Func<FileInfo, TransferOptions> copyParamsSelector,
      Func<FileInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileInfos);
      Argument.That.NotNull(copyParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileInfos, manipulationOptions,
        fileInfo => fileInfo,
        fileInfo => copyParamsSelector(fileInfo),
        (fileInfo, copyParams) => CopyCore(fileInfo, copyParams, clearReadOnly),
        (fileInfo, manipulationMarker, dstFileInfo) => fileInfo.ToTransferResult(FileSystemTraversalMarker.None, manipulationMarker, dstFileInfo),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static TransferResult<FileInfo>[] CopyFiles(
      this IEnumerable<FileInfo> fileInfos, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<FileInfo, TransferOptions> copyParamsSelector,
      Func<FileInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileInfos);
      Argument.That.NotNull(copyParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileInfos, manipulationOptions, maxConcurrents,
        fileInfo => fileInfo,
        fileInfo => copyParamsSelector(fileInfo),
        (fileInfo, copyParams) => CopyCore(fileInfo, copyParams, clearReadOnly),
        (fileInfo, manipulationMarker, dstFileInfo) => fileInfo.ToTransferResult(FileSystemTraversalMarker.None, manipulationMarker, dstFileInfo),
        errorHandler, cancellationToken)
        .ToArray();
    }

    #endregion
    #region Files asynchronus move methods

    public static IAsyncEnumerable<TTransferResult> MoveFilesAsync<TFileItem, TTransferResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, TransferOptions> moveParamsSelector,
      Func<FileInfo, FileSystemManipulationMarker, FileInfo?, TTransferResult> resultSelector,
      Func<FileInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(moveParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesAsyncCore(fileItems, manipulationOptions, fileInfoSelector, moveParamsSelector,
        (fileInfo, moveParams) => MoveCore(fileInfo, moveParams, clearReadOnly),
        resultSelector, errorHandler, cancellationToken);
    }

    public static IAsyncEnumerable<TransferResult<FileInfo>> MoveFilesAsync(
      this IEnumerable<FileInfo> fileInfos, FileSystemManipulationOptions manipulationOptions,
      Func<FileInfo, TransferOptions> moveParamsSelector,
      Func<FileInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileInfos);
      Argument.That.NotNull(moveParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesAsyncCore(fileInfos, manipulationOptions,
        _ => _,
        fileInfo => moveParamsSelector(fileInfo),
        (fileInfo, moveParams) => MoveCore(fileInfo, moveParams, clearReadOnly),
        (fileInfo, manipulationMarker, dstFileInfo) => fileInfo.ToTransferResult(FileSystemTraversalMarker.None, manipulationMarker, dstFileInfo),
        errorHandler, cancellationToken);
    }

    #endregion
    #region Files deferred move methods

    public static IEnumerable<TTransferResult> MoveFilesDefer<TFileItem, TTransferResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, TransferOptions> moveParamsSelector,
      Func<FileInfo, FileSystemManipulationMarker, FileInfo?, TTransferResult> resultSelector,
      Func<FileInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(moveParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileItems, manipulationOptions, fileInfoSelector, moveParamsSelector,
        (fileInfo, moveParams) => MoveCore(fileInfo, moveParams, clearReadOnly),
        resultSelector, errorHandler, cancellationToken);
    }

    public static IEnumerable<TTransferResult> MoveFilesDefer<TFileItem, TTransferResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, TransferOptions> moveParamsSelector,
      Func<FileInfo, FileSystemManipulationMarker, FileInfo?, TTransferResult> resultSelector,
      Func<FileInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(moveParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileItems, manipulationOptions, maxConcurrents, fileInfoSelector, moveParamsSelector,
        (fileInfo, moveParams) => MoveCore(fileInfo, moveParams, clearReadOnly),
        resultSelector, errorHandler, cancellationToken);
    }

    public static IEnumerable<TransferResult<FileInfo>> MoveFilesDefer(
      this IEnumerable<FileInfo> fileInfos, FileSystemManipulationOptions manipulationOptions,
      Func<FileInfo, TransferOptions> moveParamsSelector,
      Func<FileInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileInfos);
      Argument.That.NotNull(moveParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileInfos, manipulationOptions,
        fileInfo => fileInfo,
        fileInfo => moveParamsSelector(fileInfo),
        (fileInfo, moveParams) => MoveCore(fileInfo, moveParams, clearReadOnly),
        (fileInfo, manipulationMarker, dstFileInfo) => fileInfo.ToTransferResult(FileSystemTraversalMarker.None, manipulationMarker, dstFileInfo),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<TransferResult<FileInfo>> MoveFilesDefer(
      this IEnumerable<FileInfo> fileInfos, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<FileInfo, TransferOptions> moveParamsSelector,
      Func<FileInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileInfos);
      Argument.That.NotNull(moveParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileInfos, manipulationOptions, maxConcurrents,
        fileInfo => fileInfo,
        fileInfo => moveParamsSelector(fileInfo),
        (fileInfo, moveParams) => MoveCore(fileInfo, moveParams, clearReadOnly),
        (fileInfo, manipulationMarker, dstFileInfo) => fileInfo.ToTransferResult(FileSystemTraversalMarker.None, manipulationMarker, dstFileInfo),
        errorHandler, cancellationToken);
    }

    #endregion
    #region Files direct move methods

    public static TTransferResult[] MoveFiles<TFileItem, TTransferResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, TransferOptions> moveParamsSelector,
      Func<FileInfo, FileSystemManipulationMarker, FileInfo?, TTransferResult> resultSelector,
      Func<FileInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(moveParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileItems, manipulationOptions, fileInfoSelector, moveParamsSelector,
        (fileInfo, moveParams) => MoveCore(fileInfo, moveParams, clearReadOnly),
        resultSelector, errorHandler, cancellationToken)
        .ToArray();
    }

    public static TTransferResult[] MoveFiles<TFileItem, TTransferResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, TransferOptions> moveParamsSelector,
      Func<FileInfo, FileSystemManipulationMarker, FileInfo?, TTransferResult> resultSelector,
      Func<FileInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(moveParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileItems, manipulationOptions, maxConcurrents, fileInfoSelector, moveParamsSelector,
        (fileInfo, moveParams) => MoveCore(fileInfo, moveParams, clearReadOnly),
        resultSelector, errorHandler, cancellationToken)
        .ToArray();
    }

    public static TransferResult<FileInfo>[] MoveFiles(
      this IEnumerable<FileInfo> fileInfos, FileSystemManipulationOptions manipulationOptions,
      Func<FileInfo, TransferOptions> moveParamsSelector,
      Func<FileInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileInfos);
      Argument.That.NotNull(moveParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileInfos, manipulationOptions,
        _ => _,
        fileInfo => moveParamsSelector(fileInfo),
        (fileInfo, moveParams) => MoveCore(fileInfo, moveParams, clearReadOnly),
        (fileInfo, manipulationMarker, dstFileInfo) => fileInfo.ToTransferResult(FileSystemTraversalMarker.None, manipulationMarker, dstFileInfo),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static TransferResult<FileInfo>[] MoveFiles(
      this IEnumerable<FileInfo> fileInfos, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<FileInfo, TransferOptions> moveParamsSelector,
      Func<FileInfo, TransferOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileInfos);
      Argument.That.NotNull(moveParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileInfos, manipulationOptions, maxConcurrents,
        _ => _,
        fileInfo => moveParamsSelector(fileInfo),
        (fileInfo, moveParams) => MoveCore(fileInfo, moveParams, clearReadOnly),
        (fileInfo, manipulationMarker, dstFileInfo) => fileInfo.ToTransferResult(FileSystemTraversalMarker.None, manipulationMarker, dstFileInfo),
        errorHandler, cancellationToken)
        .ToArray();
    }

    #endregion
    #region Files asynchronus replace methods

    public static IAsyncEnumerable<TReplaceResult> ReplaceFilesAsync<TFileItem, TReplaceResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, ReplaceOptions> replaceParamsSelector,
      Func<FileInfo, FileSystemManipulationMarker, FileInfo, FileInfo?, TReplaceResult> resultSelector,
      Func<FileInfo, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(replaceParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesAsyncCore(fileItems, manipulationOptions, fileInfoSelector, replaceParamsSelector,
        (fileInfo, replaceParams) => ReplaceCore(fileInfo, replaceParams, clearReadOnly),
        (fileInfo, manipulationMarker, replaceInterim) => resultSelector(fileInfo, manipulationMarker, replaceInterim.dstFileInfo, replaceInterim.bakFileInfo),
        errorHandler, cancellationToken);
    }

    public static IAsyncEnumerable<ReplaceResult<FileInfo>> ReplaceFilesAsync(
      this IEnumerable<FileInfo> fileInfos, FileSystemManipulationOptions manipulationOptions,
      Func<FileInfo, ReplaceOptions> replaceParamsSelector,
      Func<FileInfo, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileInfos);
      Argument.That.NotNull(replaceParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesAsyncCore(fileInfos, manipulationOptions,
        fileInfo => fileInfo,
        fileInfo => replaceParamsSelector(fileInfo),
        (fileInfo, replaceParams) => ReplaceCore(fileInfo, replaceParams, clearReadOnly),
        (fileInfo, manipulationMarker, replaceInterim) => fileInfo.ToReplaceResult(FileSystemTraversalMarker.None, manipulationMarker, replaceInterim.dstFileInfo, replaceInterim.bakFileInfo),
        errorHandler, cancellationToken);
    }

    #endregion
    #region Files deferred replace methods

    public static IEnumerable<TReplaceResult> ReplaceFilesDefer<TFileItem, TReplaceResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, ReplaceOptions> replaceParamsSelector,
      Func<FileInfo, FileSystemManipulationMarker, FileInfo, FileInfo?, TReplaceResult> resultSelector,
      Func<FileInfo, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(replaceParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileItems, manipulationOptions, fileInfoSelector, replaceParamsSelector,
        (fileInfo, replaceParams) => ReplaceCore(fileInfo, replaceParams, clearReadOnly),
        (fileInfo, manipulationMarker, replaceInterim) => resultSelector(fileInfo, manipulationMarker, replaceInterim.dstFileInfo, replaceInterim.bakFileInfo),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<TReplaceResult> ReplaceFilesDefer<TFileItem, TReplaceResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, ReplaceOptions> replaceParamsSelector,
      Func<FileInfo, FileSystemManipulationMarker, FileInfo, FileInfo?, TReplaceResult> resultSelector,
      Func<FileInfo, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(replaceParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileItems, manipulationOptions, maxConcurrents, fileInfoSelector, replaceParamsSelector,
        (fileInfo, replaceParams) => ReplaceCore(fileInfo, replaceParams, clearReadOnly),
        (fileInfo, manipulationMarker, replaceInterim) => resultSelector(fileInfo, manipulationMarker, replaceInterim.dstFileInfo, replaceInterim.bakFileInfo),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<ReplaceResult<FileInfo>> ReplaceFilesDefer(
      this IEnumerable<FileInfo> fileInfos, FileSystemManipulationOptions manipulationOptions,
      Func<FileInfo, ReplaceOptions> replaceParamsSelector,
      Func<FileInfo, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileInfos);
      Argument.That.NotNull(replaceParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileInfos, manipulationOptions,
        _ => _,
        fileInfo => replaceParamsSelector(fileInfo),
        (fileInfo, replaceParams) => ReplaceCore(fileInfo, replaceParams, clearReadOnly),
        (fileInfo, manipulationMarker, replaceInterim) => fileInfo.ToReplaceResult(FileSystemTraversalMarker.None, manipulationMarker, replaceInterim.dstFileInfo, replaceInterim.bakFileInfo),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<ReplaceResult<FileInfo>> ReplaceFilesDefer(
      this IEnumerable<FileInfo> fileInfos, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<FileInfo, ReplaceOptions> replaceParamsSelector,
      Func<FileInfo, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileInfos);
      Argument.That.NotNull(replaceParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileInfos, manipulationOptions, maxConcurrents,
        _ => _,
        fileInfo => replaceParamsSelector(fileInfo),
        (fileInfo, replaceParams) => ReplaceCore(fileInfo, replaceParams, clearReadOnly),
        (fileInfo, manipulationMarker, replaceInterim) => fileInfo.ToReplaceResult(FileSystemTraversalMarker.None, manipulationMarker, replaceInterim.dstFileInfo, replaceInterim.bakFileInfo),
        errorHandler, cancellationToken);
    }

    #endregion
    #region Files direct replace methods

    public static TReplaceResult[] ReplaceFiles<TFileItem, TReplaceResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, ReplaceOptions> replaceParamsSelector,
      Func<FileInfo, FileSystemManipulationMarker, FileInfo, FileInfo?, TReplaceResult> resultSelector,
      Func<FileInfo, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(replaceParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileItems, manipulationOptions, fileInfoSelector, replaceParamsSelector,
        (fileInfo, replaceParams) => ReplaceCore(fileInfo, replaceParams, clearReadOnly),
        (fileInfo, manipulationMarker, replaceInterim) => resultSelector(fileInfo, manipulationMarker, replaceInterim.dstFileInfo, replaceInterim.bakFileInfo),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static TReplaceResult[] ReplaceFiles<TFileItem, TReplaceResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, ReplaceOptions> replaceParamsSelector,
      Func<FileInfo, FileSystemManipulationMarker, FileInfo, FileInfo?, TReplaceResult> resultSelector,
      Func<FileInfo, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(replaceParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileItems, manipulationOptions, maxConcurrents, fileInfoSelector, replaceParamsSelector,
        (fileInfo, replaceParams) => ReplaceCore(fileInfo, replaceParams, clearReadOnly),
        (fileInfo, manipulationMarker, replaceInterim) => resultSelector(fileInfo, manipulationMarker, replaceInterim.dstFileInfo, replaceInterim.bakFileInfo),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static ReplaceResult<FileInfo>[] ReplaceFiles(
      this IEnumerable<FileInfo> fileInfos, FileSystemManipulationOptions manipulationOptions,
      Func<FileInfo, ReplaceOptions> replaceParamsSelector,
      Func<FileInfo, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileInfos);
      Argument.That.NotNull(replaceParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileInfos, manipulationOptions,
        _ => _,
        fileInfo => replaceParamsSelector(fileInfo),
        (fileInfo, replaceParams) => ReplaceCore(fileInfo, replaceParams, clearReadOnly),
        (fileInfo, manipulationMarker, replaceInterim) => fileInfo.ToReplaceResult(FileSystemTraversalMarker.None, manipulationMarker, replaceInterim.dstFileInfo, replaceInterim.bakFileInfo),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static ReplaceResult<FileInfo>[] ReplaceFiles(
      this IEnumerable<FileInfo> fileInfos, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<FileInfo, ReplaceOptions> replaceParamsSelector,
      Func<FileInfo, ReplaceOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileInfos);
      Argument.That.NotNull(replaceParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileInfos, manipulationOptions, maxConcurrents,
        _ => _,
        fileInfo => replaceParamsSelector(fileInfo),
        (fileInfo, replaceParams) => ReplaceCore(fileInfo, replaceParams, clearReadOnly),
        (fileInfo, manipulationMarker, replaceInterim) => fileInfo.ToReplaceResult(FileSystemTraversalMarker.None, manipulationMarker, replaceInterim.dstFileInfo, replaceInterim.bakFileInfo),
        errorHandler, cancellationToken)
        .ToArray();
    }

    #endregion
    #region Files asynchronus delete methods

    public static IAsyncEnumerable<TDeleteResult> DeleteFilesAsync<TFileItem, TDeleteResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, DeleteOptions> deleteParamsSelector,
      Func<FileInfo, FileSystemManipulationMarker, TDeleteResult> resultSelector,
      Func<FileInfo, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(deleteParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesAsyncCore(fileItems, manipulationOptions, fileInfoSelector, deleteParamsSelector,
        (fileInfo, deleteParams) => { DeleteCore(fileInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileInfo, manipulationMarker, _) => resultSelector(fileInfo, manipulationMarker),
        errorHandler, cancellationToken);
    }

    public static IAsyncEnumerable<InfoResult<FileInfo>> DeleteFilesAsync(
      this IEnumerable<FileInfo> fileInfos, FileSystemManipulationOptions manipulationOptions,
      Func<FileInfo, DeleteOptions> deleteParamsSelector,
      Func<FileInfo, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileInfos);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesAsyncCore(fileInfos, manipulationOptions, fileInfo => fileInfo, deleteParamsSelector,
        (fileInfo, deleteParams) => { DeleteCore(fileInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileInfo, manipulationMarker, _) => fileInfo.ToInfoResult(FileSystemTraversalMarker.None, manipulationMarker),
        errorHandler, cancellationToken);
    }

    #endregion
    #region Files deferred delete methods

    public static IEnumerable<TFileResult> DeleteFilesDefer<TFileItem, TFileResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, DeleteOptions> deleteParamsSelector,
      Func<FileInfo, FileSystemManipulationMarker, TFileResult> resultSelector,
      Func<FileInfo, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(deleteParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileItems, manipulationOptions, fileInfoSelector, deleteParamsSelector,
        (fileInfo, deleteParams) => { DeleteCore(fileInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileInfo, manipulationMarker, _) => resultSelector(fileInfo, manipulationMarker),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<TFileResult> DeleteFilesDefer<TFileItem, TFileResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, DeleteOptions> deleteParamsSelector,
      Func<FileInfo, FileSystemManipulationMarker, TFileResult> resultSelector,
      Func<FileInfo, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(deleteParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileItems, manipulationOptions, maxConcurrents, fileInfoSelector, deleteParamsSelector,
        (fileInfo, deleteParams) => { DeleteCore(fileInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileInfo, manipulationMarker, _) => resultSelector(fileInfo, manipulationMarker),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<InfoResult<FileInfo>> DeleteFilesDefer(
      this IEnumerable<FileInfo> fileInfos, FileSystemManipulationOptions manipulationOptions,
      Func<FileInfo, DeleteOptions> deleteParamsSelector,
      Func<FileInfo, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileInfos);
      Argument.That.NotNull(deleteParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileInfos, manipulationOptions,
        _ => _,
        fileInfo => deleteParamsSelector(fileInfo),
        (fileInfo, deleteParams) => { DeleteCore(fileInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileInfo, manipulationMarker, _) => fileInfo.ToInfoResult(FileSystemTraversalMarker.None, manipulationMarker),
        errorHandler, cancellationToken);
    }

    public static IEnumerable<InfoResult<FileInfo>> DeleteFilesDefer(
      this IEnumerable<FileInfo> fileInfos, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<FileInfo, DeleteOptions> deleteParamsSelector,
      Func<FileInfo, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileInfos);
      Argument.That.NotNull(deleteParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileInfos, manipulationOptions, maxConcurrents,
        _ => _,
        fileInfo => deleteParamsSelector(fileInfo),
        (fileInfo, deleteParams) => { DeleteCore(fileInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileInfo, manipulationMarker, _) => fileInfo.ToInfoResult(FileSystemTraversalMarker.None, manipulationMarker),
        errorHandler, cancellationToken);
    }

    #endregion
    #region Files direct delete methods

    public static TFileResult[] DeleteFiles<TFileItem, TFileResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, DeleteOptions> deleteParamsSelector,
      Func<FileInfo, FileSystemManipulationMarker, TFileResult> resultSelector,
      Func<FileInfo, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(deleteParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileItems, manipulationOptions, fileInfoSelector, deleteParamsSelector,
        (fileInfo, deleteParams) => { DeleteCore(fileInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileInfo, manipulationMarker, _) => resultSelector(fileInfo, manipulationMarker),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static TFileResult[] DeleteFiles<TFileItem, TFileResult>(
      this IEnumerable<TFileItem> fileItems, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<TFileItem, FileInfo> fileInfoSelector,
      Func<TFileItem, DeleteOptions> deleteParamsSelector,
      Func<FileInfo, FileSystemManipulationMarker, TFileResult> resultSelector,
      Func<FileInfo, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileItems);
      Argument.That.NotNull(fileInfoSelector);
      Argument.That.NotNull(deleteParamsSelector);
      Argument.That.NotNull(resultSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileItems, manipulationOptions, maxConcurrents, fileInfoSelector, deleteParamsSelector,
        (fileInfo, deleteParams) => { DeleteCore(fileInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileInfo, manipulationMarker, _) => resultSelector(fileInfo, manipulationMarker),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static InfoResult<FileInfo>[] DeleteFiles(
      this IEnumerable<FileInfo> fileInfos, FileSystemManipulationOptions manipulationOptions,
      Func<FileInfo, DeleteOptions> deleteParamsSelector,
      Func<FileInfo, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileInfos);
      Argument.That.NotNull(deleteParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileInfos, manipulationOptions,
        _ => _,
        fileInfo => deleteParamsSelector(fileInfo),
        (fileInfo, deleteParams) => { DeleteCore(fileInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileInfo, manipulationMarker, _) => fileInfo.ToInfoResult(FileSystemTraversalMarker.None, manipulationMarker),
        errorHandler, cancellationToken)
        .ToArray();
    }

    public static InfoResult<FileInfo>[] DeleteFiles(
      this IEnumerable<FileInfo> fileInfos, FileSystemManipulationOptions manipulationOptions, int maxConcurrents,
      Func<FileInfo, DeleteOptions> deleteParamsSelector,
      Func<FileInfo, DeleteOptions, Exception, bool>? errorHandler = null,
      CancellationToken cancellationToken = default)
    {
      Argument.That.NotNull(fileInfos);
      Argument.That.NotNull(deleteParamsSelector);

      var clearReadOnly = manipulationOptions.IsFlagsSet(FileSystemManipulationOptions.ClearReadOnly);

      return ManipulateFilesCore(fileInfos, manipulationOptions, maxConcurrents,
        _ => _,
        fileInfo => deleteParamsSelector(fileInfo),
        (fileInfo, deleteParams) => { DeleteCore(fileInfo, deleteParams, clearReadOnly); return Void.Value; },
        (fileInfo, manipulationMarker, _) => fileInfo.ToInfoResult(FileSystemTraversalMarker.None, manipulationMarker),
        errorHandler, cancellationToken)
        .ToArray();
    }

    #endregion
    #region Embedded types

    private sealed class ParentalDirectoriesContext : IReadOnlyList<DirectoryInfo>
    {
      private readonly IList<DirectoryInfo> _list = new List<DirectoryInfo>();
      private int _level;

      internal void Push(DirectoryInfo directoryInfo)
        => _list.Add(directoryInfo);

      internal void Pop()
      {
        _list.RemoveLast();
        if (_level > _list.Count)
          _level--;
      }

      internal int Total => _list.Count;

      internal ParentalDirectoriesContext AtLevel(int level)
      {
        _level = level;
        return this;
      }

      public int Count => _level;

      public DirectoryInfo this[int index] => _list[Argument.That.InRangeIn(_level, index)];

      public IEnumerator<DirectoryInfo> GetEnumerator()
        => _list.Enumerate(0, _level).GetEnumerator();

      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    #endregion
  }
}

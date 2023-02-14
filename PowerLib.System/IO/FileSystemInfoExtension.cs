using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PowerLib.System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Collections.Matching;
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
    #region Internal methods

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

    private static bool HasChildrenCore(FileSystemInfo fileSystemInfo, bool refresh)
    {
      if (fileSystemInfo is DirectoryInfo directoryInfo)
      {
        if (refresh)
          directoryInfo.Refresh();
        return directoryInfo.Exists && directoryInfo.EnumerateFileSystemInfos().Any();
      }
      return false;
    }

    private static int ProcessFileSystemInfoCore(DirectoryInfo sourceDirectoryInfo, bool refresh, Func<DirectoryInfo, IEnumerable<FileSystemInfo>> getter, Func<FileSystemInfo, bool> action)
    {
      int count = 0;
      foreach (var fileSystemInfo in getter(sourceDirectoryInfo))
      {
        if (refresh)
          fileSystemInfo.Refresh();
        if (action(fileSystemInfo))
          count++;
      }
      return count;
    }

    private static bool CopyCore(FileSystemInfo fileSystemInfo, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      var options = copying(fileSystemInfo);
      if (!options.Success || string.IsNullOrEmpty(options.Value.TargetPath) || !fileSystemInfo.Exists)
        return false;
      switch (fileSystemInfo)
      {
        case FileInfo fileInfo:
          var targetDirectory = Path.GetDirectoryName(options.Value.TargetPath);
          Argument.That.NotNull(targetDirectory);
          if (!Directory.Exists(targetDirectory) && options.Value.EnsureDirectory)
            Directory.CreateDirectory(targetDirectory);
          fileInfo.CopyTo(options.Value.TargetPath, options.Value.Overwrite);
          return true;
        default:
          return false;
      }
    }

    private static bool MoveCore(FileSystemInfo fileSystemInfo, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      var options = moving(fileSystemInfo);
      if (!options.Success || string.IsNullOrEmpty(options.Value.TargetPath) || !fileSystemInfo.Exists)
        return false;
      var targetDirectory = Path.GetDirectoryName(options.Value.TargetPath);
      Argument.That.NotNull(targetDirectory);
      switch (fileSystemInfo)
      {
        case FileInfo fileInfo:
          if (!Directory.Exists(targetDirectory) && options.Value.EnsureDirectory)
            Directory.CreateDirectory(targetDirectory);
          if (fileInfo.Attributes.IsFlagsSet(FileAttributes.ReadOnly) && options.Value.ClearReadOnly)
            fileInfo.Attributes = fileInfo.Attributes.InverseFlags(FileAttributes.ReadOnly);
#if NETCOREAPP3_0_OR_GREATER
          fileInfo.MoveTo(options.Value.TargetPath, options.Value.Overwrite);
#else
          fileInfo.MoveTo(options.Value.TargetPath);
#endif
          return true;
        case DirectoryInfo directoryInfo:
          if (!Directory.Exists(targetDirectory) && options.Value.EnsureDirectory)
            Directory.CreateDirectory(targetDirectory);
          if (directoryInfo.Attributes.IsFlagsSet(FileAttributes.ReadOnly) && options.Value.ClearReadOnly)
            directoryInfo.Attributes = directoryInfo.Attributes.InverseFlags(FileAttributes.ReadOnly);
          directoryInfo.MoveTo(options.Value.TargetPath);
          return true;
        default:
          return false;
      }
    }

    private static bool DeleteCore(FileSystemInfo fileSystemInfo, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      var options = deleting(fileSystemInfo);
      if (!options.Success || !fileSystemInfo.Exists)
        return false;
      switch (fileSystemInfo)
      {
        case FileInfo fileInfo:
          if (fileInfo.Attributes.IsFlagsSet(FileAttributes.ReadOnly) && options.Value.ClearReadOnly)
            fileInfo.Attributes = fileInfo.Attributes.InverseFlags(FileAttributes.ReadOnly);
          fileInfo.Delete();
          return true;
        case DirectoryInfo directoryInfo:
          if (directoryInfo.Attributes.IsFlagsSet(FileAttributes.ReadOnly) && options.Value.ClearReadOnly)
            directoryInfo.Attributes = directoryInfo.Attributes.InverseFlags(FileAttributes.ReadOnly);
          directoryInfo.Delete(options.Value.Recursive);
          return true;
        default:
          return false;
      }
    }

    private static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosCore(DirectoryInfo parentDirectoryInfo, int maxDepth, bool excludeEmpty,
      Func<DirectoryInfo, IEnumerable<FileSystemInfo>> getChildren, Predicate<FileSystemInfo> hasChildren,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      if (maxDepth == 0 || !hasChildren(parentDirectoryInfo))
        yield break;
      var children = getChildren(parentDirectoryInfo);
      if (predicate is not null)
        children = children.Where(predicate.Invoke);
      if (comparison is not null)
        children = children.Sort(comparison);
      foreach (FileSystemInfo fileSystemInfo in children)
      {
        if (!hasChildren(fileSystemInfo))
        {
          if (!(excludeEmpty && fileSystemInfo is DirectoryInfo))
            yield return fileSystemInfo;
        }
        else if (fileSystemInfo.Exists)
        {
          if (fileSystemInfo is DirectoryInfo directoryInfo)
          {
            using var enumerator = EnumerateFileSystemInfosCore(directoryInfo, maxDepth - 1, excludeEmpty, getChildren, hasChildren, predicate, comparison)
              .GetEnumerator();
            if (enumerator.MoveNext())
            {
              yield return fileSystemInfo;
              if (maxDepth == 1)
                continue;
              else
                yield return enumerator.Current;
              while (enumerator.MoveNext())
                yield return enumerator.Current;
            }
            else if (!excludeEmpty)
              yield return fileSystemInfo;
          }
          else
            yield return fileSystemInfo;
        }
        else if (!excludeEmpty)
          yield return fileSystemInfo;
      }
    }

    private static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosCore<TFileSystemInfo>(DirectoryInfo startDirectoryInfo, int maxDepth, bool excludeEmpty,
      Func<DirectoryInfo, IEnumerable<FileSystemInfo>> getChildren, Predicate<FileSystemInfo> hasChildren,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison,
      Func<FileSystemInfo, TryOut<TFileSystemInfo>> selector)
    {
      if (maxDepth == 0 || !hasChildren(startDirectoryInfo))
        yield break;
      var children = getChildren(startDirectoryInfo);
      if (predicate is not null)
        children = children.Where(predicate.Invoke);
      if (comparison is not null)
        children = children.Sort(comparison);
      foreach (FileSystemInfo fileSystemInfo in children)
      {
        if (!hasChildren(fileSystemInfo))
        {
          if (!(excludeEmpty && fileSystemInfo is DirectoryInfo))
          {
            var result = selector(fileSystemInfo);
            if (result.Success)
              yield return result.Value!;
          }
        }
        else if (fileSystemInfo.Exists)
        {
          var directoryInfo = fileSystemInfo as DirectoryInfo;
          if (directoryInfo is not null)
          {
            using var enumerator = EnumerateFileSystemInfosCore(directoryInfo, maxDepth - 1, excludeEmpty, getChildren, hasChildren, predicate, comparison, selector)
              .GetEnumerator();
            if (enumerator.MoveNext())
            {
              var result = selector(fileSystemInfo);
              if (result.Success)
                yield return result.Value!;
              if (maxDepth == 1)
                continue;
              else
                yield return enumerator.Current;
              while (enumerator.MoveNext())
                yield return enumerator.Current;
            }
            else if (!excludeEmpty)
            {
              var result = selector(fileSystemInfo);
              if (result.Success)
                yield return result.Value!;
            }
          }
          else
          {
            var result = selector(fileSystemInfo);
            if (result.Success)
              yield return result.Value!;
          }
        }
        else if (!excludeEmpty)
        {
          var result = selector(fileSystemInfo);
          if (result.Success)
            yield return result.Value!;
        }
      }
    }

    private static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosCore(DirectoryInfo startDirectoryInfo,
      ParentalDirectoriesContext parentalDirectoriesContext, int maxDepth, bool excludeEmpty,
      Func<IReadOnlyList<DirectoryInfo>, IEnumerable<FileSystemInfo>> getChildren, Predicate<FileSystemInfo> hasChildren,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      if (maxDepth == 0 || !hasChildren(startDirectoryInfo))
        yield break;
      parentalDirectoriesContext.Push(startDirectoryInfo);
      var level = parentalDirectoriesContext.Total;
      var children = getChildren(parentalDirectoriesContext.AtLevel(level));
      if (predicate is not null)
        children = children.Where(fileSystemInfo => predicate((fileSystemInfo, parentalDirectoriesContext.AtLevel(level))));
      if (comparison is not null)
        children = children.Sort(comparison);
      foreach (FileSystemInfo fileSystemInfo in children)
      {
        if (!hasChildren(fileSystemInfo))
        {
          if (!(excludeEmpty && fileSystemInfo is DirectoryInfo))
            yield return fileSystemInfo;
        }
        else if (fileSystemInfo.Exists)
        {
          if (fileSystemInfo is DirectoryInfo directoryInfo)
          {
            using var enumerator = EnumerateFileSystemInfosCore(directoryInfo, parentalDirectoriesContext, maxDepth - 1, excludeEmpty, getChildren, hasChildren, predicate, comparison)
              .GetEnumerator();
            if (enumerator.MoveNext())
            {
              yield return fileSystemInfo!;
              if (maxDepth == 1)
                continue;
              else
                yield return enumerator.Current;
              while (enumerator.MoveNext())
                yield return enumerator.Current;
            }
            else if (!excludeEmpty)
              yield return fileSystemInfo!;
          }
          else
            yield return fileSystemInfo!;
        }
        else if (!excludeEmpty)
          yield return fileSystemInfo!;
      }
      parentalDirectoriesContext.Pop();
    }

    private static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosCore<TFileSystemInfo>(DirectoryInfo startDirectoryInfo,
      ParentalDirectoriesContext parentalDirectoriesContext, int maxDepth, bool excludeEmpty,
      Func<IReadOnlyList<DirectoryInfo>, IEnumerable<FileSystemInfo>> getChildren, Predicate<FileSystemInfo> hasChildren,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TryOut<TFileSystemInfo>> selector)
    {
      if (maxDepth == 0 || !hasChildren(startDirectoryInfo))
        yield break;
      parentalDirectoriesContext.Push(startDirectoryInfo);
      var level = parentalDirectoriesContext.Total;
      var children = getChildren(parentalDirectoriesContext.AtLevel(level));
      if (predicate is not null)
        children = children.Where(fileSystemInfo => predicate((fileSystemInfo, parentalDirectoriesContext.AtLevel(level))));
      if (comparison is not null)
        children = children.Sort(comparison);
      foreach (FileSystemInfo fileSystemInfo in children)
      {
        if (!hasChildren(fileSystemInfo))
        {
          if (!(excludeEmpty && fileSystemInfo is DirectoryInfo))
          {
            var result = selector(fileSystemInfo, parentalDirectoriesContext.AtLevel(level));
            if (result.Success)
              yield return result.Value!;
          }
        }
        else if (fileSystemInfo.Exists)
        {
          var directoryInfo = fileSystemInfo as DirectoryInfo;
          if (directoryInfo is not null)
          {
            using var enumerator = EnumerateFileSystemInfosCore(directoryInfo, parentalDirectoriesContext, maxDepth - 1, excludeEmpty, getChildren, hasChildren, predicate, comparison, selector)
              .GetEnumerator();
            if (enumerator.MoveNext())
            {
              var result = selector(fileSystemInfo, parentalDirectoriesContext.AtLevel(level));
              if (result.Success)
                yield return result.Value!;
              if (maxDepth == 1)
                continue;
              else
                yield return enumerator.Current;
              while (enumerator.MoveNext())
                yield return enumerator.Current;
            }
            else if (!excludeEmpty)
            {
              var result = selector(fileSystemInfo, parentalDirectoriesContext.AtLevel(level));
              if (result.Success)
                yield return result.Value!;
            }
          }
          else
          {
            var result = selector(fileSystemInfo, parentalDirectoriesContext.AtLevel(level));
            if (result.Success)
              yield return result.Value!;
          }
        }
        else if (!excludeEmpty)
        {
          var result = selector(fileSystemInfo, parentalDirectoriesContext.AtLevel(level));
          if (result.Success)
            yield return result.Value!;
        }
      }
      parentalDirectoriesContext.Pop();
    }

    private static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosCore(DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      var excludeStart = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeStartDirectory);
      var excludeEmpty = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeEmptyDirectory);
      var reverse = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Reverse);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, maxDepth, excludeEmpty,
        directoryInfo => GetChildrenCore(directoryInfo, searchPatternSelector),
        fileSystemInfo => HasChildrenCore(fileSystemInfo, refresh),
        predicate, comparison);
      if (reverse)
        children = children.Reverse();
      using var enumerator = children.GetEnumerator();
      if (enumerator.MoveNext())
      {
        if (!excludeStart && !reverse)
          yield return startDirectoryInfo;
        do
          yield return enumerator.Current;
        while (enumerator.MoveNext());
        if (!excludeStart && reverse)
          yield return startDirectoryInfo;
      }
      else if (!excludeEmpty && !excludeStart)
        yield return startDirectoryInfo;
    }

    private static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosCore<TFileSystemInfo>(DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, TFileSystemInfo> selector)
    {
      var excludeStart = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeStartDirectory);
      var excludeEmpty = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeEmptyDirectory);
      var reverse = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Reverse);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, maxDepth, excludeEmpty,
        directoryInfo => GetChildrenCore(directoryInfo, searchPatternSelector),
        fileSystemInfo => HasChildrenCore(fileSystemInfo, refresh),
        predicate, comparison,
        (fileSystemInfo) => TryOut.Success(selector(fileSystemInfo)));
      if (reverse)
        children = children.Reverse();
      using var enumerator = children.GetEnumerator();
      if (enumerator.MoveNext())
      {
        if (!excludeStart && !reverse)
          yield return selector(startDirectoryInfo);
        do
          yield return enumerator.Current;
        while (enumerator.MoveNext());
        if (!excludeStart && reverse)
          yield return selector(startDirectoryInfo);
      }
      else if (!excludeEmpty && !excludeStart)
        yield return selector(startDirectoryInfo);
    }

    private static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosCore(DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      var excludeStart = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeStartDirectory);
      var excludeEmpty = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeEmptyDirectory);
      var reverse = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Reverse);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var parentalDirectoriesContext = new ParentalDirectoriesContext();
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, parentalDirectoriesContext, maxDepth, excludeEmpty,
        parentalDirectoryInfos => GetChildrenCore(parentalDirectoryInfos, searchPatternSelector),
        fileSystemInfo => HasChildrenCore(fileSystemInfo, refresh),
        predicate, comparison);
      if (reverse)
        children = children.Reverse();
      using var enumerator = children.GetEnumerator();
      if (enumerator.MoveNext())
      {
        if (!excludeStart && !reverse)
          yield return startDirectoryInfo;
        do
          yield return enumerator.Current;
        while (enumerator.MoveNext());
        if (!excludeStart && reverse)
          yield return startDirectoryInfo;
      }
      else if (!excludeEmpty && !excludeStart)
        yield return startDirectoryInfo;
    }

    private static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosCore<TFileSystemInfo>(DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
    {
      var excludeStart = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeStartDirectory);
      var excludeEmpty = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeEmptyDirectory);
      var reverse = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Reverse);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var parentalDirectoriesContext = new ParentalDirectoriesContext();
      var level = parentalDirectoriesContext.Total;
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, parentalDirectoriesContext, maxDepth, excludeEmpty,
        parentalDirectoryInfos => GetChildrenCore(parentalDirectoryInfos, searchPatternSelector),
        fileSystemInfo => HasChildrenCore(fileSystemInfo, refresh),
        predicate, comparison,
        (fileSystemInfo, parentalDirectoryInfos) => TryOut.Success(selector(fileSystemInfo, parentalDirectoryInfos)));
      if (reverse)
        children = children.Reverse();
      using var enumerator = children.GetEnumerator();
      if (enumerator.MoveNext())
      {
        if (!excludeStart && !reverse)
          yield return selector(startDirectoryInfo, parentalDirectoriesContext.AtLevel(level));
        do
          yield return enumerator.Current;
        while (enumerator.MoveNext());
        if (!excludeStart && reverse)
          yield return selector(startDirectoryInfo, parentalDirectoriesContext.AtLevel(level));
      }
      else if (!excludeEmpty && !excludeStart)
        yield return selector(startDirectoryInfo, parentalDirectoriesContext.AtLevel(level));
    }

    private static IEnumerable<DirectoryInfo> EnumerateDirectoriesCore(DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      var excludeStart = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeStartDirectory);
      var excludeEmpty = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeEmptyDirectory);
      var reverse = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Reverse);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, maxDepth, excludeEmpty,
        directoryInfo => GetChildDirectoriesCore(directoryInfo, searchPatternSelector),
        fileSystemInfo => HasChildrenCore(fileSystemInfo, refresh),
        fileSystemInfo => fileSystemInfo is DirectoryInfo directoryInfo && (predicate?.Invoke(directoryInfo) ?? true),
        comparison is not null ? (x, y) => comparison((DirectoryInfo)x, (DirectoryInfo)y) : default)
        .Cast<DirectoryInfo>();
      if (reverse)
        children = children.Reverse();
      using var enumerator = children.GetEnumerator();
      if (enumerator.MoveNext())
      {
        if (!excludeStart && !reverse)
          yield return startDirectoryInfo;
        do
          yield return enumerator.Current;
        while (enumerator.MoveNext());
        if (!excludeStart && reverse)
          yield return startDirectoryInfo;
      }
      else if (!excludeEmpty && !excludeStart)
        yield return startDirectoryInfo;
    }

    private static IEnumerable<TDirectoryInfo> EnumerateDirectoriesCore<TDirectoryInfo>(DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, TDirectoryInfo> selector)
    {
      var excludeStart = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeStartDirectory);
      var excludeEmpty = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeEmptyDirectory);
      var reverse = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Reverse);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, maxDepth, excludeEmpty,
        directoryInfo => GetChildDirectoriesCore(directoryInfo, searchPatternSelector),
        fileSystemInfo => HasChildrenCore(fileSystemInfo, refresh),
        fileSystemInfo => fileSystemInfo is DirectoryInfo directoryInfo && (predicate?.Invoke(directoryInfo) ?? true),
        comparison is not null ? (x, y) => comparison((DirectoryInfo)x, (DirectoryInfo)y) : default,
        fileSystemInfo => fileSystemInfo is DirectoryInfo directoryInfo ? TryOut.Success(selector(directoryInfo)) : TryOut.Failure<TDirectoryInfo>());
      if (reverse)
        children = children.Reverse();
      using var enumerator = children.GetEnumerator();
      if (enumerator.MoveNext())
      {
        if (!excludeStart && !reverse)
          yield return selector(startDirectoryInfo);
        do
          yield return enumerator.Current;
        while (enumerator.MoveNext());
        if (!excludeStart && reverse)
          yield return selector(startDirectoryInfo);
      }
      else if (!excludeEmpty && !excludeStart)
        yield return selector(startDirectoryInfo);
    }

    private static IEnumerable<DirectoryInfo> EnumerateDirectoriesCore(DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);

      var excludeStart = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeStartDirectory);
      var excludeEmpty = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeEmptyDirectory);
      var reverse = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Reverse);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var parentalDirectoriesContext = new ParentalDirectoriesContext();
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, parentalDirectoriesContext, maxDepth, excludeEmpty,
        parentalDirectoryInfos => GetChildDirectoriesCore(parentalDirectoryInfos, searchPatternSelector),
        fileSystemInfo => HasChildrenCore(fileSystemInfo, refresh),
        ((FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos) element) =>
          element.fileSystemInfo is DirectoryInfo directoryInfo && (predicate?.Invoke((directoryInfo, element.parentalDirectoryInfos)) ?? true),
        comparison is not null ? (x, y) => comparison((DirectoryInfo)x, (DirectoryInfo)y) : default)
        .Cast<DirectoryInfo>();
      if (reverse)
        children = children.Reverse();
      using var enumerator = children.GetEnumerator();
      if (enumerator.MoveNext())
      {
        if (!excludeStart && !reverse)
          yield return startDirectoryInfo;
        do
          yield return enumerator.Current;
        while (enumerator.MoveNext());
        if (!excludeStart && reverse)
          yield return startDirectoryInfo;
      }
      else if (!excludeEmpty && !excludeStart)
        yield return startDirectoryInfo;
    }

    private static IEnumerable<TDirectoryInfo> EnumerateDirectoriesCore<TDirectoryInfo>(DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
    {
      var excludeStart = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeStartDirectory);
      var excludeEmpty = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeEmptyDirectory);
      var reverse = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Reverse);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var parentalDirectoriesContext = new ParentalDirectoriesContext();
      var level = parentalDirectoriesContext.Total;
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, parentalDirectoriesContext, maxDepth, excludeEmpty,
        parentalDirectoryInfos => GetChildDirectoriesCore(parentalDirectoryInfos, searchPatternSelector),
        fileSystemInfo => HasChildrenCore(fileSystemInfo, refresh),
        ((FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos) element) =>
          element.fileSystemInfo is DirectoryInfo directoryInfo && (predicate?.Invoke((directoryInfo, element.parentalDirectoryInfos)) ?? true),
        comparison is not null ? (x, y) => comparison((DirectoryInfo)x, (DirectoryInfo)y) : default,
        (FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)
          => fileSystemInfo is DirectoryInfo directoryInfo ? TryOut.Success(selector(directoryInfo, parentalDirectoryInfos)) : TryOut.Failure<TDirectoryInfo>());
      if (reverse)
        children = children.Reverse();
      using var enumerator = children.GetEnumerator();
      if (enumerator.MoveNext())
      {
        if (!excludeStart && !reverse)
          yield return selector(startDirectoryInfo, parentalDirectoriesContext.AtLevel(level));
        do
          yield return enumerator.Current;
        while (enumerator.MoveNext());
        if (!excludeStart && reverse)
          yield return selector(startDirectoryInfo, parentalDirectoriesContext.AtLevel(level));
      }
      else if (!excludeEmpty && !excludeStart)
        yield return selector(startDirectoryInfo, parentalDirectoriesContext.AtLevel(level));
    }

    private static IEnumerable<FileInfo> EnumerateFilesCore(DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
    {
      var reverse = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Reverse);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, maxDepth, true,
        directoryInfo => GetChildrenCore(directoryInfo, searchPatternSelector),
        fileSystemInfo => HasChildrenCore(fileSystemInfo, refresh),
        (FileSystemInfo fileSystemInfo) =>
          fileSystemInfo is DirectoryInfo directoryInfo && (directoryPredicate?.Invoke(directoryInfo) ?? true) ||
          fileSystemInfo is FileInfo fileInfo && (filePredicate?.Invoke(fileInfo) ?? true),
        comparison)
        .OfType<FileInfo>();
      if (reverse)
        children = children.Reverse();
      return children;
    }

    private static IEnumerable<TFileInfo> EnumerateFilesCore<TFileInfo>(DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, TFileInfo> selector)
    {
      var reverse = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Reverse);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, maxDepth, true,
        directoryInfo => GetChildrenCore(directoryInfo, searchPatternSelector),
        fileSystemInfo => HasChildrenCore(fileSystemInfo, refresh),
        fileSystemInfo =>
          fileSystemInfo is DirectoryInfo directoryInfo && (directoryPredicate?.Invoke(directoryInfo) ?? true) ||
          fileSystemInfo is FileInfo fileInfo && (filePredicate?.Invoke(fileInfo) ?? true),
        comparison,
        fileSystemInfo => fileSystemInfo is FileInfo fileInfo ? TryOut.Success(selector(fileInfo)) : TryOut.Failure<TFileInfo>());
      if (reverse)
        children = children.Reverse();
      return children;
    }

    private static IEnumerable<FileInfo> EnumerateFilesCore(DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison)
    {
      var reverse = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Reverse);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var parentalDirectoriesContext = new ParentalDirectoriesContext();
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, parentalDirectoriesContext, maxDepth, true,
        parentalDirectoryInfos => GetChildrenCore(parentalDirectoryInfos, searchPatternSelector),
        fileSystemInfo => HasChildrenCore(fileSystemInfo, refresh),
        ((FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos) element) =>
          element.fileSystemInfo is DirectoryInfo directoryInfo && (directoryPredicate?.Invoke((directoryInfo, element.parentalDirectoryInfos)) ?? true) ||
          element.fileSystemInfo is FileInfo fileInfo && (filePredicate?.Invoke((fileInfo, element.parentalDirectoryInfos)) ?? true),
        comparison)
        .OfType<FileInfo>();
      if (reverse)
        children = children.Reverse();
      return children;
    }

    private static IEnumerable<TFileInfo> EnumerateFilesCore<TFileInfo>(DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate,
      Comparison<FileSystemInfo>? comparison, Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
    {
      var reverse = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Reverse);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var parentalDirectoriesContext = new ParentalDirectoriesContext();
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, parentalDirectoriesContext, maxDepth, true,
        parentalDirectoryInfos => GetChildrenCore(parentalDirectoryInfos, searchPatternSelector),
        fileSystemInfo => HasChildrenCore(fileSystemInfo, refresh),
        ((FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos) element) =>
          element.fileSystemInfo is DirectoryInfo directoryInfo && (directoryPredicate?.Invoke((directoryInfo, element.parentalDirectoryInfos)) ?? true) ||
          element.fileSystemInfo is FileInfo fileInfo && (filePredicate?.Invoke((fileInfo, element.parentalDirectoryInfos)) ?? true),
        comparison,
        (fileSystemInfo, parentalDirectoryInfos) => fileSystemInfo is FileInfo fileInfo ? TryOut.Success(selector(fileInfo, parentalDirectoryInfos)) : TryOut.Failure<TFileInfo>());
      if (reverse)
        children = children.Reverse();
      return children;
    }

    private static int CopyCore(DirectoryInfo sourceDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      return ProcessFileSystemInfoCore(sourceDirectoryInfo, traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh),
        directoryInfo => EnumerateFileSystemInfosCore(directoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate,
          new SelectComparer<FileSystemInfo, bool>(fileSystemInfo => fileSystemInfo is FileInfo).Compare),
        fileSystemInfo => CopyCore(fileSystemInfo, copying));
    }

    private static int CopyCore(DirectoryInfo sourceDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      return ProcessFileSystemInfoCore(sourceDirectoryInfo, traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh),
        directoryInfo => EnumerateFileSystemInfosCore(directoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate,
          new SelectComparer<FileSystemInfo, bool>(fileSystemInfo => fileSystemInfo is FileInfo).Compare),
        fileSystemInfo => CopyCore(fileSystemInfo, copying));
    }

    private static int MoveCore(DirectoryInfo sourceDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      return ProcessFileSystemInfoCore(sourceDirectoryInfo, traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh),
        directoryInfo => EnumerateFileSystemInfosCore(directoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate,
          new SelectComparer<FileSystemInfo, bool>(fileSystemInfo => fileSystemInfo is FileInfo).Compare),
        fileSystemInfo => MoveCore(fileSystemInfo, moving));
    }

    private static int MoveCore(DirectoryInfo sourceDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      return ProcessFileSystemInfoCore(sourceDirectoryInfo, traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh),
        directoryInfo => EnumerateFileSystemInfosCore(directoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate,
          new SelectComparer<FileSystemInfo, bool>(fileSystemInfo => fileSystemInfo is FileInfo).Compare),
        fileSystemInfo => MoveCore(fileSystemInfo, moving));
    }

    private static int DeleteCore(DirectoryInfo sourceDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      return ProcessFileSystemInfoCore(sourceDirectoryInfo, traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh),
        directoryInfo => EnumerateFileSystemInfosCore(directoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate,
          new SelectComparer<FileSystemInfo, bool>(fileSystemInfo => fileSystemInfo is FileInfo).Compare),
        fileSystemInfo => DeleteCore(fileSystemInfo, deleting));
    }

    private static int DeleteCore(DirectoryInfo sourceDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      return ProcessFileSystemInfoCore(sourceDirectoryInfo, traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh),
        directoryInfo => EnumerateFileSystemInfosCore(directoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate,
          new SelectComparer<FileSystemInfo, bool>(fileSystemInfo => fileSystemInfo is FileInfo).Compare),
        fileSystemInfo => DeleteCore(fileSystemInfo, deleting));
    }

    #endregion
    #region Enumerate file system items

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, default(Predicate<FileSystemInfo>), null);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<FileSystemInfo>), null);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, default(Predicate<FileSystemInfo>), null);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<FileSystemInfo>), null);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, default(Predicate<FileSystemInfo>), null);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<FileSystemInfo>), null);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, default(Predicate<FileSystemInfo>), null);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, predicate, comparison);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, predicate, comparison);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, predicate, comparison);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate, comparison);
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
      Func<FileSystemInfo, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, default(Predicate<FileSystemInfo>), null, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<FileSystemInfo>), null, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, default(Predicate<FileSystemInfo>), null, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<FileSystemInfo>), null, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, default(Predicate<FileSystemInfo>), null, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<FileSystemInfo>), null, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, default(Predicate<FileSystemInfo>), null, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    #endregion
    #region Get custom file system items

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(traversalOptions, default(Predicate<FileSystemInfo>), null, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchOption, traversalOptions, default(Predicate<FileSystemInfo>), null, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(maxDepth, traversalOptions, default(Predicate<FileSystemInfo>), null, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, searchOption, traversalOptions, default(Predicate<FileSystemInfo>), null, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, maxDepth, traversalOptions, default(Predicate<FileSystemInfo>), null, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, searchOption, traversalOptions, default(Predicate<FileSystemInfo>), null, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, maxDepth, traversalOptions, default(Predicate<FileSystemInfo>), null, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfos<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPatternSelector, maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    #endregion
    #region Enumerate file system items with parental context

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, null, null);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, null, null);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, predicate, comparison);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, predicate, comparison);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, predicate, comparison);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison);
    }

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate, comparison);
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
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, searchOption, traversalOptions, default(Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>), null).ToArray();

    public static FileSystemInfo[] GetFileSystemInfosEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, maxDepth, traversalOptions, default(Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>), null).ToArray();

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
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>), null, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, default(Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>), null, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFileSystemInfosCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    #endregion
    #region Get custom file system items with parental context

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, searchOption, traversalOptions, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, maxDepth, traversalOptions, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPatternSelector, maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    #endregion
    #region Enumerate directories

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, default(Predicate<DirectoryInfo>), null);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<DirectoryInfo>), null);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, default(Predicate<DirectoryInfo>), null);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);

      return EnumerateDirectoriesCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<DirectoryInfo>), null);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);

      return EnumerateDirectoriesCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, default(Predicate<DirectoryInfo>), null);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);

      return EnumerateDirectoriesCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<DirectoryInfo>), null);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);

      return EnumerateDirectoriesCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, default(Predicate<DirectoryInfo>), null);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, predicate, comparison);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, predicate, comparison);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);

      return EnumerateDirectoriesCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);

      return EnumerateDirectoriesCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, predicate, comparison);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);

      return EnumerateDirectoriesCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);

      return EnumerateDirectoriesCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate, comparison);
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
      Func<DirectoryInfo, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, default(Predicate<DirectoryInfo>), null, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<DirectoryInfo>), null, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, default(Predicate<DirectoryInfo>), null, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<DirectoryInfo>), null, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, default(Predicate<DirectoryInfo>), null, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<DirectoryInfo>), null, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, default(Predicate<DirectoryInfo>), null, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    #endregion
    #region Get custom directories

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(traversalOptions, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchOption, traversalOptions, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(maxDepth, traversalOptions, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, searchOption, traversalOptions, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, maxDepth, traversalOptions, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, searchOption, traversalOptions, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, maxDepth, traversalOptions, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectories<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectories(searchPatternSelector, maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    #endregion
    #region Enumerate directories with parental context

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);

      return EnumerateDirectoriesCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>), null);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);

      return EnumerateDirectoriesCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, default(Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>), null);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, predicate, comparison);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, predicate, comparison);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);

      return EnumerateDirectoriesCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);

      return EnumerateDirectoriesCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, predicate, comparison);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);

      return EnumerateDirectoriesCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison);
    }

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);

      return EnumerateDirectoriesCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate, comparison);
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
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>), null, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, default(Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>), null, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateDirectoriesCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate, comparison, selector);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    #endregion
    #region Get custom directories with parental context

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, searchOption, traversalOptions, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, maxDepth, traversalOptions, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPatternSelector, maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    #endregion
    #region Enumerate files

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, default(Predicate<FileInfo>), null, null);
    }

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<FileInfo>), null, null);
    }

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, default(Predicate<FileInfo>), null, null);
    }

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);

      return EnumerateFilesCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<FileInfo>), null, null);
    }

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFilesCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, default(Predicate<FileInfo>), null, null);
    }

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);

      return EnumerateFilesCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<FileInfo>), null, null);
    }

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFilesCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, default(Predicate<FileInfo>), null, null);
    }

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison);
    }

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison);
    }

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison);
    }

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);

      return EnumerateFilesCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison);
    }

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFilesCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison);
    }

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);

      return EnumerateFilesCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison);
    }

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFilesCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison);
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
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, default(Predicate<FileInfo>), null, null, selector);
    }

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<FileInfo>), null, null, selector);
    }

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, default(Predicate<FileInfo>), null, null, selector);
    }

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<FileInfo>), null, null, selector);
    }

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, default(Predicate<FileInfo>), null, null, selector);
    }

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<FileInfo>), null, null, selector);
    }

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, default(Predicate<FileInfo>), null, null, selector);
    }

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison, selector);
    }

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison, selector);
    }

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison, selector);
    }

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison, selector);
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

      return EnumerateFilesCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison, selector);
    }

    public static IEnumerable<TFileInfo> EnumerateFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison, selector);
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

      return EnumerateFilesCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison, selector);
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
      => startDirectoryInfo.EnumerateFiles(traversalOptions, default(Predicate<FileInfo>), null, null, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchOption, traversalOptions, default(Predicate<FileInfo>), null, null, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(maxDepth, traversalOptions, default(Predicate<FileInfo>), null, null, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchPattern, searchOption, traversalOptions, default(Predicate<FileInfo>), null, null, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchPattern, maxDepth, traversalOptions, default(Predicate<FileInfo>), null, null, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchPatternSelector, searchOption, traversalOptions, default(Predicate<FileInfo>), null, null, selector).ToArray();

    public static TFileInfo[] GetFiles<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFiles(searchPatternSelector, maxDepth, traversalOptions, default(Predicate<FileInfo>), null, null, selector).ToArray();

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
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);

      return EnumerateFilesCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>), null, null);
    }

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFilesCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, default(Predicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>), null, null);
    }

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison);
    }

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison);
    }

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison);
    }

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);

      return EnumerateFilesCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison);
    }

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFilesCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison);
    }

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);

      return EnumerateFilesCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison);
    }

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);

      return EnumerateFilesCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison);
    }

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison());

    #endregion
    #region Get files with parental context

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, searchOption, traversalOptions, default(Predicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>), null, null).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, maxDepth, traversalOptions, default(Predicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>), null, null).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFilesEx(traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFilesEx(searchOption, traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFilesEx(maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, searchOption, traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, searchOption, traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    #endregion
    #region Enumerate custom files with parental context

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>), null, null, selector);
    }

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, default(Predicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>), null, null, selector);
    }

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison, selector);
    }

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison, selector);
    }

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison, selector);
    }

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison, selector);
    }

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison, selector);
    }

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison, selector);
    }

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      return EnumerateFilesCore(startDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison, selector);
    }

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison(), selector);

    #endregion
    #region Get custom files with parental context

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, searchOption, traversalOptions, default(Predicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>), null, null, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, maxDepth, traversalOptions, default(Predicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>), null, null, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(traversalOptions, filePredicate, directoryPredicate, comparison, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchOption, traversalOptions, filePredicate, directoryPredicate, comparison, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, searchOption, traversalOptions, filePredicate, directoryPredicate, comparison, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, searchOption, traversalOptions, filePredicate, directoryPredicate, comparison, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, Predicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(traversalOptions, filePredicate, directoryPredicate, comparer, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchOption, traversalOptions, filePredicate, directoryPredicate, comparer, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(maxDepth, traversalOptions, filePredicate, directoryPredicate, comparer, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, searchOption, traversalOptions, filePredicate, directoryPredicate, comparer, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparer, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo fileInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, searchOption, traversalOptions, filePredicate, directoryPredicate, comparer, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, IPredicate<(DirectoryInfo directoryInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPatternSelector, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparer, selector).ToArray();

    #endregion
    #region Copy

    public static int Copy(this DirectoryInfo sourceDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(copying);

      return CopyCore(sourceDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, default(Predicate<FileSystemInfo>), copying);
    }

    public static int Copy(this DirectoryInfo sourceDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(copying);

      return CopyCore(sourceDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<FileSystemInfo>), copying);
    }

    public static int Copy(this DirectoryInfo sourceDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(copying);

      return CopyCore(sourceDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, default(Predicate<FileSystemInfo>), copying);
    }

    public static int Copy(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NotNull(copying);

      return CopyCore(sourceDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<FileSystemInfo>), copying);
    }

    public static int Copy(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(copying);

      return CopyCore(sourceDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, default(Predicate<FileSystemInfo>), copying);
    }

    public static int Copy(this DirectoryInfo sourceDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(copying);

      return CopyCore(sourceDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<FileSystemInfo>), copying);
    }

    public static int Copy(this DirectoryInfo sourceDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(copying);

      return CopyCore(sourceDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, default(Predicate<FileSystemInfo>), copying);
    }

    public static int Copy(this DirectoryInfo sourceDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(copying);

      return CopyCore(sourceDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, predicate, copying);
    }

    public static int Copy(this DirectoryInfo sourceDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(copying);

      return CopyCore(sourceDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, copying);
    }

    public static int Copy(this DirectoryInfo sourceDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(copying);

      return CopyCore(sourceDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, predicate, copying);
    }

    public static int Copy(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NotNull(copying);

      return CopyCore(sourceDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, copying);
    }

    public static int Copy(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(copying);

      return CopyCore(sourceDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, predicate, copying);
    }

    public static int Copy(this DirectoryInfo sourceDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(copying);

      return CopyCore(sourceDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, copying);
    }

    public static int Copy(this DirectoryInfo sourceDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(copying);

      return CopyCore(sourceDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate, copying);
    }

    public static int Copy(this DirectoryInfo sourceDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
      => sourceDirectoryInfo.Copy(traversalOptions, predicate?.AsPredicate(), copying);

    public static int Copy(this DirectoryInfo sourceDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
      => sourceDirectoryInfo.Copy(searchOption, traversalOptions, predicate?.AsPredicate(), copying);

    public static int Copy(this DirectoryInfo sourceDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
      => sourceDirectoryInfo.Copy(maxDepth, traversalOptions, predicate?.AsPredicate(), copying);

    public static int Copy(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
      => sourceDirectoryInfo.Copy(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), copying);

    public static int Copy(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
      => sourceDirectoryInfo.Copy(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), copying);

    public static int Copy(this DirectoryInfo sourceDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
      => sourceDirectoryInfo.Copy(searchPatternSelector, searchOption, traversalOptions, predicate?.AsPredicate(), copying);

    public static int Copy(this DirectoryInfo sourceDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
      => sourceDirectoryInfo.Copy(searchPatternSelector, maxDepth, traversalOptions, predicate?.AsPredicate(), copying);

    #endregion
    #region Copy extended

    public static int CopyEx(this DirectoryInfo sourceDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(copying);

      return CopyCore(sourceDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>), copying);
    }

    public static int CopyEx(this DirectoryInfo sourceDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(copying);

      return CopyCore(sourceDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, default(Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>), copying);
    }

    public static int CopyEx(this DirectoryInfo sourceDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(copying);

      return CopyCore(sourceDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, predicate, copying);
    }

    public static int CopyEx(this DirectoryInfo sourceDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(copying);

      return CopyCore(sourceDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, copying);
    }

    public static int CopyEx(this DirectoryInfo sourceDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(copying);

      return CopyCore(sourceDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, predicate, copying);
    }

    public static int CopyEx(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NotNull(copying);

      return CopyCore(sourceDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, copying);
    }

    public static int CopyEx(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(copying);

      return CopyCore(sourceDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, predicate, copying);
    }

    public static int CopyEx(this DirectoryInfo sourceDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(copying);

      return CopyCore(sourceDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, copying);
    }

    public static int CopyEx(this DirectoryInfo sourceDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(copying);

      return CopyCore(sourceDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate, copying);
    }

    public static int CopyEx(this DirectoryInfo sourceDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
      => sourceDirectoryInfo.CopyEx(traversalOptions, predicate?.AsPredicate(), copying);

    public static int CopyEx(this DirectoryInfo sourceDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
      => sourceDirectoryInfo.CopyEx(searchOption, traversalOptions, predicate?.AsPredicate(), copying);

    public static int CopyEx(this DirectoryInfo sourceDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
      => sourceDirectoryInfo.CopyEx(maxDepth, traversalOptions, predicate?.AsPredicate(), copying);

    public static int CopyEx(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
      => sourceDirectoryInfo.CopyEx(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), copying);

    public static int CopyEx(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
      => sourceDirectoryInfo.CopyEx(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), copying);

    public static int CopyEx(this DirectoryInfo sourceDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
      => sourceDirectoryInfo.CopyEx(searchPatternSelector, searchOption, traversalOptions, predicate?.AsPredicate(), copying);

    public static int CopyEx(this DirectoryInfo sourceDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
      => sourceDirectoryInfo.CopyEx(searchPatternSelector, maxDepth, traversalOptions, predicate?.AsPredicate(), copying);

    #endregion
    #region Move

    public static int Move(this DirectoryInfo sourceDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(moving);

      return MoveCore(sourceDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, default(Predicate<FileSystemInfo>), moving);
    }

    public static int Move(this DirectoryInfo sourceDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(moving);

      return MoveCore(sourceDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<FileSystemInfo>), moving);
    }

    public static int Move(this DirectoryInfo sourceDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(moving);

      return MoveCore(sourceDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, default(Predicate<FileSystemInfo>), moving);
    }

    public static int Move(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NotNull(moving);

      return MoveCore(sourceDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<FileSystemInfo>), moving);
    }

    public static int Move(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(moving);

      return MoveCore(sourceDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, default(Predicate<FileSystemInfo>), moving);
    }

    public static int Move(this DirectoryInfo sourceDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(moving);

      return MoveCore(sourceDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<FileSystemInfo>), moving);
    }

    public static int Move(this DirectoryInfo sourceDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(moving);

      return MoveCore(sourceDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, default(Predicate<FileSystemInfo>), moving);
    }

    public static int Move(this DirectoryInfo sourceDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(moving);

      return MoveCore(sourceDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, predicate, moving);
    }

    public static int Move(this DirectoryInfo sourceDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(moving);

      return MoveCore(sourceDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, moving);
    }

    public static int Move(this DirectoryInfo sourceDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(moving);

      return MoveCore(sourceDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, predicate, moving);
    }

    public static int Move(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NotNull(moving);

      return MoveCore(sourceDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, moving);
    }

    public static int Move(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(moving);

      return MoveCore(sourceDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, predicate, moving);
    }

    public static int Move(this DirectoryInfo sourceDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(moving);

      return MoveCore(sourceDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, moving);
    }

    public static int Move(this DirectoryInfo sourceDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(moving);

      return MoveCore(sourceDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate, moving);
    }

    public static int Move(this DirectoryInfo sourceDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
      => sourceDirectoryInfo.Move(traversalOptions, predicate?.AsPredicate(), moving);

    public static int Move(this DirectoryInfo sourceDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
      => sourceDirectoryInfo.Move(searchOption, traversalOptions, predicate?.AsPredicate(), moving);

    public static int Move(this DirectoryInfo sourceDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
      => sourceDirectoryInfo.Move(maxDepth, traversalOptions, predicate?.AsPredicate(), moving);

    public static int Move(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
      => sourceDirectoryInfo.Move(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), moving);

    public static int Move(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
      => sourceDirectoryInfo.Move(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), moving);

    public static int Move(this DirectoryInfo sourceDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
      => sourceDirectoryInfo.Move(searchPatternSelector, searchOption, traversalOptions, predicate?.AsPredicate(), moving);

    public static int Move(this DirectoryInfo sourceDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
      => sourceDirectoryInfo.Move(searchPatternSelector, maxDepth, traversalOptions, predicate?.AsPredicate(), moving);

    #endregion
    #region Move extended

    public static int MoveEx(this DirectoryInfo sourceDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(moving);

      return MoveCore(sourceDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>), moving);
    }

    public static int MoveEx(this DirectoryInfo sourceDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(moving);

      return MoveCore(sourceDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, default(Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>), moving);
    }

    public static int MoveEx(this DirectoryInfo sourceDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(moving);

      return MoveCore(sourceDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, predicate, moving);
    }

    public static int MoveEx(this DirectoryInfo sourceDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(moving);

      return MoveCore(sourceDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, moving);
    }

    public static int MoveEx(this DirectoryInfo sourceDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(moving);

      return MoveCore(sourceDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, predicate, moving);
    }

    public static int MoveEx(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NotNull(moving);

      return MoveCore(sourceDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, moving);
    }

    public static int MoveEx(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(moving);

      return MoveCore(sourceDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, predicate, moving);
    }

    public static int MoveEx(this DirectoryInfo sourceDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(moving);

      return MoveCore(sourceDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, moving);
    }

    public static int MoveEx(this DirectoryInfo sourceDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(moving);

      return MoveCore(sourceDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate, moving);
    }

    public static int MoveEx(this DirectoryInfo sourceDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
      => sourceDirectoryInfo.MoveEx(traversalOptions, predicate?.AsPredicate(), moving);

    public static int MoveEx(this DirectoryInfo sourceDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
      => sourceDirectoryInfo.MoveEx(searchOption, traversalOptions, predicate?.AsPredicate(), moving);

    public static int MoveEx(this DirectoryInfo sourceDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
      => sourceDirectoryInfo.MoveEx(maxDepth, traversalOptions, predicate?.AsPredicate(), moving);

    public static int MoveEx(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
      => sourceDirectoryInfo.MoveEx(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), moving);

    public static int MoveEx(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
      => sourceDirectoryInfo.MoveEx(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), moving);

    public static int MoveEx(this DirectoryInfo sourceDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
      => sourceDirectoryInfo.MoveEx(searchPatternSelector, searchOption, traversalOptions, predicate?.AsPredicate(), moving);

    public static int MoveEx(this DirectoryInfo sourceDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
      => sourceDirectoryInfo.MoveEx(searchPatternSelector, maxDepth, traversalOptions, predicate?.AsPredicate(), moving);

    #endregion
    #region Delete

    public static int Delete(this DirectoryInfo sourceDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(deleting);

      return DeleteCore(sourceDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, default(Predicate<FileSystemInfo>), deleting);
    }

    public static int Delete(this DirectoryInfo sourceDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(deleting);

      return DeleteCore(sourceDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<FileSystemInfo>), deleting);
    }

    public static int Delete(this DirectoryInfo sourceDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(deleting);

      return DeleteCore(sourceDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, default(Predicate<FileSystemInfo>), deleting);
    }

    public static int Delete(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NotNull(deleting);

      return DeleteCore(sourceDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<FileSystemInfo>), deleting);
    }

    public static int Delete(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(deleting);

      return DeleteCore(sourceDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, default(Predicate<FileSystemInfo>), deleting);
    }

    public static int Delete(this DirectoryInfo sourceDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(deleting);

      return DeleteCore(sourceDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<FileSystemInfo>), deleting);
    }

    public static int Delete(this DirectoryInfo sourceDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(deleting);

      return DeleteCore(sourceDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, default(Predicate<FileSystemInfo>), deleting);
    }

    public static int Delete(this DirectoryInfo sourceDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(deleting);

      return DeleteCore(sourceDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, predicate, deleting);
    }

    public static int Delete(this DirectoryInfo sourceDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(deleting);

      return DeleteCore(sourceDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, deleting);
    }

    public static int Delete(this DirectoryInfo sourceDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(deleting);

      return DeleteCore(sourceDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, predicate, deleting);
    }

    public static int Delete(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NotNull(deleting);

      return DeleteCore(sourceDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, deleting);
    }

    public static int Delete(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(deleting);

      return DeleteCore(sourceDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, predicate, deleting);
    }

    public static int Delete(this DirectoryInfo sourceDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(deleting);

      return DeleteCore(sourceDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, deleting);
    }

    public static int Delete(this DirectoryInfo sourceDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(deleting);

      return DeleteCore(sourceDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate, deleting);
    }

    public static int Delete(this DirectoryInfo sourceDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
      => sourceDirectoryInfo.Delete(traversalOptions, predicate?.AsPredicate(), deleting);

    public static int Delete(this DirectoryInfo sourceDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
      => sourceDirectoryInfo.Delete(searchOption, traversalOptions, predicate?.AsPredicate(), deleting);

    public static int Delete(this DirectoryInfo sourceDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
      => sourceDirectoryInfo.Delete(maxDepth, traversalOptions, predicate?.AsPredicate(), deleting);

    public static int Delete(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
      => sourceDirectoryInfo.Delete(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), deleting);

    public static int Delete(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
      => sourceDirectoryInfo.Delete(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), deleting);

    public static int Delete(this DirectoryInfo sourceDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
      => sourceDirectoryInfo.Delete(searchPatternSelector, searchOption, traversalOptions, predicate?.AsPredicate(), deleting);

    public static int Delete(this DirectoryInfo sourceDirectoryInfo,
      Func<DirectoryInfo, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
      => sourceDirectoryInfo.Delete(searchPatternSelector, maxDepth, traversalOptions, predicate?.AsPredicate(), deleting);

    #endregion
    #region Delete extended

    public static int DeleteEx(this DirectoryInfo sourceDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(deleting);

      return DeleteCore(sourceDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, default(Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>), deleting);
    }

    public static int DeleteEx(this DirectoryInfo sourceDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(deleting);

      return DeleteCore(sourceDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, default(Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>), deleting);
    }

    public static int DeleteEx(this DirectoryInfo sourceDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(deleting);

      return DeleteCore(sourceDirectoryInfo, DefaultSearchPatternSelector, int.MaxValue, traversalOptions, predicate, deleting);
    }

    public static int DeleteEx(this DirectoryInfo sourceDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(deleting);

      return DeleteCore(sourceDirectoryInfo, DefaultSearchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, deleting);
    }

    public static int DeleteEx(this DirectoryInfo sourceDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(deleting);

      return DeleteCore(sourceDirectoryInfo, DefaultSearchPatternSelector, maxDepth, traversalOptions, predicate, deleting);
    }

    public static int DeleteEx(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NotNull(deleting);

      return DeleteCore(sourceDirectoryInfo, _ => searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, deleting);
    }

    public static int DeleteEx(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPattern);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(deleting);

      return DeleteCore(sourceDirectoryInfo, _ => searchPattern, maxDepth, traversalOptions, predicate, deleting);
    }

    public static int DeleteEx(this DirectoryInfo sourceDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NotNull(deleting);

      return DeleteCore(sourceDirectoryInfo, searchPatternSelector, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, deleting);
    }

    public static int DeleteEx(this DirectoryInfo sourceDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NotNull(searchPatternSelector);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(deleting);

      return DeleteCore(sourceDirectoryInfo, searchPatternSelector, maxDepth, traversalOptions, predicate, deleting);
    }

    public static int DeleteEx(this DirectoryInfo sourceDirectoryInfo,
      FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
      => sourceDirectoryInfo.DeleteEx(traversalOptions, predicate?.AsPredicate(), deleting);

    public static int DeleteEx(this DirectoryInfo sourceDirectoryInfo,
      SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
      => sourceDirectoryInfo.DeleteEx(searchOption, traversalOptions, predicate?.AsPredicate(), deleting);

    public static int DeleteEx(this DirectoryInfo sourceDirectoryInfo,
      int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
      => sourceDirectoryInfo.DeleteEx(maxDepth, traversalOptions, predicate?.AsPredicate(), deleting);

    public static int DeleteEx(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
      => sourceDirectoryInfo.DeleteEx(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), deleting);

    public static int DeleteEx(this DirectoryInfo sourceDirectoryInfo,
      string searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
      => sourceDirectoryInfo.DeleteEx(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), deleting);

    public static int DeleteEx(this DirectoryInfo sourceDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
      => sourceDirectoryInfo.DeleteEx(searchPatternSelector, searchOption, traversalOptions, predicate?.AsPredicate(), deleting);

    public static int DeleteEx(this DirectoryInfo sourceDirectoryInfo,
      Func<IReadOnlyList<DirectoryInfo>, string?> searchPatternSelector, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentalDirectoryInfos)>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
      => sourceDirectoryInfo.DeleteEx(searchPatternSelector, maxDepth, traversalOptions, predicate?.AsPredicate(), deleting);

    #endregion
    #region Embedded types

    private sealed class ParentalDirectoriesContext : IReadOnlyList<DirectoryInfo>
    {
      private IList<DirectoryInfo> _list = new List<DirectoryInfo>();
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

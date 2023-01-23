using System;
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

    /// <summary>
    /// Gets a value that indicates that this is a FileInfo object.
    /// </summary>
    /// <param name="fileSystemInfo">File system info object.</param>
    /// <returns>True if FileSystemInfo object represents FileInfo object, otherwise false.</returns>
    public static bool IsFile(this FileSystemInfo fileSystemInfo)
      => Argument.That.NotNull(fileSystemInfo) is FileInfo;

    /// <summary>
    /// Gets a value that indicates that this is a DirectoryInfo object.
    /// </summary>
    /// <param name="fileSystemInfo">File system info object.</param>
    /// <returns>True if FileSystemInfo object represents DirectoryInfo object, otherwise false.</returns>
    public static bool IsDirectory(this FileSystemInfo fileSystemInfo)
      => Argument.That.NotNull(fileSystemInfo) is DirectoryInfo;

    /// <summary>
    /// Gets a value that indicates that this is a FileInfo object and exists.
    /// </summary>
    /// <param name="fileSystemInfo">File system info object.</param>
    /// <returns>True if FileSystemInfo object represents FileInfo object and exists, otherwise false.</returns>
    public static bool IsFileAndExists(this FileSystemInfo fileSystemInfo)
      => Argument.That.NotNull(fileSystemInfo) is FileInfo fileInfo && fileInfo.Exists;

    /// <summary>
    /// Gets a value that indicates that this is a DirectoryInfo object and exists.
    /// </summary>
    /// <param name="fileSystemInfo">File system info object.</param>
    /// <returns>True if FileSystemInfo object represents DirectoryInfo object and exists, otherwise false.</returns>
    public static bool IsDirectoryAndExists(this FileSystemInfo fileSystemInfo)
      => Argument.That.NotNull(fileSystemInfo) is DirectoryInfo directoryInfo && directoryInfo.Exists;

    public static bool IsBaseOf(this DirectoryInfo directoryInfo, FileSystemInfo fileSystemInfo)
      => PwrPath.IsBaseOf(Argument.That.NotNull(directoryInfo).FullName, Argument.That.NotNull(fileSystemInfo).FullName);

    #endregion
    #region Internal methods

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

    private static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosCore(DirectoryInfo startDirectoryInfo, DirectoryInfo[] parentDirectoryInfos, int maxDepth, bool excludeEmpty,
      Func<DirectoryInfo, IEnumerable<FileSystemInfo>> getChildren, Predicate<FileSystemInfo> hasChildren,
      Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      if (maxDepth == 0 || !hasChildren(startDirectoryInfo))
        yield break;
      parentDirectoryInfos = parentDirectoryInfos.Concat(Enumerable.Repeat(startDirectoryInfo, 1)).ToArray();
      var children = getChildren(startDirectoryInfo);
      if (predicate is not null)
        children = children.Where(fileSystemInfo => predicate((fileSystemInfo, parentDirectoryInfos)));
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
            using var enumerator = EnumerateFileSystemInfosCore(directoryInfo, parentDirectoryInfos, maxDepth - 1, excludeEmpty, getChildren, hasChildren, predicate, comparison)
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
    }

    private static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosCore<TFileSystemInfo>(DirectoryInfo startDirectoryInfo, DirectoryInfo[] parentDirectoryInfos, int maxDepth, bool excludeEmpty,
      Func<DirectoryInfo, IEnumerable<FileSystemInfo>> getChildren, Predicate<FileSystemInfo> hasChildren,
      Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<FileSystemInfo>? comparison,
      Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TryOut<TFileSystemInfo>> selector)
    {
      if (maxDepth == 0 || !hasChildren(startDirectoryInfo))
        yield break;
      parentDirectoryInfos = parentDirectoryInfos.Concat(Enumerable.Repeat(startDirectoryInfo, 1)).ToArray();
      var children = getChildren(startDirectoryInfo);
      if (predicate is not null)
        children = children.Where(fileSystemInfo => predicate((fileSystemInfo, parentDirectoryInfos)));
      if (comparison is not null)
        children = children.Sort(comparison);
      foreach (FileSystemInfo fileSystemInfo in children)
      {
        if (!hasChildren(fileSystemInfo))
        {
          if (!(excludeEmpty && fileSystemInfo is DirectoryInfo))
          {
            var result = selector(fileSystemInfo, parentDirectoryInfos);
            if (result.Success)
              yield return result.Value!;
          }
        }
        else if (fileSystemInfo.Exists)
        {
          var directoryInfo = fileSystemInfo as DirectoryInfo;
          if (directoryInfo is not null)
          {
            using var enumerator = EnumerateFileSystemInfosCore(directoryInfo, parentDirectoryInfos, maxDepth - 1, excludeEmpty, getChildren, hasChildren, predicate, comparison, selector)
              .GetEnumerator();
            if (enumerator.MoveNext())
            {
              var result = selector(fileSystemInfo, parentDirectoryInfos);
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
              var result = selector(fileSystemInfo, parentDirectoryInfos);
              if (result.Success)
                yield return result.Value!;
            }
          }
          else
          {
            var result = selector(fileSystemInfo, parentDirectoryInfos);
            if (result.Success)
              yield return result.Value!;
          }
        }
        else if (!excludeEmpty)
        {
          var result = selector(fileSystemInfo, parentDirectoryInfos);
          if (result.Success)
            yield return result.Value!;
        }
      }
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

    #endregion
    #region Enumerate file system items

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFileSystemInfos(DefaultSearchPattern, int.MaxValue, traversalOptions, predicate, comparison);

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison);

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);

      var excludeStart = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeStartDirectory);
      var excludeEmpty = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeEmptyDirectory);
      var reverse = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Reverse);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, maxDepth, excludeEmpty,
        directoryInfo => directoryInfo.EnumerateFileSystemInfos(searchPattern ?? DefaultSearchPattern, SearchOption.TopDirectoryOnly),
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

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfos(traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    #endregion
    #region Get file system items

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFileSystemInfos(traversalOptions, predicate, comparison).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, searchOption, traversalOptions, predicate, comparison).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, maxDepth, traversalOptions, predicate, comparison).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfos(traversalOptions, predicate, comparer).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, searchOption, traversalOptions, predicate, comparer).ToArray();

    public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfos(searchPattern, maxDepth, traversalOptions, predicate, comparer).ToArray();

    #endregion
    #region Enumerate file system items with parental context

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(DefaultSearchPattern, int.MaxValue, traversalOptions, predicate, comparison);

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison);

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);

      var excludeStart = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeStartDirectory);
      var excludeEmpty = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeEmptyDirectory);
      var reverse = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Reverse);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var context = Array.Empty<DirectoryInfo>();
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, context, maxDepth, excludeEmpty,
        directoryInfo => directoryInfo.EnumerateFileSystemInfos(searchPattern ?? DefaultSearchPattern, SearchOption.TopDirectoryOnly),
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

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfosEx(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    #endregion
    #region Get file system items with parental context

    public static FileSystemInfo[] GetFileSystemInfosEx(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(traversalOptions, predicate, comparison).ToArray();

    public static FileSystemInfo[] GetFileSystemInfosEx(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, searchOption, traversalOptions, predicate, comparison).ToArray();

    public static FileSystemInfo[] GetFileSystemInfosEx(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, maxDepth, traversalOptions, predicate, comparison).ToArray();

    public static FileSystemInfo[] GetFileSystemInfosEx(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(traversalOptions, predicate, comparer).ToArray();

    public static FileSystemInfo[] GetFileSystemInfosEx(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, searchOption, traversalOptions, predicate, comparer).ToArray();

    public static FileSystemInfo[] GetFileSystemInfosEx(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, maxDepth, traversalOptions, predicate, comparer).ToArray();

    #endregion
    #region Enumerate custom file system items with parental context

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(DefaultSearchPattern, int.MaxValue, traversalOptions, predicate, comparison, selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      var excludeStart = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeStartDirectory);
      var excludeEmpty = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeEmptyDirectory);
      var reverse = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Reverse);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var context = Array.Empty<DirectoryInfo>();
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, context, maxDepth, excludeEmpty,
        directoryInfo => directoryInfo.EnumerateFileSystemInfos(searchPattern ?? DefaultSearchPattern, SearchOption.TopDirectoryOnly),
        fileSystemInfo => HasChildrenCore(fileSystemInfo, refresh),
        predicate, comparison,
        (fileSystemInfo, parentDirectoryInfos) => TryOut.Success(selector(fileSystemInfo, parentDirectoryInfos)));
      if (reverse)
        children = children.Reverse();
      using var enumerator = children.GetEnumerator();
      if (enumerator.MoveNext())
      {
        if (!excludeStart && !reverse)
          yield return selector(startDirectoryInfo, context);
        do
          yield return enumerator.Current;
        while (enumerator.MoveNext());
        if (!excludeStart && reverse)
          yield return selector(startDirectoryInfo, context);
      }
      else if (!excludeEmpty && !excludeStart)
        yield return selector(startDirectoryInfo, context);
    }

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileSystemInfo> EnumerateFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    #endregion
    #region Get custom file system items with parental context

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<FileSystemInfo>? comparison, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TFileSystemInfo[] GetFileSystemInfosEx<TFileSystemInfo>(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, IComparer<FileSystemInfo>? comparer, Func<FileSystemInfo, IReadOnlyList<DirectoryInfo>, TFileSystemInfo> selector)
      => startDirectoryInfo.EnumerateFileSystemInfosEx(searchPattern, maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    #endregion
    #region Enumerate directories

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
      => startDirectoryInfo.EnumerateDirectories(DefaultSearchPattern, int.MaxValue, traversalOptions, predicate, comparison);

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison);

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);

      var excludeStart = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeStartDirectory);
      var excludeEmpty = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeEmptyDirectory);
      var reverse = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Reverse);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, maxDepth, excludeEmpty,
        directoryInfo => directoryInfo.EnumerateDirectories(searchPattern ?? DefaultSearchPattern, SearchOption.TopDirectoryOnly),
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

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectories(traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    #endregion
    #region Get directories

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
      => startDirectoryInfo.EnumerateDirectories(traversalOptions, predicate, comparison).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, searchOption, traversalOptions, predicate, comparison).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<DirectoryInfo>? predicate, Comparison<DirectoryInfo>? comparison)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, maxDepth, traversalOptions, predicate, comparison).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectories(traversalOptions, predicate, comparer).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, searchOption, traversalOptions, predicate, comparer).ToArray();

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<DirectoryInfo>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectories(searchPattern, maxDepth, traversalOptions, predicate, comparer).ToArray();

    #endregion
    #region Enumerate directories with parental context

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<DirectoryInfo>? comparison)
      => startDirectoryInfo.EnumerateDirectoriesEx(DefaultSearchPattern, int.MaxValue, traversalOptions, predicate, comparison);

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<DirectoryInfo>? comparison)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison);

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<DirectoryInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);

      var excludeStart = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeStartDirectory);
      var excludeEmpty = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeEmptyDirectory);
      var reverse = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Reverse);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var context = Array.Empty<DirectoryInfo>();
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, context, maxDepth, excludeEmpty,
        directoryInfo => directoryInfo.EnumerateDirectories(searchPattern ?? DefaultSearchPattern, SearchOption.TopDirectoryOnly),
        fileSystemInfo => HasChildrenCore(fileSystemInfo, refresh),
        ((FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentDirectoryInfos) element) =>
          element.fileSystemInfo is DirectoryInfo directoryInfo && (predicate?.Invoke((directoryInfo, element.parentDirectoryInfos)) ?? true),
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

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectoriesEx(traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<DirectoryInfo> EnumerateDirectoriesEx(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison());

    #endregion
    #region Get directories with parental context

    public static DirectoryInfo[] GetDirectoriesEx(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<DirectoryInfo>? comparison)
      => startDirectoryInfo.EnumerateDirectoriesEx(traversalOptions, predicate, comparison).ToArray();

    public static DirectoryInfo[] GetDirectoriesEx(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<DirectoryInfo>? comparison)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, searchOption, traversalOptions, predicate, comparison).ToArray();

    public static DirectoryInfo[] GetDirectoriesEx(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<DirectoryInfo>? comparison)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, maxDepth, traversalOptions, predicate, comparison).ToArray();

    public static DirectoryInfo[] GetDirectoriesEx(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectoriesEx(traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    public static DirectoryInfo[] GetDirectoriesEx(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    public static DirectoryInfo[] GetDirectoriesEx(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? predicate, IComparer<DirectoryInfo>? comparer)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    #endregion
    #region Enumerate custom directories with parental context

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(DefaultSearchPattern, int.MaxValue, traversalOptions, predicate, comparison, selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, comparison, selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      var excludeStart = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeStartDirectory);
      var excludeEmpty = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.ExcludeEmptyDirectory);
      var reverse = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Reverse);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var context = Array.Empty<DirectoryInfo>();
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, context, maxDepth, excludeEmpty,
        directoryInfo => directoryInfo.EnumerateDirectories(searchPattern ?? DefaultSearchPattern, SearchOption.TopDirectoryOnly),
        fileSystemInfo => HasChildrenCore(fileSystemInfo, refresh),
        ((FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentDirectoryInfos) element) =>
          element.fileSystemInfo is DirectoryInfo directoryInfo && (predicate?.Invoke((directoryInfo, element.parentDirectoryInfos)) ?? true),
        comparison is not null ? (x, y) => comparison((DirectoryInfo)x, (DirectoryInfo)y) : default,
        (FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentDirectoryInfos)
          => fileSystemInfo is DirectoryInfo directoryInfo ? TryOut.Success(selector(directoryInfo, parentDirectoryInfos)) : TryOut.Failure<TDirectoryInfo>());
      if (reverse)
        children = children.Reverse();
      using var enumerator = children.GetEnumerator();
      if (enumerator.MoveNext())
      {
        if (!excludeStart && !reverse)
          yield return selector(startDirectoryInfo, context);
        do
          yield return enumerator.Current;
        while (enumerator.MoveNext());
        if (!excludeStart && reverse)
          yield return selector(startDirectoryInfo, context);
      }
      else if (!excludeEmpty && !excludeStart)
        yield return selector(startDirectoryInfo, context);
    }

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TDirectoryInfo> EnumerateDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), comparer?.AsComparison(), selector);

    #endregion
    #region Get custom directories with parental context

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, searchOption, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Comparison<DirectoryInfo>? comparison, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, maxDepth, traversalOptions, predicate, comparison, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, searchOption, traversalOptions, predicate, comparer, selector).ToArray();

    public static TDirectoryInfo[] GetDirectoriesEx<TDirectoryInfo>(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? predicate, IComparer<DirectoryInfo>? comparer, Func<DirectoryInfo, IReadOnlyList<DirectoryInfo>, TDirectoryInfo> selector)
      => startDirectoryInfo.EnumerateDirectoriesEx(searchPattern, maxDepth, traversalOptions, predicate, comparer, selector).ToArray();

    #endregion
    #region Enumerate files

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFiles(DefaultSearchPattern, int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison);

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFiles(searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison);

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);

      var reverse = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Reverse);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, maxDepth, true,
        directoryInfo => directoryInfo.EnumerateFileSystemInfos(searchPattern ?? DefaultSearchPattern, SearchOption.TopDirectoryOnly),
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

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFiles(traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFiles(searchPattern, searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFiles(searchPattern, maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison());

    #endregion
    #region Get files

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFiles(traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFiles(searchPattern, searchOption, traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileInfo>? filePredicate, Predicate<DirectoryInfo>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFiles(searchPattern, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFiles(traversalOptions, filePredicate, directoryPredicate, comparer).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
    => startDirectoryInfo.EnumerateFiles(searchPattern, searchOption, traversalOptions, filePredicate, directoryPredicate, comparer).ToArray();

    public static FileInfo[] GetFiles(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileInfo>? filePredicate, IPredicate<DirectoryInfo>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFiles(searchPattern, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparer).ToArray();

    #endregion
    #region Enumerate files with parental context

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFilesEx(DefaultSearchPattern, int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison);

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison);

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);

      var reverse = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Reverse);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var context = Array.Empty<DirectoryInfo>();
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, context, maxDepth, true,
        directoryInfo => directoryInfo.EnumerateFileSystemInfos(searchPattern ?? DefaultSearchPattern, SearchOption.TopDirectoryOnly),
        fileSystemInfo => HasChildrenCore(fileSystemInfo, refresh),
        ((FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentDirectoryInfos) element) =>
          element.fileSystemInfo is DirectoryInfo directoryInfo && (directoryPredicate?.Invoke((directoryInfo, element.parentDirectoryInfos)) ?? true) ||
          element.fileSystemInfo is FileInfo fileInfo && (filePredicate?.Invoke((fileInfo, element.parentDirectoryInfos)) ?? true),
        comparison)
        .OfType<FileInfo>();
      if (reverse)
        children = children.Reverse();
      return children;
    }

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, IPredicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, IPredicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison());

    public static IEnumerable<FileInfo> EnumerateFilesEx(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, IPredicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison());

    #endregion
    #region Get files with parental context

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFilesEx(traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, searchOption, traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? directoryPredicate, Comparison<FileSystemInfo>? comparison)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, IPredicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, IPredicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    public static FileInfo[] GetFilesEx(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, IPredicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? directoryPredicate, IComparer<FileSystemInfo>? comparer)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison()).ToArray();

    #endregion
    #region Enumerate custom files with parental context

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(DefaultSearchPattern, int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison, selector);

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, filePredicate, directoryPredicate, comparison, selector);

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
    {
      Argument.That.NotNull(startDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(selector);

      var reverse = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Reverse);
      var refresh = traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh);
      var context = Array.Empty<DirectoryInfo>();
      var children = EnumerateFileSystemInfosCore(startDirectoryInfo, context, maxDepth, true,
        directoryInfo => directoryInfo.EnumerateFileSystemInfos(searchPattern ?? DefaultSearchPattern, SearchOption.TopDirectoryOnly),
        fileSystemInfo => HasChildrenCore(fileSystemInfo, refresh),
        ((FileSystemInfo fileSystemInfo, IReadOnlyList<DirectoryInfo> parentDirectoryInfos) element) =>
          element.fileSystemInfo is DirectoryInfo directoryInfo && (directoryPredicate?.Invoke((directoryInfo, element.parentDirectoryInfos)) ?? true) ||
          element.fileSystemInfo is FileInfo fileInfo && (filePredicate?.Invoke((fileInfo, element.parentDirectoryInfos)) ?? true),
        comparison,
        (fileSystemInfo, parentDirectoryInfos) => fileSystemInfo is FileInfo fileInfo ? TryOut.Success(selector(fileInfo, parentDirectoryInfos)) : TryOut.Failure<TFileInfo>());
      if (reverse)
        children = children.Reverse();
      return children;
    }

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, IPredicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, IPredicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, searchOption, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison(), selector);

    public static IEnumerable<TFileInfo> EnumerateFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, IPredicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, maxDepth, traversalOptions, filePredicate?.AsPredicate(), directoryPredicate?.AsPredicate(), comparer?.AsComparison(), selector);

    #endregion
    #region Get custom files with parental context

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(traversalOptions, filePredicate, directoryPredicate, comparison, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, searchOption, traversalOptions, filePredicate, directoryPredicate, comparison, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, Predicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? directoryPredicate, Comparison<FileSystemInfo>? comparison,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparison, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, IPredicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(traversalOptions, filePredicate, directoryPredicate, comparer, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, IPredicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, searchOption, traversalOptions, filePredicate, directoryPredicate, comparer, selector).ToArray();

    public static TFileInfo[] GetFilesEx<TFileInfo>(this DirectoryInfo startDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileInfo, IReadOnlyList<DirectoryInfo>)>? filePredicate, IPredicate<(DirectoryInfo, IReadOnlyList<DirectoryInfo>)>? directoryPredicate, IComparer<FileSystemInfo>? comparer,
      Func<FileInfo, IReadOnlyList<DirectoryInfo>, TFileInfo> selector)
      => startDirectoryInfo.EnumerateFilesEx(searchPattern, maxDepth, traversalOptions, filePredicate, directoryPredicate, comparer, selector).ToArray();

    #endregion
    #region Copy

    public static int Copy(this DirectoryInfo sourceDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
      => sourceDirectoryInfo.Copy(DefaultSearchPattern, int.MaxValue, traversalOptions, predicate, copying);

    public static int Copy(this DirectoryInfo sourceDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
      => sourceDirectoryInfo.Copy(searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, copying);

    public static int Copy(this DirectoryInfo sourceDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(copying);

      return ProcessFileSystemInfoCore(sourceDirectoryInfo, traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh),
        directoryInfo => directoryInfo.EnumerateFileSystemInfos(searchPattern, maxDepth, traversalOptions, predicate, new SelectComparer<FileSystemInfo, bool>(fileSystemInfo => fileSystemInfo is FileInfo).Compare),
        fileSystemInfo => CopyCore(fileSystemInfo, copying));
    }

    public static int Copy(this DirectoryInfo sourceDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
      => sourceDirectoryInfo.Copy(traversalOptions, predicate?.AsPredicate(), copying);

    public static int Copy(this DirectoryInfo sourceDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
      => sourceDirectoryInfo.Copy(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), copying);

    public static int Copy(this DirectoryInfo sourceDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
      => sourceDirectoryInfo.Copy(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), copying);

    #endregion
    #region Copy extended

    public static int CopyEx(this DirectoryInfo sourceDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
      => sourceDirectoryInfo.CopyEx(DefaultSearchPattern, int.MaxValue, traversalOptions, predicate, copying);

    public static int CopyEx(this DirectoryInfo sourceDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
      => sourceDirectoryInfo.CopyEx(searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, copying);

    public static int CopyEx(this DirectoryInfo sourceDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(copying);

      return ProcessFileSystemInfoCore(sourceDirectoryInfo, traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh),
        directoryInfo => directoryInfo.EnumerateFileSystemInfosEx(searchPattern, maxDepth, traversalOptions, predicate, new SelectComparer<FileSystemInfo, bool>(fileSystemInfo => fileSystemInfo is FileInfo).Compare),
        fileSystemInfo => CopyCore(fileSystemInfo, copying));
    }

    public static int CopyEx(this DirectoryInfo sourceDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
      => sourceDirectoryInfo.CopyEx(traversalOptions, predicate?.AsPredicate(), copying);

    public static int CopyEx(this DirectoryInfo sourceDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
      => sourceDirectoryInfo.CopyEx(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), copying);

    public static int CopyEx(this DirectoryInfo sourceDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Func<FileSystemInfo, TryOut<CopyOptions>> copying)
      => sourceDirectoryInfo.CopyEx(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), copying);

    #endregion
    #region Move

    public static int Move(this DirectoryInfo sourceDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
      => sourceDirectoryInfo.Move(DefaultSearchPattern, int.MaxValue, traversalOptions, predicate, moving);

    public static int Move(this DirectoryInfo sourceDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
      => sourceDirectoryInfo.Move(searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, moving);

    public static int Move(this DirectoryInfo sourceDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(moving);

      return ProcessFileSystemInfoCore(sourceDirectoryInfo, traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh),
        directoryInfo => directoryInfo.EnumerateFileSystemInfos(searchPattern, maxDepth, traversalOptions, predicate, new SelectComparer<FileSystemInfo, bool>(fileSystemInfo => fileSystemInfo is FileInfo).Compare),
        fileSystemInfo => MoveCore(fileSystemInfo, moving));
    }

    public static int Move(this DirectoryInfo sourceDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
      => sourceDirectoryInfo.Move(traversalOptions, predicate?.AsPredicate(), moving);

    public static int Move(this DirectoryInfo sourceDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
      => sourceDirectoryInfo.Move(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), moving);

    public static int Move(this DirectoryInfo sourceDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
      => sourceDirectoryInfo.Move(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), moving);

    #endregion
    #region Move extended

    public static int MoveEx(this DirectoryInfo sourceDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
      => sourceDirectoryInfo.MoveEx(DefaultSearchPattern, int.MaxValue, traversalOptions, predicate, moving);

    public static int MoveEx(this DirectoryInfo sourceDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
      => sourceDirectoryInfo.MoveEx(searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, moving);

    public static int MoveEx(this DirectoryInfo sourceDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(moving);

      return ProcessFileSystemInfoCore(sourceDirectoryInfo, traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh),
        directoryInfo => directoryInfo.EnumerateFileSystemInfosEx(searchPattern, maxDepth, traversalOptions, predicate, new SelectComparer<FileSystemInfo, bool>(fileSystemInfo => fileSystemInfo is FileInfo).Compare),
        fileSystemInfo => MoveCore(fileSystemInfo, moving));
    }

    public static int MoveEx(this DirectoryInfo sourceDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
      => sourceDirectoryInfo.MoveEx(traversalOptions, predicate?.AsPredicate(), moving);

    public static int MoveEx(this DirectoryInfo sourceDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
      => sourceDirectoryInfo.MoveEx(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), moving);

    public static int MoveEx(this DirectoryInfo sourceDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Func<FileSystemInfo, TryOut<MoveOptions>> moving)
      => sourceDirectoryInfo.MoveEx(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), moving);

    #endregion
    #region Delete

    public static int Delete(this DirectoryInfo sourceDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
      => sourceDirectoryInfo.Delete(DefaultSearchPattern, int.MaxValue, traversalOptions, predicate, deleting);

    public static int Delete(this DirectoryInfo sourceDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
      => sourceDirectoryInfo.Delete(searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, deleting);

    public static int Delete(this DirectoryInfo sourceDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(deleting);

      return ProcessFileSystemInfoCore(sourceDirectoryInfo, traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh),
        directoryInfo => directoryInfo.EnumerateFileSystemInfos(searchPattern, maxDepth, traversalOptions, predicate, new SelectComparer<FileSystemInfo, bool>(fileSystemInfo => fileSystemInfo is FileInfo).Compare),
        fileSystemInfo => DeleteCore(fileSystemInfo, deleting));
    }

    public static int Delete(this DirectoryInfo sourceDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
      => sourceDirectoryInfo.Delete(traversalOptions, predicate?.AsPredicate(), deleting);

    public static int Delete(this DirectoryInfo sourceDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
      => sourceDirectoryInfo.Delete(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), deleting);

    public static int Delete(this DirectoryInfo sourceDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<FileSystemInfo>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
      => sourceDirectoryInfo.Delete(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), deleting);

    #endregion
    #region Delete extended

    public static int DeleteEx(this DirectoryInfo sourceDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
      => sourceDirectoryInfo.DeleteEx(DefaultSearchPattern, int.MaxValue, traversalOptions, predicate, deleting);

    public static int DeleteEx(this DirectoryInfo sourceDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
      => sourceDirectoryInfo.DeleteEx(searchPattern, searchOption == SearchOption.TopDirectoryOnly ? 1 : int.MaxValue, traversalOptions, predicate, deleting);

    public static int DeleteEx(this DirectoryInfo sourceDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      Predicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
    {
      Argument.That.NotNull(sourceDirectoryInfo);
      Argument.That.NonNegative(maxDepth);
      Argument.That.NotNull(deleting);

      return ProcessFileSystemInfoCore(sourceDirectoryInfo, traversalOptions.IsFlagsSet(FileSystemTraversalOptions.Refresh),
        directoryInfo => directoryInfo.EnumerateFileSystemInfosEx(searchPattern, maxDepth, traversalOptions, predicate, new SelectComparer<FileSystemInfo, bool>(fileSystemInfo => fileSystemInfo is FileInfo).Compare),
        fileSystemInfo => DeleteCore(fileSystemInfo, deleting));
    }

    public static int DeleteEx(this DirectoryInfo sourceDirectoryInfo, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
      => sourceDirectoryInfo.DeleteEx(traversalOptions, predicate?.AsPredicate(), deleting);

    public static int DeleteEx(this DirectoryInfo sourceDirectoryInfo, string? searchPattern, SearchOption searchOption, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
      => sourceDirectoryInfo.DeleteEx(searchPattern, searchOption, traversalOptions, predicate?.AsPredicate(), deleting);

    public static int DeleteEx(this DirectoryInfo sourceDirectoryInfo, string? searchPattern, int maxDepth, FileSystemTraversalOptions traversalOptions,
      IPredicate<(FileSystemInfo, IReadOnlyList<DirectoryInfo>)>? predicate, Func<FileSystemInfo, TryOut<DeleteOptions>> deleting)
      => sourceDirectoryInfo.DeleteEx(searchPattern, maxDepth, traversalOptions, predicate?.AsPredicate(), deleting);

    #endregion
  }
}

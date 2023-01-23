using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Linq;
using PowerLib.System.Validation;

namespace PowerLib.System.IO
{
  public static class PwrPath
  {
    private const string ExtPrefix = @"\\?\";

    private static readonly char[] directorySeparators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

    private static (int offset, int length) Normalize(string path, bool noVolume)
    {
      var offset = path.StartsWith(ExtPrefix, StringComparison.OrdinalIgnoreCase) ? ExtPrefix.Length : 0;
      if (noVolume)
      {
        var volumeIndex = path.IndexOf(Path.VolumeSeparatorChar, offset);
        if (volumeIndex >= 0)
          offset = volumeIndex + 1;
      }
      var length = path.LastIndexExceptOf(directorySeparators) + 1 - offset;
      return (offset, length);
    }

    public static bool IsDirectorySeparator(char ch)
      => ch == Path.DirectorySeparatorChar || ch == Path.AltDirectorySeparatorChar;

    public static bool EndsWithDirectorySeparator(string path)
      => Argument.That.NotNull(path).Length == 0 ? false : IsDirectorySeparator(path[path.Length - 1]);

    public static string TrimEndingDirectorySeparator(string path)
      => Argument.That.NotNull(path).TrimEnd(directorySeparators);

    public static IEnumerable<string> Split(string path, bool noVolume = false)
    {
      Argument.That.NotNull(path);

      var pathRange = Normalize(path, noVolume);
      var offset = pathRange.offset;
      var length = pathRange.length;
      while (length > 0)
      {
        int found = path.LastIndexOfAny(directorySeparators, offset + length - 1, length);
        if (found < offset)
        {
          yield return path.Substring(offset, length);
          length = 0;
        }
        else
        {
          yield return path.Substring(found + 1, offset + length - (found + 1));
          if (found > offset && path[found - 1] == Path.VolumeSeparatorChar)
          {
            yield return path.Substring(offset, found + 1 - offset);
            length = 0;
          }
          else if (found == offset)
          {
            yield return path.Substring(offset, 1);
            length = 0;
          }
          else
          {
            length = found - offset;
          }
        }
      }
    }

    public static bool IsBaseOf(string basePath, string fullPath)
    {
      Argument.That.NotNull(fullPath);
      Argument.That.NotNull(basePath);

      var fullRange = Normalize(fullPath, false);
      var baseRange = Normalize(basePath, false);

      return string.Compare(basePath, baseRange.offset, fullPath, fullRange.offset, baseRange.length, StringComparison.OrdinalIgnoreCase) == 0
        && (baseRange.length == fullRange.length || baseRange.length < fullRange.length && IsDirectorySeparator(fullPath[fullRange.offset + baseRange.length]));
    }

    public static string? GetRelativePath(string basePath, string fullPath)
    {
      Argument.That.NotNull(fullPath);
      Argument.That.NotNull(basePath);

      var fullRange = Normalize(fullPath, false);
      var baseRange = Normalize(basePath, false);

      if (baseRange.length > fullRange.length)
        return null;
      if (string.Compare(basePath, baseRange.offset, fullPath, fullRange.offset, baseRange.length, StringComparison.OrdinalIgnoreCase) != 0)
        return null;
      if (baseRange.length == fullRange.length)
        return ".";
      if (!IsDirectorySeparator(fullPath[fullRange.offset + baseRange.length]))
        return null;
      return fullPath.Substring(fullRange.offset + baseRange.length + 1);
    }
  }
}

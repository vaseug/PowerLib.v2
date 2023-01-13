using System.Collections.Generic;
using System.IO;
using System.Linq;
using PowerLib.System.Linq;
using PowerLib.System.Validation;

namespace PowerLib.System.IO
{
  public static class PwrPath
  {
    public static bool CaseSensitive { get; set; }

    private static int CompareChar(char x, char y) => CaseSensitive ? x - y : char.ToUpperInvariant(x) - char.ToUpperInvariant(y);

    public static IEnumerable<string> Split(string path)
    {
      Argument.That.NotNull(path);

      for (var filename = Path.GetFileName(path); !string.IsNullOrEmpty(filename); path = Path.GetDirectoryName(path), filename = Path.GetFileName(path))
        yield return filename;
      if (!string.IsNullOrEmpty(path))
        yield return path;
    }

    public static string Combine(IEnumerable<string> parts)
      => Argument.That.NotNull(parts).Aggregate((accum, item) => Path.IsPathRooted(item) ? item : Path.Combine(accum ?? string.Empty, item));

    public static string Combine(params string[] parts)
      => Combine((IEnumerable<string>)parts);

    public static bool IsBaseOf(string basePath, string fullPath)
    {
      Argument.That.NotNull(fullPath);
      Argument.That.NotNull(basePath);

      fullPath = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
      basePath = basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

      int result = fullPath.SequenceCompare(basePath, CompareChar);
      return result == basePath.Length + 1;
    }

    public static string? GetRelativeTo(string fullPath, string basePath)
    {
      Argument.That.NotNull(fullPath);
      Argument.That.NotNull(basePath);
      fullPath = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
      basePath = basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

      return fullPath.Length >= basePath.Length && fullPath.Take(basePath.Length).SequenceEqual(basePath) ? fullPath.Substring(basePath.Length) : null;
    }
  }
}

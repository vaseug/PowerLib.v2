using System;
using PowerLib.System.Validation;

namespace PowerLib.System.IO;

public readonly struct FileSystemCount : IComparable<FileSystemCount>, IEquatable<FileSystemCount>
{
  private static readonly FileSystemCount minValue = new () { Files = 0, Directories = 0, Size = 0L };
  private static readonly FileSystemCount maxValue = new () { Files = int.MaxValue, Directories = int.MaxValue, Size = long.MaxValue };

  private readonly int _files;
  private readonly int _directories;
  private readonly long _size;

  public int Files
  {
    get => _files;
    init => _files = Argument.That.NonNegative(value);
  }

  public int Directories
  {
    get => _directories;
    init => _directories = Argument.That.NonNegative(value);
  }

  public int Total
    => _files == int.MaxValue ? _directories == int.MaxValue ? int.MaxValue : _directories : _directories == int.MaxValue ? _files : _files + _directories;

  public long Size
  {
    get => _size;
    init => _size = Argument.That.NonNegative(value);
  }

  public static FileSystemCount MinValue
    => minValue;

  public static FileSystemCount MaxValue
    => maxValue;

  public override bool Equals(object? obj)
    => obj is FileSystemCount other && Equals(other);

  public bool Equals(FileSystemCount other)
    => Files == other.Files && Directories == other.Directories && Size == other.Size;

  public override int GetHashCode()
    => CompositeHashing.Default.GetHashCode(Files, Directories, Size);

  public int CompareTo(FileSystemCount other)
    => Comparable.Compare(Total, other.Total);

  public FileSystemCount Add(FileSystemCount value)
    => new ()
    {
      Files = Files + value.Files,
      Directories = Directories + value.Directories,
      Size = Size + value.Size
    };

  public FileSystemCount Sub(FileSystemCount value)
    => new()
    {
      Files = Files - value.Files,
      Directories = Directories - value.Directories,
      Size = Size - value.Size
    };

  public FileSystemCount Delta(int directoryUnits, int fileUnits, int sizeUnits)
    => new ()
    {
      Directories = Argument.That.NonNegative(directoryUnits) == 0 ? int.MaxValue : Comparable.Max(Directories / directoryUnits, 1),
      Files = Argument.That.NonNegative(fileUnits) == 0 ? int.MaxValue : Comparable.Max(Files / fileUnits, 1),
      Size = Argument.That.NonNegative(sizeUnits) == 0L ? long.MaxValue : Comparable.Max(Size / sizeUnits, 1),
    };

  public float FilesRatio(FileSystemCount total)
    => Files == 0 ? 0f : total.Files == 0 ? 1f : Files / (float)total.Files;

  public float DirectoriesRatio(FileSystemCount total)
    => Directories == 0 ? 0f : total.Directories == 0 ? 1f : Directories / (float)total.Directories;

  public float TotalRatio(FileSystemCount total)
    => Total == 0 ? 0f : total.Total == 0 ? 1f : Total / (float)total.Total;

  public float SizeRatio(FileSystemCount total)
    => Size == 0 ? 0f :  total.Size == 0L ? 1f : Size / (float)total.Size;

  public static FileSystemCount operator +(FileSystemCount accum, FileSystemCount value)
    => accum.Add(value);

  public static FileSystemCount operator -(FileSystemCount accum, FileSystemCount value)
    => accum.Sub(value);

  public static bool operator ==(FileSystemCount value, FileSystemCount other)
    => value.Equals(other);

  public static bool operator !=(FileSystemCount value, FileSystemCount other)
    => !value.Equals(other);

  public static bool operator >(FileSystemCount x, FileSystemCount y)
    => Comparable.Match(x, y, ComparisonCriteria.GreaterThan);

  public static bool operator <(FileSystemCount x, FileSystemCount y)
    => Comparable.Match(x, y, ComparisonCriteria.LessThan);

  public static bool operator >=(FileSystemCount x, FileSystemCount y)
    => Comparable.Match(x, y, ComparisonCriteria.GreaterThanOrEqual);

  public static bool operator <=(FileSystemCount x, FileSystemCount y)
    => Comparable.Match(x, y, ComparisonCriteria.LessThanOrEqual);
}

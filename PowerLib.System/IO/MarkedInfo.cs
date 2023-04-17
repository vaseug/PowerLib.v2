using System.IO;
using PowerLib.System.Validation;

namespace PowerLib.System.IO;

public readonly record struct MarkedInfo<TInfo>
  where TInfo : FileSystemInfo
{
  internal MarkedInfo(TInfo info, FileSystemTraversalMarker traversalMarker)
  {
    switch (Argument.That.NotNull(info))
    {
      case FileInfo:
        Argument.That.IsValid(traversalMarker, traversalMarker == FileSystemTraversalMarker.None);
        break;
      case DirectoryInfo:
        break;
      default:
        Argument.That.Invalid(info);
        break;
    }
    Info = info;
    TraversalMarker = traversalMarker;
  }

  public TInfo Info { get; }

  public FileSystemTraversalMarker TraversalMarker { get; }
}

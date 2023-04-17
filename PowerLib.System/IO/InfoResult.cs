using System.IO;
using PowerLib.System.Validation;

namespace PowerLib.System.IO;

public readonly record struct InfoResult<TInfo>
  where TInfo : FileSystemInfo
{
  internal InfoResult(TInfo info, FileSystemTraversalMarker traversalMarker, FileSystemManipulationMarker manipulationMarker)
  {
    Info = Argument.That.NotNull(info);
    TraversalMarker = traversalMarker;
    ManipulationMarker = manipulationMarker;
  }

  public TInfo Info { get; }

  public FileSystemTraversalMarker TraversalMarker { get; }

  public FileSystemManipulationMarker ManipulationMarker { get; }
}

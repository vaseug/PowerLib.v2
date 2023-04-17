using System.IO;
using PowerLib.System.Validation;

namespace PowerLib.System.IO;

public readonly record struct TransferResult<TInfo>
  where TInfo : FileSystemInfo
{
  internal TransferResult(TInfo sourceInfo, FileSystemTraversalMarker traversalMarker, FileSystemManipulationMarker manipulationMarker, TInfo? destinationInfo)
  {
    SourceInfo = Argument.That.NotNull(sourceInfo);
    DestinationInfo = Argument.That.NotNull(destinationInfo);
    TraversalMarker = traversalMarker;
    ManipulationMarker = manipulationMarker;
  }

  public TInfo SourceInfo { get; }

  public TInfo? DestinationInfo { get; }

  public FileSystemTraversalMarker TraversalMarker { get; }

  public FileSystemManipulationMarker ManipulationMarker { get; }
}

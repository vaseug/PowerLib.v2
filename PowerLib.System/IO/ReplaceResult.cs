using System.IO;
using PowerLib.System.Validation;

namespace PowerLib.System.IO;

public readonly record struct ReplaceResult<TInfo>
  where TInfo : FileSystemInfo
{
  internal ReplaceResult(TInfo sourceInfo, FileSystemTraversalMarker traversalMarker, FileSystemManipulationMarker manipulationMarker, TInfo? destinationInfo, TInfo? destinationBackupInfo)
  {
    SourceInfo = Argument.That.NotNull(sourceInfo);
    DestinationInfo = Argument.That.NotNull(destinationInfo);
    DestinationBackupInfo = destinationBackupInfo;
    TraversalMarker = traversalMarker;
    ManipulationMarker = manipulationMarker;
  }

  public TInfo SourceInfo { get; }

  public TInfo? DestinationInfo { get; }

  public TInfo? DestinationBackupInfo { get; }

  public FileSystemTraversalMarker TraversalMarker { get; }

  public FileSystemManipulationMarker ManipulationMarker { get; }
}

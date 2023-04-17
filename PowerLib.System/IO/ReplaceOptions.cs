using System.Collections.Generic;
using System.IO;

namespace PowerLib.System.IO;

public readonly record struct ReplaceOptions : IEnsuringOptions, IProcessingOptions
{
  public string DestinationPath { get; init; }

  public string? DestinationBackupPath { get; init; }

  public bool IgnoreMetadataErrors { get; init; }

  public bool NoProcessing { get; init; }

  IEnumerable<string> IEnsuringOptions.EnsuringDirectories
  {
    get
    {
      if (DestinationBackupPath is null)
        yield break;
      else if (NoProcessing)
        yield return DestinationBackupPath;
      else
      {
        var destinationBackupDirectory = Path.GetDirectoryName(DestinationBackupPath);
        if (destinationBackupDirectory is not null)
          yield return destinationBackupDirectory;
      }
    }
  }
}

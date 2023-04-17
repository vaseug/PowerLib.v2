using System.Collections.Generic;

namespace PowerLib.System.IO;

public readonly record struct ReplicateOptions : IEnsuringOptions, IProcessingOptions
{
  public string DestinationPath { get; init; }

  IEnumerable<string> IEnsuringOptions.EnsuringDirectories
  {
    get
    {
      if (DestinationPath is not null)
        yield return DestinationPath;
    }
  }

  bool IProcessingOptions.NoProcessing => true;
}

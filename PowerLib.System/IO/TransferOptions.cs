using System.Collections.Generic;
using System.IO;

namespace PowerLib.System.IO;

public readonly record struct TransferOptions : IEnsuringOptions, IProcessingOptions
{
  public string DestinationPath { get; init; }

  public bool Overwrite { get; init; }

  public bool NoProcessing { get; init; }

  IEnumerable<string> IEnsuringOptions.EnsuringDirectories
  {
    get
    {
      if (DestinationPath is null)
        yield break;
      else if (NoProcessing)
        yield return DestinationPath;
      else
      {
        var destinationDirectory = Path.GetDirectoryName(DestinationPath);
        if (destinationDirectory is not null)
          yield return destinationDirectory;
      }
    }
  }
}

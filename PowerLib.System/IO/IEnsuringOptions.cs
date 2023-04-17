using System.Collections.Generic;

namespace PowerLib.System.IO;

public interface IEnsuringOptions
{
  IEnumerable<string> EnsuringDirectories { get; }
}

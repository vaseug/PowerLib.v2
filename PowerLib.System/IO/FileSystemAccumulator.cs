using PowerLib.System.Validation;

namespace PowerLib.System.IO;

public class FileSystemAccumulator : DeltaAccumulator<FileSystemCount>
{
  public FileSystemAccumulator(FileSystemCount delta)
    : base(FileSystemCount.MinValue, FileSystemCount.MinValue, delta)
  { }

  public FileSystemAccumulator(FileSystemCount total, FileSystemCount delta)
    : base(FileSystemCount.MinValue, total, delta)
  { }

  public FileSystemAccumulator(FileSystemCount total, int directoryUnits, int fileUnits, int capacityUnits)
    : base(FileSystemCount.MinValue, total, new FileSystemCount
    {
      Directories = Argument.That.NonNegative(directoryUnits) == 0 ? int.MaxValue : total.Directories / directoryUnits,
      Files = Argument.That.NonNegative(fileUnits) == 0 ? int.MaxValue : total.Files / fileUnits,
      Size = Argument.That.NonNegative(capacityUnits) == 0L ? long.MaxValue : total.Size / capacityUnits,
    })
  { }

  protected override FileSystemCount Accumulate(FileSystemCount accum, FileSystemCount value)
    => accum.Add(value);

  protected override int Compare(FileSystemCount xValue, FileSystemCount yValue)
    => Comparable.Compare(xValue, yValue);
}

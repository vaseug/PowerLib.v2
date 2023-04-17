namespace PowerLib.System.IO;

public readonly record struct DeleteOptions : IProcessingOptions
{
  public bool Recursive { get; init; }

  public bool NoProcessing { get; init; }
}

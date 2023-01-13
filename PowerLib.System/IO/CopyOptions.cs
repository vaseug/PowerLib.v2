namespace PowerLib.System.IO;

public record struct CopyOptions
{
  public string? TargetPath { get; init; }

  public bool Overwrite { get; init; }

  public bool EnsureDirectory { get; init; }
}

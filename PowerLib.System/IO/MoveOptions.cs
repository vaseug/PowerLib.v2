namespace PowerLib.System.IO;

public record struct MoveOptions
{
  public string? TargetPath { get; init; }

  public bool Overwrite { get; init; }

  public bool EnsureDirectory { get; init; }

  public bool ClearReadOnly { get; init; }
}

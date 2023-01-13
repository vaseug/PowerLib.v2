namespace PowerLib.System.IO;

public record struct DeleteOptions
{
  public bool Recursive { get; init; }

  public bool ClearReadOnly { get; init; }
}

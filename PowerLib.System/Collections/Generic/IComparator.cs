namespace PowerLib.System.Collections.Generic;

public interface IComparator<in T>
{
  int Compare(T? obj);
}

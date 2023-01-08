using System.Collections.Generic;

namespace PowerLib.System.Collections.Generic;

public interface IRangedList<T> : IList<T>, IRangedCollection<T>
{
    void ReplaceRange(int index, IEnumerable<T> coll);

    void InsertRange(int index, IEnumerable<T> coll);

    void RemoveRange(int index, int count);

    void MoveRange(int sIndex, int dIndex, int count);

    void SwapRanges(int xIndex, int xCount, int yIndex, int yCount);

    void ReverseRange(int index, int count);
}

using System.Collections.Generic;

namespace PowerLib.System.Collections.Generic;

public interface IListEx<T> : IList<T>
{
    void Move(int sIndex, int dIndex);

    void Swap(int xIndex, int yIndex);
}

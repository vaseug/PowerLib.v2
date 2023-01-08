using System.Collections.Generic;

namespace PowerLib.System.Collections.Generic;

public interface IRangedCollection<T> : ICollection<T>
{
  /// <summary>
  /// 
  /// </summary>
  /// <param name="coll"></param>
  /// <returns></returns>
  void AddRange(IEnumerable<T> coll);

  /// <summary>
  /// 
  /// </summary>
  /// <param name="coll"></param>
  /// <returns></returns>
  void RemoveRange(IEnumerable<T> coll);
}

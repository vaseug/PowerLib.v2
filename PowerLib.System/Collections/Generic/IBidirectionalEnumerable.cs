using System.Collections.Generic;

namespace PowerLib.System.Collections.Generic;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IBidirectionalEnumerable<T> : IEnumerable<T>
{
  /// <summary>
  /// 
  /// </summary>
  /// <returns></returns>
  IEnumerator<T> GetReverseEnumerator();
}

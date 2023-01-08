using System;
using System.Collections.Generic;

namespace PowerLib.System.Collections.Disposables;

public sealed class PwrDisposablesList : DisposablesList
{
  #region Constructors

  public PwrDisposablesList()
    : base(new List<IDisposable>())
  { }

  #endregion
}

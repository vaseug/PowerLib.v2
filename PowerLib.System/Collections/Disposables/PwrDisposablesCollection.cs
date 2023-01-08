using System;
using System.Collections.Generic;

namespace PowerLib.System.Collections.Disposables;

public sealed class PwrDisposablesCollection : DisposablesCollection
{
  #region Constructors

  public PwrDisposablesCollection()
    : base(new HashSet<IDisposable>())
  { }

  #endregion
}

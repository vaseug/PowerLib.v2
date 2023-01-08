using System;
using System.Threading.Tasks;
using PowerLib.System.Validation;

namespace PowerLib.System;

public delegate ValueTask AsyncOutFactory<T>(out T v);

public static class AsyncDisposable
{
  #region Methods

  public static async ValueTask<TAsyncDisposable> CreateAsync<TAsyncDisposable>(AsyncOutFactory<TAsyncDisposable> factory)
    where TAsyncDisposable : IAsyncDisposable
  {
    Argument.That.NotNull(factory);

    var asyncDisposable = default(TAsyncDisposable);
    try
    {
      await factory(out asyncDisposable);
      return asyncDisposable;
    }
    catch
    {
      if (asyncDisposable is not null)
        await asyncDisposable.DisposeAsync();
      throw;
    }
  }

  public static ValueTask DisposeAsync<TAsyncDisposable>(ref TAsyncDisposable? asyncDisposable)
    where TAsyncDisposable : IAsyncDisposable
    => Variable.Take(ref asyncDisposable)?.DisposeAsync() ?? default;

  public static ValueTask BlindDisposeAsync<TSource>(ref TSource? source)
    => (Variable.Take(ref source) as IAsyncDisposable)?.DisposeAsync() ?? default;

  #endregion
}

using System.Diagnostics.CodeAnalysis;

namespace PowerLib.System;

public delegate bool ElementPredicate<in T>(T v, int index);

public delegate bool ElementDimPredicate<in T>(T v, int index, int[] indices);

public delegate void ElementAction<in T>(T v, int index);

public delegate void ElementDimAction<in T>(T v, int index, int[] indices);

public delegate bool ElementLongPredicate<in T>(T v, long index);

public delegate bool ElementDimLongPredicate<in T>(T v, long index, long[] indices);

public delegate void ElementLongAction<in T>(T v, long index);

public delegate void ElementDimLongAction<in T>(T v, long index, long[] indices);

public delegate TResult ElementConverter<in TSource, out TResult>(TSource v, int index);

public delegate TResult ElementDimConverter<in TSource, out TResult>(TSource v, int index, int[] indices);

public delegate bool Equality<in T>(T x, T y);

public delegate int Comparator<in T>(T v);

public delegate int Hasher<in T>([DisallowNull] T v);

[return: NotNull]
public delegate TResult Factory<out TResult>();

[return: NotNull]
public delegate TResult ParameterizedFactory<in TParam, out TResult>();

[return: NotNull]
public delegate TResult ElementFactory<out TResult>(int index);

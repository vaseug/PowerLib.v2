namespace PowerLib.System.Arrays;

internal enum ArrayMessage
{
  IndexOpenBracket,
  IndexCloseBracket,
  IndexItemDelimiter,
  IndexItemFormat,
  IndexLevelDelimiter,
  ArrayIsEmpty,
  InvalidArrayRank,
  InvalidArrayLength,
  InvalidArrayDimLength,
  InvalidArrayDimBase,
  InvalidArrayElementType,
  InvalidArrayElement,

  OneOrMoreInvalidArrayElements,
  OneOrMoreArrayElementsOutOfRange,

  TypeIsNotArray,
  ArrayIsNotJagged,
  ArrayElementOutOfRange,
  ArrayIndexOutOfRange,
  ArrayDimIndicesOutOfRange,

  ArrayElementIndexOutOfRange,
  RegularArrayElementIndexOutOfRange,
  JaggedArrayElementIndexOutOfRange,
  JaggedRegularArrayElementIndexOutOfRange,

  JaggedArrayElementIndexLevelNotSpecified,
}

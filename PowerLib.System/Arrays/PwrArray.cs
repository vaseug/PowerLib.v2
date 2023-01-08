using PowerLib.System;
using PowerLib.System.Collections;
using PowerLib.System.Collections.Matching;
using PowerLib.System.Linq;
using PowerLib.System.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace PowerLib.System.Arrays;

/// <summary>
/// Array extension methods
/// <remarks>
/// All methods to work with multidimension and jagged arrays are nonrecursive.
/// </remarks>
/// </summary>^
public static class PwrArray
{
  //  Pattern formatting parameters: 0 - delimiters, 1 - spaces, 2 - escapes, 3 - open brackets, 4 - close brackets
  //private const string arrayItemsFormat = @"(?:[^{0}{1}{2}]|(?:[{2}][{0}{1}]))*";
  //private const string arrayItemsFormat = @"(?:(?:[^{0}{1}{2}{3}{4}]|(?:[{2}].))(?:(?:[^{0}{2}{3}{4}]|(?:[{2}].))*(?:[^{0}{1}{2}{3}{4}]|(?:[{2}].)))?)?";
  //  Pattern formatting parameters: 0 - delimiters, 1 - spaces, 2 - escapes, 3 - open brackets, 4 - close brackets
  //private const string arrayDelimiterFormat = @"[{1}]*(?<![{2}])[{0}][{1}]*";
  //  Pattern formatting parameters: 0 - item pattern, 1 - delimiters, 2 - spaces, 3 - escapes, 4 - open brackets, 5 - close brackets
  //private const string arrayPatternFormat =
  //  @"^[{2}]*(?'Open'[{4}][{2}]*)(?'Items'(?:(?:(?<![^{3}][{4}])[{2}]*(?<![{3}])[{1}][{2}]*)?{0})*)[{2}]*(?'Close-Open'[{5}][{2}]*)[{2}]*(?(Open)(?!))$";
  //  Pattern formatting parameters: 0 - item pattern, 1 - delimiters, 2 - spaces, 3 - escapes, 4 - open brackets, 5 - close brackets
  //private const string regularArrayPatternFormat = @"^[{2}]*(?:(?:(?:(?<=[^{3}][{5}][{2}]*)[{1}][{2}]*)?" +
  //  @"(?'Openings'(?'Open'[{4}][{2}]*)+))(?'Items'(?:(?:(?<![^{3}][{4}])[{2}]*(?<![{3}])[{1}][{2}]*)?{0})*)[{2}]*(?'Closings'(?'Close-Open'[{5}][{2}]*)+))*[{2}]*(?(Open)(?!))$";

  private static readonly string IndexOpenBracket = GetMessageString(ArrayMessage.IndexOpenBracket);
  private static readonly string IndexCloseBracket = GetMessageString(ArrayMessage.IndexCloseBracket);
  //private static readonly string IndexLevelDelimiter = GetMessageString(ArrayMessage.IndexLevelDelimiter);
  private static readonly string IndexItemDelimiter = GetMessageString(ArrayMessage.IndexItemDelimiter);
  private static readonly string IndexItemFormat = GetMessageString(ArrayMessage.IndexItemFormat);

  #region Internal methods

  private static string GetMessageString(ArrayMessage arrayMessage)
  {
    var messageString = ArrayResources.Default.GetString(arrayMessage);
    return Argument.That.NotNull(messageString);
  }

  #endregion
  #region Formatting methods
  #region Index formatting

  public static string FormatAsRegularIndices(int[] indices)
  {
    Argument.That.MatchCollection(indices, length => length > 0, index => index >= 0);

    var sb = new StringBuilder();
    var itemFormat = string.Format(null, "{{0{0}}}", IndexItemFormat);
    sb.Append(IndexOpenBracket);
    itemFormat = IndexItemDelimiter + itemFormat;
    for (int i = 0; i < indices.Length; i++)
    {
      if (i > 0)
        sb.Append(IndexItemDelimiter);
      sb.AppendFormat(null, itemFormat, indices[i]);
    }
    sb.Append(IndexCloseBracket);
    return sb.ToString();
  }

  public static string FormatAsLongRegularIndices(long[] indices)
  {
    Argument.That.MatchCollection(indices, length => length > 0L, index => index >= 0L);

    var sb = new StringBuilder();
    var itemFormat = string.Format(null, "{{0{0}}}", IndexItemFormat);
    sb.Append(IndexOpenBracket);
    itemFormat = IndexItemDelimiter + itemFormat;
    for (int i = 0; i < indices.Length; i++)
    {
      if (i > 0)
        sb.Append(IndexItemDelimiter);
      sb.AppendFormat(null, itemFormat, indices[i]);
    }
    sb.Append(IndexCloseBracket);
    return sb.ToString();
  }

  #endregion
  #endregion
}

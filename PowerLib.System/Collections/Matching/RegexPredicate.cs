using System.Text.RegularExpressions;
using PowerLib.System.Collections.Generic;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

/// <summary>
/// Match strings by regular expression.
/// </summary>
public sealed class RegexPredicate : IPredicate<string>
{
  private readonly Regex _regex;

  #region Constructors

  /// <summary>
  /// 
  /// </summary>
  /// <param name="pattern">Regular expression pattern</param>
  public RegexPredicate(string pattern)
    : this(new Regex(Argument.That.NotNull(pattern)))
  { }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="pattern">Regular expression pattern.</param>
  /// <param name="options">Regular expression options.</param>
  public RegexPredicate(string pattern, RegexOptions options)
    : this(new Regex(Argument.That.NotNull(pattern), options))
  { }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="regex">Regex object.</param>
  public RegexPredicate(Regex regex)
  {
    _regex = Argument.That.NotNull(regex);
  }

  #endregion
  #region Properties

  /// <summary>
  /// 
  /// </summary>
  public RegexOptions Options => _regex.Options;

  #endregion
  #region Methods

  /// <summary>
  ///
  /// </summary>
  /// <param name="obj"></param>
  /// <returns></returns>
  public bool Match(string? obj)
    => obj is not null && _regex.IsMatch(obj);

  #endregion
}

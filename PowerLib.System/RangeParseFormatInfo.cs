using PowerLib.System.Text;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using PowerLib.System.Validation;

namespace PowerLib.System;

public class RangeParseFormatInfo : IFormatter<Range>, IParser<Range>, INumberParser<Range>
{
  private const string DecPattern = "[-+]?[0-9]+";
  private const string HexPattern = "[0-9A-Fa-f]+";
  private const string FormatString = @"{2}{{0{0}}}{1}{{1{0}}}{3}"; // parameters: 0 - number format, 1 - delimiter, 2 - opening, 3 - closing
  private const string PatternString =
    @"^(?:" +
      @"(?'open'{2}[{4}]*)" +
        @"(?'index'{0})[{4}]*{1}[{4}]*(?'count'{0})" +
      @"(?'close-open'[{4}]*{3})(?(open)(?!))" +
    @")$"; // parameters: 0 - item pattern, 1 - delimiter, 2 - opening, 3 - closing, 4 - spaces

  private readonly string _delimiter;
  private readonly string _opening;
  private readonly string _closing;
  private readonly char[] _spaces;

  private readonly static Lazy<RangeParseFormatInfo> instance = new(() => new RangeParseFormatInfo(",", "(", ")", new[] { ' ', '\t' }));

  public static RangeParseFormatInfo Default
    => instance.Value;

  public RangeParseFormatInfo(string delimiter, string opening, string closing, char[] spaces)
  {
    _delimiter = Argument.That.NotNull(delimiter);
    _opening = Argument.That.NotNull(opening);
    _closing = Argument.That.NotNull(closing);
    _spaces = (char[])Argument.That.NotNull(spaces).Clone();
  }

  protected virtual NumberStyles DefaultNumberStyles
    => NumberStyles.Integer;

  protected virtual string GetFormat(string? format, IFormatProvider? formatProvider)
  {
    return string.Format(formatProvider, FormatString,
      string.IsNullOrEmpty(format) ? string.Empty : ":" + format.Replace(@"{", @"{{").Replace(@"}", @"}}"),
      _delimiter.Replace(@"{", @"{{").Replace(@"}", @"}}"),
      _opening.Replace(@"{", @"{{").Replace(@"}", @"}}"),
      _closing.Replace(@"{", @"{{").Replace(@"}", @"}}"));
  }

  protected virtual string GetPattern(NumberStyles styles, IFormatProvider? formatProvider)
    => string.Format(formatProvider, PatternString, (styles & NumberStyles.AllowHexSpecifier) != 0 ? HexPattern : DecPattern,
      Regex.Escape(_delimiter), Regex.Escape(_opening), Regex.Escape(_closing), Regex.Escape(new string(_spaces)));

  public string Format(Range value, string? format, IFormatProvider? formatProvider)
    => string.Format(formatProvider, GetFormat(format, formatProvider), value.Index, value.Count);

  public Range Parse(string s, IFormatProvider? formatProvider)
    => Parse(s, DefaultNumberStyles, formatProvider);

  public Range Parse(string s, NumberStyles styles, IFormatProvider? formatProvider)
    => TryParse(s, styles, formatProvider, out var result) ? result : Validation.Format.That.Bad<Range>();

  public bool TryParse(string s, IFormatProvider? formatProvider, out Range result)
    => TryParse(s, DefaultNumberStyles, formatProvider, out result);

  public bool TryParse(string s, NumberStyles styles, IFormatProvider? formatProvider, out Range result)
  {
    if (!string.IsNullOrWhiteSpace(s))
    {
      var match = Regex.Match(s, GetPattern(styles, formatProvider), RegexOptions.Singleline | RegexOptions.IgnoreCase);
      if (match.Success)
      {
        int index = match.Groups["index"].Success ? int.Parse(match.Groups["index"].Value, styles, formatProvider) : 0;
        int count = match.Groups["count"].Success ? int.Parse(match.Groups["count"].Value, styles, formatProvider) : 0;
        result = new Range(index, count);
        return true;
      }
    }
    result = default;
    return false;
  }
}

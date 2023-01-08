using System;
using PowerLib.System.Validation;

namespace PowerLib.System;

public readonly struct DateTimeInterval : IEquatable<DateTimeInterval>
{
  private readonly DateTime _dateTime;
  private readonly TimeSpan _timeSpan;

  public DateTimeInterval(DateTime dateTime, TimeSpan timeSpan)
  {
    Argument.That.Between(timeSpan, DateTime.MinValue - dateTime, DateTime.MaxValue - dateTime);

    _dateTime = dateTime;
    _timeSpan = timeSpan;
  }

  public DateTimeInterval(DateTime dateTimeStart, DateTime dateTimeEnd)
  {
    _dateTime = dateTimeStart <= dateTimeEnd ? dateTimeStart : dateTimeEnd;
    _timeSpan = dateTimeStart <= dateTimeEnd ? dateTimeEnd - dateTimeStart : dateTimeStart - dateTimeEnd;
  }

  public DateTime DateTimeStart
    => _timeSpan >= TimeSpan.Zero ? _dateTime : _dateTime + _timeSpan;

  public DateTime DateTimeEnd
    => _timeSpan >= TimeSpan.Zero ? _dateTime + _timeSpan : _dateTime;

  public DateTime DateTime
    => _dateTime;

  public TimeSpan TimeSpan
    => _timeSpan;

  public DateTimeInterval Shift(TimeSpan shift)
  {
    Argument.That.InRange(DateTime, shift);

    return new DateTimeInterval(DateTime + shift, TimeSpan);
  }

  public DateTimeInterval Shift(TimeSpan shiftStart, TimeSpan shiftEnd)
  {
    Argument.That.InRange(DateTimeStart, shiftStart);
    Argument.That.InRange(DateTimeEnd, shiftEnd);
    var dateTimeStart = DateTimeStart + shiftStart;
    var dateTimeEnd = DateTimeEnd + shiftEnd;
    Argument.That.LessThanOrEqual(dateTimeStart, dateTimeEnd);

    return new DateTimeInterval(DateTimeStart + shiftStart, DateTimeEnd + shiftEnd);
  }

  public DateTimeIntervalMatchResult Match(DateTimeInterval dateTimeInterval)
    => Match(this, dateTimeInterval);

  public static DateTimeIntervalMatchResult Match(DateTimeInterval dateTimeFirst, DateTimeInterval dateTimeSecond)
    => dateTimeFirst.DateTimeEnd < dateTimeSecond.DateTimeStart ? DateTimeIntervalMatchResult.Before :
      dateTimeFirst.DateTimeStart > dateTimeSecond.DateTimeEnd ? DateTimeIntervalMatchResult.After :
      dateTimeFirst.DateTimeStart == dateTimeSecond.DateTimeStart && dateTimeFirst.DateTimeEnd == dateTimeSecond.DateTimeEnd ? DateTimeIntervalMatchResult.Equal :
      dateTimeFirst.DateTimeStart >= dateTimeSecond.DateTimeStart && dateTimeFirst.DateTimeEnd <= dateTimeSecond.DateTimeEnd ? DateTimeIntervalMatchResult.Belong :
      dateTimeFirst.DateTimeStart < dateTimeSecond.DateTimeStart && dateTimeFirst.DateTimeEnd > dateTimeSecond.DateTimeEnd ? DateTimeIntervalMatchResult.Enclose :
      dateTimeFirst.DateTimeStart < dateTimeSecond.DateTimeStart ? DateTimeIntervalMatchResult.OverlapBefore :
      dateTimeFirst.DateTimeEnd > dateTimeSecond.DateTimeEnd ? DateTimeIntervalMatchResult.OverlapAfter :
      Operation.That.Failed<DateTimeIntervalMatchResult>();

  public bool Equals(DateTimeInterval other)
    => _dateTime == other._dateTime && _timeSpan == other._timeSpan;

  public override bool Equals(object? obj)
    => obj is DateTimeInterval value && Equals(value);

  public override int GetHashCode()
    => CompositeHashing.Default.GetHashCode(_dateTime, _timeSpan);

  public static bool operator ==(DateTimeInterval left, DateTimeInterval right)
    => left.Equals(right);

  public static bool operator !=(DateTimeInterval left, DateTimeInterval right)
    => !left.Equals(right);
}

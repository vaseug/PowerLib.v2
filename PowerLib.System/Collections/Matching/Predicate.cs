using System;
using System.Collections.Generic;
using PowerLib.System.Collections.Generic;

namespace PowerLib.System.Collections.Matching
{
  public static class Predicate
  {
    public static IPredicate<T> False<T>() => PersistentPredicate<T>.False;

    public static IPredicate<T> True<T>() => PersistentPredicate<T>.True;

    public static IPredicate<T> IsNull<T>() => NullPredicate<T>.Default;

    public static IPredicate<object> TypeOf<T>() => TypeOfPredicate<T>.Default;

    public static IPredicate<T> Not<T>(IPredicate<T> predicate) => new InversePredicate<T>(predicate);

    public static IPredicate<T> Not<T>(Predicate<T?> predicate) => new InversePredicate<T>(predicate);

    public static IPredicate<T> And<T>(IEnumerable<IPredicate<T>> predicates) => new GroupPredicate<T>(predicates, GroupCriteria.And);

    public static IPredicate<T> And<T>(IEnumerable<Predicate<T?>> predicates) => new GroupPredicate<T>(predicates, GroupCriteria.And);

    public static IPredicate<T> Or<T>(IEnumerable<IPredicate<T>> predicates) => new GroupPredicate<T>(predicates, GroupCriteria.Or);

    public static IPredicate<T> Or<T>(IEnumerable<Predicate<T?>> predicates) => new GroupPredicate<T>(predicates, GroupCriteria.Or);

    public static IPredicate<T> And<T>(params IPredicate<T>[] predicates) => new GroupPredicate<T>(predicates, GroupCriteria.And);

    public static IPredicate<T> And<T>(params Predicate<T?>[] predicates) => new GroupPredicate<T>(predicates, GroupCriteria.And);

    public static IPredicate<T> Or<T>(params IPredicate<T>[] predicates) => new GroupPredicate<T>(predicates, GroupCriteria.Or);

    public static IPredicate<T> Or<T>(params Predicate<T?>[] predicates) => new GroupPredicate<T>(predicates, GroupCriteria.Or);

    public static IPredicate<T> Equality<T>(T value, IEqualityComparer<T> equalityComparer) => new EqualityPredicate<T>(value, equalityComparer);

    public static IPredicate<T> Equality<T>(T value, Equality<T?> equality) => new EqualityPredicate<T>(value, equality);

    public static IPredicate<T> Comparison<T>(T value, ComparisonCriteria criteria, IComparer<T> comparer) => new ComparisonPredicate<T>(value, comparer, criteria);

    public static IPredicate<T> Comparison<T>(T value, ComparisonCriteria criteria, Comparison<T?> comparison) => new ComparisonPredicate<T>(value, comparison, criteria);

    public static IPredicate<T> In<T>(IEnumerable<T> coll, IEqualityComparer<T> equalityComparer) => new InPredicate<T>(coll, equalityComparer);

    public static IPredicate<T> In<T>(IEnumerable<T> coll, Equality<T?> equality) => new InPredicate<T>(coll, equality);

    public static IPredicate<IEnumerable<T>> Quantify<T>(IPredicate<T> predicate, QuantifyCriteria criteria) => new QuantifyPredicate<T>(predicate, criteria);

    public static IPredicate<IEnumerable<T>> Quantify<T>(Predicate<T?> predicate, QuantifyCriteria criteria) => new QuantifyPredicate<T>(predicate, criteria);
  }
}

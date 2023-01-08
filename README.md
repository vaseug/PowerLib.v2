# **PowerLib v2**

This solution contains now the following projects:

* **[PowerLib.System](#PowerLib.System)**

---
# PowerLib.System

Contains many classes, structures, interfaces and extension methods that expedite and optimize the development process. All of them are divided into the following sections:

---
### **General**

Below is information about some of the classes from the `PowerLib.System` namespace.

#### **`TryOut`**

There is a `TryOut<T>` structure that is designed to pass a value along with its validity flag and `TryOut` static class with methods for creating this structure. Used in methods to return value when it is impossible to use in output parameter.

<details><summary>Definition</summary>

```csharp
public readonly struct TryOut<T> : IEquatable<TryOut<T>>
{
  public static readonly TryOut<T> Failed;

  public T? Value { get; }

  public bool Success { get; }
}

public static class TryOut
{
  public static ref readonly TryOut<T> Failure<T>()
    => ref TryOut<T>.Failed;

  public static TryOut<T> Success<T>(T? value)
    => new(value);
}
```

</details><p/>

#### **`Variable`**

Static class `Variable` contains methods for replacing values by reference. Well suited for use in inline expressions.

<details><summary>Definition</summary>

```csharp
public static class Variable
{
  // Sets a new value to a variable passed by reference and returns the previous value.
  public static T? Replace<T>(ref T? variable, T? value);

  // Returns the value of the variable passed by reference and sets it to the default value.
  public static T? Take<T>(ref T? variable);
}
```

</details><p/>

#### **`Range`** and **`LongRange`**

The `Range` and `LongRange` structures are used to work with elements ranges. They contain start index and element count properties and some methods.

Will be described a little later...

#### **`PwrEnum`**

A static class `PwrEnum` contains methods and extensions to work with values of enumerated types.

<details><summary>Definition</summary>

```csharp
public static class PwrEnum
{
  // Convert Byte to TEnum
  public static TEnum ToEnum<TEnum>(byte value)
    where TEnum : struct, Enum, IComparable, IConvertible, IFormattable;

  // Convert UInt16 to TEnum
  public static TEnum ToEnum<TEnum>(ushort value)
    where TEnum : struct, Enum, IComparable, IConvertible, IFormattable;

  // Convert UInt32 to TEnum
  public static TEnum ToEnum<TEnum>(uint value)
    where TEnum : struct, Enum, IComparable, IConvertible, IFormattable;

  // Convert UInt64 to TEnum
  public static TEnum ToEnum<TEnum>(ulong value)
    where TEnum : struct, Enum, IComparable, IConvertible, IFormattable;

  // Convert SByte to TEnum
  public static TEnum ToEnum<TEnum>(sbyte value)
    where TEnum : struct, Enum, IComparable, IConvertible, IFormattable;

  // Convert Int16 to TEnum
  public static TEnum ToEnum<TEnum>(short value)
    where TEnum : struct, Enum, IComparable, IConvertible, IFormattable;

  // Convert Int32 to TEnum
  public static TEnum ToEnum<TEnum>(int value)
    where TEnum : struct, Enum, IComparable, IConvertible, IFormattable;

  // Convert Int64 to TEnum
  public static TEnum ToEnum<TEnum>(long value)
    where TEnum : struct, Enum, IComparable, IConvertible, IFormattable;

  // Convert object to TEnum
  public static TEnum ToEnum<TEnum>(object value)
    where TEnum : struct, Enum, IComparable, IConvertible, IFormattable;

  // Convert TEnum to value of underlying type
  public static object ToUnderlying<TEnum>(TEnum value)
    where TEnum : struct, Enum, IComparable, IConvertible, IFormattable;

  // Parse TEnum flags from separated string.
  // If item prefixed with '+' then flag added to value,
  // if item prefixed with '-' then flag removed from value,
  // if item prefixed with '!' then flag inverted in value.
  public static TEnum ParseFlags<TEnum>(this TEnum value, string input, bool ignoreCase, params char[] separators)
    where TEnum : struct, Enum, IComparable, IConvertible, IFormattable;

  // Add flags to value
  public static TEnum CombineFlags<TEnum>(this TEnum value, params TEnum[] flags)
    where TEnum : struct, Enum, IComparable, IConvertible, IFormattable;

  // Overlap flags with value
  public static TEnum OverlapFlags<TEnum>(this TEnum value, params TEnum[] flags)
    where TEnum : struct, Enum, IComparable, IConvertible, IFormattable;

  // Remove flags from value
  public static TEnum RemoveFlags<TEnum>(this TEnum value, params TEnum[] flags)
    where TEnum : struct, Enum, IComparable, IConvertible, IFormattable;

  // Inverse flags in value
  public static TEnum InverseFlags<TEnum>(this TEnum value, TEnum mask)
    where TEnum : struct, Enum, IComparable, IConvertible, IFormattable;

  // Match two flags values
  public static FlagsMatchResult MatchFlags<TEnum>(this TEnum xValue, TEnum yValue)
    where TEnum : struct, Enum, IComparable, IConvertible, IFormattable;

  // Determines whether the values of the flags parameter are set in the value.
  public static bool IsFlagsSet<TEnum>(this TEnum value, TEnum flags)
    where TEnum : struct, Enum, IComparable, IConvertible, IFormattable;

  // Determines whether the values of the flags parameter are overlapped with the value.
  public static bool IsFlagsOverlapped<TEnum>(this TEnum value, TEnum flags)
    where TEnum : struct, Enum, IComparable, IConvertible, IFormattable;
}

public enum FlagsMatchResult
{
    NonOverlap,
    Overlap,
    Equal,
    Belong,
    Enclose
}
```

</details><p/>

#### **`DateTimeInterval`**

A read only struct `DateTimeInterval` contains the start date with time and interval duration. It allso contains some methods for working with values of this type.

<details><summary>Definition</summary>

```csharp
public readonly struct DateTimeInterval : IEquatable<DateTimeInterval>
{
  public DateTimeInterval(DateTime dateTime, TimeSpan timeSpan);

  public DateTimeInterval(DateTime dateTimeStart, DateTime dateTimeEnd);

  public DateTime DateTimeStart { get; }

  public DateTime DateTimeEnd { get; }

  public DateTime DateTime { get; }

  public TimeSpan TimeSpan { get; }

  public DateTimeInterval Shift(TimeSpan shift);

  public DateTimeInterval Shift(TimeSpan shiftStart, TimeSpan shiftEnd);

  public DateTimeIntervalMatchResult Match(DateTimeInterval dateTimeInterval);

  public static DateTimeIntervalMatchResult Match(DateTimeInterval dateTimeFirst, DateTimeInterval dateTimeSecond);
}

public enum DateTimeIntervalMatchResult
{
  Before,
  After,
  OverlapBefore,
  OverlapAfter,
  Equal,
  Belong,
  Enclose,
}
```

</details><p/>

#### **`Safe`**

Methods of the Safe class are for doing actions with the suppression of the exceptions that occur.
The Invoke methods call the passed delegates. The predicate passed as a parameter determines which types of exceptions will be suppressed. Also, one of the parameters is a delegate that will be called in case of exception suppression (can be used for logging). There are exception filtering methods that can be used in the suppression predicate.

<details><summary>Definition</summary>

```csharp
public static class Safe
{
  public static IEnumerable<Exception> EnumerateExceptions(Exception exception, int maxDepth = -1);

  public static bool SuppressException(Exception exception, bool suppress, bool strong, params Type[] exceptionTypes);

  public static bool SuppressExceptions(IEnumerable<Exception> exceptions, bool suppress, bool strong, params Type[] exceptionTypes);

  public static void Invoke(Action action, Predicate<Exception>? suppressPredicate = null, Action<Exception>? suppressAction = null);

  public static T? Invoke<T>(Func<T?> functor, T? defaultResult = default, Predicate<Exception>? suppressPredicate = null, Action<Exception>? suppressAction = null);

  public static T? Invoke<T>(Func<T?> functor, Predicate<Exception>? suppressPredicate = default, Func<Exception, T?>? suppressFunctor = default);
}
```

</details>

---
### **Reflection**

The static *Reflector* class from namespace *PowerLib.System.Reflection* implements methods for both getting and setting field and property values, and calling methods or constructors using reflection. The peculiarity of this functionality is that the search for the desired member (field, property, method, class) is performed by its name, access specifiers and the signature of the types of parameters and arguments, as well as by the value for fields and properties or the return value for methods. All methods are divided into several categories.
The first category is determined by where the member is defined: instance or type. To work with static members, the type is used as the first parameter or generic method argument. To work with instance members, an instance of the required type specified by the first parameter is used. The second category determines whether the method must be performed on the member. If a required member is not found or the required functionality is not available for direct methods an exception is thrown. Methods with optional performing return a Boolean value indicating the success of the required operation, and their name begins with the ***Try*** prefix.
All member access methods require the MemberAccessibility enum parameter to be set, which specifies flags that specify how the required member is to be found:

>  *IgnoreCase* - specifies that the case of the member name should not be considered when member searching;<br/>
  *DeclaredOnly* - specifies that only members declared at the level of the supplied type's hierarchy should be considered. Inherited members are not considered;<br/>
  *FlattenHierarchy* - specifies that public and protected static members up the hierarchy should be returned. Private static members in inherited classes are not returned;<br/>
  *Family* - specifies that members with family access are to be included in the search;<br/>
  *Assembly* - specifies that members with assembly access are to be included in the search;<br/>
  *FamilyOrAssembly* - specifies that members with family or assembly access are to be included in the search;<br/>
  *FamilyAndAssembly* - specifies that members with family and assembly access are to be included in the search;<br/>
  *Private* - specifies that private members are to be included in the search;<br/>
  *Public* - specifies that public members are to be included in the search;<br/>
  *NonPublic* - specifies that non-public members are to be included in the search;<br/>
  *AnyAccess* - specifies that any access members are to be included in the search.<br/>

Also, for all members except for constructors, it is required to specify its name.
All member methods allow a typed stub value of the TypedValue type, in which, along with the value, its type is specified. A *Type* can be a real *Value* type, any of its base types, or any interface it implements.
For static type members with generic arguments, they can be specified in the typeArguments parameter. This list is required when specifying a generic type definition as the type containing the required member. The length of this list must be equal to the number of generic type arguments. Although the values of the arguments that can be specified via the parameters or the specified value or type of the member can be omitted by padding its position with a null value. For members of generic methods, arguments can be specified in the methodArguments parameter. If the type of the argument can be inferred from the parameters or the return value, then the value at the corresponding position can be filled with a null value. Passing empty lists in the above parameters indicates that the type or method is not generic. If the above argument lists are not specified, then control over this parameter is passed to the framework.
For method members, constructors, and indexed properties, it is possible to set parameter values in two collections. The positionalParameterValues list contains the values of the positional parameters. The namedParameterValues dictionary contains the values of the named parameters. Parameters with default values may not be specified. If any parameters in the original method are returned or passed by reference, then, accordingly, after the method call, there will be new values in the position of these parameters of the corresponding collections.
There are also methods that support calling asynchronous methods through reflection.
Reflector can generate two expected errors: a member may not be found, or it may be found ambiguously.

- **Methods for working with fields**
  + Instance fields methods

    <details><summary>Try methods</summary>

    ```csharp
    public static bool TryGetField(object source, string name, MemberAccessibility memberAccessibility, Type? valueType, out object? value);

    public static bool TryGetField<TValue>(object source, string name, MemberAccessibility memberAccessibility, out TValue? value);

    public static bool TryGetField<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, Type? valueType, out object? value);

    public static bool TryGetField<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, out TValue? value);

    public static bool TrySetField(object source, string name, MemberAccessibility memberAccessibility, object? value);

    public static bool TrySetField<TValue>(object source, string name, MemberAccessibility memberAccessibility, TValue? value);

    public static bool TrySetField<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, object? value);

    public static bool TrySetField<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, TValue? value);

    public static bool TryReplaceField(object source, string name, MemberAccessibility memberAccessibility, object? newValue, out object? oldValue);

    public static bool TryReplaceField<TValue>(object source, string name, MemberAccessibility memberAccessibility, TValue? newValue, out TValue? oldValue);

    public static bool TryReplaceField<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, object? newValue, out object? oldValue);

    public static bool TryReplaceField<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, TValue? newValue, out TValue? oldValue);

    public static bool TryExchangeField(object source, string name, MemberAccessibility memberAccessibility, ref object? value);

    public static bool TryExchangeField<TValue>(object source, string name, MemberAccessibility memberAccessibility, ref TValue? value);

    public static bool TryExchangeField<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, ref object? value);

    public static bool TryExchangeField<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, ref TValue? value);
    ```

    </details>
    <details><summary>Direct methods</summary>
  
    ```csharp
    public static object? GetField(object source, string name, MemberAccessibility memberAccessibility, Type? valueType);

    public static TValue? GetField<TValue>(object source, string name, MemberAccessibility memberAccessibility);

    public static object? GetField<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, Type? valueType);

    public static TValue? GetField<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility);

    public static void SetField(object source, string name, MemberAccessibility memberAccessibility, object? value);

    public static void SetField<TValue>(object source, string name, MemberAccessibility memberAccessibility, TValue? value);

    public static void SetField<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, object? value);

    public static void SetField<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, TValue? value);

    public static object? ReplaceField(object source, string name, MemberAccessibility memberAccessibility, object? value);

    public static TValue? ReplaceField<TValue>(object source, string name, MemberAccessibility memberAccessibility, TValue? value);

    public static object? ReplaceField<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, object? value);

    public static TValue? ReplaceField<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, TValue? value);

    public static void ExchangeField(object source, string name, MemberAccessibility memberAccessibility, ref object? value);

    public static void ExchangeField<TValue>(object source, string name, MemberAccessibility memberAccessibility, ref TValue? value);

    public static void ExchangeField<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, ref object? value);

    public static void ExchangeField<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, ref TValue? value);
    ```

    </details>

  + Static fields methods

    <details><summary>Try methods</summary>
  
    ```csharp
    public static bool TryGetField(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, Type? valueType, out object? value);

    public static bool TryGetField<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, out TValue? value);

    public static bool TryGetField<TSource>(string name, MemberAccessibility memberAccessibility, Type? valueType, out object? value);

    public static bool TryGetField<TSource, TValue>(string name, MemberAccessibility memberAccessibility, out TValue? value);

    public static bool TrySetField(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, object? value);

    public static bool TrySetField<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, TValue? value);

    public static bool TrySetField<TSource>(string name, MemberAccessibility memberAccessibility, object? value);

    public static bool TrySetField<TSource, TValue>(string name, MemberAccessibility memberAccessibility, TValue? value);

    public static bool TryReplaceField(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, object? newValue, out object? oldValue);

    public static bool TryReplaceField<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, TValue? newValue, out TValue? oldValue);

    public static bool TryReplaceField<TSource>(string name, MemberAccessibility memberAccessibility, object? newValue, out object? oldValue);

    public static bool TryReplaceField<TSource, TValue>(string name, MemberAccessibility memberAccessibility, TValue? newValue, out TValue? oldValue);

    public static bool TryExchangeField(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, ref object? value);

    public static bool TryExchangeField<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, ref TValue? value);

    public static bool TryExchangeField<TSource>(string name, MemberAccessibility memberAccessibility, ref object? value);

    public static bool TryExchangeField<TSource, TValue>(string name, MemberAccessibility memberAccessibility, ref TValue? value);
    ```

    </details>
    <details><summary>Direct methods</summary>
  
    ```csharp
    public static object? GetField(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, Type? valueType);

    public static TValue? GetField<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments);

    public static object? GetField<TSource>(string name, MemberAccessibility memberAccessibility, Type? valueType);

    public static TValue? GetField<TSource, TValue>(string name, MemberAccessibility memberAccessibility);

    public static void SetField(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, object? value);

    public static void SetField<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, TValue? value);

    public static void SetField<TSource>(string name, MemberAccessibility memberAccessibility, object? value);

    public static void SetField<TSource, TValue>(string name, MemberAccessibility memberAccessibility, TValue? value);

    public static object? ReplaceField(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, object? value);

    public static TValue? ReplaceField<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, TValue? value);

    public static object? ReplaceField<TSource>(string name, MemberAccessibility memberAccessibility, object? value);

    public static TValue? ReplaceField<TSource, TValue>(string name, MemberAccessibility memberAccessibility, TValue? value);

    public static void ExchangeField(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, ref object? value);

    public static void ExchangeField<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, ref TValue? value);

    public static void ExchangeField<TSource>(string name, MemberAccessibility memberAccessibility, ref object? value);

    public static void ExchangeField<TSource, TValue>(string name, MemberAccessibility memberAccessibility, ref TValue? value);
    ```

    </details>
- **Methods for working with properties**
  + Methods for instance properties without indices

    <details><summary>Try methods</summary>

    ```csharp
    public static bool TryGetProperty(object source, string name, MemberAccessibility memberAccessibility, Type? valueType, out object? value);

    public static bool TryGetProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility, out TValue? value);

    public static bool TryGetProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, Type? valueType, out object? value);

    public static bool TryGetProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, out TValue? value);

    public static bool TrySetProperty(object source, string name, MemberAccessibility memberAccessibility, object? value);

    public static bool TrySetProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility, TValue? value);

    public static bool TrySetProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, object? value);

    public static bool TrySetProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, TValue? value);

    public static bool TryReplaceProperty(object source, string name, MemberAccessibility memberAccessibility, object? newValue, out object? oldValue);

    public static bool TryReplaceProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility, TValue? newValue, out TValue? oldValue);

    public static bool TryReplaceProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, object? newValue, out object? oldValue);

    public static bool TryReplaceProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, TValue? newValue, out TValue? oldValue);

    public static bool TryExchangeProperty(object source, string name, MemberAccessibility memberAccessibility, ref object? value);

    public static bool TryExchangeProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility, ref TValue? value);

    public static bool TryExchangeProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, ref object? value);

    public static bool TryExchangeProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, ref TValue? value);
    ```

    </details>
    <details><summary>Direct methods</summary>
  
    ```csharp
    public static object? GetProperty(object source, string name, MemberAccessibility memberAccessibility, Type? valueType);

    public static TValue? GetProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility);

    public static object? GetProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, Type? valueType);

    public static TValue? GetProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility);

    public static void SetProperty(object source, string name, MemberAccessibility memberAccessibility, object? value);

    public static void SetProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility, TValue? value);

    public static void SetProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, object? value);

    public static void SetProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, TValue? value);

    public static object? ReplaceProperty(object source, string name, MemberAccessibility memberAccessibility, object? value);

    public static TValue? ReplaceProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility, TValue? value);

    public static object? ReplaceProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, object? value);

    public static TValue? ReplaceProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, TValue? value);

    public static void ExchangeProperty(object source, string name, MemberAccessibility memberAccessibility, ref object? value);

    public static void ExchangeProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility, ref TValue? value);

    public static void ExchangeProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, ref object? value);

    public static void ExchangeProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, ref TValue? value);
    ```

    </details>

  + Methods for instance properties with indices

    <details><summary>Try methods</summary>

    ```csharp
    public static bool TryGetProperty(object source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType, out object? value);

    public static bool TryGetProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, out TValue? value);

    public static bool TryGetProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType, out object? value);

    public static bool TryGetProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, out TValue? value);

    public static bool TrySetProperty(object source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value);

    public static bool TrySetProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value);

    public static bool TrySetProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value);

    public static bool TrySetProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value);

    public static bool TryReplaceProperty(object source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? newValue, out object? oldValue);

    public static bool TryReplaceProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? newValue, out TValue? oldValue);

    public static bool TryReplaceProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? newValue, out object? oldValue);

    public static bool TryReplaceProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? newValue, out TValue? oldValue);

    public static bool TryExchangeProperty(object source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref object? value);

    public static bool TryExchangeProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref TValue? value);

    public static bool TryExchangeProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref object? value);

    public static bool TryExchangeProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref TValue? value);
    ```

    </details>
    <details><summary>Direct methods</summary>
  
    ```csharp
    public static object? GetProperty(object source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType);

    public static TValue? GetProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues);

    public static object? GetProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType);

    public static TValue? GetProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues);

    public static void SetProperty(object source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value);

    public static void SetProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value);

    public static void SetProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value);

    public static void SetProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value);

    public static object? ReplaceProperty(object source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value);

    public static TValue? ReplaceProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value);

    public static object? ReplaceProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value);

    public static TValue? ReplaceProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value);

    public static void ExchangeProperty(object source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref object? value);

    public static void ExchangeProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref TValue? value);

    public static void ExchangeProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref object? value);

    public static void ExchangeProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref TValue? value);
    ```

    </details>

  + Methods for static properties without indices

    <details><summary>Try methods</summary>

    ```csharp
    public static bool TryGetProperty(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, Type? valueType, out object? value);

    public static bool TryGetProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, out TValue? value);

    public static bool TryGetProperty<TSource>(string name, MemberAccessibility memberAccessibility, Type? valueType, out object? value);

    public static bool TryGetProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility, out TValue? value);

    public static bool TrySetProperty(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, object? value);

    public static bool TrySetProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, TValue? value);

    public static bool TrySetProperty<TSource>(string name, MemberAccessibility memberAccessibility, object? value);

    public static bool TrySetProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility, TValue? value);

    public static bool TryReplaceProperty(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, object? newValue, out object? oldValue);

    public static bool TryReplaceProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, TValue? newValue, out TValue? oldValue);

    public static bool TryReplaceProperty<TSource>(string name, MemberAccessibility memberAccessibility, object? newValue, out object? oldValue);

    public static bool TryReplaceProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility, TValue? newValue, out TValue? oldValue);

    public static bool TryExchangeProperty(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, ref object? value);

    public static bool TryExchangeProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, ref TValue? value);

    public static bool TryExchangeProperty<TSource>(string name, MemberAccessibility memberAccessibility, ref object? value);

    public static bool TryExchangeProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility, ref TValue? value);
    ```

    </details>
    <details><summary>Direct methods</summary>
  
    ```csharp
    public static object? GetProperty(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, Type? valueType);

    public static TValue? GetProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments);

    public static object? GetProperty<TSource>(string name, MemberAccessibility memberAccessibility, Type? valueType);

    public static TValue? GetProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility);

    public static void SetProperty(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, object? value);

    public static void SetProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, TValue? value);

    public static void SetProperty<TSource>(string name, MemberAccessibility memberAccessibility, object? value);

    public static void SetProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility, TValue? value);

    public static object? ReplaceProperty(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, object? value);

    public static TValue? ReplaceProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, TValue? value);

    public static object? ReplaceProperty<TSource>(string name, MemberAccessibility memberAccessibility, object? value);

    public static TValue? ReplaceProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility, TValue? value);

    public static void ExchangeProperty(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, ref object? value);

    public static void ExchangeProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, ref TValue? value);

    public static void ExchangeProperty<TSource>(string name, MemberAccessibility memberAccessibility, ref object? value);

    public static void ExchangeProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility, ref TValue? value);
    ```

    </details>

  + Methods for static properties with indices

    <details><summary>Try methods</summary>

    ```csharp
    public static bool TryGetProperty(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType, out object? value);

    public static bool TryGetProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, out TValue? value);

    public static bool TryGetProperty<TSource>(string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType, out object? value);

    public static bool TryGetProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, out TValue? value);

    public static bool TrySetProperty(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value);

    public static bool TrySetProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value);

    public static bool TrySetProperty<TSource>(string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value);

    public static bool TrySetProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value);

    public static bool TryReplaceProperty(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? newValue, out object? oldValue);

    public static bool TryReplaceProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? newValue, out TValue? oldValue);

    public static bool TryReplaceProperty<TSource>(string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? newValue, out object? oldValue);

    public static bool TryReplaceProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? newValue, out TValue? oldValue);

    public static bool TryExchangeProperty(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref object? value);

    public static bool TryExchangeProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref TValue? value);

    public static bool TryExchangeProperty<TSource>(string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref object? value);

    public static bool TryExchangeProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref TValue? value);
    ```

    </details>
    <details><summary>Direct methods</summary>
  
    ```csharp
    public static object? GetProperty(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType);

    public static TValue? GetProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues);

    public static object? GetProperty<TSource>(string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType);

    public static TValue? GetProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues);

    public static void SetProperty(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value);

    public static void SetProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value);

    public static void SetProperty<TSource>(string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value);

    public static void SetProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value);

    public static object? ReplaceProperty(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value);

    public static TValue? ReplaceProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value);

    public static object? ReplaceProperty<TSource>(string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value);

    public static TValue? ReplaceProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value);

    public static void ExchangeProperty(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref object? value);

    public static void ExchangeProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref TValue? value);

    public static void ExchangeProperty<TSource>(string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref object? value);

    public static void ExchangeProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref TValue? value);
    ```

    </details>

- **Methods for working with methods**
  + Methods for instance methods

    <details><summary>Try methods</summary>

    ```csharp
    public static bool TryCallMethod(object source, string name, MemberAccessibility memberAccessibility, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues);

    public static bool TryCallMethod<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues);

    public static bool TryCallMethod(object source, string name, MemberAccessibility memberAccessibility, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType, out object? result);

    public static bool TryCallMethod<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType, out object? result);

    public static async Task<bool> TryCallMethodAsync(object source, string name, MemberAccessibility memberAccessibility, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues);

    public static async Task<bool> TryCallMethodAsync<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues);

    public static async Task<TryOut<object>> TryCallMethodAsync(object source, string name, MemberAccessibility memberAccessibility, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType);

    public static async Task<TryOut<object>> TryCallMethodAsync<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType);
    ```

    </details>
    <details><summary>Direct methods</summary>

    ```csharp
    public static void CallMethod(object source, string name, MemberAccessibility memberAccessibility, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues);

    public static void CallMethod<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues);

    public static object? CallMethod(object source, string name, MemberAccessibility memberAccessibility, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType);

    public static object? CallMethod<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType);

    public static async Task CallMethodAsync(object source, string name, MemberAccessibility memberAccessibility, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues);

    public static async Task CallMethodAsync<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues);

    public static async Task<object?> CallMethodAsync(object source, string name, MemberAccessibility memberAccessibility, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType);

    public static async Task<object?> CallMethodAsync<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType);
    ```

    </details>

  + Methods for static methods

    <details><summary>Try methods</summary>

    ```csharp
    public static bool TryCallMethod(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues);

    public static bool TryCallMethod<TSource>(string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues);

    public static bool TryCallMethod(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType, out object? result);

    public static bool TryCallMethod<TSource>(string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType, out object? result);

    public static async Task<bool> TryCallMethodAsync(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues);

    public static async Task<bool> TryCallMethodAsync<TSource>(string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues);

    public static async Task<TryOut<object>> TryCallMethodAsync(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType);

    public static async Task<TryOut<object>> TryCallMethodAsync<TSource>(string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType);
    ```

    </details>
    <details><summary>Direct methods</summary>

    ```csharp
    public static void CallMethod(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues);

    public static void CallMethod<TSource>(string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues);

    public static object? CallMethod(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType);

    public static object? CallMethod<TSource>(string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType);

    public static async Task CallMethodAsync(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues);

    public static async Task CallMethodAsync<TSource>(string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues);

    public static async Task<object?> CallMethodAsync(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType);

    public static async Task<object?> CallMethodAsync<TSource>(string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType);
    ```

    </details>

- **Methods for working with constructors**

  <details><summary>Try methods</summary>

  ```csharp
  public static bool TryConstruct(Type type, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, out object? result);

  public static bool TryConstruct<TSource>(MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, out object? result);
  ```

  </details>
  <details><summary>Direct methods</summary>

  ```csharp
  public static object Construct(Type type, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues);

  public static object Construct<TSource>(MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues);
  ```

  </details>

<details><summary>Sample</summary>

```csharp
using System;
using System.Collections.Generic;
using PowerLib.System.Reflection;

namespace ReflectorTestApp;

internal class A<T>
{
  protected readonly T _default;

  protected A(T value)
  {
    _default = value;
  }
}

internal class B<T> : A<T>
{
  protected B(T value)
    : base(value)
  { }

  public B()
    : base(default)
  { }

  public T Default
    => _default;

  public void AddItem<L>(L list, T item)
    where L : IList<T>
    => list.Add(item is null ? Default : item);

  public void GetItem<L>(L list, int index, out T result)
    where L : IList<T>
    => result = list[index];

  private bool ExchangeItem<L>(L list, int index, ref T item, bool reserved = false, bool skipOutOfRange = false)
    where L : List<T>
  {
    if (skipOutOfRange && index >= list.Count)
      return false;
    var curr = list[index];
    list[index] = item;
    item = curr;
    return true;
  }
}

internal static class ReflectorTest
{
  internal static void Test()
  {
    var list = new List<double>();

    //  Create instance of class B<T>.
    var posParams_1 = new object[] { 99.5d };
    var typeArgs_1 = new Type[] { null };
    var inst_B = Reflector.Construct(typeof(B<>), MemberAccessibility.Family, typeArgs_1, posParams_1, null);
    //var inst_B = Reflector.Construct(typeof(B<double>), MemberAccessibility.Family, null, posParams_1, null);
    //var inst_B = Reflector.Construct<B<double>>(MemberAccessibility.Family, null, posParams_1, null);
    //var inst_B = Reflector.Construct(typeof(B<>), MemberAccessibility.Public, new[] { typeof(double) }, null, null);
    //var inst_B = Reflector.Construct(typeof(B<double>), MemberAccessibility.Public, null, null, null);
    // inst_B instance of B<double>, typeArgs_1[0] == typeof(double)

    //  Add value 678.99d to empty list.
    var posParams_2 = new object?[] { list, 678.99d };
    Reflector.CallMethod(inst_B, "AddItem", MemberAccessibility.Public, null, posParams_2, null);
    // list[0] == 678.99

    // Get list item at index 0 via output parameter and detect method argument type.
    var posParams_3 = new object[] { list, 0, null };
    var methodArgs_3 = new Type[] { null };
    Reflector.CallMethod(inst_B, "GetItem", MemberAccessibility.Public, methodArgs_3, posParams_3, null);
    // methodArgs_3[0] == typeof(List<double>), posParams_3[2] == 678.99

    // Exchange list item at index 0 via value reference.
    var posParams_4 = new object[] { list, 0, -56789.1234d };
    var namedParams_4 = new Dictionary<string, object>() { { "SKIPOUTOFRANGE", true } };
    var result_4 = Reflector.CallMethod(inst_B, "ExchangeITEM", MemberAccessibility.Private | MemberAccessibility.IgnoreCase, null, posParams_4, namedParams_4, typeof(bool));
    //var result_4 = Reflector.CallMethod(inst_B, "ExchangeITEM", MemberAccessibility.Private | MemberAccessibility.IgnoreCase, null, posParams_4, namedParams_4, null);
    // result_4 == true, posParams_4[2] == 678.99, list[0] == -56789.1234

    //  Get internal '_default' field and 'Default' property.
    var field_5 = Reflector.GetField(inst_B, "_default", MemberAccessibility.Family, null);
    var prop_5 = Reflector.GetProperty<double>(inst_B, "Default", MemberAccessibility.Public);
    // field_5 == 99.5, prop_5 == 99.5
  }
}
```

</details>

---
### Resources

There are several simple and useful classes for mapping keys of a specified type to resources. The main base class is the `ResourceAccessor<TKey>` abstract class from the PowerLib.System.Resources namespace. It provides public methods for getting resources (strings, streams, or other objects). Classes that inherit from it must ensure that the key value is converted to a string representation of the resource identifier. Resource access methods contain a formatProvider parameter of type IFormatProvider. If it can be cast to or through a CultureInfo object, then the requested resource is looked up in the appropriate locale. If it was not possible to get the CultureInfo, then the default CultureInfo will be used.
The `CustomResourceAccessor<TKey>` class inherits the `ResourceAccessor<TKey>` class, one of its constructor parameters is a delegate that maps the generic TKey type to a resource identifier. Another class, `EnumResourceAccessor<TKey>`, accepts only an enumerated type as a TKey key type, and its name is a resource identifier. If no resource source is specified in the constructor, then the resource manager will attempt to load the resources attached to the TKey.
To work with such a mapper for resource files (.resx), it is necessary to remove the wrapper class generation in the Custom Tool property, because it is no longer required.

<details><summary>Methods to access resources</summary>
  
```csharp
public string FormatString(TKey key, params object[] args);

public string FormatString(IFormatProvider? formatProvider, TKey key, params object?[] args);

public string? GetString(TKey key);

public string? GetString(TKey key, IFormatProvider? formatProvider);

public Stream? GetStream(TKey key);

public Stream? GetStream(TKey key, IFormatProvider? formatProvider);

public object? GetObject(TKey key);

public object? GetObject(TKey key, IFormatProvider? formatProvider);
```

</details>

<details><summary>EnumResourceAccessor constructors</summary>

```csharp
public EnumResourceAccessor();

public EnumResourceAccessor(CultureInfo? cultureInfo);

public EnumResourceAccessor(Type resourceSource);

public EnumResourceAccessor(Type resourceSource, CultureInfo? cultureInfo);

public EnumResourceAccessor(ResourceManager resourceManager);

public EnumResourceAccessor(ResourceManager resourceManager, CultureInfo? cultureInfo);
```

</details>
<details><summary>CustomResourceAccessor constructors</summary>

```csharp
public CustomResourceAccessor(Func<TKey, string> keySelector);

public CustomResourceAccessor(Func<TKey, string> keySelector, CultureInfo? cultureInfo);

public CustomResourceAccessor(Type resourceSource, Func<TKey, string> keySelector);

public CustomResourceAccessor(Type resourceSource, Func<TKey, string> keySelector, CultureInfo? cultureInfo);

public CustomResourceAccessor(ResourceManager resourceManager, Func<TKey, string> keySelector);

public CustomResourceAccessor(ResourceManager resourceManager, Func<TKey, string> keySelector, CultureInfo? cultureInfo);
```

</details>

--- Arrays

Will be added and described a little later...

---
### Collections

### Equality and comparison

There are a lot of classes that implement the equality and comparison interfaces and extension methods for working with them.
Added two delegates `Equality<T>` and `Comparator<T>` and interface `IComparator<T>`. 
The `Equality<T>` delegate is a delegate for equality comparison methods such as Equals of the `IEqualityComparer<T>` interface.
The `Comparator<T>` delegate is a delegate for single-parameter comparison methods, such as the `Compare` method of the `IComparator<T>` interface. This interface is used, for example, in search methods on sorted data. In the search method, instead of a pattern for searching and an comparer interface with two parameters, a method with one parameter is passed, which will be called by the search function with a list element. The pattern comparison logic will be in the comparator. 

```csharp

public delegate bool Equality<in T>(T x, T y);

public delegate int Comparator<in T>(T v);

public interface IComparator<in T>
{
  int Compare(T? obj);
}
```

There are extension methods and classes where, in conjunction with the `IEqualityComparer<T>` interface or the `Equality<T>` delegate, an additional boolean `nullVarious` parameter is used for nullable values. This parameter affects the comparison of `null` values and when set, two `null` values are considered unequal. Also found in conjunction with the `IComparer<T>` interface or the `Comparison<T>` delegate for nullable values is an additional `nullOrder` parameter of type `RelativeOrder`. This parameter indicates the order of null values. When set to `RelativeOrder.Lower`, `null` values always precede any other values. With `RelativeOrder.Upper` , `null` values are always behind any other values.

Also, added several enum types with criteria for matching the condition:

- `ComparisonCriteria` - criteria for comparing values.

  ```csharp
  public enum ComparisonCriteria
  {
    Equal,
    NotEqual,
    LessThan,
    GreaterThan,
    LessThanOrEqual,
    GreaterThanOrEqual,
  }
  ```

- `BetweenCriteria` - criteria for entering a value in the interval.

  ```csharp
  public enum BetweenCriteria
  {
    IncludeBoth,
    ExcludeLower,
    ExcludeUpper,
    ExcludeBoth
  }
  ```

- `GroupCriteria` - criteria for grouping conditions.

  ```csharp
  public enum GroupCriteria
  {
    And,
    Or
  }
  ```

- `QuantifyCriteria` - criteria for the set of elements that satisfy the condition.

  ```csharp
  public enum QuantifyCriteria
  {
    Any,
    All
  }
  ```

#### **List extensions**

The ListExtension class contains a lot of extension methods for working with objects that implement the IList interface. For the non-generic *IList* interface, the methods of the *PowerLib.System.Collections.NonGeneric.Extensions.ListExtension* class are used. For the generic `IList<T>` interface, methods of the *PowerLib.System.Collections.Generic.Extensions.ListExtension* class are used. Extension methods are divided into several sections:

- **Getting methods**

  The methods in this section return an item or a range of items from a list.
  
  <details><summary>Methods</summary>
  
  ```csharp
  public static TSource GetAt<TSource>(this IList<TSource> list, int index);

  public static TSource GetBound<TSource>(this IList<TSource> list, Bound bound);

  public static TSource GetFirst<TSource>(this IList<TSource> list);

  public static TSource GetLast<TSource>(this IList<TSource> list);

  public static bool TryGetBound<TSource>(this IList<TSource> list, Bound bound, out TSource? result);

  public static bool TryGetFirst<TSource>(this IList<TSource> list, out TSource? result);

  public static bool TryGetLast<TSource>(this IList<TSource> list, out TSource? result);

  public static IReadOnlyList<TSource> GetRange<TSource>(this IList<TSource> list, int index, int count);

  public static IReadOnlyList<TSource> GetRange<TSource>(this IList<TSource> list, (int index, int count) range);
  ```
  
  </details>

- **Setting methods**

  The methods in this section set an item or a range of items in a list.

  <details><summary>Methods</summary>

  ```csharp
  public static void SetAt<TSource>(this IList<TSource> list, int index, TSource item);

  public static void SetBound<TSource>(this IList<TSource> list, Bound bound, TSource item);

  public static void SetFirst<TSource>(this IList<TSource> list, TSource item);

  public static void SetLast<TSource>(this IList<TSource> list, TSource item);

  public static bool TrySetBound<TSource>(this IList<TSource> list, Bound bound, TSource item);

  public static bool TryGetFirst<TSource>(this IList<TSource> list, TSource item);

  public static bool TrySetLast<TSource>(this IList<TSource> list, TSource item);

  public static void SetRepeat<TSource>(this IList<TSource> list, int index, TSource value, int count);

  public static void SetRange<TSource>(this IList<TSource> list, int index, IEnumerable<TSource> items);

  public static void SetRange<TSource>(this IList<TSource> list, int index, int count, IEnumerable<TSource> items);
  ```

  </details>

- **Adding methods**

  The methods in this section add an item or a range of items to a list.
  <details><summary>Methods</summary>

  ```csharp

  public static void AddBound<TSource>(this IList<TSource> list, Bound bound, TSource item);

  public static void AddFirst<TSource>(this IList<TSource> list, TSource item);

  public static void AddLast<TSource>(this IList<TSource> list, TSource item);

  public static void AddRepeat<TSource>(this IList<TSource> list, TSource value, int count);

  public static void AddRange<TSource>(this IList<TSource> list, IEnumerable<TSource> items);

  ```

  </details>

- **Inserting methods**

  The methods in this section insert a range of items to a list.

  <details><summary>Methods</summary>

  ```csharp
  public static void InsertRepeat<T>(this IList<T> list, int index, T value, int count);

  public static void InsertRange<T>(this IList<T> list, int index, IEnumerable<T> items);
  ```

  </details>

- **Removing methods**

  The methods in this section remove a range of items from a list.

  <details><summary>Methods</summary>

  ```csharp
  public static void RemoveRange<T>(this IList<T> list, int index, int count);

  public static void RemoveRange<TSource>(this IList<TSource> list, (int index, int count) range);
  ```

  </details>

- **Taking methods**

  The methods in this section remove an item or a range of items from a list and return them to caller.

  <details><summary>Methods</summary>

  ```csharp
  public static T TakeAt<T>(this IList<T> list, int index);

  public static T TakeBound<T>(this IList<T> list, Bound bound);

  public static T TakeFirst<T>(this IList<T> list);

  public static T TakeLast<T>(this IList<T> list);

  public static bool TryTakeBound<T>(this IList<T> list, Bound bound, out T? result);

  public static bool TryTakeFirst<T>(this IList<T> list, out T? result);

  public static bool TryTakeLast<T>(this IList<T> list, out T? result);

  public static IReadOnlyList<T> TakeRange<T>(this IList<T> list, int index, int count);

  public static IReadOnlyList<T> TakeRange<T>(this IList<T> list, (int index, int count) range);
  ```

  </details>

- **Replacing methods**

  The methods in this section set an item or a range of items in a list and return replaced values to caller.

  <details><summary>Methods</summary>

  ```csharp
  public static T ReplaceAt<T>(this IList<T> list, int index, T item);

  public static T ReplaceBound<T>(this IList<T> list, Bound bound, T item);

  public static T ReplaceFirst<T>(this IList<T> list, T item);

  public static T ReplaceLast<T>(this IList<T> list, T item);

  public static bool TryReplaceBound<T>(this IList<T?> list, Bound bound, T? newItem, out T? oldItem);

  public static bool TryReplaceFirst<T>(this IList<T?> list, T? newItem, out T? oldItem);

  public static bool TryReplaceLast<T>(this IList<T?> list, T? newItem, out T? oldItem);

  public static void ExchangeAt<T>(this IList<T> list, int index, ref T item);

  public static void ExchangeBound<T>(this IList<T> list, Bound bound, ref T item);

  public static void ExchangeFirst<T>(this IList<T> list, ref T item);

  public static void ExchangeLast<T>(this IList<T> list, ref T item);

  public static bool TryExchangeBound<T>(this IList<T> list, Bound bound, ref T item);

  public static bool TryExchangeFirst<T>(this IList<T> list, ref T item);

  public static bool TryExchangeLast<T>(this IList<T> list, ref T item);

  public static IReadOnlyList<T> ReplaceRange<T>(this IList<T> list, int index, IEnumerable<T> items);

  public static IReadOnlyList<T> ReplaceRange<T>(this IList<T> list, int index, int count, IEnumerable<T> items);

  public static IReadOnlyList<T> ReplaceRange<T>(this IList<T> list, (int index, int count) range, IEnumerable<T> items);
  ```

  </details>

- **Moving methods**

  The methods in this section move an item or a range of items in a list.

  <details><summary>Methods</summary>

  ```csharp
  public static void Move<T>(this IList<T> list, int sIndex, int dIndex);

  public static void MoveRange<T>(this IList<T> list, int sIndex, int dIndex, int count);
  ```

  </details>

- **Swapping methods**

  The methods in this section swap two items or two ranges of items with each other in a list.

  <details><summary>Methods</summary>

  ```csharp
  public static void Swap<T>(this IList<T> list, int xIndex, int yIndex);

  public static void SwapRange<T>(this IList<T> list, int xIndex, int yIndex, int count);

  public static void SwapRanges<T>(this IList<T> list, int xIndex, int xCount, int yIndex, int yCount);
  ```

  </details>

- **Reversing methods**

  The methods in this section reverse a range of items in a list.

  <details><summary>Methods</summary>

  ```csharp
  public static void Reverse<T>(this IList<T> list);

  public static void Reverse<T>(this IList<T> list, int index);

  public static void Reverse<T>(this IList<T> list, int index, int count);

  public static void Reverse<T>(this IList<T> list, (int index, int count) range);
  ```

  </details>

- **Sort manipulation methods**

  The methods in this section manipulate an item in a sorted list.

  <details><summary>Methods</summary>

  ```csharp
  public static int AddSorted<T>(this IList<T> list, T item, Comparison<T> comparison, SortingOption option = SortingOption.None);

  public static bool InsertSorted<T>(this IList<T> list, int index, T item, Comparison<T> comparison, SortingOption option = SortingOption.None);

  public static bool SetSorted<T>(this IList<T> list, int index, T item, Comparison<T> comparison, SortingOption option = SortingOption.None);
  ```

  </details>

- **Filling methods**

  The methods in this section fill a range of items in a list.

  <details><summary>Methods</summary>

  ```csharp
  public static void Fill<TSource>(this IList<TSource> list, TSource value);

  public static void Fill<TSource>(this IList<TSource> list, int index, TSource value);

  public static void Fill<TSource>(this IList<TSource> list, int index, int count, TSource value);

  public static void Fill<TSource>(this IList<TSource> list, (int index, int count) range, TSource value);

  public static void Fill<TSource>(this IList<TSource> list, Func<TSource> filler);

  public static void Fill<TSource>(this IList<TSource> list, int index, Func<TSource> filler);

  public static void Fill<TSource>(this IList<TSource> list, int index, int count, Func<TSource> filler);

  public static void Fill<TSource>(this IList<TSource> list, (int index, int count) range, Func<TSource> filler);

  public static void Fill<TSource>(this IList<TSource> list, Func<int, TSource> filler);

  public static void Fill<TSource>(this IList<TSource> list, int index, Func<int, TSource> filler);

  public static void Fill<TSource>(this IList<TSource> list, int index, int count, Func<int, TSource> filler);

  public static void Fill<TSource>(this IList<TSource> list, (int index, int count) range, Func<int, TSource> filler);
  ```

  </details>

- **Applying methods**

  The methods in this section apply action to a range of items in a list.

  <details><summary>Methods</summary>

  ```csharp
  public static void Apply<TSource>(this IList<TSource> list, Action<TSource> action);

  public static void Apply<TSource>(this IList<TSource> list, int index, Action<TSource> action);

  public static void Apply<TSource>(this IList<TSource> list, int index, int count, Action<TSource> action);

  public static void Apply<TSource>(this IList<TSource> list, (int index, int count) range, Action<TSource> action);

  public static void Apply<TSource>(this IList<TSource> list, ElementAction<TSource> action);

  public static void Apply<TSource>(this IList<TSource> list, int index, ElementAction<TSource> action);

  public static void Apply<TSource>(this IList<TSource> list, int index, int count, ElementAction<TSource> action);

  public static void Apply<TSource>(this IList<TSource> list, (int index, int count) range, ElementAction<TSource> action);
  ```
  
  </details>

- **Sorting methods**

  There are implemented six sorting algorithms: Bubble, Selection, Insertion, Merge, Quick, Heap. For each algorithm, there are many methods with different signatures that are combined from three blocks: sorting data, range of items to be sorted and comparator as delegate or interface. The data to be sorted can be specified as a single list of values or two paired lists of keys and associated values. The sorting range may not be specified, which means the entire list will be sorted. It can be specified by the start index and defined to the end of the list. It can also be specified by the start index and elements count, or by a tuple containing both of these. Below are the method signatures of the list sorting extensions. The ***Algorithm*** prefix at the beginning of the method name should be replaced with the name of the sorting algorithm used: Bubble, Selection, Insertion, Merge, Quick, Heap.

  <details><summary>Methods</summary>

  ```csharp
  public static void AlgorithmSort<TSource>(this IList<TSource> list);

  public static void AlgorithmSort<TSource>(this IList<TSource> list, int index);

  public static void AlgorithmSort<TSource>(this IList<TSource> list, int index, int count);

  public static void AlgorithmSort<TSource>(this IList<TSource> list, (int index, int count) range);

  public static void AlgorithmSort<TSource>(this IList<TSource> list, Comparison<TSource>? comparison);

  public static void AlgorithmSort<TSource>(this IList<TSource> list, int index, Comparison<TSource>? comparison);

  public static void AlgorithmSort<TSource>(this IList<TSource> list, int index, int count, Comparison<TSource>? comparison);

  public static void AlgorithmSort<TSource>(this IList<TSource> list, (int index, int count) range, Comparison<TSource>? comparison);

  public static void AlgorithmSort<TSource>(this IList<TSource> list, IComparer<TSource>? comparer);

  public static void AlgorithmSort<TSource>(this IList<TSource> list, int index, IComparer<TSource>? comparer);

  public static void AlgorithmSort<TSource>(this IList<TSource> list, int index, int count, IComparer<TSource>? comparer);

  public static void AlgorithmSort<TSource>(this IList<TSource> list, (int index, int count) range, IComparer<TSource>? comparer);

  public static void AlgorithmSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys);

  public static void AlgorithmSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index);

  public static void AlgorithmSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, int count);

  public static void AlgorithmSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, (int index, int count) range);

  public static void AlgorithmSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, Comparison<TKey>? comparison);

  public static void AlgorithmSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, Comparison<TKey>? comparison);

  public static void AlgorithmSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, int count, Comparison<TKey>? comparison);

  public static void AlgorithmSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, (int index, int count) range, Comparison<TKey>? comparison);

  public static void AlgorithmSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, IComparer<TKey>? comparer);

  public static void AlgorithmSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, IComparer<TKey>? comparer);

  public static void AlgorithmSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, int index, int count, IComparer<TKey>? comparer);

  public static void AlgorithmSort<TKey, TSource>(this IList<TSource> list, IList<TKey> keys, (int index, int count) range, IComparer<TKey>? comparer);
  ```

  </details>

- **Enumerating methods**

  The methods in this section enumerate a range of items from a list.
  
  <details><summary>Methods</summary>
  
  ```csharp
  public static IEnumerable<TSource> Enumerate<TSource>(this IList<TSource> list, bool reverse = false);

  public static IEnumerable<TSource> Enumerate<TSource>(this IList<TSource> list, int index, bool reverse = false);

  public static IEnumerable<TSource> Enumerate<TSource>(this IList<TSource> list, int index, int count, bool reverse = false);

  public static IEnumerable<TSource> Enumerate<TSource>(this IList<TSource> list, (int index, int count) range, bool reverse = false);
  ```

  </details>

- **Copying methods**

  The methods in this section copy a range of items from one to another list.

  <details><summary>Methods</summary>

  ```csharp
  public static void Copy<TSource>(this IList<TSource> srcList, IList<TSource> dstList, bool reverse = false);

  public static void Copy<TSource>(this IList<TSource> srcList, IList<TSource> dstList, int dstIndex, int srcIndex, bool reverse = false);

  public static void Copy<TSource>(this IList<TSource> srcList, IList<TSource> dstList, int dstIndex, int srcIndex, int count, bool reverse = false);

  public static void Copy<TSource>(this IList<TSource> srcList, IList<TSource> dstList, int dstIndex, (int index, int count) srcRange, bool reverse = false);
  ```

  </details>

- **Matching methods**

  The methods in this section match a range of items in a list.

  <details><summary>Methods</summary>

  ```csharp
  public static bool Match<T>(this IList<T> list, Predicate<T> predicate, bool all);

  public static bool Match<T>(this IList<T> list, int index, Predicate<T> predicate, bool all);

  public static bool Match<T>(this IList<T> list, int index, int count, Predicate<T> predicate, bool all);

  public static bool Match<T>(this IList<T> list, (int index, int count) range, Predicate<T> predicate, bool all);

  public static bool Match<T>(this IList<T> list, ElementPredicate<T> predicate, bool all);

  public static bool Match<T>(this IList<T> list, int index, ElementPredicate<T> predicate, bool all);

  public static bool Match<T>(this IList<T> list, int index, int count, ElementPredicate<T> predicate, bool all);

  public static bool Match<T>(this IList<T> list, (int index, int count) range, ElementPredicate<T> predicate, bool all);

  public static bool MatchAny<T>(this IList<T> list, Predicate<T> predicate);

  public static bool MatchAny<T>(this IList<T> list, int index, Predicate<T> predicate);

  public static bool MatchAny<T>(this IList<T> list, int index, int count, Predicate<T> predicate);

  public static bool MatchAny<T>(this IList<T> list, (int index, int count) range, Predicate<T> predicate);

  public static bool MatchAny<T>(this IList<T> list, ElementPredicate<T> predicate);

  public static bool MatchAny<T>(this IList<T> list, int index, ElementPredicate<T> predicate);

  public static bool MatchAny<T>(this IList<T> list, int index, int count, ElementPredicate<T> predicate);

  public static bool MatchAny<T>(this IList<T> list, (int index, int count) range, ElementPredicate<T> predicate);

  public static bool MatchAll<T>(this IList<T> list, Predicate<T> predicate);

  public static bool MatchAll<T>(this IList<T> list, int index, Predicate<T> predicate);

  public static bool MatchAll<T>(this IList<T> list, int index, int count, Predicate<T> predicate);

  public static bool MatchAll<T>(this IList<T> list, (int index, int count) range, Predicate<T> predicate);

  public static bool MatchAll<T>(this IList<T> list, ElementPredicate<T> predicate);

  public static bool MatchAll<T>(this IList<T> list, int index, ElementPredicate<T> predicate);

  public static bool MatchAll<T>(this IList<T> list, int index, int count, ElementPredicate<T> predicate);

  public static bool MatchAll<T>(this IList<T> list, (int index, int count) range, ElementPredicate<T> predicate);
  ```
  
  </details>

- **Element index finding methods**

  The methods in this section find the index of the element that matches the predicate.

  <details><summary>Methods</summary>

  ```csharp
  public static int FindIndex<T>(this IList<T> list, Predicate<T> predicate);

  public static int FindIndex<T>(this IList<T> list, int index, Predicate<T> predicate);

  public static int FindIndex<T>(this IList<T> list, int index, int count, Predicate<T> predicate);

  public static int FindIndex<T>(this IList<T> list, (int index, int count) range, Predicate<T> predicate);

  public static int FindIndex<T>(this IList<T> list, ElementPredicate<T> predicate);

  public static int FindIndex<T>(this IList<T> list, int index, ElementPredicate<T> predicate);

  public static int FindIndex<T>(this IList<T> list, int index, int count, ElementPredicate<T> predicate);

  public static int FindIndex<T>(this IList<T> list, (int index, int count) range, ElementPredicate<T> predicate);

  public static int FindLastIndex<T>(this IList<T> list, Predicate<T> predicate);

  public static int FindLastIndex<T>(this IList<T> list, int index, Predicate<T> predicate);

  public static int FindLastIndex<T>(this IList<T> list, int index, int count, Predicate<T> predicate);

  public static int FindLastIndex<T>(this IList<T> list, (int index, int count) range, Predicate<T> predicate);

  public static int FindLastIndex<T>(this IList<T> list, ElementPredicate<T> predicate);

  public static int FindLastIndex<T>(this IList<T> list, int index, ElementPredicate<T> predicate);

  public static int FindLastIndex<T>(this IList<T> list, int index, int count, ElementPredicate<T> predicate);

  public static int FindLastIndex<T>(this IList<T> list, (int index, int count) range, ElementPredicate<T> predicate);
  ```

  </details>

- **Elements finding methods**

  The methods in this section find all elements in the range of the list that match the predicate.

  <details><summary>Methods</summary>

  ```csharp
  public static IEnumerable<T> FindAll<T>(this IList<T> list, Predicate<T> predicate, bool reverse = false);

  public static IEnumerable<T> FindAll<T>(this IList<T> list, int index, Predicate<T> predicate, bool reverse = false);

  public static IEnumerable<T> FindAll<T>(this IList<T> list, int index, int count, Predicate<T> predicate, bool reverse = false);

  public static IEnumerable<T> FindAll<T>(this IList<T> list, (int index, int count) range, Predicate<T> predicate, bool reverse = false);

  public static IEnumerable<T> FindAll<T>(this IList<T> list, ElementPredicate<T> predicate, bool reverse = false);

  public static IEnumerable<T> FindAll<T>(this IList<T> list, int index, ElementPredicate<T> predicate, bool reverse = false);

  public static IEnumerable<T> FindAll<T>(this IList<T> list, int index, int count, ElementPredicate<T> predicate, bool reverse = false);

  public static IEnumerable<T> FindAll<T>(this IList<T> list, (int index, int count) range, ElementPredicate<T> predicate, bool reverse = false);
  ```

  </details>

- **Elements indices finding methods**

  The methods in this section find all elements in the range of the list that match the predicate and return an enumeration of their indices.

  <details><summary>Methods</summary>

  ```csharp
  public static IEnumerable<int> FindAllIndices<T>(this IList<T> list, Predicate<T> predicate, bool reverse = false);

  public static IEnumerable<int> FindAllIndices<T>(this IList<T> list, int index, Predicate<T> predicate, bool reverse = false);

  public static IEnumerable<int> FindAllIndices<T>(this IList<T> list, int index, int count, Predicate<T> predicate, bool reverse = false);

  public static IEnumerable<int> FindAllIndices<T>(this IList<T> list, (int index, int count) range, Predicate<T> predicate, bool reverse = false);

  public static IEnumerable<int> FindAllIndices<T>(this IList<T> list, ElementPredicate<T> predicate, bool reverse = false);

  public static IEnumerable<int> FindAllIndices<T>(this IList<T> list, int index, ElementPredicate<T> predicate, bool reverse = false);

  public static IEnumerable<int> FindAllIndices<T>(this IList<T> list, int index, int count, ElementPredicate<T> predicate, bool reverse = false);

  public static IEnumerable<int> FindAllIndices<T>(this IList<T> list, (int index, int count) range, ElementPredicate<T> predicate, bool reverse = false);
  ```

  </details>

- **Element index binary searching methods**

  The methods in this section perform binary search for the position of an element in a sorted list.

  <details><summary>Methods</summary>

  ``` csharp
  public static int BinarySearch<T>(this IList<T> list, Comparator<T> comparator, SearchingOption option = SearchingOption.None);

  public static int BinarySearch<T>(this IList<T> list, int index, Comparator<T> comparator, SearchingOption option = SearchingOption.None);

  public static int BinarySearch<T>(this IList<T> list, int index, int count, Comparator<T> comparator, SearchingOption option = SearchingOption.None);

  public static int BinarySearch<T>(this IList<T> list, (int index, int count) range, Comparator<T> comparator, SearchingOption option = SearchingOption.None);
  ```

  </details>

- **Element index interpolation searching methods**

  The methods in this section perform interpolation search for the position of an element in a sorted list.

  <details><summary>Methods</summary>

  ``` csharp
  public static int InterpolationSearch<T>(this IList<T> list, Comparator<T> comparator, Func<T, T, float> interpolator, SearchingOption option = SearchingOption.None);

  public static int InterpolationSearch<T>(this IList<T> list, int index, Comparator<T> comparator, Func<T, T, float> interpolator, SearchingOption option = SearchingOption.None);

  public static int InterpolationSearch<T>(this IList<T> list, int index, int count, Comparator<T> comparator, Func<T, T, float> interpolator, SearchingOption option = SearchingOption.None);

  public static int InterpolationSearch<T>(this IList<T> list, (int index, int count) range, Comparator<T> comparator, Func<T, T, float> interpolator, SearchingOption option = SearchingOption.None);
  ```

  </details>

- **Sequence comparing methods**

  The methods in this section compare elements of two lists. Parameter *emptyOrder* sets the mode for comparing elements of two sequences with their different lengths. The comparison is performed element by element, and if at the current step the values of the sequences are element by element equal and the end is reached on one of them, then with the RelativeOrder.Lower value, the longer sequence is considered to be greater than the short sequence. With a value of RelativeOrder.Upper, a shorter sequence is considered to be greater than the long sequence.

  <details><summary>Methods</summary>

  ``` csharp
  public static int SequenceCompare<T>(this IList<T> xList, IList<T> yList);

  public static int SequenceCompare<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex);

  public static int SequenceCompare<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, int count);

  public static int SequenceCompare<T>(this IList<T> xList, IList<T> yList, Comparison<T>? comparison);

  public static int SequenceCompare<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, Comparison<T>? comparison);

  public static int SequenceCompare<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, Comparison<T> comparison);

  public static int SequenceCompare<T>(this IList<T> xList, IList<T> yList, IComparer<T>? comparer);

  public static int SequenceCompare<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, IComparer<T>? comparer);

  public static int SequenceCompare<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, IComparer<T>? comparer);

  public static int SequenceCompare<T>(this IList<T> xList, IList<T> yList, RelativeOrder emptyOrder);

  public static int SequenceCompare<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, RelativeOrder emptyOrder);

  public static int SequenceCompare<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, RelativeOrder emptyOrder);

  public static int SequenceCompare<T>(this IList<T> xList, IList<T> yList, Comparison<T>? comparison, RelativeOrder emptyOrder);

  public static int SequenceCompare<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, Comparison<T>? comparison, RelativeOrder emptyOrder);

  public static int SequenceCompare<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, Comparison<T> comparison, RelativeOrder emptyOrder);

  public static int SequenceCompare<T>(this IList<T> xList, IList<T> yList, IComparer<T>? comparer, RelativeOrder emptyOrder);

  public static int SequenceCompare<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, IComparer<T>? comparer, RelativeOrder emptyOrder);

  public static int SequenceCompare<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, IComparer<T>? comparer, RelativeOrder emptyOrder);
  ```

  </details>

- **Sequence equaling methods**

  The methods in this section compare elements of two lists for equality.

  <details><summary>Methods</summary>

  ``` csharp
  public static bool SequenceEqual<T>(this IList<T> xList, IList<T> yList);

  public static bool SequenceEqual<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex);

  public static bool SequenceEqual<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, int count);

  public static bool SequenceEqual<T>(this IList<T> xList, IList<T> yList, Equality<T> equality);

  public static bool SequenceEqual<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, Equality<T>? equality);

  public static bool SequenceEqual<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, Equality<T>? equality);

  public static bool SequenceEqual<T>(this IList<T> xList, IList<T> yList, IEqualityComparer<T>? equalityComparer);

  public static bool SequenceEqual<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, IEqualityComparer<T>? equalityComparer);

  public static bool SequenceEqual<T>(this IList<T> xList, int xIndex, IList<T> yList, int yIndex, int count, IEqualityComparer<T>? equalityComparer);
  ```

  </details>

---
### **Linq**

There are several classes for LINQ: `PwrEnumerable`, `PwrAsyncEnumerable`.

Will be described a little later...

---
### **Validation**

There are several classes for validating values, actions and data: `Argument`, `Operation`, `Format`.

Will be described a little later...

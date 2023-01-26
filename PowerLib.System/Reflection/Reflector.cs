using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Collections.Matching;
using PowerLib.System.Linq;
using PowerLib.System.Resources;
using PowerLib.System.Validation;

namespace PowerLib.System.Reflection;

public static class Reflector
{
  #region Constants

  private const string Invoke = nameof(Invoke);
  private const string GetAwaiter = nameof(GetAwaiter);
  private const string GetResult = nameof(GetResult);
  private const string IsComplete = nameof(IsComplete);

  #endregion
  #region Internal fields

  private static readonly Lazy<ResourceAccessor<ReflectionMessage>> resourceAccessor = new(() => new EnumResourceAccessor<ReflectionMessage>());

  #endregion
  #region Internal properties

  private static ResourceAccessor<ReflectionMessage> ResourceAccessor
    => resourceAccessor.Value;

  #endregion
  #region Internal methods

  private static string FormatMessage(ReflectionMessage reflectionMessage, params object?[] args)
    => ResourceAccessor.FormatString(CultureInfo.CurrentCulture, reflectionMessage, args);

  #endregion
  #region General internal methods

  private static BindingFlags GetBindingFlags(bool staticMember, MemberAccessibility memberAccessibility)
    => (staticMember ? BindingFlags.Static : BindingFlags.Instance)
    | (memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase) ? BindingFlags.IgnoreCase : BindingFlags.Default)
    | (memberAccessibility.IsFlagsSet(MemberAccessibility.DeclaredOnly) ? BindingFlags.DeclaredOnly : BindingFlags.Default)
    | (memberAccessibility.IsFlagsSet(MemberAccessibility.FlattenHierarchy) ? BindingFlags.FlattenHierarchy : BindingFlags.Default)
    | (memberAccessibility.IsFlagsSet(MemberAccessibility.Public) ? BindingFlags.Public : BindingFlags.Default)
    | (memberAccessibility.IsFlagsOverlapped(MemberAccessibility.NonPublic) ? BindingFlags.NonPublic : BindingFlags.Default);

  private static bool MatchType(Type sourceType, TypeVariance variance, Type memberType,
    IReadOnlyList<Type?>? sourceTypeArguments, IList<Type?>? resultTypeArguments,
    IReadOnlyList<Type?>? sourceMethodArguments, IList<Type?>? resultMethodArguments)
  {
    if (memberType.IsGenericParameter)
    {
      var parameterAttributes = memberType.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;
      if ((parameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0 && sourceType.IsValueType)
        return false;
      if ((parameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0 && sourceType.IsNullAssignable())
        return false;
      if ((parameterAttributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0 && !sourceType.CanCreateParameterless())
        return false;
      var constraints = memberType.GetGenericParameterConstraints();
      var baseTypeConstraint = constraints.SingleOrDefault(constraint => !constraint.IsInterface);
      if (baseTypeConstraint is not null)
      {
        var baseType = sourceType;
        while (baseType is not null && !MatchType(baseType, TypeVariance.Invariant, baseTypeConstraint, sourceTypeArguments, resultTypeArguments, sourceMethodArguments, resultMethodArguments))
          baseType = baseType.BaseType;
        if (baseType is null)
          return false;
      }
      var interfaceConstraints = constraints.Where(constraints => constraints.IsInterface);
      if (!interfaceConstraints.All(prmInterface => MatchType(sourceType, TypeVariance.Contravariant, prmInterface, sourceTypeArguments, resultTypeArguments, sourceMethodArguments, resultMethodArguments)))
        return false;

      var resultArguments = !memberType.IsGenericParameter ? null : memberType.DeclaringMethod is null ? resultTypeArguments : resultMethodArguments;
      if (resultArguments is not null)
      {
        var sourceArguments = !memberType.IsGenericParameter ? null : memberType.DeclaringMethod is null ? sourceTypeArguments : sourceMethodArguments;
        var position = memberType.GenericParameterPosition;
        var resultArgument = resultArguments[position];
        var sourceArgument = sourceArguments?[position];
        if (resultArgument is null)
        {
          if (sourceArgument is null)
            resultArguments[position] = sourceType;
          else if (!MatchType(sourceType, TypeVariance.Contravariant, sourceArgument, sourceTypeArguments, resultTypeArguments, sourceMethodArguments, resultMethodArguments))
            return false;
          else
            resultArguments[position] = sourceArgument;
        }
        else if (!MatchType(sourceType, variance, resultArgument, sourceTypeArguments, resultTypeArguments, sourceMethodArguments, resultMethodArguments))
        {
          if (sourceArgument is not null)
            return false;
          else if (MatchType(sourceType,
            variance switch { TypeVariance.Covariant => TypeVariance.Contravariant, TypeVariance.Contravariant => TypeVariance.Covariant, _ => variance },
            resultArgument, sourceTypeArguments, resultTypeArguments, sourceMethodArguments, resultMethodArguments))
            resultArguments[position] = sourceType;
          else
            return false;
        }
      }
      return true;
    }
    else
    {
      if (sourceType == memberType)
        return true;
      if (!sourceType.IsByRef && memberType.IsByRef && sourceType == memberType.GetElementType())
        return true;
      if ((sourceType == typeof(Array) || sourceType.BaseType == typeof(Array)) && (memberType == typeof(Array) || memberType.BaseType == typeof(Array)))
        return MatchType(sourceType.GetElementType()!, TypeVariance.Invariant, memberType.GetElementType()!, sourceTypeArguments, resultTypeArguments, sourceMethodArguments, resultMethodArguments);
      if (sourceType.IsGenericType && memberType.IsGenericType &&
        (sourceType.IsGenericTypeDefinition ? sourceType : sourceType.GetGenericTypeDefinition()) == (memberType.IsGenericTypeDefinition ? memberType : memberType.GetGenericTypeDefinition()) &&
        sourceType.GetGenericArguments()
          .Zip(memberType.GetGenericArguments(), (sType, mType) => MatchType(sType, variance, mType, sourceTypeArguments, resultTypeArguments, sourceMethodArguments, resultMethodArguments))
          .All(result => result))
        return true;
      switch (variance)
      {
        case TypeVariance.Contravariant:
          if (!sourceType.IsInterface)
          {
            if (memberType.IsInterface)
            {
              foreach (var iface in sourceType.GetInterfaces())
                if (MatchType(iface, TypeVariance.Contravariant, memberType, sourceTypeArguments, resultTypeArguments, sourceMethodArguments, resultMethodArguments))
                  return true;
            }
            else if (sourceType.IsValueType && memberType.IsNullable() &&
              MatchType(sourceType, TypeVariance.Invariant, memberType.GenericTypeArguments[0], sourceTypeArguments, resultTypeArguments, sourceMethodArguments, resultMethodArguments))
              return true;
            else if (sourceType.BaseType is not null && MatchType(sourceType.BaseType, TypeVariance.Contravariant, memberType, sourceTypeArguments, resultTypeArguments, sourceMethodArguments, resultMethodArguments))
              return true;
          }
          break;
        case TypeVariance.Covariant:
          if (!memberType.IsInterface)
          {
            if (sourceType.IsInterface)
            {
              foreach (var iface in memberType.GetInterfaces())
                if (MatchType(sourceType, TypeVariance.Covariant, iface, sourceTypeArguments, resultTypeArguments, sourceMethodArguments, resultMethodArguments))
                  return true;
            }
            else if (sourceType.IsNullable() && memberType.IsValueType &&
              MatchType(sourceType.GenericTypeArguments[0], TypeVariance.Invariant, memberType, sourceTypeArguments, resultTypeArguments, sourceMethodArguments, resultMethodArguments))
              return true;
            else if (memberType.BaseType is not null && MatchType(sourceType, TypeVariance.Covariant, memberType.BaseType, sourceTypeArguments, resultTypeArguments, sourceMethodArguments, resultMethodArguments))
              return true;
          }
          break;
      }
      return false;
    }
  }

  private static IReadOnlyList<Type?> GetTypesOfValues(this IList<object?> values)
    => Argument.That.NotNull(values)
      .Select(parameterValue => TypedValue.GetType(parameterValue))
      .AsReadOnlyList();

  private static IReadOnlyList<Type?> GetTypesOfValues(this IReadOnlyList<object?> values)
    => Argument.That.NotNull(values)
      .Select(parameterValue => TypedValue.GetType(parameterValue))
      .AsReadOnlyList();

  private static IReadOnlyDictionary<string, Type?> GetTypesOfValues(this IDictionary<string, object?> values, bool ignoreCase)
    => Argument.That.NotNull(values)
      .ToDictionary(parameterValue => parameterValue.Key, parameterValue => TypedValue.GetType(parameterValue.Value), ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
      .AsReadOnlyDictionary();

  private static IReadOnlyDictionary<string, Type?> GetTypesOfValues(this IReadOnlyDictionary<string, object?> values, bool ignoreCase)
    => Argument.That.NotNull(values)
      .ToDictionary(parameterValue => parameterValue.Key, parameterValue => TypedValue.GetType(parameterValue.Value), ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
      .AsReadOnlyDictionary();

  private static bool ContainsReturnedParameters(this ParameterInfo[] parameters)
    => parameters.Any(paramInfo => paramInfo.IsOut || paramInfo.ParameterType.IsByRef);

  private static object?[] GetParameterValues(this ParameterInfo[] parameters, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues)
    => parameters
      .Select(paramInfo =>
        paramInfo.IsOut ? default :
        positionalParameterValues is not null && paramInfo.Position < positionalParameterValues.Count ? TypedValue.GetValue(positionalParameterValues[paramInfo.Position]) :
        namedParameterValues is not null && !string.IsNullOrEmpty(paramInfo.Name) && namedParameterValues.TryGetValue(paramInfo.Name, out var value) ? TypedValue.GetValue(value) :
        paramInfo.DefaultValue)
      .ToArray();

  private static object?[] GetParameterValues(this ParameterInfo[] parameters, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
    => Argument.That.NotNull(parameters)
      .Select(paramInfo =>
        paramInfo.IsOut ? default :
        positionalParameterValues is not null && paramInfo.Position < positionalParameterValues.Count ? TypedValue.GetValue(positionalParameterValues[paramInfo.Position]) :
        namedParameterValues is not null && !string.IsNullOrEmpty(paramInfo.Name) && namedParameterValues.TryGetValue(paramInfo.Name, out var value) ? TypedValue.GetValue(value) :
        paramInfo.DefaultValue)
      .ToArray();

  private static void SetParameterValues(this ParameterInfo[] parameters, object?[] values, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    if (positionalParameterValues is not null || namedParameterValues is not null)
      parameters.ForEach(paramInfo =>
      {
        if (values is not null && paramInfo.Position < values.Length && paramInfo.ParameterType.IsByRef)
        {
          if (paramInfo.Position < positionalParameterValues?.Count)
            positionalParameterValues[paramInfo.Position] = values[paramInfo.Position];
          else if (paramInfo.Name is not null && (namedParameterValues?.ContainsKey(paramInfo.Name) ?? false))
            namedParameterValues[paramInfo.Name] = values[paramInfo.Position];
        }
      });
  }

  private static void SetArgumentTypes(this Type[] genericArguments, IList<Type?>? sourceArguments)
  {
    if (sourceArguments is not null)
      genericArguments.ForEach((argument, index) => sourceArguments[index] ??= argument);
  }

  private static object? GetAwaitableResult(object awaiter)
  {
    var methodInfo = awaiter.GetType().GetMethod(nameof(TaskAwaiter.GetResult));
    Operation.That.IsValid(methodInfo, methodInfo is not null);
    return methodInfo.Invoke(awaiter, Array.Empty<object?>());
  }

  private static async Task<object?> InvokeAwaitableDelegate(Delegate @delegate, object?[]? parameters, bool hasReturnedParameters)
  {
    var invocationList = @delegate.GetInvocationList();
    var tasks = invocationList
      .Select((deleg, index) => InvokeAwaitableMethod(deleg.Target, deleg.Method, !hasReturnedParameters || index == invocationList.Length - 1 ? parameters : (object?[]?)parameters?.Clone()))
      .ToArray();
    var results = await Task.WhenAll(tasks);
    return results[results.Length - 1];
  }

  private static async Task<object?> InvokeAwaitableMethod(object? source, MethodInfo method, object?[]? parameters)
  {
    var result = method.Invoke(source, parameters);
    dynamic awaitable = result;
    await awaitable;
    return GetAwaitableResult(awaitable.GetAwaiter());
  }

  private static async Task<object?> InvokeAsync(Delegate @delegate, object?[]? parameters, bool hasReturnedParameters, TaskFactory taskFactory)
  {
    var invocationList = @delegate.GetInvocationList();
    var tasks = invocationList
      .Select((deleg, index) => taskFactory.StartNew(DelegateRunner, new DelegateState(deleg, !hasReturnedParameters || index == invocationList.Length - 1 ? parameters : (object?[]?)parameters?.Clone())))
      .ToArray();
    var results = await Task.WhenAll(tasks);
    return results[results.Length - 1];

    object? DelegateRunner(object? state)
    {
      var delegateState = (DelegateState?)state;
      return delegateState?.Delegate.Method.Invoke(delegateState.Delegate.Target, delegateState.Parameters);
    }
  }

  private sealed class DelegateState
  {
    internal DelegateState(Delegate @delegate, object?[]? parameters)
    {
      Delegate = Argument.That.NotNull(@delegate);
      Parameters = parameters;
    }

    internal readonly Delegate Delegate;

    internal readonly object?[]? Parameters;
  }

  #endregion
  #region Field internal methods

  private static IEnumerable<FieldInfo> GetFields(Type sourceType, bool staticMember, string name, MemberAccessibility memberAccessibility, bool shouldWrite)
  {
    var bindingFlags = GetBindingFlags(staticMember, memberAccessibility);
    var nameComparison = memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    var predicate = (FieldInfo fieldInfo) =>
      string.Equals(fieldInfo.Name, name, nameComparison) &&
      (!shouldWrite || !fieldInfo.Attributes.IsFlagsSet(FieldAttributes.InitOnly)) &&
      (fieldInfo.Attributes & FieldAttributes.FieldAccessMask) switch
      {
        FieldAttributes.Private => memberAccessibility.IsFlagsSet(MemberAccessibility.Private),
        FieldAttributes.Public => memberAccessibility.IsFlagsSet(MemberAccessibility.Public),
        FieldAttributes.Assembly => memberAccessibility.IsFlagsSet(MemberAccessibility.Assembly),
        FieldAttributes.Family => memberAccessibility.IsFlagsSet(MemberAccessibility.Family),
        FieldAttributes.FamORAssem => memberAccessibility.IsFlagsSet(MemberAccessibility.FamilyOrAssembly),
        FieldAttributes.FamANDAssem => memberAccessibility.IsFlagsSet(MemberAccessibility.FamilyAndAssembly),
        _ => false
      };
    return sourceType.GetFields(bindingFlags).Where(predicate);
  }

  private static bool MatchField(FieldInfo fieldInfo, IReadOnlyList<Type?>? sourceTypeArguments, IList<Type?>? resultTypeArguments, Type? valueType, TypeVariance typeVariance)
  {
    var genericTypeArguments = fieldInfo.DeclaringType?.GetGenericArguments();
    if (MatchArguments(genericTypeArguments, sourceTypeArguments, resultTypeArguments, null, null, null))
    {
      var fieldType = fieldInfo.FieldType;
      return valueType is null
        ? typeVariance == TypeVariance.Covariant || fieldType.IsNullAssignable()
        : MatchType(valueType, typeVariance, fieldType, sourceTypeArguments, resultTypeArguments, null, null);
    }
    return false;
  }

  private static FieldInfo? MatchField(Type sourceType, bool staticMember, string name, MemberAccessibility memberAccessibility, bool shouldWrite,
    IReadOnlyList<Type?>? sourceTypeArguments, Type? valueType, TypeVariance variance)
  {
    var topHierarchy = memberAccessibility.IsFlagsSet(MemberAccessibility.TopHierarchy);
    var resultFields = GetFields(sourceType, staticMember, name, memberAccessibility, shouldWrite)
      .Select(fieldInfo =>
      {
        var inheritanceLevel = sourceType.GetInheritanceLevel(fieldInfo.DeclaringType!);
        var resultTypeArguments = new Type[sourceType.GetGenericArguments().Length];
        var matched = MatchField(fieldInfo, sourceTypeArguments, resultTypeArguments, valueType, variance);
        return (fieldInfo: matched ? fieldInfo : null, inheritanceLevel, typeArguments: resultTypeArguments);
      })
      .Where(tuple => tuple.fieldInfo is not null)
      .OrderBy(tuple => tuple.inheritanceLevel)
      .TakeWhile((fieldInfo: default(FieldInfo), inheritanceLevel: -1),
        (state, item) => (item.fieldInfo, item.inheritanceLevel),
        (state, item) => state.fieldInfo is null || (!topHierarchy || state.inheritanceLevel == item.inheritanceLevel))
      .Take(2);
    var fieldInfo = default(FieldInfo);
    var typeArguments = default(Type?[]);
    var inheritanceLevel = default(int);
    foreach (var resultField in resultFields)
    {
      if (fieldInfo is null)
      {
        fieldInfo = resultField.fieldInfo;
        typeArguments = resultField.typeArguments;
        inheritanceLevel = resultField.inheritanceLevel;
        continue;
      }
      if (!topHierarchy || inheritanceLevel == resultField.inheritanceLevel)
        Operation.That.Failed(FormatMessage(ReflectionMessage.FieldAmbiguousMatch));
      break;
    }
    if (fieldInfo is not null)
    {
      if (sourceType.IsGenericType && !sourceType.IsConstructedGenericType && typeArguments is not null)
      {
        sourceType = sourceType.MakeGenericType(typeArguments);
        fieldInfo = MatchField(sourceType, staticMember, name, memberAccessibility, shouldWrite, null, valueType, variance);
      }
    }
    return fieldInfo;
  }

  private static bool TryGetFieldCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, Type? valueType,
    [NotNullWhen(true)] out FieldInfo? result)
  {
    var fieldInfo = MatchField(sourceType, source is null, name, memberAccessibility, false, typeArguments?.AsReadOnly(), valueType, TypeVariance.Covariant);
    if (fieldInfo is null)
    {
      result = null;
      return false;
    }
    fieldInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
    result = fieldInfo;
    return true;
  }

  private static FieldInfo GetFieldCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, Type? valueType)
  {
    var fieldInfo = MatchField(sourceType, source is null, name, memberAccessibility, false, typeArguments?.AsReadOnly(), valueType, TypeVariance.Covariant);
    Operation.That.IsValid(fieldInfo is not null, FormatMessage(ReflectionMessage.FieldNotFound));
    fieldInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
    return fieldInfo;
  }

  private static bool TryGetFieldValueCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, Type? valueType, out object? value)
  {
    var fieldInfo = MatchField(sourceType, source is null, name, memberAccessibility, false, typeArguments?.AsReadOnly(), valueType, TypeVariance.Covariant);
    if (fieldInfo is null)
    {
      value = default;
      return false;
    }
    var result = fieldInfo.GetValue(source);
    fieldInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
    value = result;
    return true;
  }

  private static bool TrySetFieldValueCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, Type? valueType, object? value)
  {
    var fieldInfo = MatchField(sourceType, source is null, name, memberAccessibility, true, typeArguments?.AsReadOnly(), valueType, TypeVariance.Contravariant);
    if (fieldInfo is null)
      return false;
    fieldInfo.SetValue(source, value);
    fieldInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
    return true;
  }

  private static bool TryReplaceFieldValueCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, Type? valueType, object? newValue, out object? oldValue)
  {
    var fieldInfo = MatchField(sourceType, source is null, name, memberAccessibility, true, typeArguments?.AsReadOnly(), valueType, TypeVariance.Invariant);
    if (fieldInfo is null)
    {
      oldValue = default;
      return false;
    }
    oldValue = fieldInfo.GetValue(source);
    fieldInfo.SetValue(source, newValue);
    fieldInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
    return true;
  }

  private static object? GetFieldValueCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, Type? valueType)
  {
    var fieldInfo = MatchField(sourceType, source is null, name, memberAccessibility, false, typeArguments?.AsReadOnly(), valueType, TypeVariance.Covariant);
    Operation.That.IsValid(fieldInfo is not null, FormatMessage(ReflectionMessage.FieldNotFound));
    var result = fieldInfo.GetValue(source);
    fieldInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
    return result;
  }

  private static void SetFieldValueCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, Type? valueType, object? value)
  {
    var fieldInfo = MatchField(sourceType, source is null, name, memberAccessibility, true, typeArguments?.AsReadOnly(), valueType, TypeVariance.Contravariant);
    Operation.That.IsValid(fieldInfo is not null, FormatMessage(ReflectionMessage.FieldNotFound));
    fieldInfo.SetValue(source, value);
    fieldInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
  }

  private static object? ReplaceFieldValueCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, Type? valueType, object? value)
  {
    var fieldInfo = MatchField(sourceType, source is null, name, memberAccessibility, true, typeArguments?.AsReadOnly(), valueType, TypeVariance.Invariant);
    Operation.That.IsValid(fieldInfo is not null, FormatMessage(ReflectionMessage.FieldNotFound));
    var result = fieldInfo.GetValue(source);
    fieldInfo.SetValue(source, value);
    fieldInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
    return result;
  }

  #endregion
  #region Instance field public methods
  #region Try info methods

  public static bool TryGetField(object source, string name, MemberAccessibility memberAccessibility, Type? valueType,
    [NotNullWhen(true)] out FieldInfo? fieldInfo)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryGetFieldCore(sourceType, sourceObject, name, memberAccessibility, null, valueType, out fieldInfo);
  }

  public static bool TryGetField<TValue>(object source, string name, MemberAccessibility memberAccessibility,
    [NotNullWhen(true)] out FieldInfo? fieldInfo)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryGetFieldCore(sourceType, sourceObject, name, memberAccessibility, null, typeof(TValue), out fieldInfo);
  }

  public static bool TryGetField<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, Type? valueType,
    [NotNullWhen(true)] out FieldInfo? fieldInfo)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetFieldCore(typeof(TSource), source, name, memberAccessibility, null, valueType, out fieldInfo);
  }

  public static bool TryGetField<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility,
    [NotNullWhen(true)] out FieldInfo? fieldInfo)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetFieldCore(typeof(TSource), source, name, memberAccessibility, null, typeof(TValue), out fieldInfo);
  }

  #endregion
  #region Try get methods

  public static bool TryGetFieldValue(object source, string name, MemberAccessibility memberAccessibility, Type? valueType, out object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryGetFieldValueCore(sourceType, sourceObject, name, memberAccessibility, null, valueType, out value);
  }

  public static bool TryGetFieldValue<TValue>(object source, string name, MemberAccessibility memberAccessibility, out TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    if (TryGetFieldValueCore(sourceType, sourceObject, name, memberAccessibility, null, typeof(TValue), out var result))
    {
      value = (TValue?)result;
      return true;
    }
    else
    {
      value = default;
      return false;
    }
  }

  public static bool TryGetFieldValue<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, Type? valueType, out object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetFieldValueCore(typeof(TSource), source, name, memberAccessibility, null, valueType, out value);
  }

  public static bool TryGetFieldValue<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, out TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    if (TryGetFieldValueCore(typeof(TSource), source, name, memberAccessibility, null, typeof(TValue), out var result))
    {
      value = (TValue?)result;
      return true;
    }
    else
    {
      value = default;
      return false;
    }
  }

  #endregion
  #region Try set methods

  public static bool TrySetFieldValue(object source, string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TrySetFieldValueCore(sourceType, sourceObject, name, memberAccessibility, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static bool TrySetFieldValue<TValue>(object source, string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TrySetFieldValueCore(sourceType, sourceObject, name, memberAccessibility, null, typeof(TValue), value);
  }

  public static bool TrySetFieldValue<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TrySetFieldValueCore(typeof(TSource), source, name, memberAccessibility, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static bool TrySetFieldValue<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TrySetFieldValueCore(typeof(TSource), source, name, memberAccessibility, null, typeof(TValue), value);
  }

  #endregion
  #region Try replace methods

  public static bool TryReplaceFieldValue(object source, string name, MemberAccessibility memberAccessibility, object? newValue, out object? oldValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryReplaceFieldValueCore(sourceType, sourceObject, name, memberAccessibility, null, TypedValue.GetType(newValue), TypedValue.GetValue(newValue), out oldValue);
  }

  public static bool TryReplaceFieldValue<TValue>(object source, string name, MemberAccessibility memberAccessibility, TValue? newValue, out TValue? oldValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    if (TryReplaceFieldValueCore(sourceType, sourceObject, name, memberAccessibility, null, typeof(TValue), newValue, out var result))
    {
      oldValue = (TValue?)result;
      return true;
    }
    else
    {
      oldValue = default;
      return false;
    }
  }

  public static bool TryReplaceFieldValue<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, object? newValue, out object? oldValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryReplaceFieldValueCore(typeof(TSource), source, name, memberAccessibility, null, TypedValue.GetType(newValue), TypedValue.GetValue(newValue), out oldValue);
  }

  public static bool TryReplaceFieldValue<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, TValue? newValue, out TValue? oldValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    if (TryReplaceFieldValueCore(typeof(TSource), source, name, memberAccessibility, null, typeof(TValue), newValue, out var result))
    {
      oldValue = (TValue?)result;
      return true;
    }
    else
    {
      oldValue = default;
      return false;
    }
  }

  #endregion
  #region Try exchange methods

  public static bool TryExchangeFieldValue(object source, string name, MemberAccessibility memberAccessibility, ref object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    if (!TryReplaceFieldValueCore(sourceType, sourceObject, name, memberAccessibility, null, TypedValue.GetType(value), TypedValue.GetValue(value), out var result))
      return false;
    value = result;
    return true;
  }

  public static bool TryExchangeFieldValue<TValue>(object source, string name, MemberAccessibility memberAccessibility, ref TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    if (!TryReplaceFieldValueCore(sourceType, sourceObject, name, memberAccessibility, null, typeof(TValue), value, out var result))
      return false;
    value = (TValue?)result;
    return true;
  }

  public static bool TryExchangeFieldValue<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, ref object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    if (!TryReplaceFieldValueCore(typeof(TSource), source, name, memberAccessibility, null, TypedValue.GetType(value), TypedValue.GetValue(value), out var result))
      return false;
    value = result;
    return true;
  }

  public static bool TryExchangeFieldValue<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, ref TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    if (!TryReplaceFieldValueCore(typeof(TSource), source, name, memberAccessibility, null, typeof(TValue), value, out var result))
      return false;
    value = (TValue?)result;
    return true;
  }

  #endregion
  #region Direct info methods

  public static FieldInfo GetField(object source, string name, MemberAccessibility memberAccessibility, Type? valueType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return GetFieldCore(sourceType, sourceObject, name, memberAccessibility, null, valueType);
  }

  public static FieldInfo GetField<TValue>(object source, string name, MemberAccessibility memberAccessibility)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return GetFieldCore(sourceType, sourceObject, name, memberAccessibility, null, typeof(TValue));
  }

  public static FieldInfo GetField<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, Type? valueType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return GetFieldCore(typeof(TSource), source, name, memberAccessibility, null, valueType);
  }

  public static FieldInfo GetField<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return GetFieldCore(typeof(TSource), source, name, memberAccessibility, null, typeof(TValue));
  }

  #endregion
  #region Direct get methods

  public static object? GetFieldValue(object source, string name, MemberAccessibility memberAccessibility, Type? valueType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return GetFieldValueCore(sourceType, sourceObject, name, memberAccessibility, null, valueType);
  }

  public static TValue? GetFieldValue<TValue>(object source, string name, MemberAccessibility memberAccessibility)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return (TValue?)GetFieldValueCore(sourceType, sourceObject, name, memberAccessibility, null, typeof(TValue));
  }

  public static object? GetFieldValue<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, Type? valueType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return GetFieldValueCore(typeof(TSource), source, name, memberAccessibility, null, valueType);
  }

  public static TValue? GetFieldValue<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return (TValue?)GetFieldValueCore(typeof(TSource), source, name, memberAccessibility, null, typeof(TValue));
  }

  #endregion
  #region Direct set methods

  public static void SetFieldValue(object source, string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    SetFieldValueCore(sourceType, sourceObject, name, memberAccessibility, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static void SetFieldValue<TValue>(object source, string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    SetFieldValueCore(sourceType, sourceObject, name, memberAccessibility, null, typeof(TValue), value);
  }

  public static void SetFieldValue<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    SetFieldValueCore(typeof(TSource), source, name, memberAccessibility, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static void SetFieldValue<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    SetFieldValueCore(typeof(TSource), source, name, memberAccessibility, null, typeof(TValue), value);
  }

  #endregion
  #region Direct replace methods

  public static object? ReplaceFieldValue(object source, string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return ReplaceFieldValueCore(sourceType, sourceObject, name, memberAccessibility, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static TValue? ReplaceFieldValue<TValue>(object source, string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return (TValue?)ReplaceFieldValueCore(sourceType, sourceObject, name, memberAccessibility, null, typeof(TValue), value);
  }

  public static object? ReplaceFieldValue<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return ReplaceFieldValueCore(typeof(TSource), source, name, memberAccessibility, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static TValue? ReplaceFieldValue<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return (TValue?)ReplaceFieldValueCore(typeof(TSource), source, name, memberAccessibility, null, typeof(TValue), value);
  }

  #endregion
  #region Direct exchange methods

  public static void ExchangeFieldValue(object source, string name, MemberAccessibility memberAccessibility, ref object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    value = ReplaceFieldValueCore(sourceType, sourceObject, name, memberAccessibility, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static void ExchangeFieldValue<TValue>(object source, string name, MemberAccessibility memberAccessibility, ref TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    value = (TValue?)ReplaceFieldValueCore(sourceType, sourceObject, name, memberAccessibility, null, typeof(TValue), value);
  }

  public static void ExchangeFieldValue<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, ref object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    value = ReplaceFieldValueCore(typeof(TSource), source, name, memberAccessibility, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static void ExchangeFieldValue<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, ref TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    value = (TValue?)ReplaceFieldValueCore(typeof(TSource), source, name, memberAccessibility, null, typeof(TValue), value);
  }

  #endregion
  #endregion
  #region Static field public methods
  #region Try info methods

  public static bool TryGetField(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, Type? valueType,
    [NotNullWhen(true)] out FieldInfo? fieldInfo)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetFieldCore(sourceType, null, name, memberAccessibility, typeArguments, valueType, out fieldInfo);
  }

  public static bool TryGetField<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    [NotNullWhen(true)] out FieldInfo? fieldInfo)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetFieldCore(sourceType, null, name, memberAccessibility, typeArguments, typeof(TValue), out fieldInfo);
  }

  public static bool TryGetField<TSource>(string name, MemberAccessibility memberAccessibility, Type? valueType,
    [NotNullWhen(true)] out FieldInfo? fieldInfo)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryGetFieldCore(typeof(TSource), null, name, memberAccessibility, null, valueType, out fieldInfo);
  }

  public static bool TryGetField<TSource, TValue>(string name, MemberAccessibility memberAccessibility,
    [NotNullWhen(true)] out FieldInfo? fieldInfo)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryGetFieldCore(typeof(TSource), null, name, memberAccessibility, null, typeof(TValue), out fieldInfo);
  }

  #endregion
  #region Try get methods

  public static bool TryGetFieldValue(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, Type? valueType, out object? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetFieldValueCore(sourceType, null, name, memberAccessibility, typeArguments, valueType, out value);
  }

  public static bool TryGetFieldValue<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, out TValue? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    if (TryGetFieldValueCore(sourceType, null, name, memberAccessibility, typeArguments, typeof(TValue), out var result))
    {
      value = (TValue?)result;
      return true;
    }
    else
    {
      value = default;
      return false;
    }
  }

  public static bool TryGetFieldValue<TSource>(string name, MemberAccessibility memberAccessibility, Type? valueType, out object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryGetFieldValueCore(typeof(TSource), null, name, memberAccessibility, null, valueType, out value);
  }

  public static bool TryGetFieldValue<TSource, TValue>(string name, MemberAccessibility memberAccessibility, out TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    if (TryGetFieldValueCore(typeof(TSource), null, name, memberAccessibility, null, typeof(TValue), out var result))
    {
      value = (TValue?)result;
      return true;
    }
    else
    {
      value = default;
      return false;
    }
  }

  #endregion
  #region Try set methods

  public static bool TrySetFieldValue(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, object? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TrySetFieldValueCore(sourceType, null, name, memberAccessibility, typeArguments, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static bool TrySetFieldValue<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, TValue? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TrySetFieldValueCore(sourceType, null, name, memberAccessibility, typeArguments, typeof(TValue), value);
  }

  public static bool TrySetFieldValue<TSource>(string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TrySetFieldValueCore(typeof(TSource), null, name, memberAccessibility, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static bool TrySetFieldValue<TSource, TValue>(string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TrySetFieldValueCore(typeof(TSource), null, name, memberAccessibility, null, typeof(TValue), value);
  }

  #endregion
  #region Try replace methods

  public static bool TryReplaceFieldValue(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, object? newValue, out object? oldValue)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryReplaceFieldValueCore(sourceType, null, name, memberAccessibility, typeArguments, TypedValue.GetType(newValue), TypedValue.GetValue(newValue), out oldValue);
  }

  public static bool TryReplaceFieldValue<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, TValue? newValue, out TValue? oldValue)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    if (TryReplaceFieldValueCore(sourceType, null, name, memberAccessibility, typeArguments, typeof(TValue), newValue, out var result))
    {
      oldValue = (TValue?)result;
      return true;
    }
    else
    {
      oldValue = default;
      return false;
    }
  }

  public static bool TryReplaceFieldValue<TSource>(string name, MemberAccessibility memberAccessibility, object? newValue, out object? oldValue)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryReplaceFieldValueCore(typeof(TSource), null, name, memberAccessibility, null, TypedValue.GetType(newValue), TypedValue.GetValue(newValue), out oldValue);
  }

  public static bool TryReplaceFieldValue<TSource, TValue>(string name, MemberAccessibility memberAccessibility, TValue? newValue, out TValue? oldValue)
  {
    Argument.That.NotNullOrWhitespace(name);

    if (TryReplaceFieldValueCore(typeof(TSource), null, name, memberAccessibility, null, typeof(TValue), newValue, out var result))
    {
      oldValue = (TValue?)result;
      return true;
    }
    else
    {
      oldValue = default;
      return false;
    }
  }

  #endregion
  #region Try exchange methods

  public static bool TryExchangeFieldValue(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, ref object? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    if (!TryReplaceFieldValueCore(sourceType, null, name, memberAccessibility, typeArguments, TypedValue.GetType(value), TypedValue.GetValue(value), out var result))
      return false;
    value = result;
    return true;
  }

  public static bool TryExchangeFieldValue<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, ref TValue? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    if (!TryReplaceFieldValueCore(sourceType, null, name, memberAccessibility, typeArguments, typeof(TValue), value, out var result))
      return false;
    value = (TValue?)result;
    return true;
  }

  public static bool TryExchangeFieldValue<TSource>(string name, MemberAccessibility memberAccessibility, ref object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    if (!TryReplaceFieldValueCore(typeof(TSource), null, name, memberAccessibility, null, TypedValue.GetType(value), TypedValue.GetValue(value), out var result))
      return false;
    value = result;
    return true;
  }

  public static bool TryExchangeFieldValue<TSource, TValue>(string name, MemberAccessibility memberAccessibility, ref TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    if (!TryReplaceFieldValueCore(typeof(TSource), null, name, memberAccessibility, null, typeof(TValue), value, out var result))
      return false;
    value = (TValue?)result;
    return true;
  }

  #endregion
  #region Direct info methods

  public static FieldInfo GetField(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, Type? valueType)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return GetFieldCore(sourceType, null, name, memberAccessibility, typeArguments, valueType);
  }

  public static FieldInfo GetField<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return GetFieldCore(sourceType, null, name, memberAccessibility, typeArguments, typeof(TValue));
  }

  public static FieldInfo GetField<TSource>(string name, MemberAccessibility memberAccessibility, Type? valueType)
  {
    Argument.That.NotNullOrWhitespace(name);

    return GetFieldCore(typeof(TSource), null, name, memberAccessibility, null, valueType);
  }

  public static FieldInfo GetField<TSource, TValue>(string name, MemberAccessibility memberAccessibility)
  {
    Argument.That.NotNullOrWhitespace(name);

    return GetFieldCore(typeof(TSource), null, name, memberAccessibility, null, typeof(TValue));
  }

  #endregion
  #region Direct get methods

  public static object? GetFieldValue(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, Type? valueType)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return GetFieldValueCore(sourceType, null, name, memberAccessibility, typeArguments, valueType);
  }

  public static TValue? GetFieldValue<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return (TValue?)GetFieldValueCore(sourceType, null, name, memberAccessibility, typeArguments, typeof(TValue));
  }

  public static object? GetFieldValue<TSource>(string name, MemberAccessibility memberAccessibility, Type? valueType)
  {
    Argument.That.NotNullOrWhitespace(name);

    return GetFieldValueCore(typeof(TSource), null, name, memberAccessibility, null, valueType);
  }

  public static TValue? GetFieldValue<TSource, TValue>(string name, MemberAccessibility memberAccessibility)
  {
    Argument.That.NotNullOrWhitespace(name);

    return (TValue?)GetFieldValueCore(typeof(TSource), null, name, memberAccessibility, null, typeof(TValue));
  }

  #endregion
  #region Direct set methods

  public static void SetFieldValue(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, object? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    SetFieldValueCore(sourceType, null, name, memberAccessibility, typeArguments, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static void SetFieldValue<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, TValue? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    SetFieldValueCore(sourceType, null, name, memberAccessibility, typeArguments, typeof(TValue), value);
  }

  public static void SetFieldValue<TSource>(string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    SetFieldValueCore(typeof(TSource), null, name, memberAccessibility, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static void SetFieldValue<TSource, TValue>(string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    SetFieldValueCore(typeof(TSource), null, name, memberAccessibility, null, typeof(TValue), value);
  }

  #endregion
  #region Direct replace methods

  public static object? ReplaceFieldValue(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, object? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return ReplaceFieldValueCore(sourceType, null, name, memberAccessibility, typeArguments, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static TValue? ReplaceFieldValue<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, TValue? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return (TValue?)ReplaceFieldValueCore(sourceType, null, name, memberAccessibility, typeArguments, typeof(TValue), value);
  }

  public static object? ReplaceFieldValue<TSource>(string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    return ReplaceFieldValueCore(typeof(TSource), null, name, memberAccessibility, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static TValue? ReplaceFieldValue<TSource, TValue>(string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    return (TValue?)ReplaceFieldValueCore(typeof(TSource), null, name, memberAccessibility, null, typeof(TValue), value);
  }

  #endregion
  #region Direct exchange methods

  public static void ExchangeFieldValue(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, ref object? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    value = ReplaceFieldValueCore(sourceType, null, name, memberAccessibility, typeArguments, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static void ExchangeFieldValue<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, ref TValue? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    value = (TValue?)ReplaceFieldValueCore(sourceType, null, name, memberAccessibility, typeArguments, typeof(TValue), value);
  }

  public static void ExchangeFieldValue<TSource>(string name, MemberAccessibility memberAccessibility, ref object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    value = ReplaceFieldValueCore(typeof(TSource), null, name, memberAccessibility, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static void ExchangeFieldValue<TSource, TValue>(string name, MemberAccessibility memberAccessibility, ref TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    value = (TValue?)ReplaceFieldValueCore(typeof(TSource), null, name, memberAccessibility, null, typeof(TValue), value);
  }

  #endregion
  #endregion
  #region Property internal methods

  private static IEnumerable<PropertyInfo> GetProperties(Type sourceType, bool staticMember, string name, MemberAccessibility memberAccessibility, bool shouldRead, bool shouldWrite)
  {
    var bindingFlags = GetBindingFlags(staticMember, memberAccessibility);
    var nameComparison = memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    var predicate = (PropertyInfo propertyInfo) =>
      string.Equals(propertyInfo.Name, name, nameComparison) &&
      ((shouldRead && propertyInfo.GetMethod is not null &&
        (propertyInfo.GetMethod.Attributes & MethodAttributes.MemberAccessMask) switch
        {
          MethodAttributes.Private => memberAccessibility.IsFlagsSet(MemberAccessibility.Private),
          MethodAttributes.Public => memberAccessibility.IsFlagsSet(MemberAccessibility.Public),
          MethodAttributes.Family => memberAccessibility.IsFlagsSet(MemberAccessibility.Family),
          MethodAttributes.Assembly => memberAccessibility.IsFlagsSet(MemberAccessibility.Assembly),
          MethodAttributes.FamORAssem => memberAccessibility.IsFlagsSet(MemberAccessibility.FamilyOrAssembly),
          MethodAttributes.FamANDAssem => memberAccessibility.IsFlagsSet(MemberAccessibility.FamilyAndAssembly),
          _ => false
        })
        || (shouldWrite && propertyInfo.SetMethod is not null &&
        (propertyInfo.SetMethod.Attributes & MethodAttributes.MemberAccessMask) switch
        {
          MethodAttributes.Private => memberAccessibility.IsFlagsSet(MemberAccessibility.Private),
          MethodAttributes.Public => memberAccessibility.IsFlagsSet(MemberAccessibility.Public),
          MethodAttributes.Family => memberAccessibility.IsFlagsSet(MemberAccessibility.Family),
          MethodAttributes.Assembly => memberAccessibility.IsFlagsSet(MemberAccessibility.Assembly),
          MethodAttributes.FamORAssem => memberAccessibility.IsFlagsSet(MemberAccessibility.FamilyOrAssembly),
          MethodAttributes.FamANDAssem => memberAccessibility.IsFlagsSet(MemberAccessibility.FamilyAndAssembly),
          _ => false
        }));
    return sourceType.GetProperties(bindingFlags).Where(predicate);
  }

  private static bool MatchProperty(PropertyInfo propertyInfo, IReadOnlyList<Type?>? sourceTypeArguments, IList<Type?>? resultTypeArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? valueType, TypeVariance typeVariance,
    out SignatureWeight signatureWeight)
  {
    var genericTypeArguments = propertyInfo.DeclaringType?.GetGenericArguments();
    if (MatchArguments(genericTypeArguments, sourceTypeArguments, resultTypeArguments, null, null, null))
    {
      var propertyParameters = propertyInfo.GetIndexParameters();
      if (propertyParameters.Length >= (positionalParameterTypes?.Count ?? 0))
      {
        var currentWeight = new SignatureWeight();
        if (MatchParameters(propertyParameters, sourceTypeArguments, resultTypeArguments, null, null, positionalParameterTypes, namedParameterTypes, ref currentWeight))
        {
          var propertyType = propertyInfo.PropertyType;
          if (valueType is null
            ? (typeVariance == TypeVariance.Covariant || propertyType.IsNullAssignable())
            : MatchType(valueType, typeVariance, propertyType, sourceTypeArguments, resultTypeArguments, null, null))
          {
            signatureWeight = currentWeight;
            return true;
          }
        }
      }
    }
    signatureWeight = default;
    return false;
  }

  private static PropertyInfo? MatchProperty(Type sourceType, bool staticMember, string name, MemberAccessibility memberAccessibility, bool shouldRead, bool shouldWrite,
    IReadOnlyList<Type?>? sourceTypeArguments, IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes,
    Type? valueType, TypeVariance variance)
  {
    var topHierarchy = memberAccessibility.IsFlagsSet(MemberAccessibility.TopHierarchy);
    var resultProperties = GetProperties(sourceType, staticMember, name, memberAccessibility, shouldRead, shouldWrite)
      .Select(propertyInfo =>
      {
        var inheritanceLevel = sourceType.GetInheritanceLevel(propertyInfo.DeclaringType!);
        var resultTypeArguments = new Type[sourceType.GetGenericArguments().Length];
        var matched = MatchProperty(propertyInfo, sourceTypeArguments, resultTypeArguments, positionalParameterTypes, namedParameterTypes, valueType, variance, out var propertyWeight);
        return (propertyInfo: matched ? propertyInfo : null, inheritanceLevel, propertyWeight, typeArguments: resultTypeArguments);
      })
      .Where(tuple => tuple.propertyInfo is not null)
      .OrderByDescending(tuple => tuple.propertyWeight, SignatureWeightComparer.Value)
      .ThenBy(tuple => tuple.inheritanceLevel)
      .TakeWhile((signatureWeight: default(SignatureWeight?), inheritanceLevel: -1),
        (state, item) => (item.propertyWeight, item.inheritanceLevel),
        (state, item) => state.signatureWeight is null || CompareSignatureWeight(item.propertyWeight, state.signatureWeight.Value) == 0 && (!topHierarchy || state.inheritanceLevel == item.inheritanceLevel))
      .Take(2);
    var propertyInfo = default(PropertyInfo);
    var typeArguments = default(Type?[]);
    var inheritanceLevel = default(int);
    foreach (var resultProperty in resultProperties)
    {
      if (propertyInfo is null)
      {
        propertyInfo = resultProperty.propertyInfo;
        typeArguments = resultProperty.typeArguments;
        inheritanceLevel = resultProperty.inheritanceLevel;
        continue;
      }
      if (!topHierarchy || inheritanceLevel == resultProperty.inheritanceLevel)
        Operation.That.Failed(FormatMessage(ReflectionMessage.PropertyAmbiguousMatch));
      break;
    }
    if (propertyInfo is not null)
    {
      if (sourceType.IsGenericType && !sourceType.IsConstructedGenericType && typeArguments is not null)
      {
        sourceType = sourceType.MakeGenericType(typeArguments);
        propertyInfo = MatchProperty(sourceType, staticMember, name, memberAccessibility, shouldRead, shouldWrite, sourceTypeArguments, positionalParameterTypes, namedParameterTypes, valueType, variance);
      }
    }
    return propertyInfo;
  }

  private static bool TryGetPropertyCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? valueType,
    [NotNullWhen(true)] out PropertyInfo? result)
  {
    var propertyInfo = MatchProperty(sourceType, source is null, name, memberAccessibility, false, false,
      typeArguments?.AsReadOnly(), positionalParameterTypes, namedParameterTypes, valueType, TypeVariance.Covariant);
    if (propertyInfo is null)
    {
      result = null;
      return false;
    }
    propertyInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
    result = propertyInfo;
    return true;
  }

  private static PropertyInfo GetPropertyCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? valueType)
  {
    var propertyInfo = MatchProperty(sourceType, source is null, name, memberAccessibility, false, false,
      typeArguments?.AsReadOnly(), positionalParameterTypes, namedParameterTypes, valueType, TypeVariance.Covariant);
    Operation.That.IsValid(propertyInfo is not null, FormatMessage(ReflectionMessage.PropertyNotFound));
    propertyInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
    return propertyInfo;
  }

  private static bool TryGetPropertyValueCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType, out object? value)
  {
    var propertyInfo = MatchProperty(sourceType, source is null, name, memberAccessibility, true, false, typeArguments?.AsReadOnly(),
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)), valueType, TypeVariance.Covariant);
    if (propertyInfo is null)
    {
      value = default;
      return false;
    }
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = propertyInfo.GetValue(source, indices);
    propertyInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
    value = result;
    return true;
  }

  private static bool TrySetPropertyValueCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType, object? value)
  {
    var propertyInfo = MatchProperty(sourceType, source is null, name, memberAccessibility, false, true, typeArguments?.AsReadOnly(),
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)), TypedValue.GetType(value), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    propertyInfo.SetValue(source, value, indices);
    propertyInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
    return true;
  }

  private static bool TryReplacePropertyValueCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType, object? newValue, out object? oldValue)
  {
    var propertyInfo = MatchProperty(sourceType, source is null, name, memberAccessibility, true, true, typeArguments?.AsReadOnly(),
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)), valueType, TypeVariance.Invariant);
    if (propertyInfo is null)
    {
      oldValue = default;
      return false;
    }
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = propertyInfo.GetValue(source, indices);
    propertyInfo.SetValue(source, newValue, indices);
    propertyInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
    oldValue = result;
    return true;
  }

  private static object? GetPropertyValueCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType)
  {
    var propertyInfo = MatchProperty(sourceType, source is null, name, memberAccessibility, true, false, typeArguments?.AsReadOnly(),
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)), valueType, TypeVariance.Covariant);
    Operation.That.IsValid(propertyInfo is not null, FormatMessage(ReflectionMessage.PropertyNotFound));
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = propertyInfo.GetValue(source, indices);
    propertyInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
    return result;
  }

  private static void SetPropertyValueCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType, object? value)
  {
    var propertyInfo = MatchProperty(sourceType, source is null, name, memberAccessibility, false, true, typeArguments?.AsReadOnly(),
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)), valueType, TypeVariance.Contravariant);
    Operation.That.IsValid(propertyInfo is not null, FormatMessage(ReflectionMessage.PropertyNotFound));
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    propertyInfo.SetValue(source, value, indices);
    propertyInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
  }

  private static object? ReplacePropertyValueCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType, object? value)
  {
    var propertyInfo = MatchProperty(sourceType, source is null, name, memberAccessibility, true, true, typeArguments?.AsReadOnly(),
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)), valueType, TypeVariance.Invariant);
    Operation.That.IsValid(propertyInfo is not null, FormatMessage(ReflectionMessage.PropertyNotFound));
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = propertyInfo.GetValue(source, indices);
    propertyInfo.SetValue(source, value, indices);
    propertyInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
    return result;
  }

  #endregion
  #region Instance property public methods
  #region Without parameters
  #region Try info methods

  public static bool TryGetProperty(object source, string name, MemberAccessibility memberAccessibility, Type? valueType,
    [NotNullWhen(true)] out PropertyInfo? propertyInfo)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryGetPropertyCore(sourceType, sourceObject, name, memberAccessibility, null, null, null, valueType, out propertyInfo);
  }

  public static bool TryGetProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility,
    [NotNullWhen(true)] out PropertyInfo? propertyInfo)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryGetPropertyCore(sourceType, sourceObject, name, memberAccessibility, null, null, null, typeof(TValue), out propertyInfo);
  }

  public static bool TryGetProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, Type? valueType,
    [NotNullWhen(true)] out PropertyInfo? propertyInfo)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetPropertyCore(typeof(TSource), source, name, memberAccessibility, null, null, null, valueType, out propertyInfo);
  }

  public static bool TryGetProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility,
    [NotNullWhen(true)] out PropertyInfo? propertyInfo)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetPropertyCore(typeof(TSource), source, name, memberAccessibility, null, null, null, typeof(TValue), out propertyInfo);
  }

  #endregion
  #region Try get methods

  public static bool TryGetPropertyValue(object source, string name, MemberAccessibility memberAccessibility, Type? valueType, out object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryGetPropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, null, null, valueType, out value);
  }

  public static bool TryGetPropertyValue<TValue>(object source, string name, MemberAccessibility memberAccessibility, out TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    if (TryGetPropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, null, null, typeof(TValue?), out var result))
    {
      value = (TValue?)result;
      return true;
    }
    else
    {
      value = default;
      return false;
    }
  }

  public static bool TryGetPropertyValue<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, Type? valueType, out object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetPropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, null, null, valueType, out value);
  }

  public static bool TryGetPropertyValue<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, out TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    if (TryGetPropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, null, null, typeof(TValue?), out var result))
    {
      value = (TValue?)result;
      return true;
    }
    else
    {
      value = default;
      return false;
    }
  }

  #endregion
  #region Try set methods

  public static bool TrySetPropertyValue(object source, string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TrySetPropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, null, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static bool TrySetPropertyValue<TValue>(object source, string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TrySetPropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, null, null, typeof(TValue?), value);
  }

  public static bool TrySetPropertyValue<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TrySetPropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, null, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static bool TrySetPropertyValue<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TrySetPropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, null, null, typeof(TValue?), value);
  }

  #endregion
  #region Try replace methods

  public static bool TryReplacePropertyValue(object source, string name, MemberAccessibility memberAccessibility, object? newValue, out object? oldValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryReplacePropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, null, null, TypedValue.GetType(newValue), TypedValue.GetValue(newValue), out oldValue);
  }

  public static bool TryReplacePropertyValue<TValue>(object source, string name, MemberAccessibility memberAccessibility, TValue? newValue, out TValue? oldValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    if (TryReplacePropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, null, null, typeof(TValue), newValue, out var result))
    {
      oldValue = (TValue?)result;
      return true;
    }
    else
    {
      oldValue = default;
      return false;
    }
  }

  public static bool TryReplacePropertyValue<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, object? newValue, out object? oldValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryReplacePropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, null, null, TypedValue.GetType(newValue), TypedValue.GetValue(newValue), out oldValue);
  }

  public static bool TryReplacePropertyValue<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, TValue? newValue, out TValue? oldValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    if (TryReplacePropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, null, null, typeof(TValue), newValue, out var result))
    {
      oldValue = (TValue?)result;
      return true;
    }
    else
    {
      oldValue = default;
      return false;
    }
  }

  #endregion
  #region Try exchange methods

  public static bool TryExchangePropertyValue(object source, string name, MemberAccessibility memberAccessibility, ref object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    if (!TryReplacePropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, null, null, TypedValue.GetType(value), TypedValue.GetValue(value), out var result))
      return false;
    value = result;
    return true;
  }

  public static bool TryExchangePropertyValue<TValue>(object source, string name, MemberAccessibility memberAccessibility, ref TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    if (!TryReplacePropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, null, null, typeof(TValue), value, out var result))
      return false;
    value = (TValue?)result;
    return true;
  }

  public static bool TryExchangePropertyValue<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, ref object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    if (!TryReplacePropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, null, null, TypedValue.GetType(value), TypedValue.GetValue(value), out var result))
      return false;
    value = result;
    return true;
  }

  public static bool TryExchangePropertyValue<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, ref TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    if (!TryReplacePropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, null, null, typeof(TValue), value, out var result))
      return false;
    value = (TValue?)result;
    return true;
  }

  #endregion
  #region Direct info methods

  public static PropertyInfo GetProperty(object source, string name, MemberAccessibility memberAccessibility, Type? valueType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return GetPropertyCore(sourceType, sourceObject, name, memberAccessibility, null, null, null, valueType);
  }

  public static PropertyInfo GetProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return GetPropertyCore(sourceType, sourceObject, name, memberAccessibility, null, null, null, typeof(TValue));
  }

  public static PropertyInfo GetProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, Type? valueType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return GetPropertyCore(typeof(TSource), source, name, memberAccessibility, null, null, null, valueType);
  }

  public static PropertyInfo GetProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return GetPropertyCore(typeof(TSource), source, name, memberAccessibility, null, null, null, typeof(TValue));
  }

  #endregion
  #region Direct get methods

  public static object? GetPropertyValue(object source, string name, MemberAccessibility memberAccessibility, Type? valueType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return GetPropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, null, null, valueType);
  }

  public static TValue? GetPropertyValue<TValue>(object source, string name, MemberAccessibility memberAccessibility)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return (TValue?)GetPropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, null, null, typeof(TValue));
  }

  public static object? GetPropertyValue<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, Type? valueType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return GetPropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, null, null, valueType);
  }

  public static TValue? GetPropertyValue<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return (TValue?)GetPropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, null, null, typeof(TValue));
  }

  #endregion
  #region Direct set methods

  public static void SetPropertyValue(object source, string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    SetPropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, null, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static void SetPropertyValue<TValue>(object source, string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    SetPropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, null, null, typeof(TValue), value);
  }

  public static void SetPropertyValue<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    SetPropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, null, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static void SetPropertyValue<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    SetPropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, null, null, typeof(TValue), value);
  }

  #endregion
  #region Direct replace methods

  public static object? ReplacePropertyValue(object source, string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return ReplacePropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, null, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static TValue? ReplacePropertyValue<TValue>(object source, string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return (TValue?)ReplacePropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, null, null, typeof(TValue), value);
  }

  public static object? ReplacePropertyValue<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return ReplacePropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, null, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static TValue? ReplacePropertyValue<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return (TValue?)ReplacePropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, null, null, typeof(TValue), value);
  }

  #endregion
  #region Direct exchange methods

  public static void ExchangePropertyValue(object source, string name, MemberAccessibility memberAccessibility, ref object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    value = ReplacePropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, null, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static void ExchangePropertyValue<TValue>(object source, string name, MemberAccessibility memberAccessibility, ref TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    value = (TValue?)ReplacePropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, null, null, typeof(TValue), value);
  }

  public static void ExchangePropertyValue<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, ref object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    value = ReplacePropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, null, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static void ExchangePropertyValue<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, ref TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    value = (TValue?)ReplacePropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, null, null, typeof(TValue), value);
  }

  #endregion
  #endregion
  #region With parameters
  #region Try info methods

  public static bool TryGetProperty(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? valueType,
    [NotNullWhen(true)] out PropertyInfo? propertyInfo)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryGetPropertyCore(sourceType, sourceObject, name, memberAccessibility, null, positionalParameterTypes, namedParameterTypes, valueType, out propertyInfo);
  }

  public static bool TryGetProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes,
    [NotNullWhen(true)] out PropertyInfo? propertyInfo)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryGetPropertyCore(sourceType, sourceObject, name, memberAccessibility, null, positionalParameterTypes, namedParameterTypes, typeof(TValue), out propertyInfo);
  }

  public static bool TryGetProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? valueType,
    [NotNullWhen(true)] out PropertyInfo? propertyInfo)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetPropertyCore(typeof(TSource), source, name, memberAccessibility, null, positionalParameterTypes, namedParameterTypes, valueType, out propertyInfo);
  }

  public static bool TryGetProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes,
    [NotNullWhen(true)] out PropertyInfo? propertyInfo)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetPropertyCore(typeof(TSource), source, name, memberAccessibility, null, positionalParameterTypes, namedParameterTypes, typeof(TValue), out propertyInfo);
  }

  #endregion
  #region Try get methods

  public static bool TryGetPropertyValue(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType, out object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryGetPropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, valueType, out value);
  }

  public static bool TryGetPropertyValue<TValue>(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, out TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    if (TryGetPropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, typeof(TValue?), out var result))
    {
      value = (TValue?)result;
      return true;
    }
    else
    {
      value = default;
      return false;
    }
  }

  public static bool TryGetPropertyValue<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType, out object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetPropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, valueType, out value);
  }

  public static bool TryGetPropertyValue<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, out TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    if (TryGetPropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, typeof(TValue?), out var result))
    {
      value = (TValue?)result;
      return true;
    }
    else
    {
      value = default;
      return false;
    }
  }

  #endregion
  #region Try set methods

  public static bool TrySetPropertyValue(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TrySetPropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static bool TrySetPropertyValue<TValue>(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TrySetPropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, typeof(TValue?), value);
  }

  public static bool TrySetPropertyValue<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TrySetPropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static bool TrySetPropertyValue<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TrySetPropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, typeof(TValue?), value);
  }

  #endregion
  #region Try replace methods

  public static bool TryReplacePropertyValue(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? newValue, out object? oldValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryReplacePropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(newValue), TypedValue.GetValue(newValue), out oldValue);
  }

  public static bool TryReplacePropertyValue<TValue>(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? newValue, out TValue? oldValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    if (TryReplacePropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, typeof(TValue), newValue, out var result))
    {
      oldValue = (TValue?)result;
      return true;
    }
    else
    {
      oldValue = default;
      return false;
    }
  }

  public static bool TryReplacePropertyValue<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? newValue, out object? oldValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryReplacePropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(newValue), TypedValue.GetValue(newValue), out oldValue);
  }

  public static bool TryReplacePropertyValue<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? newValue, out TValue? oldValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    if (TryReplacePropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, typeof(TValue), newValue, out var result))
    {
      oldValue = (TValue?)result;
      return true;
    }
    else
    {
      oldValue = default;
      return false;
    }
  }

  #endregion
  #region Try exchange methods

  public static bool TryExchangePropertyValue(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    if (!TryReplacePropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypedValue.GetValue(value), out var result))
      return false;
    value = result;
    return true;
  }

  public static bool TryExchangePropertyValue<TValue>(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    if (!TryReplacePropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, typeof(TValue), value, out var result))
      return false;
    value = (TValue?)result;
    return true;
  }

  public static bool TryExchangePropertyValue<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    if (!TryReplacePropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypedValue.GetValue(value), out var result))
      return false;
    value = result;
    return true;
  }

  public static bool TryExchangePropertyValue<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    if (!TryReplacePropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, typeof(TValue), value, out var result))
      return false;
    value = (TValue?)result;
    return true;
  }

  #endregion
  #region Direct info methods

  public static PropertyInfo GetProperty(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return GetPropertyCore(sourceType, sourceObject, name, memberAccessibility, null,
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)), valueType);
  }

  public static PropertyInfo GetProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return GetPropertyCore(sourceType, sourceObject, name, memberAccessibility, null,
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)), typeof(TValue));
  }

  public static PropertyInfo GetProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return GetPropertyCore(typeof(TSource), source, name, memberAccessibility, null,
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)), valueType);
  }

  public static PropertyInfo GetProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return GetPropertyCore(typeof(TSource), source, name, memberAccessibility, null,
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)), typeof(TValue));
  }

  #endregion
  #region Direct get methods

  public static object? GetPropertyValue(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return GetPropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, valueType);
  }

  public static TValue? GetPropertyValue<TValue>(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return (TValue?)GetPropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, typeof(TValue));
  }

  public static object? GetPropertyValue<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return GetPropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, valueType);
  }

  public static TValue? GetPropertyValue<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return (TValue?)GetPropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, typeof(TValue));
  }

  #endregion
  #region Direct set methods

  public static void SetPropertyValue(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    SetPropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static void SetPropertyValue<TValue>(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    SetPropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, typeof(TValue), value);
  }

  public static void SetPropertyValue<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    SetPropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static void SetPropertyValue<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    SetPropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, typeof(TValue), value);
  }

  #endregion
  #region Direct replace methods

  public static object? ReplacePropertyValue(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return ReplacePropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static TValue? ReplacePropertyValue<TValue>(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return (TValue?)ReplacePropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, typeof(TValue), value);
  }

  public static object? ReplacePropertyValue<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return ReplacePropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static TValue? ReplacePropertyValue<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return (TValue?)ReplacePropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, typeof(TValue), value);
  }

  #endregion
  #region Direct exchange methods

  public static void ExchangePropertyValue(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    value = ReplacePropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static void ExchangePropertyValue<TValue>(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    value = (TValue?)ReplacePropertyValueCore(sourceType, sourceObject, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, typeof(TValue), value);
  }

  public static void ExchangePropertyValue<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    value = ReplacePropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static void ExchangePropertyValue<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    value = (TValue?)ReplacePropertyValueCore(typeof(TSource), source, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, typeof(TValue), value);
  }

  #endregion
  #endregion
  #endregion
  #region Static property public methods
  #region Without parameters
  #region Try info methods

  public static bool TryGetProperty(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, Type? valueType,
    [NotNullWhen(true)] out PropertyInfo? propertyInfo)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetPropertyCore(sourceType, null, name, memberAccessibility, typeArguments, null, null, valueType, out propertyInfo);
  }

  public static bool TryGetProperty<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    [NotNullWhen(true)] out PropertyInfo? propertyInfo)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetPropertyCore(sourceType, null, name, memberAccessibility, typeArguments, null, null, typeof(TValue), out propertyInfo);
  }

  public static bool TryGetProperty<TSource>(string name, MemberAccessibility memberAccessibility, Type? valueType,
    [NotNullWhen(true)] out PropertyInfo? propertyInfo)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryGetPropertyCore(typeof(TSource), null, name, memberAccessibility, null, null, null, valueType, out propertyInfo);
  }

  public static bool TryGetProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility,
    [NotNullWhen(true)] out PropertyInfo? propertyInfo)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryGetPropertyCore(typeof(TSource), null, name, memberAccessibility, null, null, null, typeof(TValue), out propertyInfo);
  }

  #endregion
  #region Try get methods

  public static bool TryGetPropertyValue(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, Type? valueType, out object? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetPropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, null, null, valueType, out value);
  }

  public static bool TryGetPropertyValue<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, out TValue? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    if (TryGetPropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, null, null, typeof(TValue?), out var result))
    {
      value = (TValue?)result;
      return true;
    }
    else
    {
      value = default;
      return false;
    }
  }

  public static bool TryGetPropertyValue<TSource>(string name, MemberAccessibility memberAccessibility, Type? valueType, out object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryGetPropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, null, null, valueType, out value);
  }

  public static bool TryGetPropertyValue<TSource, TValue>(string name, MemberAccessibility memberAccessibility, out TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    if (TryGetPropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, null, null, typeof(TValue?), out var result))
    {
      value = (TValue?)result;
      return true;
    }
    else
    {
      value = default;
      return false;
    }
  }

  #endregion
  #region Try set methods

  public static bool TrySetPropertyValue(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, object? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TrySetPropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, null, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static bool TrySetPropertyValue<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, TValue? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TrySetPropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, null, null, typeof(TValue?), value);
  }

  public static bool TrySetPropertyValue<TSource>(string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TrySetPropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, null, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static bool TrySetPropertyValue<TSource, TValue>(string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TrySetPropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, null, null, typeof(TValue?), value);
  }

  #endregion
  #region Try replace methods

  public static bool TryReplacePropertyValue(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, object? newValue, out object? oldValue)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryReplacePropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, null, null, TypedValue.GetType(newValue), TypedValue.GetValue(newValue), out oldValue);
  }

  public static bool TryReplacePropertyValue<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, TValue? newValue, out TValue? oldValue)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    if (TryReplacePropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, null, null, typeof(TValue), newValue, out var result))
    {
      oldValue = (TValue?)result;
      return true;
    }
    else
    {
      oldValue = default;
      return false;
    }
  }

  public static bool TryReplacePropertyValue<TSource>(string name, MemberAccessibility memberAccessibility, object? newValue, out object? oldValue)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryReplacePropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, null, null, TypedValue.GetType(newValue), TypedValue.GetValue(newValue), out oldValue);
  }

  public static bool TryReplacePropertyValue<TSource, TValue>(string name, MemberAccessibility memberAccessibility, TValue? newValue, out TValue? oldValue)
  {
    Argument.That.NotNullOrWhitespace(name);

    if (TryReplacePropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, null, null, typeof(TValue), newValue, out var result))
    {
      oldValue = (TValue?)result;
      return true;
    }
    else
    {
      oldValue = default;
      return false;
    }
  }

  #endregion
  #region Try exchange methods

  public static bool TryExchangePropertyValue(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, ref object? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    if (!TryReplacePropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, null, null, TypedValue.GetType(value), TypedValue.GetValue(value), out var result))
      return false;
    value = result;
    return true;
  }

  public static bool TryExchangePropertyValue<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, ref TValue? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    if (!TryReplacePropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, null, null, typeof(TValue), value, out var result))
      return false;
    value = (TValue?)result;
    return true;
  }

  public static bool TryExchangePropertyValue<TSource>(string name, MemberAccessibility memberAccessibility, ref object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    if (!TryReplacePropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, null, null, TypedValue.GetType(value), TypedValue.GetValue(value), out var result))
      return false;
    value = result;
    return true;
  }

  public static bool TryExchangePropertyValue<TSource, TValue>(string name, MemberAccessibility memberAccessibility, ref TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    if (!TryReplacePropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, null, null, typeof(TValue), value, out var result))
      return false;
    value = (TValue?)result;
    return true;
  }

  #endregion
  #region Direct info methods

  public static PropertyInfo GetProperty(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, Type? valueType)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return GetPropertyCore(sourceType, null, name, memberAccessibility, typeArguments, null, null, valueType);
  }

  public static PropertyInfo GetProperty<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return GetPropertyCore(sourceType, null, name, memberAccessibility, typeArguments, null, null, typeof(TValue));
  }

  public static PropertyInfo GetProperty<TSource>(string name, MemberAccessibility memberAccessibility, Type? valueType)
  {
    Argument.That.NotNullOrWhitespace(name);

    return GetPropertyCore(typeof(TSource), null, name, memberAccessibility, null, null, null, valueType);
  }

  public static PropertyInfo GetProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility)
  {
    Argument.That.NotNullOrWhitespace(name);

    return GetPropertyCore(typeof(TSource), null, name, memberAccessibility, null, null, null, typeof(TValue));
  }

  #endregion
  #region Direct get methods

  public static object? GetPropertyValue(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, Type? valueType)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return GetPropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, null, null, valueType);
  }

  public static TValue? GetPropertyValue<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return (TValue?)GetPropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, null, null, typeof(TValue));
  }

  public static object? GetPropertyValue<TSource>(string name, MemberAccessibility memberAccessibility, Type? valueType)
  {
    Argument.That.NotNullOrWhitespace(name);

    return GetPropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, null, null, valueType);
  }

  public static TValue? GetPropertyValue<TSource, TValue>(string name, MemberAccessibility memberAccessibility)
  {
    Argument.That.NotNullOrWhitespace(name);

    return (TValue?)GetPropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, null, null, typeof(TValue));
  }

  #endregion
  #region Direct set methods

  public static void SetPropertyValue(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, object? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    SetPropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, null, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static void SetPropertyValue<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, TValue? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    SetPropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, null, null, typeof(TValue), value);
  }

  public static void SetPropertyValue<TSource>(string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    SetPropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, null, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static void SetPropertyValue<TSource, TValue>(string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    SetPropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, null, null, typeof(TValue), value);
  }

  #endregion
  #region Direct replace methods

  public static object? ReplacePropertyValue(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, object? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return ReplacePropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, null, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static TValue? ReplacePropertyValue<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, TValue? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return (TValue?)ReplacePropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, null, null, typeof(TValue), value);
  }

  public static object? ReplacePropertyValue<TSource>(string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    return ReplacePropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, null, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static TValue? ReplacePropertyValue<TSource, TValue>(string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    return (TValue?)ReplacePropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, null, null, typeof(TValue), value);
  }

  #endregion
  #region Direct exchange methods

  public static void ExchangePropertyValue(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, ref object? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    value = ReplacePropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, null, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static void ExchangePropertyValue<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, ref TValue? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    value = (TValue?)ReplacePropertyValueCore(sourceType, null, name, memberAccessibility, null, null, null, typeof(TValue), value);
  }

  public static void ExchangePropertyValue<TSource>(string name, MemberAccessibility memberAccessibility, ref object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    value = ReplacePropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, null, null, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static void ExchangePropertyValue<TSource, TValue>(string name, MemberAccessibility memberAccessibility, ref TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    value = (TValue?)ReplacePropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, null, null, typeof(TValue), value);
  }

  #endregion
  #endregion
  #region With parameters
  #region Try info methods

  public static bool TryGetProperty(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? valueType,
    [NotNullWhen(true)] out PropertyInfo? propertyInfo)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetPropertyCore(sourceType, null, name, memberAccessibility, typeArguments, positionalParameterTypes, namedParameterTypes, valueType, out propertyInfo);
  }

  public static bool TryGetProperty<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes,
    [NotNullWhen(true)] out PropertyInfo? propertyInfo)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetPropertyCore(sourceType, null, name, memberAccessibility, typeArguments, positionalParameterTypes, namedParameterTypes, typeof(TValue), out propertyInfo);
  }

  public static bool TryGetProperty<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? valueType,
    [NotNullWhen(true)] out PropertyInfo? propertyInfo)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryGetPropertyCore(typeof(TSource), null, name, memberAccessibility, null, positionalParameterTypes, namedParameterTypes, valueType, out propertyInfo);
  }

  public static bool TryGetProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes,
    [NotNullWhen(true)] out PropertyInfo? propertyInfo)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryGetPropertyCore(typeof(TSource), null, name, memberAccessibility, null, positionalParameterTypes, namedParameterTypes, typeof(TValue), out propertyInfo);
  }

  #endregion
  #region Try get methods

  public static bool TryGetPropertyValue(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType, out object? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetPropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, positionalParameterValues, namedParameterValues, valueType, out value);
  }

  public static bool TryGetPropertyValue<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, out TValue? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    if (TryGetPropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, positionalParameterValues, namedParameterValues, typeof(TValue?), out var result))
    {
      value = (TValue?)result;
      return true;
    }
    else
    {
      value = default;
      return false;
    }
  }

  public static bool TryGetPropertyValue<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType, out object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryGetPropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, valueType, out value);
  }

  public static bool TryGetPropertyValue<TSource, TValue>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, out TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    if (TryGetPropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, typeof(TValue?), out var result))
    {
      value = (TValue?)result;
      return true;
    }
    else
    {
      value = default;
      return false;
    }
  }

  #endregion
  #region Try set methods

  public static bool TrySetPropertyValue(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TrySetPropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static bool TrySetPropertyValue<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TrySetPropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, positionalParameterValues, namedParameterValues, typeof(TValue?), value);
  }

  public static bool TrySetPropertyValue<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TrySetPropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static bool TrySetPropertyValue<TSource, TValue>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TrySetPropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, typeof(TValue?), value);
  }

  #endregion
  #region Try replace methods

  public static bool TryReplacePropertyValue(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? newValue, out object? oldValue)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryReplacePropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, positionalParameterValues, namedParameterValues, TypedValue.GetType(newValue), TypedValue.GetValue(newValue), out oldValue);
  }

  public static bool TryReplacePropertyValue<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? newValue, out TValue? oldValue)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    if (TryReplacePropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, positionalParameterValues, namedParameterValues, typeof(TValue), newValue, out var result))
    {
      oldValue = (TValue?)result;
      return true;
    }
    else
    {
      oldValue = default;
      return false;
    }
  }

  public static bool TryReplacePropertyValue<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? newValue, out object? oldValue)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryReplacePropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(newValue), TypedValue.GetValue(newValue), out oldValue);
  }

  public static bool TryReplacePropertyValue<TSource, TValue>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? newValue, out TValue? oldValue)
  {
    Argument.That.NotNullOrWhitespace(name);

    if (TryReplacePropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, typeof(TValue), newValue, out var result))
    {
      oldValue = (TValue?)result;
      return true;
    }
    else
    {
      oldValue = default;
      return false;
    }
  }

  #endregion
  #region Try exchange methods

  public static bool TryExchangePropertyValue(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref object? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    if (!TryReplacePropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypedValue.GetValue(value), out var result))
      return false;
    value = result;
    return true;
  }

  public static bool TryExchangePropertyValue<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref TValue? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    if (!TryReplacePropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, positionalParameterValues, namedParameterValues, typeof(TValue), value, out var result))
      return false;
    value = (TValue?)result;
    return true;
  }

  public static bool TryExchangePropertyValue<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    if (!TryReplacePropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypedValue.GetValue(value), out var result))
      return false;
    value = result;
    return true;
  }

  public static bool TryExchangePropertyValue<TSource, TValue>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    if (!TryReplacePropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, typeof(TValue), value, out var result))
      return false;
    value = (TValue?)result;
    return true;
  }

  #endregion
  #region Direct info methods

  public static PropertyInfo GetProperty(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return GetPropertyCore(sourceType, null, name, memberAccessibility, typeArguments,
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)), valueType);
  }

  public static PropertyInfo GetProperty<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return GetPropertyCore(sourceType, null, name, memberAccessibility, typeArguments,
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)), typeof(TValue));
  }

  public static PropertyInfo GetProperty<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType)
  {
    Argument.That.NotNullOrWhitespace(name);

    return GetPropertyCore(typeof(TSource), null, name, memberAccessibility, null,
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)), valueType);
  }

  public static PropertyInfo GetProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNullOrWhitespace(name);

    return GetPropertyCore(typeof(TSource), null, name, memberAccessibility, null,
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)), typeof(TValue));
  }

  #endregion
  #region Direct get methods

  public static object? GetPropertyValue(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return GetPropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, positionalParameterValues, namedParameterValues, valueType);
  }

  public static TValue? GetPropertyValue<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return (TValue?)GetPropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, positionalParameterValues, namedParameterValues, typeof(TValue));
  }

  public static object? GetPropertyValue<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType)
  {
    Argument.That.NotNullOrWhitespace(name);

    return GetPropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, valueType);
  }

  public static TValue? GetPropertyValue<TSource, TValue>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNullOrWhitespace(name);

    return (TValue?)GetPropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, typeof(TValue));
  }

  #endregion
  #region Direct set methods

  public static void SetPropertyValue(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    SetPropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static void SetPropertyValue<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    SetPropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, positionalParameterValues, namedParameterValues, typeof(TValue), value);
  }

  public static void SetPropertyValue<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    SetPropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static void SetPropertyValue<TSource, TValue>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    SetPropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, typeof(TValue), value);
  }

  #endregion
  #region Direct replace methods

  public static object? ReplacePropertyValue(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return ReplacePropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static TValue? ReplacePropertyValue<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return (TValue?)ReplacePropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, positionalParameterValues, namedParameterValues, typeof(TValue), value);
  }

  public static object? ReplacePropertyValue<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    return ReplacePropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static TValue? ReplacePropertyValue<TSource, TValue>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    return (TValue?)ReplacePropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, typeof(TValue), value);
  }

  #endregion
  #region Direct exchange methods

  public static void ExchangePropertyValue(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref object? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    value = ReplacePropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static void ExchangePropertyValue<TValue>(Type sourceType, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref TValue? value)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    value = (TValue?)ReplacePropertyValueCore(sourceType, null, name, memberAccessibility, typeArguments, positionalParameterValues, namedParameterValues, typeof(TValue), value);
  }

  public static void ExchangePropertyValue<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    value = ReplacePropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypedValue.GetValue(value));
  }

  public static void ExchangePropertyValue<TSource, TValue>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    value = (TValue?)ReplacePropertyValueCore(typeof(TSource), null, name, memberAccessibility, null, positionalParameterValues, namedParameterValues, typeof(TValue), value);
  }

  #endregion
  #endregion
  #endregion
  #region Method internal methods

  private record struct SignatureWeight
  {
    public int GenericArguments;

    public int TypeConstraints;

    public int InterfacesConstraints;

    public int ConstructorConstraints;

    public int Identical;

    public int Reqired;

    public int Specified;

    public int TotalTypes;

    public int GenericTypes;

    public int TotalInterfaces;

    public int GenericInterfaces;

    public int Total;
  }

  private static readonly Lazy<IComparer<SignatureWeight>> SignatureWeightComparer = new(() => new CustomComparer<SignatureWeight>(CompareSignatureWeight));

  private static int CompareSignatureWeight(SignatureWeight x, SignatureWeight y)
  {
    var result = Comparable.Compare(x.TotalTypes, y.TotalTypes);
    if (result != 0) return result;
    result = Comparable.Compare(x.GenericTypes, y.GenericTypes);
    if (result != 0) return result;
    result = Comparable.Compare(x.TotalInterfaces, y.TotalInterfaces);
    if (result != 0) return result;
    result = Comparable.Compare(x.GenericInterfaces, y.GenericInterfaces);
    if (result != 0) return result;
    result = -Comparable.Compare(x.GenericArguments, y.GenericArguments);
    if (result != 0) return result;
    result = Comparable.Compare(x.Reqired, y.Reqired);
    if (result != 0) return result;
    result = -Comparable.Compare(x.Specified, y.Specified);
    if (result != 0) return result;
    result = Comparable.Compare(x.TypeConstraints, y.TypeConstraints);
    if (result != 0) return result;
    result = Comparable.Compare(x.InterfacesConstraints, y.InterfacesConstraints);
    if (result != 0) return result;
    result = Comparable.Compare(x.ConstructorConstraints, y.ConstructorConstraints);
    if (result != 0) return result;
    result = -Comparable.Compare(x.Total, y.Total);
    return result;
  }

  private static IEnumerable<MethodInfo> GetMethods(Type sourceType, bool staticMember, string name, MemberAccessibility memberAccessibility)
  {
    var bindingFlags = GetBindingFlags(staticMember, memberAccessibility);
    var nameComparison = memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    var predicate = (MethodInfo methodInfo) =>
      string.Equals(methodInfo.Name, name, nameComparison) && !methodInfo.IsAbstract &&
      (methodInfo.Attributes & MethodAttributes.MemberAccessMask) switch
      {
        MethodAttributes.Private => memberAccessibility.IsFlagsSet(MemberAccessibility.Private),
        MethodAttributes.Public => memberAccessibility.IsFlagsSet(MemberAccessibility.Public),
        MethodAttributes.Assembly => memberAccessibility.IsFlagsSet(MemberAccessibility.Assembly),
        MethodAttributes.Family => memberAccessibility.IsFlagsSet(MemberAccessibility.Family),
        MethodAttributes.FamORAssem => memberAccessibility.IsFlagsSet(MemberAccessibility.FamilyOrAssembly),
        MethodAttributes.FamANDAssem => memberAccessibility.IsFlagsSet(MemberAccessibility.FamilyAndAssembly),
        _ => false
      };
    return sourceType.GetMethods(bindingFlags).Where(predicate);
  }

  private static bool MatchMethod(Type sourceType, MethodInfo methodInfo,
    IReadOnlyList<Type?>? sourceTypeArguments, IList<Type?>? resultTypeArguments, IReadOnlyList<Type?>? sourceMethodArguments, IList<Type?>? resultMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType, bool awaitable,
    out SignatureWeight methodWeight)
  {
    if (MatchMethod(methodInfo, sourceTypeArguments, resultTypeArguments, sourceMethodArguments, resultMethodArguments, positionalParameterTypes, namedParameterTypes, out var signatureWeight))
    {
      var awaitableResultType = methodInfo.ReturnType.GetAwaitableResultType();
      if (!awaitable || awaitableResultType is not null)
      {
        var resultType = awaitable ? awaitableResultType : methodInfo.ReturnType;
        if (resultType is not null)
        {
          if (returnType is null || MatchType(returnType, TypeVariance.Covariant, resultType, sourceTypeArguments, resultTypeArguments, sourceMethodArguments, resultMethodArguments))
          {
            methodWeight = signatureWeight;
            return true;
          }
        }
      }
    }
    methodWeight = default;
    return false;
  }

  private static bool MatchParameters(IReadOnlyList<ParameterInfo> parameters,
    IReadOnlyList<Type?>? sourceTypeArguments, IList<Type?>? resultTypeArguments, IReadOnlyList<Type?>? sourceMethodArguments, IList<Type?>? resultMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes,
    ref SignatureWeight methodWeight)
  {
    for (var parameterIndex = 0; parameterIndex < parameters.Count; parameterIndex++)
    {
      var parameterInfo = parameters[parameterIndex];
      var parameterType = parameterInfo.ParameterType;
      var sourceType = default(Type?);
      if (positionalParameterTypes is not null && parameterIndex < positionalParameterTypes.Count ||
        namedParameterTypes is not null && !string.IsNullOrEmpty(parameterInfo.Name) && namedParameterTypes.TryGetValue(parameterInfo.Name, out sourceType))
      {
        if (positionalParameterTypes is not null && parameterIndex < positionalParameterTypes.Count)
          sourceType = positionalParameterTypes[parameterIndex];
        if (sourceType is null)
        {
          if (!parameterType.IsNullAssignable())
            return false;
        }
        else
        {
          var variance = parameterInfo.IsOut ? TypeVariance.Covariant : parameterType.IsByRef ? TypeVariance.Invariant : TypeVariance.Contravariant;
          if (!MatchType(sourceType, variance, parameterType, sourceTypeArguments, resultTypeArguments, sourceMethodArguments, resultMethodArguments))
            return false;
          if (parameterType == sourceType || parameterType.IsByRef && !sourceType.IsByRef && parameterType.GetElementType() == sourceType)
            methodWeight.Identical++;
        }
        if (parameterType.IsInterface)
        {
          methodWeight.TotalInterfaces++;
          if (parameterType.IsGenericType && !parameterType.IsConstructedGenericType)
            methodWeight.GenericInterfaces++;
        }
        else
        {
          methodWeight.TotalTypes++;
          if (parameterType.IsGenericType && !parameterType.IsConstructedGenericType)
            methodWeight.GenericTypes++;
        }
        if (!parameterInfo.HasDefaultValue)
          methodWeight.Reqired++;
        methodWeight.Specified++;
      }
      else
      {
        if (!parameterInfo.HasDefaultValue)
          return false;
      }
      methodWeight.Total++;
    }
    return true;
  }

  private static bool MatchArguments(
    IReadOnlyList<Type>? genericTypeArguments, IReadOnlyList<Type?>? sourceTypeArguments, IList<Type?>? resultTypeArguments,
    IReadOnlyList<Type>? genericMethodArguments, IReadOnlyList<Type?>? sourceMethodArguments, IList<Type?>? resultMethodArguments)
  {
    if (genericTypeArguments is not null)
    {
      if (resultTypeArguments is not null)
        if (resultTypeArguments.Count != genericTypeArguments.Count)
          return false;
      if (sourceTypeArguments is not null)
      {
        if (sourceTypeArguments.Count != genericTypeArguments.Count)
          return false;
        if (!sourceTypeArguments
          .Zip(genericTypeArguments, (srcType, genType) =>
            srcType is null || MatchType(srcType, TypeVariance.Invariant, genType, sourceTypeArguments, resultTypeArguments, sourceMethodArguments, resultMethodArguments))
          .All(isConformed => isConformed))
          return false;
      }
    }
    if (genericMethodArguments is not null)
    {
      if (resultMethodArguments is not null)
        if (resultMethodArguments.Count != genericMethodArguments.Count)
          return false;
      if (sourceMethodArguments is not null)
      {
        if (sourceMethodArguments.Count != genericMethodArguments.Count)
          return false;
        if (!sourceMethodArguments
          .Zip(genericMethodArguments, (srcType, genType) =>
            srcType is null || MatchType(srcType, TypeVariance.Invariant, genType, sourceTypeArguments, resultTypeArguments, sourceMethodArguments, resultMethodArguments))
          .All(isConformed => isConformed))
          return false;
      }
    }
    return true;
  }

  private static void MatchConstraints(IReadOnlyList<Type> genericArguments, ref SignatureWeight signatureWeight)
  {
    for (var index = 0; index < genericArguments.Count; index++)
    {
      var argumentType = genericArguments[index];
      var hasTypeConstraint = (argumentType.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0 ||
          (argumentType.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0 ||
          argumentType.GetGenericParameterConstraints().Any(type => type.IsClass);
      if (hasTypeConstraint)
        signatureWeight.TypeConstraints += 1;
      var hasInterfacesConstraint = argumentType.GetGenericParameterConstraints().Any(type => type.IsInterface);
      if (hasInterfacesConstraint)
        signatureWeight.InterfacesConstraints += 1;
      var hasConstructorConstraint = (argumentType.GenericParameterAttributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0;
      if (hasConstructorConstraint)
        signatureWeight.ConstructorConstraints += 1;
    }
    signatureWeight.GenericArguments += genericArguments.Count;
  }

  private static bool MatchMethod(MethodBase methodBase,
    IReadOnlyList<Type?>? sourceTypeArguments, IList<Type?>? resultTypeArguments, IReadOnlyList<Type?>? sourceMethodArguments, IList<Type?>? resultMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes,
    out SignatureWeight methodWeight)
  {
    methodWeight = default;
    var currentWeight = new SignatureWeight();
    var genericTypeArguments = methodBase.ReflectedType?.GetGenericArguments();
    var genericMethodArguments = methodBase.IsGenericMethod ? methodBase.GetGenericArguments() : Type.EmptyTypes;
    if (!MatchArguments(genericTypeArguments, sourceTypeArguments, resultTypeArguments, genericMethodArguments, sourceMethodArguments, resultMethodArguments))
      return false;
    var methodParameters = methodBase.GetParameters();
    if (methodParameters.Length < (positionalParameterTypes?.Count ?? 0))
      return false;
    if (!MatchParameters(methodParameters, sourceTypeArguments, resultTypeArguments, sourceMethodArguments, resultMethodArguments, positionalParameterTypes, namedParameterTypes, ref currentWeight))
      return false;
    if (!methodBase.IsConstructor && methodBase.IsGenericMethod)
    {
      var genericArguments = methodBase.GetGenericArguments();
      MatchConstraints(genericArguments, ref currentWeight);
    }
    methodWeight = currentWeight;
    return true;
  }

  private static MethodInfo? MatchMethod(Type sourceType, bool staticMember, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? sourceTypeArguments, IReadOnlyList<Type?>? sourceMethodArguments, IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType, bool awaitable)
  {
    var topHierarchy = memberAccessibility.IsFlagsSet(MemberAccessibility.TopHierarchy);
    var resultMethods = GetMethods(sourceType, staticMember, name, memberAccessibility)
      .Select(methodInfo =>
      {
        var inheritanceLevel = methodInfo.ReflectedType!.GetInheritanceLevel(methodInfo.DeclaringType!);
        var resultTypeArguments = new Type[sourceType.GetGenericArguments().Length];
        var resultMethodArguments = new Type[methodInfo.GetGenericArguments().Length];
        var matched = MatchMethod(sourceType, methodInfo, sourceTypeArguments, resultTypeArguments, sourceMethodArguments, resultMethodArguments, positionalParameterTypes, namedParameterTypes, returnType, awaitable, out var methodWeight);
        return (methodInfo: matched ? methodInfo : null, inheritanceLevel, methodWeight, typeArguments: resultTypeArguments, methodArguments: resultMethodArguments);
      })
      .Where(tuple => tuple.methodInfo is not null)
      .OrderByDescending(tuple => tuple.methodWeight, SignatureWeightComparer.Value)
      .ThenBy(tuple => tuple.inheritanceLevel)
      .TakeWhile((signatureWeight: default(SignatureWeight?), inheritanceLevel: -1),
        (state, item) => (item.methodWeight, item.inheritanceLevel),
        (state, item) => state.signatureWeight is null || CompareSignatureWeight(item.methodWeight, state.signatureWeight.Value) == 0 && (!topHierarchy || state.inheritanceLevel == item.inheritanceLevel))
      .Take(2);
    var methodInfo = default(MethodInfo);
    var typeArguments = default(Type?[]);
    var methodArguments = default(Type?[]);
    var inheritanceLevel = default(int);
    foreach (var resultMethod in resultMethods)
    {
      if (methodInfo is null)
      {
        methodInfo = resultMethod.methodInfo;
        typeArguments = resultMethod.typeArguments;
        methodArguments = resultMethod.methodArguments;
        inheritanceLevel = resultMethod.inheritanceLevel;
        continue;
      }
      if (!topHierarchy || inheritanceLevel == resultMethod.inheritanceLevel)
        Operation.That.Failed(FormatMessage(ReflectionMessage.MethodAmbiguousMatch));
      break;
    }
    if (methodInfo is not null)
    {
      if (sourceType.IsGenericType && !sourceType.IsConstructedGenericType)
      {
        sourceType = sourceType.MakeGenericType(typeArguments);
        methodInfo = MatchMethod(sourceType, staticMember, name, memberAccessibility, typeArguments, methodArguments, positionalParameterTypes, namedParameterTypes, returnType, awaitable);
      }
      if (methodInfo is not null && methodInfo.IsGenericMethod && methodInfo.IsGenericMethodDefinition)
      {
        methodInfo = methodInfo.MakeGenericMethod(methodArguments);
      }
    }
    return methodInfo;
  }

  private static bool TryGetMethodCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType,
    [NotNullWhen(true)] out MethodInfo? result)
  {
    var methodInfo = MatchMethod(sourceType, source is null, name, memberAccessibility, typeArguments?.AsReadOnly(), methodArguments?.AsReadOnly(), positionalParameterTypes, namedParameterTypes, returnType, false);
    if (methodInfo is not null)
    {
      methodInfo.GetGenericArguments().SetArgumentTypes(methodArguments);
      methodInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
      result = methodInfo;
      return true;
    }
    result = null;
    return false;
  }

  private static MethodInfo GetMethodCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    var methodInfo = MatchMethod(sourceType, source is null, name, memberAccessibility, typeArguments?.AsReadOnly(), methodArguments?.AsReadOnly(), positionalParameterTypes, namedParameterTypes, returnType, false);
    Operation.That.IsValid(methodInfo is not null, FormatMessage(ReflectionMessage.MethodNotFound));
    methodInfo.GetGenericArguments().SetArgumentTypes(methodArguments);
    methodInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
    return methodInfo;
  }

  private static bool TryCallMethodCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType, out object? returnValue)
  {
    var methodInfo = MatchMethod(sourceType, source is null, name, memberAccessibility, typeArguments?.AsReadOnly(), methodArguments?.AsReadOnly(),
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)), returnType, false);
    if (methodInfo is not null)
    {
      var parameters = methodInfo.GetParameters();
      var values = parameters.GetParameterValues(positionalParameterValues, namedParameterValues);
      var result = methodInfo.Invoke(source, values);
      parameters.SetParameterValues(values, positionalParameterValues, namedParameterValues);
      methodInfo.GetGenericArguments().SetArgumentTypes(methodArguments);
      methodInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
      returnValue = result;
      return true;
    }
    returnValue = null;
    return false;
  }

  private static object? CallMethodCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    var methodInfo = MatchMethod(sourceType, source is null, name, memberAccessibility, typeArguments?.AsReadOnly(), methodArguments?.AsReadOnly(),
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)), returnType, false);
    Operation.That.IsValid(methodInfo is not null, FormatMessage(ReflectionMessage.MethodNotFound));
    var parameters = methodInfo.GetParameters();
    var values = parameters.GetParameterValues(positionalParameterValues, namedParameterValues);
    var result = methodInfo.Invoke(source, values);
    parameters.SetParameterValues(values, positionalParameterValues, namedParameterValues);
    methodInfo.GetGenericArguments().SetArgumentTypes(methodArguments);
    methodInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
    return result;
  }

  private static async Task<TryOut<object>> TryCallMethodAsyncCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    var methodInfo = MatchMethod(sourceType, source is null, name, memberAccessibility, typeArguments?.AsReadOnly(), methodArguments?.AsReadOnly(),
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)), returnType, true);
    if (methodInfo is not null)
    {
      var parameters = methodInfo.GetParameters();
      var values = parameters.GetParameterValues(positionalParameterValues, namedParameterValues);
      var awaitableResult = await InvokeAwaitableMethod(source, methodInfo, values);
      parameters.SetParameterValues(values, positionalParameterValues, namedParameterValues);
      methodInfo.GetGenericArguments().SetArgumentTypes(methodArguments);
      methodInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
      return TryOut.Success(awaitableResult);
    }
    return TryOut.Failure<object>();
  }

  private static async Task<object?> CallMethodAsyncCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    var methodInfo = MatchMethod(sourceType, source is null, name, memberAccessibility, typeArguments?.AsReadOnly(), methodArguments?.AsReadOnly(),
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)), returnType, true);
    Operation.That.IsValid(methodInfo is not null, FormatMessage(ReflectionMessage.MethodNotFound));
    var parameters = methodInfo.GetParameters();
    var values = parameters.GetParameterValues(positionalParameterValues, namedParameterValues);
    var awaitableResult = await InvokeAwaitableMethod(source, methodInfo, values);
    parameters.SetParameterValues(values, positionalParameterValues, namedParameterValues);
    methodInfo.GetGenericArguments().SetArgumentTypes(methodArguments);
    methodInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
    return awaitableResult;
  }

  #endregion
  #region Instance method public methods
  #region Try info methods

  public static bool TryGetMethod(object source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes,
    [NotNullWhen(true)] out MethodInfo? methodInfo)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryGetMethodCore(sourceType, sourceObject, name, memberAccessibility, null, methodArguments, positionalParameterTypes, namedParameterTypes, typeof(void), out methodInfo);
  }

  public static bool TryGetMethod(object source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType,
    [NotNullWhen(true)] out MethodInfo? methodInfo)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryGetMethodCore(sourceType, sourceObject, name, memberAccessibility, null, methodArguments, positionalParameterTypes, namedParameterTypes, returnType, out methodInfo);
  }

  public static bool TryGetMethod<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes,
    [NotNullWhen(true)] out MethodInfo? methodInfo)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetMethodCore(typeof(TSource), source, name, memberAccessibility, null, methodArguments, positionalParameterTypes, namedParameterTypes, typeof(void), out methodInfo);
  }

  public static bool TryGetMethod<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType,
    [NotNullWhen(true)] out MethodInfo? methodInfo)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetMethodCore(typeof(TSource), source, name, memberAccessibility, null, methodArguments, positionalParameterTypes, namedParameterTypes, returnType, out methodInfo);
  }

  #endregion
  #region Direct info methods

  public static MethodInfo GetMethod(object source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return GetMethodCore(sourceType, sourceObject, name, memberAccessibility, null, methodArguments, positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static MethodInfo GetMethod(object source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return GetMethodCore(sourceType, sourceObject, name, memberAccessibility, null, methodArguments, positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static MethodInfo GetMethod<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return GetMethodCore(typeof(TSource), source, name, memberAccessibility, null, methodArguments, positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static MethodInfo GetMethod<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return GetMethodCore(typeof(TSource), source, name, memberAccessibility, null, methodArguments, positionalParameterTypes, namedParameterTypes, returnType);
  }

  #endregion
  #region Try call methods

  public static bool TryCallMethod(object source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryCallMethodCore(sourceType, sourceObject, name, memberAccessibility, null, methodArguments, positionalParameterValues, namedParameterValues, typeof(void), out var returnValue);
  }

  public static bool TryCallMethod(object source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType, out object? returnValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryCallMethodCore(sourceType, sourceObject, name, memberAccessibility, null, methodArguments, positionalParameterValues, namedParameterValues, returnType, out returnValue);
  }

  public static bool TryCallMethod<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryCallMethodCore(typeof(TSource), source, name, memberAccessibility, null, methodArguments, positionalParameterValues, namedParameterValues, typeof(void), out var returnValue);
  }

  public static bool TryCallMethod<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType, out object? returnValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryCallMethodCore(typeof(TSource), source, name, memberAccessibility, null, methodArguments, positionalParameterValues, namedParameterValues, returnType, out returnValue);
  }

  #endregion
  #region Direct call methods

  public static void CallMethod(object source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    CallMethodCore(sourceType, sourceObject, name, memberAccessibility, null, methodArguments, positionalParameterValues, namedParameterValues, typeof(void));
  }

  public static object? CallMethod(object source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return CallMethodCore(sourceType, sourceObject, name, memberAccessibility, null, methodArguments, positionalParameterValues, namedParameterValues, returnType);
  }

  public static void CallMethod<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    CallMethodCore(typeof(TSource), source, name, memberAccessibility, null, methodArguments, positionalParameterValues, namedParameterValues, typeof(void));
  }

  public static object? CallMethod<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return CallMethodCore(typeof(TSource), source, name, memberAccessibility, null, methodArguments, positionalParameterValues, namedParameterValues, returnType);
  }

  #endregion
  #region Try call async methods

  public static async Task<bool> TryCallMethodAsync(object source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return (await TryCallMethodAsyncCore(sourceType, sourceObject, name, memberAccessibility, null, methodArguments, positionalParameterValues, namedParameterValues, null)).Success;
  }

  public static Task<TryOut<object>> TryCallMethodAsync(object source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryCallMethodAsyncCore(sourceType, sourceObject, name, memberAccessibility, null, methodArguments, positionalParameterValues, namedParameterValues, returnType);
  }

  public static async Task<bool> TryCallMethodAsync<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return (await TryCallMethodAsyncCore(typeof(TSource), source, name, memberAccessibility, null, methodArguments, positionalParameterValues, namedParameterValues, null)).Success;
  }

  public static Task<TryOut<object>> TryCallMethodAsync<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryCallMethodAsyncCore(typeof(TSource), source, name, memberAccessibility, null, methodArguments, positionalParameterValues, namedParameterValues, returnType);
  }

  #endregion
  #region Direct call async methods

  public static Task CallMethodAsync(object source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return CallMethodAsyncCore(sourceType, sourceObject, name, memberAccessibility, null, methodArguments, positionalParameterValues, namedParameterValues, null);
  }

  public static Task<object?> CallMethodAsync(object source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return CallMethodAsyncCore(sourceType, sourceObject, name, memberAccessibility, null, methodArguments, positionalParameterValues, namedParameterValues, returnType);
  }

  public static Task CallMethodAsync<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return CallMethodAsyncCore(typeof(TSource), source, name, memberAccessibility, null, methodArguments, positionalParameterValues, namedParameterValues, null);
  }

  public static Task<object?> CallMethodAsync<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return CallMethodAsyncCore(typeof(TSource), source, name, memberAccessibility, null, methodArguments, positionalParameterValues, namedParameterValues, returnType);
  }

  #endregion
  #endregion
  #region Static method public methods
  #region Try info methods

  public static bool TryGetMethod(Type sourceType, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes,
    [NotNullWhen(true)] out MethodInfo? methodInfo)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetMethodCore(sourceType, null, name, memberAccessibility, typeArguments, methodArguments, positionalParameterTypes, namedParameterTypes, typeof(void), out methodInfo);
  }

  public static bool TryGetMethod(Type sourceType, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType,
    [NotNullWhen(true)] out MethodInfo? methodInfo)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetMethodCore(sourceType, null, name, memberAccessibility, typeArguments, methodArguments, positionalParameterTypes, namedParameterTypes, returnType, out methodInfo);
  }

  public static bool TryGetMethod<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes,
    [NotNullWhen(true)] out MethodInfo? methodInfo)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryGetMethodCore(typeof(TSource), null, name, memberAccessibility, null, methodArguments, positionalParameterTypes, namedParameterTypes, typeof(void), out methodInfo);
  }

  public static bool TryGetMethod<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType,
    [NotNullWhen(true)] out MethodInfo? methodInfo)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryGetMethodCore(typeof(TSource), null, name, memberAccessibility, null, methodArguments, positionalParameterTypes, namedParameterTypes, returnType, out methodInfo);
  }

  #endregion
  #region Direct info methods

  public static MethodInfo GetMethod(Type sourceType, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return GetMethodCore(sourceType, null, name, memberAccessibility, typeArguments, methodArguments, positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static MethodInfo GetMethod(Type sourceType, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return GetMethodCore(sourceType, null, name, memberAccessibility, typeArguments, methodArguments, positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static MethodInfo GetMethod<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNullOrWhitespace(name);

    return GetMethodCore(typeof(TSource), null, name, memberAccessibility, null, methodArguments, positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static MethodInfo GetMethod<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNullOrWhitespace(name);

    return GetMethodCore(typeof(TSource), null, name, memberAccessibility, null, methodArguments, positionalParameterTypes, namedParameterTypes, returnType);
  }

  #endregion
  #region Try call methods

  public static bool TryCallMethod(Type sourceType, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryCallMethodCore(sourceType, null, name, memberAccessibility, typeArguments, methodArguments, positionalParameterValues, namedParameterValues, typeof(void), out var returnValue);
  }

  public static bool TryCallMethod(Type sourceType, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType, out object? returnValue)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryCallMethodCore(sourceType, null, name, memberAccessibility, typeArguments, methodArguments, positionalParameterValues, namedParameterValues, returnType, out returnValue);
  }

  public static bool TryCallMethod<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryCallMethodCore(typeof(TSource), null, name, memberAccessibility, null, methodArguments, positionalParameterValues, namedParameterValues, typeof(void), out var returnValue);
  }

  public static bool TryCallMethod<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType, out object? returnValue)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryCallMethodCore(typeof(TSource), null, name, memberAccessibility, null, methodArguments, positionalParameterValues, namedParameterValues, returnType, out returnValue);
  }

  #endregion
  #region Direct call methods

  public static void CallMethod(Type sourceType, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    CallMethodCore(sourceType, null, name, memberAccessibility, typeArguments, methodArguments, positionalParameterValues, namedParameterValues, typeof(void));
  }

  public static object? CallMethod(Type sourceType, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return CallMethodCore(sourceType, null, name, memberAccessibility, typeArguments, methodArguments, positionalParameterValues, namedParameterValues, returnType);
  }

  public static void CallMethod<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNullOrWhitespace(name);

    CallMethodCore(typeof(TSource), null, name, memberAccessibility, null, methodArguments, positionalParameterValues, namedParameterValues, typeof(void));
  }

  public static object? CallMethod<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    Argument.That.NotNullOrWhitespace(name);

    return CallMethodCore(typeof(TSource), null, name, memberAccessibility, null, methodArguments, positionalParameterValues, namedParameterValues, returnType);
  }

  #endregion
  #region Try call async methods

  public static async Task<bool> TryCallMethodAsync(Type sourceType, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return (await TryCallMethodAsyncCore(sourceType, null, name, memberAccessibility, typeArguments, methodArguments, positionalParameterValues, namedParameterValues, null)).Success;
  }

  public static Task<TryOut<object>> TryCallMethodAsync(Type sourceType, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryCallMethodAsyncCore(sourceType, null, name, memberAccessibility, typeArguments, methodArguments, positionalParameterValues, namedParameterValues, returnType);
  }

  public static async Task<bool> TryCallMethodAsync<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNullOrWhitespace(name);

    return (await TryCallMethodAsyncCore(typeof(TSource), null, name, memberAccessibility, null, methodArguments, positionalParameterValues, namedParameterValues, null)).Success;
  }

  public static Task<TryOut<object>> TryCallMethodAsync<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryCallMethodAsyncCore(typeof(TSource), null, name, memberAccessibility, null, methodArguments, positionalParameterValues, namedParameterValues, returnType);
  }

  #endregion
  #region Direct call async method

  public static Task CallMethodAsync(Type sourceType, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return CallMethodAsyncCore(sourceType, null, name, memberAccessibility, typeArguments, methodArguments, positionalParameterValues, namedParameterValues, null);
  }

  public static Task<object?> CallMethodAsync(Type sourceType, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return CallMethodAsyncCore(sourceType, null, name, memberAccessibility, typeArguments, methodArguments, positionalParameterValues, namedParameterValues, returnType);
  }

  public static Task CallMethodAsync<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNullOrWhitespace(name);

    return CallMethodAsyncCore(typeof(TSource), null, name, memberAccessibility, null, methodArguments, positionalParameterValues, namedParameterValues, null);
  }

  public static Task<object?> CallMethodAsync<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    Argument.That.NotNullOrWhitespace(name);

    return CallMethodAsyncCore(typeof(TSource), null, name, memberAccessibility, null, methodArguments, positionalParameterValues, namedParameterValues, returnType);
  }

  #endregion
  #endregion
  #region Constructor internal methods

  private static IEnumerable<ConstructorInfo> GetConstructors(Type sourceType, bool staticMember, MemberAccessibility memberAccessibility)
  {
    var bindingFlags = GetBindingFlags(staticMember, memberAccessibility);
    var predicate = (ConstructorInfo constructorInfo) =>
      (constructorInfo.Attributes & MethodAttributes.MemberAccessMask) switch
      {
        MethodAttributes.Private => memberAccessibility.IsFlagsSet(MemberAccessibility.Private),
        MethodAttributes.Public => memberAccessibility.IsFlagsSet(MemberAccessibility.Public),
        MethodAttributes.Assembly => memberAccessibility.IsFlagsSet(MemberAccessibility.Assembly),
        MethodAttributes.Family => memberAccessibility.IsFlagsSet(MemberAccessibility.Family),
        MethodAttributes.FamORAssem => memberAccessibility.IsFlagsSet(MemberAccessibility.FamilyOrAssembly),
        MethodAttributes.FamANDAssem => memberAccessibility.IsFlagsSet(MemberAccessibility.FamilyAndAssembly),
        _ => false
      };
    return sourceType.GetConstructors(bindingFlags).Where(predicate);
  }

  private static ConstructorInfo? MatchConstructor(Type sourceType, bool staticMember, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? sourceTypeArguments, IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    var resultConstructors = GetConstructors(sourceType, staticMember, memberAccessibility)
      .Select(constructorInfo =>
      {
        var resultTypeArguments = new Type[sourceTypeArguments?.Count ?? constructorInfo.DeclaringType?.GetGenericArguments().Length ?? 0];
        var matched = MatchMethod(constructorInfo, sourceTypeArguments, resultTypeArguments, null, null, positionalParameterTypes, namedParameterTypes, out var constructorWeight);
        return (constructorInfo: matched ? constructorInfo : null, constructorWeight, typeArguments: resultTypeArguments);
      })
      .Where(tuple => tuple.constructorInfo is not null)
      .OrderByDescending(tuple => tuple.constructorWeight, SignatureWeightComparer.Value)
      .TakeWhile(default(SignatureWeight?),
        (state, item) => item.constructorWeight,
        (state, item) => state is null || CompareSignatureWeight(item.constructorWeight, state.Value) == 0)
      .Take(2);
    var constructorInfo = default(ConstructorInfo);
    var typeArguments = default(Type?[]);
    foreach (var resultConstructor in resultConstructors)
    {
      if (constructorInfo is null)
      {
        constructorInfo = resultConstructor.constructorInfo;
        typeArguments = resultConstructor.typeArguments;
        continue;
      }
      Operation.That.Failed(FormatMessage(ReflectionMessage.PropertyAmbiguousMatch));
    }
    if (constructorInfo is not null)
    {
      if (sourceType.IsGenericType && !sourceType.IsConstructedGenericType)
      {
        sourceType = sourceType.MakeGenericType(typeArguments);
        constructorInfo = MatchConstructor(sourceType, staticMember, memberAccessibility, typeArguments, positionalParameterTypes, namedParameterTypes);
      }
    }
    return constructorInfo;
  }

  private static bool TryGetConstructorCore(Type sourceType, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues,
    [NotNullWhen(true)] out ConstructorInfo? result)
  {
    var constructorInfo = MatchConstructor(sourceType, false, memberAccessibility, typeArguments?.AsReadOnly(),
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)));
    if (constructorInfo is not null)
    {
      constructorInfo.DeclaringType?.GetGenericArguments().SetArgumentTypes(typeArguments);
      result = constructorInfo;
      return true;
    }
    result = null;
    return false;
  }

  private static ConstructorInfo GetConstructorCore(Type sourceType, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues)
  {
    var constructorInfo = MatchConstructor(sourceType, false, memberAccessibility, typeArguments?.AsReadOnly(),
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)));
    Operation.That.IsValid(constructorInfo is not null, FormatMessage(ReflectionMessage.ConstructorNotFound));
    constructorInfo.DeclaringType?.GetGenericArguments().SetArgumentTypes(typeArguments);
    return constructorInfo;
  }

  private static bool TryConstructCore(Type sourceType, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues,
    out object? constructedValue)
  {
    var constructorInfo = MatchConstructor(sourceType, false, memberAccessibility, typeArguments?.AsReadOnly(),
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)));
    if (constructorInfo is not null)
    {
      var parameters = constructorInfo.GetParameters();
      var values = parameters.GetParameterValues(positionalParameterValues, namedParameterValues);
      var result = constructorInfo.Invoke(values);
      constructorInfo.DeclaringType?.GetGenericArguments().SetArgumentTypes(typeArguments);
      constructedValue = result;
      return true;
    }
    constructedValue = null;
    return false;
  }

  private static object ConstructCore(Type sourceType, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues)
  {
    var constructorInfo = MatchConstructor(sourceType, false, memberAccessibility, typeArguments?.AsReadOnly(),
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)));
    Operation.That.IsValid(constructorInfo is not null, FormatMessage(ReflectionMessage.ConstructorNotFound));
    var parameters = constructorInfo.GetParameters();
    var values = parameters.GetParameterValues(positionalParameterValues, namedParameterValues);
    var result = constructorInfo.Invoke(values);
    constructorInfo.DeclaringType?.GetGenericArguments().SetArgumentTypes(typeArguments);
    return result;
  }

  #endregion
  #region Constructor public methods
  #region Try info methods

  public static bool TryGetConstructor(Type sourceType, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues,
    [NotNullWhen(true)] out ConstructorInfo? constructorInfo)
  {
    Argument.That.NotNull(sourceType);

    return TryGetConstructorCore(sourceType, memberAccessibility, typeArguments, positionalParameterValues, namedParameterValues, out constructorInfo);
  }

  public static bool TryGetConstructor<TSource>(MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues,
    [NotNullWhen(true)] out ConstructorInfo? constructorInfo)
  {
    return TryGetConstructorCore(typeof(TSource), memberAccessibility, null, positionalParameterValues, namedParameterValues, out constructorInfo);
  }

  #endregion
  #region Direct info methods

  public static ConstructorInfo GetConstructor(Type sourceType, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(sourceType);

    return GetConstructorCore(sourceType, memberAccessibility, typeArguments, positionalParameterValues, namedParameterValues);
  }

  public static ConstructorInfo GetConstructor<TSource>(MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues)
  {
    return GetConstructorCore(typeof(TSource), memberAccessibility, null, positionalParameterValues, namedParameterValues);
  }

  #endregion
  #region Try construct methods

  public static bool TryConstruct(Type sourceType, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues,
    out object? constructedValue)
  {
    Argument.That.NotNull(sourceType);

    return TryConstructCore(sourceType, memberAccessibility, typeArguments, positionalParameterValues, namedParameterValues, out constructedValue);
  }

  public static bool TryConstruct<TSource>(MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues,
    out object? constructedValue)
  {
    return TryConstructCore(typeof(TSource), memberAccessibility, null, positionalParameterValues, namedParameterValues, out constructedValue);
  }

  #endregion
  #region Direct construct methods

  public static object Construct(Type sourceType, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(sourceType);

    return ConstructCore(sourceType, memberAccessibility, typeArguments, positionalParameterValues, namedParameterValues);
  }

  public static object Construct<TSource>(MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues)
  {
    return ConstructCore(typeof(TSource), memberAccessibility, null, positionalParameterValues, namedParameterValues);
  }

  #endregion
  #endregion
  #region Event internal methods

  private static IEnumerable<EventInfo> GetEvents(Type sourceType, bool staticMember, string name, MemberAccessibility memberAccessibility)
  {
    var bindingFlags = GetBindingFlags(staticMember, memberAccessibility);
    var nameComparison = memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    var predicate = (EventInfo eventInfo) =>
      string.Equals(eventInfo.Name, name, nameComparison) &&
      ((eventInfo.AddMethod?.Attributes ?? eventInfo.RemoveMethod?.Attributes ?? MethodAttributes.PrivateScope) & MethodAttributes.MemberAccessMask) switch
      {
        MethodAttributes.Private => memberAccessibility.IsFlagsSet(MemberAccessibility.Private),
        MethodAttributes.Public => memberAccessibility.IsFlagsSet(MemberAccessibility.Public),
        MethodAttributes.Assembly => memberAccessibility.IsFlagsSet(MemberAccessibility.Assembly),
        MethodAttributes.Family => memberAccessibility.IsFlagsSet(MemberAccessibility.Family),
        MethodAttributes.FamORAssem => memberAccessibility.IsFlagsSet(MemberAccessibility.FamilyOrAssembly),
        MethodAttributes.FamANDAssem => memberAccessibility.IsFlagsSet(MemberAccessibility.FamilyAndAssembly),
        _ => false
      };
    return sourceType.GetEvents(bindingFlags).Where(predicate);
  }

  private static EventInfo? MatchEvent(Type sourceType, bool staticMember, string name, MemberAccessibility memberAccessibility, IReadOnlyList<Type?>? sourceTypeArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType, bool awaitable)
  {
    var topHierarchy = memberAccessibility.IsFlagsSet(MemberAccessibility.TopHierarchy);
    var resultEvents = GetEvents(sourceType, staticMember, name, memberAccessibility)
      .Where(eventInfo => eventInfo.EventHandlerType is not null)
      .Select(eventInfo =>
      {
        var inheritanceLevel = eventInfo.ReflectedType!.GetInheritanceLevel(eventInfo.DeclaringType!);
        var resultTypeArguments = new Type[sourceType.GetGenericArguments().Length];
        var methodInfo = eventInfo.EventHandlerType!.GetMethod(Invoke)!;
        var resultMethodArguments = new Type[methodInfo.GetGenericArguments().Length];
        var matched = MatchMethod(sourceType, methodInfo, sourceTypeArguments, resultTypeArguments, null, resultMethodArguments, positionalParameterTypes, namedParameterTypes, returnType, awaitable, out var methodWeight);
        return (eventInfo: matched ? eventInfo : null, inheritanceLevel, methodWeight, typeArguments: resultTypeArguments);
      })
      .Where(tuple => tuple.eventInfo is not null)
      .OrderByDescending(tuple => tuple.methodWeight, SignatureWeightComparer.Value)
      .ThenBy(tuple => tuple.inheritanceLevel)
      .TakeWhile((signatureWeight: default(SignatureWeight?), inheritanceLevel: -1),
        (state, item) => (item.methodWeight, item.inheritanceLevel),
        (state, item) => state.signatureWeight is null || CompareSignatureWeight(item.methodWeight, state.signatureWeight.Value) == 0 && (!topHierarchy || state.inheritanceLevel == item.inheritanceLevel))
      .Take(2);
    var eventInfo = default(EventInfo);
    var typeArguments = default(Type?[]);
    var inheritanceLevel = default(int);
    foreach (var resultEvent in resultEvents)
    {
      if (eventInfo is null)
      {
        eventInfo = resultEvent.eventInfo;
        typeArguments = resultEvent.typeArguments;
        inheritanceLevel = resultEvent.inheritanceLevel;
        continue;
      }
      if (!topHierarchy || inheritanceLevel == resultEvent.inheritanceLevel)
        Operation.That.Failed(FormatMessage(ReflectionMessage.PropertyAmbiguousMatch));
      break;
    }
    if (eventInfo is not null)
    {
      if (sourceType.IsGenericType && !sourceType.IsConstructedGenericType)
      {
        sourceType = sourceType.MakeGenericType(typeArguments);
        eventInfo = MatchEvent(sourceType, staticMember, name, memberAccessibility, typeArguments, positionalParameterTypes, namedParameterTypes, returnType, awaitable);
      }
    }
    return eventInfo;
  }

  private static bool TryGetEventCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType,
    [NotNullWhen(true)] out EventInfo? result)
  {
    var eventInfo = MatchEvent(sourceType, source is null, name, memberAccessibility, typeArguments?.AsReadOnly(), positionalParameterTypes, namedParameterTypes, returnType, false);
    if (eventInfo is not null)
    {
      eventInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
      result = eventInfo;
      return true;
    }
    result = null;
    return false;
  }

  private static EventInfo GetEventCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    var eventInfo = MatchEvent(sourceType, source is null, name, memberAccessibility, typeArguments?.AsReadOnly(), positionalParameterTypes, namedParameterTypes, returnType, false);
    Operation.That.IsValid(eventInfo is not null, FormatMessage(ReflectionMessage.EventNotFound));
    eventInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
    return eventInfo;
  }

  private static bool TryAddEventHandlerCore(
    Type eventType, object? eventSource, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    Type methodType, object? methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    if (!TryGetEventCore(eventType, eventSource, eventName, eventAccessibility, eventTypeArguments, positionalParameterTypes, namedParameterTypes, returnType, out var eventInfo))
      return false;
    if (eventInfo.AddMethod is null || eventInfo.EventHandlerType is null)
      return false;
    if (!TryGetMethodCore(methodType, methodSource, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments, positionalParameterTypes, namedParameterTypes, returnType, out var methodInfo))
      return false;
    var eventDelegate = Delegate.CreateDelegate(eventInfo.EventHandlerType, methodSource, methodInfo);
    eventInfo.AddMethod.Invoke(eventSource, new[] { eventDelegate });
    return true;
  }

  private static void AddEventHandlerCore(
    Type eventType, object? eventSource, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    Type methodType, object? methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    var eventInfo = GetEventCore(eventType, eventSource, eventName, eventAccessibility, eventTypeArguments, positionalParameterTypes, namedParameterTypes, returnType);
    Operation.That.IsValid(eventInfo.AddMethod is not null && eventInfo.EventHandlerType is not null);
    var methodInfo = GetMethodCore(methodType, methodSource, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments, positionalParameterTypes, namedParameterTypes, returnType);
    var eventDelegate = Delegate.CreateDelegate(eventInfo.EventHandlerType, methodSource, methodInfo);
    eventInfo.AddMethod.Invoke(eventSource, new[] { eventDelegate });
  }

  private static bool TryRemoveEventHandlerCore(
    Type eventType, object? eventSource, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    Type methodType, object? methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    if (!TryGetEventCore(eventType, eventSource, eventName, eventAccessibility, eventTypeArguments, positionalParameterTypes, namedParameterTypes, returnType, out var eventInfo))
      return false;
    if (eventInfo.RemoveMethod is null || eventInfo.EventHandlerType is null)
      return false;
    if (!TryGetMethodCore(methodType, methodSource, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments, positionalParameterTypes, namedParameterTypes, returnType, out var methodInfo))
      return false;
    var eventDelegate = Delegate.CreateDelegate(eventInfo.EventHandlerType, methodSource, methodInfo);
    eventInfo.RemoveMethod.Invoke(eventSource, new[] { eventDelegate });
    return true;
  }

  private static void RemoveEventHandlerCore(
    Type eventType, object? eventSource, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    Type methodType, object? methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    var eventInfo = GetEventCore(eventType, eventSource, eventName, eventAccessibility, eventTypeArguments, positionalParameterTypes, namedParameterTypes, returnType);
    Operation.That.IsValid(eventInfo.RemoveMethod is not null && eventInfo.EventHandlerType is not null);
    var methodInfo = GetMethodCore(methodType, methodSource, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments, positionalParameterTypes, namedParameterTypes, returnType);
    var eventDelegate = Delegate.CreateDelegate(eventInfo.EventHandlerType, methodSource, methodInfo);
    eventInfo.RemoveMethod.Invoke(eventSource, new[] { eventDelegate });
  }

  private static bool TryClearEventHandlersCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility,
    Func<EventInfo, object?, Delegate?>? eventHandlerResolver, IList<Type?>? typeArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    var eventInfo = MatchEvent(sourceType, source is null, name, memberAccessibility, typeArguments?.AsReadOnly(), positionalParameterTypes, namedParameterTypes, returnType, false);
    if (eventInfo is null || eventInfo.EventHandlerType is null || eventInfo.RemoveMethod is null)
      return false;
    var methodInfo = eventInfo.EventHandlerType.GetMethod(Invoke);
    if (methodInfo is null)
      return false;
    var eventHandler = default(Delegate);
    if (eventHandlerResolver is not null)
    {
      eventHandler = eventHandlerResolver(eventInfo, source);
    }
    else
    {
      var fieldInfo = MatchField(eventInfo.DeclaringType!, source is null, eventInfo.Name, MemberAccessibility.DeclaredOnly | MemberAccessibility.Private, false, null, eventInfo.EventHandlerType, TypeVariance.Invariant);
      if (fieldInfo is null)
        return false;
      eventHandler = (Delegate?)fieldInfo.GetValue(source);
    }
    if (eventHandler is not null)
    {
      var invocationList = eventHandler.GetInvocationList();
      if (invocationList is not null)
      {
        var removeParameters = new object?[1];
        foreach (var handlerDelegate in invocationList.AsEnumerable().Reverse())
        {
          removeParameters[0] = handlerDelegate;
          eventInfo.RemoveMethod.Invoke(source, removeParameters);
        }
      }
    }
    return true;
  }

  private static void ClearEventHandlersCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility,
    Func<EventInfo, object?, Delegate?>? eventHandlerResolver, IList<Type?>? typeArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    var eventInfo = MatchEvent(sourceType, source is null, name, memberAccessibility, typeArguments?.AsReadOnly(), positionalParameterTypes, namedParameterTypes, returnType, false);
    Operation.That.IsValid(eventInfo is not null, FormatMessage(ReflectionMessage.EventNotFound));
    Operation.That.IsValid(eventInfo.EventHandlerType is not null && eventInfo.RemoveMethod is not null);
    var methodInfo = eventInfo.EventHandlerType.GetMethod(Invoke);
    Operation.That.IsValid(methodInfo is not null, FormatMessage(ReflectionMessage.MethodNotFound));
    var eventHandler = default(Delegate?);
    if (eventHandlerResolver is not null)
    {
      eventHandler = eventHandlerResolver(eventInfo, source);
    }
    else
    {
      var fieldInfo = MatchField(eventInfo.DeclaringType!, source is null, eventInfo.Name, MemberAccessibility.DeclaredOnly | MemberAccessibility.Private, false, null, eventInfo.EventHandlerType, TypeVariance.Invariant);
      Operation.That.IsValid(fieldInfo is not null, FormatMessage(ReflectionMessage.FieldNotFound));
      eventHandler = (Delegate?)fieldInfo.GetValue(source);
    }
    if (eventHandler is not null)
    {
      var invocationList = eventHandler.GetInvocationList();
      if (invocationList is null)
        return;
      var removeParameters = new object?[1];
      foreach (var handlerDelegate in invocationList.AsEnumerable().Reverse())
      {
        removeParameters[0] = handlerDelegate;
        eventInfo.RemoveMethod.Invoke(source, removeParameters);
      }
    }
  }

  private static bool TryRaiseEventCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility,
    Func<EventInfo, object?, Delegate?>? eventHandlerResolver, IList<Type?>? typeArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType,
    out object? returnValue)
  {
    var eventInfo = MatchEvent(sourceType, source is null, name, memberAccessibility, typeArguments?.AsReadOnly(),
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)), returnType, false);
    if (eventInfo is not null)
    {
      var methodInfo = eventInfo.EventHandlerType?.GetMethod(Invoke);
      if (methodInfo is not null)
      {
        var eventHandler = default(Delegate);
        if (eventHandlerResolver is not null)
        {
          eventHandler = eventHandlerResolver(eventInfo, source);
        }
        else
        {
          var fieldInfo = MatchField(eventInfo.DeclaringType!, source is null, eventInfo.Name, MemberAccessibility.DeclaredOnly | MemberAccessibility.Private, false, null, eventInfo.EventHandlerType, TypeVariance.Invariant);
          if (fieldInfo is null)
          {
            returnValue = null;
            return false;
          }
          eventHandler = (Delegate?)fieldInfo.GetValue(source);
        }
        if (eventHandler is not null)
        {
          var parameters = methodInfo.GetParameters();
          var values = parameters.GetParameterValues(positionalParameterValues, namedParameterValues);
          var result = methodInfo.Invoke(eventHandler, values);
          parameters.SetParameterValues(values, positionalParameterValues, namedParameterValues);
          eventInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
          returnValue = result;
        }
        else
        {
          returnValue = null;
        }
        return true;
      }
    }
    returnValue = null;
    return false;
  }

  private static object? RaiseEventCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility,
    Func<EventInfo, object?, Delegate?>? eventHandlerResolver, IList<Type?>? typeArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    var eventInfo = MatchEvent(sourceType, source is null, name, memberAccessibility, typeArguments?.AsReadOnly(),
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)), returnType, false);
    Operation.That.IsValid(eventInfo is not null, FormatMessage(ReflectionMessage.EventNotFound));
    Operation.That.IsValid(eventInfo.EventHandlerType is not null && eventInfo.RemoveMethod is not null);
    var methodInfo = eventInfo.EventHandlerType.GetMethod(Invoke);
    Operation.That.IsValid(methodInfo is not null, FormatMessage(ReflectionMessage.MethodNotFound));
    var eventHandler = default(Delegate?);
    if (eventHandlerResolver is not null)
    {
      eventHandler = eventHandlerResolver(eventInfo, source);
    }
    else
    {
      var fieldInfo = MatchField(eventInfo.DeclaringType!, source is null, eventInfo.Name, MemberAccessibility.DeclaredOnly | MemberAccessibility.Private, false, null, eventInfo.EventHandlerType, TypeVariance.Invariant);
      Operation.That.IsValid(fieldInfo is not null, FormatMessage(ReflectionMessage.FieldNotFound));
      eventHandler = (Delegate?)fieldInfo.GetValue(source);
    }
    if (eventHandler is not null)
    {
      var parameters = methodInfo.GetParameters();
      var values = parameters.GetParameterValues(positionalParameterValues, namedParameterValues);
      var result = methodInfo.Invoke(eventHandler, values);
      parameters.SetParameterValues(values, positionalParameterValues, namedParameterValues);
      return result;
    }
    return null;
  }

  private static async Task<TryOut<object>> TryRaiseEventAsyncCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility,
    Func<EventInfo, object?, Delegate?>? eventHandlerResolver, IList<Type?>? typeArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType,
    TaskFactory taskFactory)
  {
    var eventInfo = MatchEvent(sourceType, source is null, name, memberAccessibility, typeArguments?.AsReadOnly(),
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)), returnType, false);
    if (eventInfo is not null)
    {
      var methodInfo = eventInfo.EventHandlerType?.GetMethod(Invoke);
      if (methodInfo is not null)
      {
        var eventHandler = default(Delegate);
        if (eventHandlerResolver is not null)
        {
          eventHandler = eventHandlerResolver(eventInfo, source);
        }
        else
        {
          var fieldInfo = MatchField(eventInfo.DeclaringType!, source is null, eventInfo.Name, MemberAccessibility.DeclaredOnly | MemberAccessibility.Private, false, null, eventInfo.EventHandlerType, TypeVariance.Invariant);
          if (fieldInfo is null)
          {
            return TryOut<object>.Failed;
          }
          eventHandler = (Delegate?)fieldInfo.GetValue(source);
        }
        if (eventHandler is not null)
        {
          var parameters = methodInfo.GetParameters();
          var hasReturned = parameters.ContainsReturnedParameters();
          var values = parameters.GetParameterValues(positionalParameterValues, namedParameterValues);
          var result = await InvokeAsync(eventHandler, values, hasReturned, taskFactory);
          parameters.SetParameterValues(values, positionalParameterValues, namedParameterValues);
          eventInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
          return TryOut.Success(result);
        }
        return TryOut.Success(default(object));
      }
    }
    return TryOut<object>.Failed;
  }

  private static async Task<object?> RaiseEventAsyncCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility,
    Func<EventInfo, object?, Delegate?>? eventHandlerResolver, IList<Type?>? typeArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType,
    TaskFactory taskFactory)
  {
    var eventInfo = MatchEvent(sourceType, source is null, name, memberAccessibility, typeArguments?.AsReadOnly(),
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)), returnType, false);
    Operation.That.IsValid(eventInfo is not null, FormatMessage(ReflectionMessage.EventNotFound));
    Operation.That.IsValid(eventInfo.EventHandlerType is not null && eventInfo.RemoveMethod is not null);
    var methodInfo = eventInfo.EventHandlerType.GetMethod(Invoke);
    Operation.That.IsValid(methodInfo is not null, FormatMessage(ReflectionMessage.MethodNotFound));
    var eventHandler = default(Delegate?);
    if (eventHandlerResolver is not null)
    {
      eventHandler = eventHandlerResolver(eventInfo, source);
    }
    else
    {
      var fieldInfo = MatchField(eventInfo.DeclaringType!, source is null, eventInfo.Name, MemberAccessibility.DeclaredOnly | MemberAccessibility.Private, false, null, eventInfo.EventHandlerType, TypeVariance.Invariant);
      Operation.That.IsValid(fieldInfo is not null, FormatMessage(ReflectionMessage.FieldNotFound));
      eventHandler = (Delegate?)fieldInfo.GetValue(source);
    }
    if (eventHandler is not null)
    {
      var parameters = methodInfo.GetParameters();
      var hasReturned = parameters.ContainsReturnedParameters();
      var values = parameters.GetParameterValues(positionalParameterValues, namedParameterValues);
      var result = await InvokeAsync(eventHandler, values, hasReturned, taskFactory);
      parameters.SetParameterValues(values, positionalParameterValues, namedParameterValues);
      return result;
    }
    return null;
  }

  private static async Task<TryOut<object>> TryRaiseEventAsyncCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility,
    Func<EventInfo, object?, Delegate?>? eventHandlerResolver, IList<Type?>? typeArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    var eventInfo = MatchEvent(sourceType, source is null, name, memberAccessibility, typeArguments?.AsReadOnly(),
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)), returnType, true);
    if (eventInfo is not null)
    {
      var methodInfo = eventInfo.EventHandlerType?.GetMethod(Invoke);
      if (methodInfo is not null)
      {
        var eventHandler = default(Delegate);
        if (eventHandlerResolver is not null)
        {
          eventHandler = eventHandlerResolver(eventInfo, source);
        }
        else
        {
          var fieldInfo = MatchField(eventInfo.DeclaringType!, source is null, eventInfo.Name, MemberAccessibility.DeclaredOnly | MemberAccessibility.Private, false, null, eventInfo.EventHandlerType, TypeVariance.Invariant);
          if (fieldInfo is null)
          {
            return TryOut<object>.Failed;
          }
          eventHandler = (Delegate?)fieldInfo.GetValue(source);
        }
        if (eventHandler is not null)
        {
          var parameters = methodInfo.GetParameters();
          var hasReturned = parameters.ContainsReturnedParameters();
          var values = parameters.GetParameterValues(positionalParameterValues, namedParameterValues);
          var result = await InvokeAwaitableDelegate(eventHandler, values, hasReturned);
          parameters.SetParameterValues(values, positionalParameterValues, namedParameterValues);
          eventInfo.ReflectedType?.GetGenericArguments().SetArgumentTypes(typeArguments);
          return TryOut.Success(result);
        }
        return TryOut.Success(default(object));
      }
    }
    return TryOut<object>.Failed;
  }

  private static async Task<object?> RaiseEventAsyncCore(Type sourceType, object? source, string name, MemberAccessibility memberAccessibility,
    Func<EventInfo, object?, Delegate?>? eventHandlerResolver, IList<Type?>? typeArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    var eventInfo = MatchEvent(sourceType, source is null, name, memberAccessibility, typeArguments?.AsReadOnly(),
      positionalParameterValues?.GetTypesOfValues(), namedParameterValues?.GetTypesOfValues(memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase)), returnType, true);
    Operation.That.IsValid(eventInfo is not null, FormatMessage(ReflectionMessage.EventNotFound));
    Operation.That.IsValid(eventInfo.EventHandlerType is not null && eventInfo.RemoveMethod is not null);
    var methodInfo = eventInfo.EventHandlerType.GetMethod(Invoke);
    Operation.That.IsValid(methodInfo is not null, FormatMessage(ReflectionMessage.MethodNotFound));
    var eventHandler = default(Delegate?);
    if (eventHandlerResolver is not null)
    {
      eventHandler = eventHandlerResolver(eventInfo, source);
    }
    else
    {
      var fieldInfo = MatchField(eventInfo.DeclaringType!, source is null, eventInfo.Name, MemberAccessibility.DeclaredOnly | MemberAccessibility.Private, false, null, eventInfo.EventHandlerType, TypeVariance.Invariant);
      Operation.That.IsValid(fieldInfo is not null, FormatMessage(ReflectionMessage.FieldNotFound));
      eventHandler = (Delegate?)fieldInfo.GetValue(source);
    }
    if (eventHandler is not null)
    {
      var parameters = methodInfo.GetParameters();
      var hasReturned = parameters.ContainsReturnedParameters();
      var values = parameters.GetParameterValues(positionalParameterValues, namedParameterValues);
      var result = await InvokeAwaitableDelegate(eventHandler, values, hasReturned);
      parameters.SetParameterValues(values, positionalParameterValues, namedParameterValues);
      return result;
    }
    return null;
  }

  #endregion
  #region Instance event public methods
  #region Try info methods

  public static bool TryGetEvent(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes,
    [NotNullWhen(true)] out EventInfo? eventInfo)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryGetEventCore(sourceType, sourceObject, name, memberAccessibility, null, positionalParameterTypes, namedParameterTypes, typeof(void), out eventInfo);
  }

  public static bool TryGetEvent(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType,
    [NotNullWhen(true)] out EventInfo? eventInfo)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryGetEventCore(sourceType, sourceObject, name, memberAccessibility, null, positionalParameterTypes, namedParameterTypes, returnType, out eventInfo);
  }

  public static bool TryGetEvent<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes,
    [NotNullWhen(true)] out EventInfo? eventInfo)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetEventCore(typeof(TSource), source, name, memberAccessibility, null, positionalParameterTypes, namedParameterTypes, typeof(void), out eventInfo);
  }

  public static bool TryGetEvent<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType,
    [NotNullWhen(true)] out EventInfo? eventInfo)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetEventCore(typeof(TSource), source, name, memberAccessibility, null, positionalParameterTypes, namedParameterTypes, returnType, out eventInfo);
  }

  #endregion
  #region Direct info methods

  public static EventInfo GetEvent(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return GetEventCore(sourceType, sourceObject, name, memberAccessibility, null, positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static EventInfo GetEvent(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return GetEventCore(sourceType, sourceObject, name, memberAccessibility, null, positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static EventInfo GetEvent<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return GetEventCore(typeof(TSource), source, name, memberAccessibility, null, positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static EventInfo GetEvent<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return GetEventCore(typeof(TSource), source, name, memberAccessibility, null, positionalParameterTypes, namedParameterTypes, returnType);
  }

  #endregion
  #region Try add handler methods

  public static bool TryAddEventHandler(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    return TryAddEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryAddEventHandler(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    return TryAddEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryAddEventHandler(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryAddEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryAddEventHandler(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryAddEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryAddEventHandler<TMethodSource>(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryAddEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryAddEventHandler<TMethodSource>(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryAddEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryAddEventHandler<TMethodSource>(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNullOrWhitespace(methodName);

    return TryAddEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryAddEventHandler<TMethodSource>(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNullOrWhitespace(methodName);

    return TryAddEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryAddEventHandler<TEventSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    return TryAddEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryAddEventHandler<TEventSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    return TryAddEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryAddEventHandler<TEventSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryAddEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryAddEventHandler<TEventSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryAddEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryAddEventHandler<TEventSource, TMethodSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryAddEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryAddEventHandler<TEventSource, TMethodSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryAddEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryAddEventHandler<TEventSource, TMethodSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNullOrWhitespace(methodName);

    return TryAddEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryAddEventHandler<TEventSource, TMethodSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNullOrWhitespace(methodName);

    return TryAddEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  #endregion
  #region Direct add handler methods

  public static void AddEventHandler(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    AddEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void AddEventHandler(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    AddEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void AddEventHandler(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    AddEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void AddEventHandler(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    AddEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void AddEventHandler<TMethodSource>(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    AddEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void AddEventHandler<TMethodSource>(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    AddEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void AddEventHandler<TMethodSource>(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNullOrWhitespace(methodName);

    AddEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void AddEventHandler<TMethodSource>(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNullOrWhitespace(methodName);

    AddEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void AddEventHandler<TEventSource>(
  TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
  object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
  IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    AddEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void AddEventHandler<TEventSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    AddEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void AddEventHandler<TEventSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    AddEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void AddEventHandler<TEventSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    AddEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void AddEventHandler<TEventSource, TMethodSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    AddEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void AddEventHandler<TEventSource, TMethodSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    AddEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void AddEventHandler<TEventSource, TMethodSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNullOrWhitespace(methodName);

    AddEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void AddEventHandler<TEventSource, TMethodSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNullOrWhitespace(methodName);

    AddEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  #endregion
  #region Try remove handler methods

  public static bool TryRemoveEventHandler(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    return TryRemoveEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryRemoveEventHandler(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    return TryRemoveEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryRemoveEventHandler(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryRemoveEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryRemoveEventHandler(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryRemoveEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryRemoveEventHandler<TMethodSource>(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryRemoveEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryRemoveEventHandler<TMethodSource>(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryRemoveEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryRemoveEventHandler<TMethodSource>(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNullOrWhitespace(methodName);

    return TryRemoveEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryRemoveEventHandler<TMethodSource>(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNullOrWhitespace(methodName);

    return TryRemoveEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryRemoveEventHandler<TEventSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    return TryRemoveEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryRemoveEventHandler<TEventSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    return TryRemoveEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryRemoveEventHandler<TEventSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryRemoveEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryRemoveEventHandler<TEventSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryRemoveEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryRemoveEventHandler<TEventSource, TMethodSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryRemoveEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryRemoveEventHandler<TEventSource, TMethodSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryRemoveEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryRemoveEventHandler<TEventSource, TMethodSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNullOrWhitespace(methodName);

    return TryRemoveEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryRemoveEventHandler<TEventSource, TMethodSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNullOrWhitespace(methodName);

    return TryRemoveEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  #endregion
  #region Direct remove handler methods

  public static void RemoveEventHandler(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    RemoveEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void RemoveEventHandler(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    RemoveEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void RemoveEventHandler(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    RemoveEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void RemoveEventHandler(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    RemoveEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void RemoveEventHandler<TMethodSource>(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    RemoveEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void RemoveEventHandler<TMethodSource>(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    RemoveEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void RemoveEventHandler<TMethodSource>(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNullOrWhitespace(methodName);

    RemoveEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void RemoveEventHandler<TMethodSource>(
    object eventSource, string eventName, MemberAccessibility eventAccessibility,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);
    var eventObject = TypedValue.GetValue(eventSource);
    Argument.That.NotNull(eventObject);
    var eventType = TypedValue.GetType(eventSource)!;

    Argument.That.NotNullOrWhitespace(methodName);

    RemoveEventHandlerCore(
      eventType, eventObject, eventName, eventAccessibility, null,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void RemoveEventHandler<TEventSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    RemoveEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void RemoveEventHandler<TEventSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    RemoveEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void RemoveEventHandler<TEventSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    RemoveEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void RemoveEventHandler<TEventSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    RemoveEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void RemoveEventHandler<TEventSource, TMethodSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    RemoveEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void RemoveEventHandler<TEventSource, TMethodSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    RemoveEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void RemoveEventHandler<TEventSource, TMethodSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNullOrWhitespace(methodName);

    RemoveEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void RemoveEventHandler<TEventSource, TMethodSource>(
    TEventSource eventSource, string eventName, MemberAccessibility eventAccessibility,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventSource);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNullOrWhitespace(methodName);

    RemoveEventHandlerCore(
      typeof(TEventSource), eventSource, eventName, eventAccessibility, null,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  #endregion
  #region Try clear event handlers methods

  public static bool TryClearInstantEventHandlers(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes,
    Func<EventInfo, object, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryClearEventHandlersCore(sourceType, sourceObject, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, source!),
      null, positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryClearInstantEventHandlers(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType,
    Func<EventInfo, object, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryClearEventHandlersCore(sourceType, sourceObject, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, source!),
      null, positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryClearInstantEventHandlers<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes,
    Func<EventInfo, TSource, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryClearEventHandlersCore(typeof(TSource), source, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, (TSource)source!),
      null, positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryClearInstantEventHandlers<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType,
    Func<EventInfo, TSource, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryClearEventHandlersCore(typeof(TSource), source, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, (TSource)source!),
      null, positionalParameterTypes, namedParameterTypes, returnType);
  }

  #endregion
  #region Direct clear event handlers methods

  public static void ClearEventHandlers(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes,
    Func<EventInfo, object, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    ClearEventHandlersCore(sourceType, sourceObject, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, source!),
      null, positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void ClearEventHandlers(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType,
    Func<EventInfo, object, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    ClearEventHandlersCore(sourceType, sourceObject, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, source!),
      null, positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void ClearEventHandlers<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes,
    Func<EventInfo, TSource, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    ClearEventHandlersCore(typeof(TSource), source, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, (TSource)source!),
      null, positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void ClearEventHandlers<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType,
    Func<EventInfo, TSource, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    ClearEventHandlersCore(typeof(TSource), null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, (TSource)source!),
      null, positionalParameterTypes, namedParameterTypes, returnType);
  }

  #endregion
  #region Try raise event handler methods

  public static bool TryRaiseEvent(object source, string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues,
    Func<EventInfo, object, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryRaiseEventCore(sourceType, sourceObject, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, source!),
      null, positionalParameterValues, namedParameterValues, typeof(void), out var returnValue);
  }

  public static bool TryRaiseEvent(object source, string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType, out object? returnValue,
    Func<EventInfo, object, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryRaiseEventCore(sourceType, sourceObject, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, source!),
      null, positionalParameterValues, namedParameterValues, returnType, out returnValue);
  }

  public static bool TryRaiseEvent<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues,
    Func<EventInfo, TSource, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryRaiseEventCore(typeof(TSource), source, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, (TSource)source!),
      null, positionalParameterValues, namedParameterValues, typeof(void), out var returnValue);
  }

  public static bool TryRaiseEvent<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType, out object? returnValue,
    Func<EventInfo, TSource, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryRaiseEventCore(typeof(TSource), source, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, (TSource)source!),
      null, positionalParameterValues, namedParameterValues, returnType, out returnValue);
  }

  #endregion
  #region Direct raise event handler methods

  public static void RaiseEvent(object source, string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues,
    Func<EventInfo, object, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    RaiseEventCore(sourceType, sourceObject, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, source!),
      null, positionalParameterValues, namedParameterValues, typeof(void));
  }

  public static object? RaiseEvent(object source, string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType,
    Func<EventInfo, object, Delegate?>? eventHandlerResolver)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return RaiseEventCore(sourceType, sourceObject, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, source!),
      null, positionalParameterValues, namedParameterValues, returnType);
  }

  public static void RaiseEvent<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues,
    Func<EventInfo, TSource, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    RaiseEventCore(typeof(TSource), source, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, (TSource)source!),
      null, positionalParameterValues, namedParameterValues, typeof(void));
  }

  public static object? RaiseEvent<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType,
    Func<EventInfo, TSource, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return RaiseEventCore(typeof(TSource), source, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, (TSource)source!),
      null, positionalParameterValues, namedParameterValues, returnType);
  }

  #endregion
  #region Try raise event sync handler async methods

  public static async Task<bool> TryRaiseEventAsync(object source, string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues,
    TaskFactory? taskFactory, Func<EventInfo, object, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var result = await TryRaiseEventAsyncCore(sourceType, sourceObject, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, source!),
      null, positionalParameterValues, namedParameterValues, typeof(void), taskFactory ?? Task.Factory);
    return result.Success;
  }

  public static Task<TryOut<object>> TryRaiseEventAsync(object source, string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType,
    TaskFactory? taskFactory, Func<EventInfo, object, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryRaiseEventAsyncCore(sourceType, sourceObject, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, source!),
      null, positionalParameterValues, namedParameterValues, returnType, taskFactory ?? Task.Factory);
  }

  public static async Task<bool> TryRaiseEventAsync<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues,
    TaskFactory? taskFactory, Func<EventInfo, TSource, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var result = await TryRaiseEventAsyncCore(typeof(TSource), source, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, (TSource)source!),
      null, positionalParameterValues, namedParameterValues, typeof(void), taskFactory ?? Task.Factory);
    return result.Success;
  }

  public static Task<TryOut<object>> TryRaiseEventAsync<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType,
    TaskFactory? taskFactory, Func<EventInfo, TSource, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryRaiseEventAsyncCore(typeof(TSource), source, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, (TSource)source!),
      null, positionalParameterValues, namedParameterValues, returnType, taskFactory ?? Task.Factory);
  }

  #endregion
  #region Direct raise event sync handler async methods

  public static Task RaiseEventAsync(object source, string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues,
    TaskFactory? taskFactory, Func<EventInfo, object, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return RaiseEventAsyncCore(sourceType, sourceObject, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, source!),
      null, positionalParameterValues, namedParameterValues, typeof(void), taskFactory ?? Task.Factory);
  }

  public static Task<object?> RaiseEventAsync(object source, string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType,
    TaskFactory? taskFactory, Func<EventInfo, object, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return RaiseEventAsyncCore(sourceType, sourceObject, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, source!),
      null, positionalParameterValues, namedParameterValues, returnType, taskFactory ?? Task.Factory);
  }

  public static Task RaiseEventAsync<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues,
    TaskFactory? taskFactory, Func<EventInfo, TSource, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return RaiseEventAsyncCore(typeof(TSource), source, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, (TSource)source!),
      null, positionalParameterValues, namedParameterValues, typeof(void), taskFactory ?? Task.Factory);
  }

  public static Task<object?> RaiseEventAsync<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType,
    TaskFactory? taskFactory, Func<EventInfo, TSource, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return RaiseEventAsyncCore(typeof(TSource), source, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, (TSource)source!),
      null, positionalParameterValues, namedParameterValues, returnType, taskFactory ?? Task.Factory);
  }

  #endregion
  #region Try raise event handler async methods

  public static async Task<bool> TryRaiseEventAsync(object source, string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues,
    Func<EventInfo, object, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var result = await TryRaiseEventAsyncCore(sourceType, sourceObject, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, source!),
      null, positionalParameterValues, namedParameterValues, typeof(void));
    return result.Success;
  }

  public static Task<TryOut<object>> TryRaiseEventAsync(object source, string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType,
    Func<EventInfo, object, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return TryRaiseEventAsyncCore(sourceType, sourceObject, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, source!),
      null, positionalParameterValues, namedParameterValues, returnType);
  }

  public static async Task<bool> TryRaiseEventAsync<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues,
    Func<EventInfo, TSource, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var result = await TryRaiseEventAsyncCore(typeof(TSource), source, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, (TSource)source!),
      null, positionalParameterValues, namedParameterValues, typeof(void));
    return result.Success;
  }

  public static Task<TryOut<object>> TryRaiseEventAsync<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType,
    Func<EventInfo, TSource, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return TryRaiseEventAsyncCore(typeof(TSource), source, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, (TSource)source!),
      null, positionalParameterValues, namedParameterValues, returnType);
  }

  #endregion
  #region Direct raise event handler async methods

  public static async Task RaiseEventAsync(object source, string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues,
    Func<EventInfo, object, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    await RaiseEventAsyncCore(sourceType, sourceObject, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, source!),
      null, positionalParameterValues, namedParameterValues, typeof(void));
    return;
  }

  public static Task<object?> RaiseEventAsync(object source, string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType,
    Func<EventInfo, object, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    return RaiseEventAsyncCore(sourceType, sourceObject, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, source!),
      null, positionalParameterValues, namedParameterValues, returnType);
  }

  public static async Task RaiseEventAsync<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues,
    Func<EventInfo, TSource, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    await RaiseEventAsyncCore(typeof(TSource), source, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, (TSource)source!),
      null, positionalParameterValues, namedParameterValues, typeof(void));
    return;
  }

  public static Task<object?> RaiseEventAsync<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType,
    Func<EventInfo, TSource, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    return RaiseEventAsyncCore(typeof(TSource), source, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo, (TSource)source!),
      null, positionalParameterValues, namedParameterValues, returnType);
  }

  #endregion
  #endregion
  #region Static event public methods
  #region Try info methods

  public static bool TryGetEvent(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes,
    [NotNullWhen(true)] out EventInfo? eventInfo)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetEventCore(sourceType, null, name, memberAccessibility, typeArguments, positionalParameterTypes, namedParameterTypes, typeof(void), out eventInfo);
  }

  public static bool TryGetEvent(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType,
    [NotNullWhen(true)] out EventInfo? eventInfo)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryGetEventCore(sourceType, null, name, memberAccessibility, typeArguments, positionalParameterTypes, namedParameterTypes, returnType, out eventInfo);
  }

  public static bool TryGetEvent<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes,
    [NotNullWhen(true)] out EventInfo? eventInfo)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryGetEventCore(typeof(TSource), null, name, memberAccessibility, null, positionalParameterTypes, namedParameterTypes, typeof(void), out eventInfo);
  }

  public static bool TryGetEvent<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType,
    [NotNullWhen(true)] out EventInfo? eventInfo)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryGetEventCore(typeof(TSource), null, name, memberAccessibility, null, positionalParameterTypes, namedParameterTypes, returnType, out eventInfo);
  }

  #endregion
  #region Direct info methods

  public static EventInfo GetEvent(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return GetEventCore(sourceType, null, name, memberAccessibility, typeArguments, positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static EventInfo GetEvent(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return GetEventCore(sourceType, null, name, memberAccessibility, typeArguments, positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static EventInfo GetEvent<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNullOrWhitespace(name);

    return GetEventCore(typeof(TSource), null, name, memberAccessibility, null, positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static EventInfo GetEvent<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNullOrWhitespace(name);

    return GetEventCore(typeof(TSource), null, name, memberAccessibility, null, positionalParameterTypes, namedParameterTypes, returnType);
  }

  #endregion
  #region Try add handler methods

  public static bool TryAddEventHandler(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    return TryAddEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryAddEventHandler(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    return TryAddEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryAddEventHandler(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryAddEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryAddEventHandler(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryAddEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryAddEventHandler<TMethodSource>(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryAddEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryAddEventHandler<TMethodSource>(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryAddEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryAddEventHandler<TMethodSource>(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNullOrWhitespace(methodName);

    return TryAddEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryAddEventHandler<TMethodSource>(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNullOrWhitespace(methodName);

    return TryAddEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryAddEventHandler<TEventSource>(
    string eventName, MemberAccessibility eventAccessibility,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    return TryAddEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryAddEventHandler<TEventSource>(
    string eventName, MemberAccessibility eventAccessibility,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    return TryAddEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryAddEventHandler<TEventSource>(
    string eventName, MemberAccessibility eventAccessibility,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryAddEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryAddEventHandler<TEventSource>(
    string eventName, MemberAccessibility eventAccessibility,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryAddEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryAddEventHandler<TEventSource, TMethodSource>(
    string eventName, MemberAccessibility eventAccessibility,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryAddEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryAddEventHandler<TEventSource, TMethodSource>(
    string eventName, MemberAccessibility eventAccessibility,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryAddEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryAddEventHandler<TEventSource, TMethodSource>(
    string eventName, MemberAccessibility eventAccessibility, 
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNullOrWhitespace(methodName);

    return TryAddEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryAddEventHandler<TEventSource, TMethodSource>(
    string eventName, MemberAccessibility eventAccessibility,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNullOrWhitespace(methodName);

    return TryAddEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  #endregion
  #region Direct add handler methods

  public static void AddEventHandler(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    AddEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void AddEventHandler(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    AddEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void AddEventHandler(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    AddEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void AddEventHandler(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    AddEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void AddEventHandler<TMethodSource>(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    AddEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void AddEventHandler<TMethodSource>(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    AddEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void AddEventHandler<TMethodSource>(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNullOrWhitespace(methodName);

    AddEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void AddEventHandler<TMethodSource>(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNullOrWhitespace(methodName);

    AddEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void AddEventHandler<TEventSource>(
    string eventName, MemberAccessibility eventAccessibility,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    AddEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void AddEventHandler<TEventSource>(
    string eventName, MemberAccessibility eventAccessibility,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    AddEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void AddEventHandler<TEventSource>(
    string eventName, MemberAccessibility eventAccessibility,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    AddEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void AddEventHandler<TEventSource>(
    string eventName, MemberAccessibility eventAccessibility,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    AddEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void AddEventHandler<TEventSource, TMethodSource>(
    string eventName, MemberAccessibility eventAccessibility,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    AddEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void AddEventHandler<TEventSource, TMethodSource>(
    string eventName, MemberAccessibility eventAccessibility,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    AddEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void AddEventHandler<TEventSource, TMethodSource>(
    string eventName, MemberAccessibility eventAccessibility,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNullOrWhitespace(methodName);

    AddEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void AddEventHandler<TEventSource, TMethodSource>(
    string eventName, MemberAccessibility eventAccessibility,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNullOrWhitespace(methodName);

    AddEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  #endregion
  #region Try remove handler methods

  public static bool TryRemoveEventHandler(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    return TryRemoveEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryRemoveEventHandler(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    return TryRemoveEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryRemoveEventHandler(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryRemoveEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryRemoveEventHandler(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryRemoveEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryRemoveEventHandler<TMethodSource>(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryRemoveEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryRemoveEventHandler<TMethodSource>(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryRemoveEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryRemoveEventHandler<TMethodSource>(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNullOrWhitespace(methodName);

    return TryRemoveEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryRemoveEventHandler<TMethodSource>(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNullOrWhitespace(methodName);

    return TryRemoveEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryRemoveEventHandler<TEventSource>(
    string eventName, MemberAccessibility eventAccessibility,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    return TryRemoveEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryRemoveEventHandler<TEventSource>(
    string eventName, MemberAccessibility eventAccessibility,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    return TryRemoveEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryRemoveEventHandler<TEventSource>(
    string eventName, MemberAccessibility eventAccessibility,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryRemoveEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryRemoveEventHandler<TEventSource>(
    string eventName, MemberAccessibility eventAccessibility,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryRemoveEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryRemoveEventHandler<TEventSource, TMethodSource>(
    string eventName, MemberAccessibility eventAccessibility,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryRemoveEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryRemoveEventHandler<TEventSource, TMethodSource>(
    string eventName, MemberAccessibility eventAccessibility,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    return TryRemoveEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryRemoveEventHandler<TEventSource, TMethodSource>(
    string eventName, MemberAccessibility eventAccessibility,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNullOrWhitespace(methodName);

    return TryRemoveEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryRemoveEventHandler<TEventSource, TMethodSource>(
    string eventName, MemberAccessibility eventAccessibility,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNullOrWhitespace(methodName);

    return TryRemoveEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  #endregion
  #region Direct remove handler methods

  public static void RemoveEventHandler(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    RemoveEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void RemoveEventHandler(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    RemoveEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void RemoveEventHandler(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    RemoveEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void RemoveEventHandler(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    RemoveEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void RemoveEventHandler<TMethodSource>(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    RemoveEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void RemoveEventHandler<TMethodSource>(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    RemoveEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void RemoveEventHandler<TMethodSource>(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNullOrWhitespace(methodName);

    RemoveEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void RemoveEventHandler<TMethodSource>(
    Type eventType, string eventName, MemberAccessibility eventAccessibility, IList<Type?>? eventTypeArguments,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNull(eventType);
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNullOrWhitespace(methodName);

    RemoveEventHandlerCore(
      eventType, null, eventName, eventAccessibility, eventTypeArguments,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void RemoveEventHandler<TEventSource>(
    string eventName, MemberAccessibility eventAccessibility,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    RemoveEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void RemoveEventHandler<TEventSource>(
    string eventName, MemberAccessibility eventAccessibility,
    object methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);
    var methodObject = TypedValue.GetValue(methodSource);
    Argument.That.NotNull(methodObject);
    var methodType = TypedValue.GetType(methodSource)!;

    RemoveEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      methodType, methodObject, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void RemoveEventHandler<TEventSource>(
    string eventName, MemberAccessibility eventAccessibility,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    RemoveEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void RemoveEventHandler<TEventSource>(
    string eventName, MemberAccessibility eventAccessibility,
    Type methodType, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodTypeArguments, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodType);
    Argument.That.NotNullOrWhitespace(methodName);

    RemoveEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      methodType, null, methodName, methodAccessibility, methodTypeArguments, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void RemoveEventHandler<TEventSource, TMethodSource>(
    string eventName, MemberAccessibility eventAccessibility,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    RemoveEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void RemoveEventHandler<TEventSource, TMethodSource>(
    string eventName, MemberAccessibility eventAccessibility,
    TMethodSource methodSource, string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNull(methodSource);
    Argument.That.NotNullOrWhitespace(methodName);

    RemoveEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      typeof(TMethodSource), methodSource, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void RemoveEventHandler<TEventSource, TMethodSource>(
    string eventName, MemberAccessibility eventAccessibility,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNullOrWhitespace(methodName);

    RemoveEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void RemoveEventHandler<TEventSource, TMethodSource>(
    string eventName, MemberAccessibility eventAccessibility,
    string methodName, MemberAccessibility methodAccessibility, IList<Type?>? methodMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType)
  {
    Argument.That.NotNullOrWhitespace(eventName);

    Argument.That.NotNullOrWhitespace(methodName);

    RemoveEventHandlerCore(
      typeof(TEventSource), null, eventName, eventAccessibility, null,
      typeof(TMethodSource), null, methodName, methodAccessibility, null, methodMethodArguments,
      positionalParameterTypes, namedParameterTypes, returnType);
  }

  #endregion
  #region Try clear event handlers methods

  public static bool TryClearEventHandlers(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes,
    Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryClearEventHandlersCore(sourceType, null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      typeArguments, positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryClearEventHandlers(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType,
    Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryClearEventHandlersCore(sourceType, null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      typeArguments, positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static bool TryClearEventHandlers<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes,
    Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryClearEventHandlersCore(typeof(TSource), null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      null, positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static bool TryClearEventHandlers<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType,
    Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryClearEventHandlersCore(typeof(TSource), null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      null, positionalParameterTypes, namedParameterTypes, returnType);
  }

  #endregion
  #region Direct clear event handlers methods

  public static void ClearEventHandlers(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes,
    Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    ClearEventHandlersCore(sourceType, null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      typeArguments, positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void ClearEventHandlers(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType,
    Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    ClearEventHandlersCore(sourceType, null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      typeArguments, positionalParameterTypes, namedParameterTypes, returnType);
  }

  public static void ClearEventHandlers<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes,
    Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNullOrWhitespace(name);

    ClearEventHandlersCore(typeof(TSource), null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      null, positionalParameterTypes, namedParameterTypes, typeof(void));
  }

  public static void ClearEventHandlers<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType,
    Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNullOrWhitespace(name);

    ClearEventHandlersCore(typeof(TSource), null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      null, positionalParameterTypes, namedParameterTypes, returnType);
  }

  #endregion
  #region Try raise event handler methods

  public static bool TryRaiseEvent(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues,
    Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryRaiseEventCore(sourceType, null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      typeArguments, positionalParameterValues, namedParameterValues, typeof(void), out var returnValue);
  }

  public static bool TryRaiseEvent(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType, out object? returnValue,
    Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryRaiseEventCore(sourceType, null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      typeArguments, positionalParameterValues, namedParameterValues, returnType, out returnValue);
  }

  public static bool TryRaiseEvent<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues,
    Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryRaiseEventCore(typeof(TSource), null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      null, positionalParameterValues, namedParameterValues, typeof(void), out var returnValue);
  }

  public static bool TryRaiseEvent<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType, out object? returnValue,
    Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryRaiseEventCore(typeof(TSource), null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      null, positionalParameterValues, namedParameterValues, returnType, out returnValue);
  }

  #endregion
  #region Direct raise event handler methods

  public static void RaiseEvent(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues,
    Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    RaiseEventCore(sourceType, null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      typeArguments, positionalParameterValues, namedParameterValues, typeof(void));
  }

  public static object? RaiseEvent(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType,
    Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return RaiseEventCore(sourceType, null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      typeArguments, positionalParameterValues, namedParameterValues, returnType);
  }

  public static void RaiseEvent<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues,
    Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNullOrWhitespace(name);

    RaiseEventCore(typeof(TSource), null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      null, positionalParameterValues, namedParameterValues, typeof(void));
  }

  public static object? RaiseEvent<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType,
    Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNullOrWhitespace(name);

    return RaiseEventCore(typeof(TSource), null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      null, positionalParameterValues, namedParameterValues, returnType);
  }

  #endregion
  #region Try raise event sync handler async methods

  public static async Task<bool> TryRaiseEventAsync(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues,
    TaskFactory? taskFactory, Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    var result = await TryRaiseEventAsyncCore(sourceType, null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      typeArguments, positionalParameterValues, namedParameterValues, typeof(void),
      taskFactory ?? Task.Factory);
    return result.Success;
  }

  public static Task<TryOut<object>> TryRaiseEventAsync(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType,
    TaskFactory? taskFactory, Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryRaiseEventAsyncCore(sourceType, null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      typeArguments, positionalParameterValues, namedParameterValues, returnType,
      taskFactory ?? Task.Factory);
  }

  public static async Task<bool> TryRaiseEventAsync<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues,
    TaskFactory? taskFactory, Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNullOrWhitespace(name);

    var result = await TryRaiseEventAsyncCore(typeof(TSource), null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      null, positionalParameterValues, namedParameterValues, typeof(void),
      taskFactory ?? Task.Factory);
    return result.Success;
  }

  public static Task<TryOut<object>> TryRaiseEventAsync<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType,
    TaskFactory? taskFactory, Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryRaiseEventAsyncCore(typeof(TSource), null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      null, positionalParameterValues, namedParameterValues, returnType,
      taskFactory ?? Task.Factory);
  }

  #endregion
  #region Direct raise event sync handler async methods

  public static Task RaiseEventAsync(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues,
    TaskFactory? taskFactory, Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return RaiseEventAsyncCore(sourceType, null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      typeArguments, positionalParameterValues, namedParameterValues, typeof(void),
      taskFactory ?? Task.Factory);
  }

  public static Task<object?> RaiseEventAsync(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType,
    TaskFactory? taskFactory, Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return RaiseEventAsyncCore(sourceType, null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      typeArguments, positionalParameterValues, namedParameterValues, returnType,
      taskFactory ?? Task.Factory);
  }

  public static Task RaiseEventAsync<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues,
    TaskFactory? taskFactory, Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNullOrWhitespace(name);

    return RaiseEventAsyncCore(typeof(TSource), null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      null, positionalParameterValues, namedParameterValues, typeof(void),
      taskFactory ?? Task.Factory);
  }

  public static Task<object?> RaiseEventAsync<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType,
    TaskFactory? taskFactory, Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNullOrWhitespace(name);

    return RaiseEventAsyncCore(typeof(TSource), null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      null, positionalParameterValues, namedParameterValues, returnType,
      taskFactory ?? Task.Factory);
  }

  #endregion
  #region Try raise event handler async methods

  public static async Task<bool> TryRaiseEventAsync(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues,
    Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    var result = await TryRaiseEventAsyncCore(sourceType, null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      typeArguments, positionalParameterValues, namedParameterValues, typeof(void));
    return result.Success;
  }

  public static Task<TryOut<object>> TryRaiseEventAsync(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType,
    Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return TryRaiseEventAsyncCore(sourceType, null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      typeArguments, positionalParameterValues, namedParameterValues, returnType);
  }

  public static async Task<bool> TryRaiseEventAsync<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues,
    Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNullOrWhitespace(name);

    var result = await TryRaiseEventAsyncCore(typeof(TSource), null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      null, positionalParameterValues, namedParameterValues, typeof(void));
    return result.Success;
  }

  public static Task<TryOut<object>> TryRaiseEventAsync<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType,
    Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNullOrWhitespace(name);

    return TryRaiseEventAsyncCore(typeof(TSource), null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      null, positionalParameterValues, namedParameterValues, returnType);
  }

  #endregion
  #region Direct raise event handler async methods

  public static Task RaiseEventAsync(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues,
    Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return RaiseEventAsyncCore(sourceType, null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      typeArguments, positionalParameterValues, namedParameterValues, typeof(void));
  }

  public static Task<object?> RaiseEventAsync(Type sourceType, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType,
    Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNull(sourceType);
    Argument.That.NotNullOrWhitespace(name);

    return RaiseEventAsyncCore(sourceType, null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      typeArguments, positionalParameterValues, namedParameterValues, returnType);
  }

  public static Task RaiseEventAsync<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues,
    Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNullOrWhitespace(name);

    return RaiseEventAsyncCore(typeof(TSource), null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      null, positionalParameterValues, namedParameterValues, typeof(void));
  }

  public static Task<object?> RaiseEventAsync<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType,
    Func<EventInfo, Delegate?>? eventHandlerResolver = null)
  {
    Argument.That.NotNullOrWhitespace(name);

    return RaiseEventAsyncCore(typeof(TSource), null, name, memberAccessibility,
      eventHandlerResolver is null ? null : (eventInfo, source) => eventHandlerResolver(eventInfo),
      null, positionalParameterValues, namedParameterValues, returnType);
  }

  #endregion
  #endregion
  #region Custom attributes

  public static IEnumerable<Attribute> GetCustomAttributes(ICustomAttributeProvider provider, Type type, bool inherit = true)
    => Argument.That.NotNull(provider).GetCustomAttributes(Argument.That.NotNull(type), inherit).Cast<Attribute>();

  public static Attribute? GetCustomAttribute(ICustomAttributeProvider provider, Type type, bool inherit = true)
    => Argument.That.NotNull(provider).GetCustomAttributes(type, inherit).Cast<Attribute>().FirstOrDefault();

  public static IEnumerable<T> GetCustomAttributes<T>(ICustomAttributeProvider provider, bool inherit = true)
    where T : Attribute
    => Argument.That.NotNull(provider).GetCustomAttributes(typeof(T), inherit).Cast<T>();

  public static T? GetCustomAttribute<T>(ICustomAttributeProvider provider, bool inherit = true)
    where T : Attribute
    => Argument.That.NotNull(provider).GetCustomAttributes(typeof(T), inherit).Cast<T>().FirstOrDefault();

  public static bool IsAttributeDefined<T>(ICustomAttributeProvider provider, bool inherit = true)
    where T : Attribute
    => Argument.That.NotNull(provider).IsDefined(typeof(T), inherit);

  #endregion
  #region Generic parameters

  public static Type[] GetGenericArguments(object obj)
  {
    Argument.That.NotNull(obj);
    if (obj is Delegate deleg)
      return GetGenericArguments(deleg);
    var type = obj.GetType();
    return !type.IsGenericType ? Array.Empty<Type>() : type.GetGenericArguments();
  }

  public static Type[] GetGenericArguments(Delegate deleg)
    => !Argument.That.NotNull(deleg).Method.IsGenericMethod ? Array.Empty<Type>() : deleg.Method.GetGenericArguments();

  #endregion
}

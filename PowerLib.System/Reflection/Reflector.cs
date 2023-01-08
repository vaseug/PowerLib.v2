using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using PowerLib.System.Collections;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Collections.Matching;
using PowerLib.System.Linq;
using PowerLib.System.Resources;
using PowerLib.System.Validation;

namespace PowerLib.System.Reflection;

public static class Reflector
{
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
          //else if (!sourceArgument.IsAssignableFrom(sourceType))
          else if (!MatchType(sourceType, TypeVariance.Contravariant, sourceArgument, sourceTypeArguments, resultTypeArguments, sourceMethodArguments, resultMethodArguments))
            //sourceArgument.IsAssignableFrom(sourceType))
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

  private static object?[] GetParameterValues(ParameterInfo[] parameters, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues)
    => Argument.That.NotNull(parameters)
      .Select(paramInfo =>
        paramInfo.IsOut ? default :
        positionalParameterValues is not null && paramInfo.Position < positionalParameterValues.Count ? TypedValue.GetValue(positionalParameterValues[paramInfo.Position]) :
        namedParameterValues is not null && !string.IsNullOrEmpty(paramInfo.Name) && namedParameterValues.TryGetValue(paramInfo.Name, out var value) ? TypedValue.GetValue(value) :
        paramInfo.DefaultValue)
      .ToArray();

  private static object?[] GetParameterValues(ParameterInfo[] parameters, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
    => Argument.That.NotNull(parameters)
      .Select(paramInfo =>
        paramInfo.IsOut ? default :
        positionalParameterValues is not null && paramInfo.Position < positionalParameterValues.Count ? TypedValue.GetValue(positionalParameterValues[paramInfo.Position]) :
        namedParameterValues is not null && !string.IsNullOrEmpty(paramInfo.Name) && namedParameterValues.TryGetValue(paramInfo.Name, out var value) ? TypedValue.GetValue(value) :
        paramInfo.DefaultValue)
      .ToArray();

  private static void SetParameterValues(ParameterInfo[] parameters, object?[] values, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
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

  private static void SetArgumentTypes(Type[] genericArguments, IList<Type?>? sourceArguments)
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

  #endregion
  #region Field internal methods

  private static IEnumerable<FieldInfo> GetFields(Type sourceType, bool staticMember, string name, MemberAccessibility memberAccessibility, bool shouldWrite)
  {
    var bindingFlags = GetBindingFlags(staticMember, memberAccessibility);
    var nameComparison = memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase) ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
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

  private static bool MatchField(FieldInfo fieldInfo,
    IReadOnlyList<Type?>? sourceTypeArguments, IList<Type?>? resultTypeArguments, Type? valueType, TypeVariance typeVariance)
  {
    var genericTypeArguments = fieldInfo.DeclaringType?.GetGenericArguments();
    if (!MatchArguments(genericTypeArguments, sourceTypeArguments, resultTypeArguments, null, null, null))
      return false;

    var fieldType = fieldInfo.FieldType;
    if (valueType is null)
    {
      if (!fieldType.IsNullAssignable())
        return false;
    }
    else
    {
      if (!MatchType(valueType, typeVariance, fieldType, sourceTypeArguments, resultTypeArguments, null, null))
        return false;
    }
    return true;
  }

  private static FieldInfo? MatchField(Type sourceType, bool staticMember, string name, MemberAccessibility memberAccessibility, bool shouldWrite, bool required,
    IReadOnlyList<Type?>? sourceTypeArguments, Type? valueType, TypeVariance variance)
  {
    var fieldResults = GetFields(sourceType, staticMember, name, memberAccessibility, shouldWrite)
      .Select(fieldInfo =>
      {
        var inheritanceLevel = sourceType.GetInheritanceLevel(fieldInfo.DeclaringType!);
        var resultTypeArguments = new Type[sourceType.GetGenericArguments().Length];
        var matched = MatchField(fieldInfo, sourceTypeArguments, resultTypeArguments, valueType, variance);
        return (fieldInfo: matched ? fieldInfo : null, inheritanceLevel, typeArguments: resultTypeArguments);
      })
      .Where(tuple => tuple.fieldInfo is not null)
      .OrderBy(tuple => tuple.inheritanceLevel)
      .ToArray();
    var fieldResult = fieldResults.FirstOrDefault();
    var fieldInfo = fieldResult.fieldInfo;
    if (fieldInfo is not null)
    {
      if (sourceType.IsGenericType && !sourceType.IsConstructedGenericType && fieldResult.typeArguments is not null)
      {
        sourceType = sourceType.MakeGenericType(fieldResult.typeArguments);
        fieldInfo = MatchField(sourceType, staticMember, name, memberAccessibility, shouldWrite, required, null, valueType, variance);
      }
    }
    if (required && fieldInfo is null)
      throw new InvalidOperationException(FormatMessage(ReflectionMessage.FieldNotFound));
    return fieldInfo;
  }

  #endregion
  #region Instance field public methods
  #region Try methods

  public static bool TryGetField(object source, string name, MemberAccessibility memberAccessibility, Type? valueType, out object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var fieldInfo = MatchField(sourceType, false, name, memberAccessibility, false, false, null, valueType ?? typeof(object), TypeVariance.Covariant);
    if (fieldInfo is null)
    {
      value = default;
      return false;
    }
    value = fieldInfo.GetValue(sourceObject);
    return true;
  }

  public static bool TryGetField<TValue>(object source, string name, MemberAccessibility memberAccessibility, out TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var fieldInfo = MatchField(sourceType, false, name, memberAccessibility, false, false, null, typeof(TValue?), TypeVariance.Covariant);
    if (fieldInfo is null)
    {
      value = default;
      return false;
    }
    value = (TValue?)fieldInfo.GetValue(sourceObject);
    return true;
  }

  public static bool TryGetField<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, Type? valueType, out object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), false, name, memberAccessibility, false, false, null, valueType ?? typeof(object), TypeVariance.Covariant);
    if (fieldInfo is null)
    {
      value = default;
      return false;
    }
    value = fieldInfo.GetValue(source);
    return true;
  }

  public static bool TryGetField<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, out TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), false, name, memberAccessibility, false, false, null, typeof(TValue?), TypeVariance.Covariant);
    if (fieldInfo is null)
    {
      value = default;
      return false;
    }
    value = (TValue?)fieldInfo.GetValue(source);
    return true;
  }

  public static bool TrySetField(object source, string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var fieldInfo = MatchField(sourceType, false, name, memberAccessibility, true, false, null, TypedValue.GetType(value), TypeVariance.Contravariant);
    if (fieldInfo is null)
      return false;
    fieldInfo.SetValue(sourceObject, TypedValue.GetValue(value));
    return true;
  }

  public static bool TrySetField<TValue>(object source, string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var fieldInfo = MatchField(sourceType, false, name, memberAccessibility, true, false, null, typeof(TValue?), TypeVariance.Contravariant);
    if (fieldInfo is null)
      return false;
    fieldInfo.SetValue(sourceObject, value);
    return true;
  }

  public static bool TrySetField<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), false, name, memberAccessibility, true, false, null, TypedValue.GetType(value), TypeVariance.Contravariant);
    if (fieldInfo is null)
      return false;
    fieldInfo.SetValue(source, TypedValue.GetValue(value));
    return true;
  }

  public static bool TrySetField<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), false, name, memberAccessibility, true, false, null, typeof(TValue?), TypeVariance.Contravariant);
    if (fieldInfo is null)
      return false;
    fieldInfo.SetValue(source, value);
    return true;
  }

  public static bool TryReplaceField(object source, string name, MemberAccessibility memberAccessibility, object? newValue, out object? oldValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var fieldInfo = MatchField(sourceType, false, name, memberAccessibility, true, false, null, TypedValue.GetType(newValue), TypeVariance.Contravariant);
    if (fieldInfo is null)
    {
      oldValue = default;
      return false;
    }
    oldValue = fieldInfo.GetValue(sourceObject);
    fieldInfo.SetValue(sourceObject, TypedValue.GetValue(newValue));
    return true;
  }

  public static bool TryReplaceField<TValue>(object source, string name, MemberAccessibility memberAccessibility, TValue? newValue, out TValue? oldValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var fieldInfo = MatchField(sourceType, false, name, memberAccessibility, true, false, null, typeof(TValue?), TypeVariance.Invariant);
    if (fieldInfo is null)
    {
      oldValue = default;
      return false;
    }
    oldValue = (TValue?)fieldInfo.GetValue(sourceObject);
    fieldInfo.SetValue(sourceObject, newValue);
    return true;
  }

  public static bool TryReplaceField<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, object? newValue, out object? oldValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), false, name, memberAccessibility, true, false, null, TypedValue.GetType(newValue), TypeVariance.Contravariant);
    if (fieldInfo is null)
    {
      oldValue = default;
      return false;
    }
    oldValue = fieldInfo.GetValue(source);
    fieldInfo.SetValue(source, TypedValue.GetValue(newValue));
    return true;
  }

  public static bool TryReplaceField<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, TValue? newValue, out TValue? oldValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), false, name, memberAccessibility, true, false, null, typeof(TValue?), TypeVariance.Invariant);
    if (fieldInfo is null)
    {
      oldValue = default;
      return false;
    }
    oldValue = (TValue?)fieldInfo.GetValue(source);
    fieldInfo.SetValue(source, newValue);
    return true;
  }

  public static bool TryExchangeField(object source, string name, MemberAccessibility memberAccessibility, ref object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var fieldInfo = MatchField(sourceType, false, name, memberAccessibility, true, false, null, TypedValue.GetType(value), TypeVariance.Contravariant);
    if (fieldInfo is null)
      return false;
    var result = fieldInfo.GetValue(sourceObject);
    fieldInfo.SetValue(sourceObject, TypedValue.GetValue(value));
    value = result;
    return true;
  }

  public static bool TryExchangeField<TValue>(object source, string name, MemberAccessibility memberAccessibility, ref TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var fieldInfo = MatchField(sourceType, false, name, memberAccessibility, true, false, null, typeof(TValue?), TypeVariance.Invariant);
    if (fieldInfo is null)
      return false;
    var result = (TValue?)fieldInfo.GetValue(sourceObject);
    fieldInfo.SetValue(sourceObject, value);
    value = result;
    return true;
  }

  public static bool TryExchangeField<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, ref object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), false, name, memberAccessibility, true, false, null, TypedValue.GetType(value), TypeVariance.Contravariant);
    if (fieldInfo is null)
      return false;
    var result = fieldInfo.GetValue(source);
    fieldInfo.SetValue(source, TypedValue.GetValue(value));
    value = result;
    return true;
  }

  public static bool TryExchangeField<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, ref TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), false, name, memberAccessibility, true, false, null, typeof(TValue?), TypeVariance.Invariant);
    if (fieldInfo is null)
      return false;
    var result = (TValue?)fieldInfo.GetValue(source);
    fieldInfo.SetValue(source, value);
    value = result;
    return true;
  }

  #endregion
  #region Direct methods

  public static object? GetField(object source, string name, MemberAccessibility memberAccessibility, Type? valueType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var fieldInfo = MatchField(sourceType, false, name, memberAccessibility, false, true, null, valueType ?? typeof(object), TypeVariance.Covariant)!;
    return fieldInfo.GetValue(sourceObject);
  }

  public static TValue? GetField<TValue>(object source, string name, MemberAccessibility memberAccessibility)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var fieldInfo = MatchField(sourceType, false, name, memberAccessibility, false, true, null, typeof(TValue?), TypeVariance.Covariant)!;
    return (TValue?)fieldInfo.GetValue(sourceObject);
  }

  public static object? GetField<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, Type? valueType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), false, name, memberAccessibility, false, true, null, valueType ?? typeof(object), TypeVariance.Covariant)!;
    return fieldInfo.GetValue(source);
  }

  public static TValue? GetField<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), false, name, memberAccessibility, false, true, null, typeof(TValue?), TypeVariance.Covariant)!;
    return (TValue?)fieldInfo.GetValue(source);
  }

  public static void SetField(object source, string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var fieldInfo = MatchField(sourceType, false, name, memberAccessibility, true, true, null, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    fieldInfo.SetValue(sourceObject, TypedValue.GetValue(value));
  }

  public static void SetField<TValue>(object source, string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var fieldInfo = MatchField(sourceType, false, name, memberAccessibility, true, true, null, typeof(TValue?), TypeVariance.Contravariant)!;
    fieldInfo.SetValue(sourceObject, value);
  }

  public static void SetField<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), false, name, memberAccessibility, true, true, null, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    fieldInfo.SetValue(source, TypedValue.GetValue(value));
  }

  public static void SetField<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), false, name, memberAccessibility, true, true, null, typeof(TValue?), TypeVariance.Contravariant)!;
    fieldInfo.SetValue(source, value);
  }

  public static object? ReplaceField(object source, string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var fieldInfo = MatchField(sourceType, false, name, memberAccessibility, true, true, null, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var result = fieldInfo.GetValue(sourceObject);
    fieldInfo.SetValue(sourceObject, TypedValue.GetValue(value));
    return result;
  }

  public static TValue? ReplaceField<TValue>(object source, string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var fieldInfo = MatchField(sourceType, false, name, memberAccessibility, true, true, null, typeof(TValue?), TypeVariance.Invariant)!;
    var result = (TValue?)fieldInfo.GetValue(sourceObject);
    fieldInfo.SetValue(sourceObject, value);
    return result;
  }

  public static object? ReplaceField<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), false, name, memberAccessibility, true, true, null, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var result = fieldInfo.GetValue(source);
    fieldInfo.SetValue(source, TypedValue.GetValue(value));
    return result;
  }

  public static TValue? ReplaceField<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), false, name, memberAccessibility, true, true, null, typeof(TValue?), TypeVariance.Invariant)!;
    var result = (TValue?)fieldInfo.GetValue(source);
    fieldInfo.SetValue(source, value);
    return result;
  }

  public static void ExchangeField(object source, string name, MemberAccessibility memberAccessibility, ref object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var fieldInfo = MatchField(sourceType, false, name, memberAccessibility, true, true, null, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var result = fieldInfo.GetValue(sourceObject);
    fieldInfo.SetValue(sourceObject, TypedValue.GetValue(value));
    value = result;
  }

  public static void ExchangeField<TValue>(object source, string name, MemberAccessibility memberAccessibility, ref TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var fieldInfo = MatchField(sourceType, false, name, memberAccessibility, true, true, null, typeof(TValue?), TypeVariance.Invariant)!;
    var result = (TValue?)fieldInfo.GetValue(sourceObject);
    fieldInfo.SetValue(sourceObject, value);
    value = result;
  }

  public static void ExchangeField<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, ref object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), false, name, memberAccessibility, true, true, null, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var result = fieldInfo.GetValue(source);
    fieldInfo.SetValue(source, TypedValue.GetValue(value));
    value = result;
  }

  public static void ExchangeField<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, ref TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), false, name, memberAccessibility, true, true, null, typeof(TValue?), TypeVariance.Invariant)!;
    var result = (TValue?)fieldInfo.GetValue(source);
    fieldInfo.SetValue(source, value);
    value = result;
  }

  #endregion
  #endregion
  #region Static field public methods
  #region Try methods

  public static bool TryGetField(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, Type? valueType, out object? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(type, true, name, memberAccessibility, false, false, typeArguments?.AsReadOnly(), valueType ?? typeof(object), TypeVariance.Covariant);
    if (fieldInfo is null)
    {
      value = default;
      return false;
    }
    if (fieldInfo.DeclaringType is not null)
      SetArgumentTypes(fieldInfo.DeclaringType.GetGenericArguments(), typeArguments);
    value = fieldInfo.GetValue(null);
    return true;
  }

  public static bool TryGetField<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, out TValue? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(type, true, name, memberAccessibility, false, false, typeArguments?.AsReadOnly(), typeof(TValue?), TypeVariance.Covariant);
    if (fieldInfo is null)
    {
      value = default;
      return false;
    }
    if (fieldInfo.DeclaringType is not null)
      SetArgumentTypes(fieldInfo.DeclaringType.GetGenericArguments(), typeArguments);
    value = (TValue?)fieldInfo.GetValue(null);
    return true;
  }

  public static bool TryGetField<TSource>(string name, MemberAccessibility memberAccessibility, Type? valueType, out object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), true, name, memberAccessibility, false, false, null, valueType ?? typeof(object), TypeVariance.Covariant);
    if (fieldInfo is null)
    {
      value = default;
      return false;
    }
    value = fieldInfo.GetValue(null);
    return true;
  }

  public static bool TryGetField<TSource, TValue>(string name, MemberAccessibility memberAccessibility, out TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), true, name, memberAccessibility, false, false, null, typeof(TValue?), TypeVariance.Covariant);
    if (fieldInfo is null)
    {
      value = default;
      return false;
    }
    value = (TValue?)fieldInfo.GetValue(null);
    return true;
  }

  public static bool TrySetField(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, object? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(type, true, name, memberAccessibility, true, false, typeArguments?.AsReadOnly(), TypedValue.GetType(value), TypeVariance.Contravariant);
    if (fieldInfo is null)
      return false;
    if (fieldInfo.DeclaringType is not null)
      SetArgumentTypes(fieldInfo.DeclaringType.GetGenericArguments(), typeArguments);
    fieldInfo.SetValue(null, TypedValue.GetValue(value));
    return true;
  }

  public static bool TrySetField<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, TValue? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(type, true, name, memberAccessibility, true, false, typeArguments?.AsReadOnly(), typeof(TValue?), TypeVariance.Contravariant);
    if (fieldInfo is null)
      return false;
    if (fieldInfo.DeclaringType is not null)
      SetArgumentTypes(fieldInfo.DeclaringType.GetGenericArguments(), typeArguments);
    fieldInfo.SetValue(null, value);
    return true;
  }

  public static bool TrySetField<TSource>(string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), true, name, memberAccessibility, true, false, null, TypedValue.GetType(value), TypeVariance.Contravariant);
    if (fieldInfo is null)
      return false;
    fieldInfo.SetValue(null, TypedValue.GetValue(value));
    return true;
  }

  public static bool TrySetField<TSource, TValue>(string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), true, name, memberAccessibility, true, false, null, typeof(TValue?), TypeVariance.Contravariant);
    if (fieldInfo is null)
      return false;
    fieldInfo.SetValue(null, value);
    return true;
  }

  public static bool TryReplaceField(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, object? newValue, out object? oldValue)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(type, true, name, memberAccessibility, true, false, typeArguments?.AsReadOnly(), TypedValue.GetType(newValue), TypeVariance.Contravariant);
    if (fieldInfo is null)
    {
      oldValue = default;
      return false;
    }
    if (fieldInfo.DeclaringType is not null)
      SetArgumentTypes(fieldInfo.DeclaringType.GetGenericArguments(), typeArguments);
    oldValue = fieldInfo.GetValue(null);
    fieldInfo.SetValue(null, TypedValue.GetValue(newValue));
    return true;
  }

  public static bool TryReplaceField<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, TValue? newValue, out TValue? oldValue)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(type, true, name, memberAccessibility, true, false, typeArguments?.AsReadOnly(), typeof(TValue?), TypeVariance.Invariant);
    if (fieldInfo is null)
    {
      oldValue = default;
      return false;
    }
    if (fieldInfo.DeclaringType is not null)
      SetArgumentTypes(fieldInfo.DeclaringType.GetGenericArguments(), typeArguments);
    oldValue = (TValue?)fieldInfo.GetValue(null);
    fieldInfo.SetValue(null, newValue);
    return true;
  }

  public static bool TryReplaceField<TSource>(string name, MemberAccessibility memberAccessibility, object? newValue, out object? oldValue)
  {
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), true, name, memberAccessibility, true, false, null, TypedValue.GetType(newValue), TypeVariance.Contravariant);
    if (fieldInfo is null)
    {
      oldValue = default;
      return false;
    }
    oldValue = fieldInfo.GetValue(null);
    fieldInfo.SetValue(null, TypedValue.GetValue(newValue));
    return true;
  }

  public static bool TryReplaceField<TSource, TValue>(string name, MemberAccessibility memberAccessibility, TValue? newValue, out TValue? oldValue)
  {
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), true, name, memberAccessibility, true, false, null, typeof(TValue?), TypeVariance.Invariant);
    if (fieldInfo is null)
    {
      oldValue = default;
      return false;
    }
    oldValue = (TValue?)fieldInfo.GetValue(null);
    fieldInfo.SetValue(null, newValue);
    return true;
  }

  public static bool TryExchangeField(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, ref object? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(type, true, name, memberAccessibility, true, false, typeArguments?.AsReadOnly(), TypedValue.GetType(value), TypeVariance.Contravariant);
    if (fieldInfo is null)
      return false;
    if (fieldInfo.DeclaringType is not null)
      SetArgumentTypes(fieldInfo.DeclaringType.GetGenericArguments(), typeArguments);
    var result = fieldInfo.GetValue(null);
    fieldInfo.SetValue(null, TypedValue.GetValue(value));
    value = result;
    return true;
  }

  public static bool TryExchangeField<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, ref TValue? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(type, true, name, memberAccessibility, true, false, typeArguments?.AsReadOnly(), typeof(TValue?), TypeVariance.Invariant);
    if (fieldInfo is null)
      return false;
    if (fieldInfo.DeclaringType is not null)
      SetArgumentTypes(fieldInfo.DeclaringType.GetGenericArguments(), typeArguments);
    var result = (TValue?)fieldInfo.GetValue(null);
    fieldInfo.SetValue(null, value);
    value = result;
    return true;
  }

  public static bool TryExchangeField<TSource>(string name, MemberAccessibility memberAccessibility, ref object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), true, name, memberAccessibility, true, false, null, TypedValue.GetType(value), TypeVariance.Contravariant);
    if (fieldInfo is null)
      return false;
    var result = fieldInfo.GetValue(null);
    fieldInfo.SetValue(null, TypedValue.GetValue(value));
    value = result;
    return true;
  }

  public static bool TryExchangeField<TSource, TValue>(string name, MemberAccessibility memberAccessibility, ref TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), true, name, memberAccessibility, true, false, null, typeof(TValue?), TypeVariance.Invariant);
    if (fieldInfo is null)
      return false;
    var result = (TValue?)fieldInfo.GetValue(null);
    fieldInfo.SetValue(null, value);
    value = result;
    return true;
  }

  #endregion
  #region Direct methods

  public static object? GetField(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, Type? valueType)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(type, true, name, memberAccessibility, false, true, typeArguments?.AsReadOnly(), valueType ?? typeof(object), TypeVariance.Covariant)!;
    if (fieldInfo.DeclaringType is not null)
      SetArgumentTypes(fieldInfo.DeclaringType.GetGenericArguments(), typeArguments);
    return fieldInfo.GetValue(null);
  }

  public static TValue? GetField<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(type, true, name, memberAccessibility, false, true, typeArguments?.AsReadOnly(), typeof(TValue?), TypeVariance.Covariant)!;
    if (fieldInfo.DeclaringType is not null)
      SetArgumentTypes(fieldInfo.DeclaringType.GetGenericArguments(), typeArguments);
    return (TValue?)fieldInfo.GetValue(null);
  }

  public static object? GetField<TSource>(string name, MemberAccessibility memberAccessibility, Type? valueType)
  {
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), true, name, memberAccessibility, false, true, null, valueType, TypeVariance.Covariant)!;
    return fieldInfo.GetValue(null);
  }

  public static TValue? GetField<TSource, TValue>(string name, MemberAccessibility memberAccessibility)
  {
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), true, name, memberAccessibility, false, true, null, typeof(TValue?), TypeVariance.Covariant)!;
    return (TValue?)fieldInfo.GetValue(null);
  }

  public static void SetField(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, object? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(type, true, name, memberAccessibility, true, true, typeArguments?.AsReadOnly(), TypedValue.GetType(value), TypeVariance.Contravariant)!;
    if (fieldInfo.DeclaringType is not null)
      SetArgumentTypes(fieldInfo.DeclaringType.GetGenericArguments(), typeArguments);
    fieldInfo.SetValue(null, TypedValue.GetValue(value));
  }

  public static void SetField<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, TValue? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(type, true, name, memberAccessibility, true, true, typeArguments?.AsReadOnly(), typeof(TValue?), TypeVariance.Contravariant)!;
    if (fieldInfo.DeclaringType is not null)
      SetArgumentTypes(fieldInfo.DeclaringType.GetGenericArguments(), typeArguments);
    fieldInfo.SetValue(null, value);
  }

  public static void SetField<TSource>(string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), true, name, memberAccessibility, true, true, null, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    fieldInfo.SetValue(null, TypedValue.GetValue(value));
  }

  public static void SetField<TSource, TValue>(string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), true, name, memberAccessibility, true, true, null, typeof(TValue?), TypeVariance.Contravariant)!;
    fieldInfo.SetValue(null, value);
  }

  public static object? ReplaceField(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, object? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(type, true, name, memberAccessibility, true, true, typeArguments?.AsReadOnly(), TypedValue.GetType(value), TypeVariance.Contravariant)!;
    if (fieldInfo.DeclaringType is not null)
      SetArgumentTypes(fieldInfo.DeclaringType.GetGenericArguments(), typeArguments);
    var result = fieldInfo.GetValue(null);
    fieldInfo.SetValue(null, TypedValue.GetValue(value));
    return result;
  }

  public static TValue? ReplaceField<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, TValue? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(type, true, name, memberAccessibility, true, true, typeArguments?.AsReadOnly(), typeof(TValue?), TypeVariance.Invariant)!;
    if (fieldInfo.DeclaringType is not null)
      SetArgumentTypes(fieldInfo.DeclaringType.GetGenericArguments(), typeArguments);
    var result = (TValue?)fieldInfo.GetValue(null);
    fieldInfo.SetValue(null, value);
    return result;
  }

  public static object? ReplaceField<TSource>(string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), true, name, memberAccessibility, true, true, null, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var result = fieldInfo.GetValue(null);
    fieldInfo.SetValue(null, TypedValue.GetValue(value));
    return result;
  }

  public static TValue? ReplaceField<TSource, TValue>(string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), true, name, memberAccessibility, true, true, null, typeof(TValue?), TypeVariance.Invariant)!;
    var result = (TValue?)fieldInfo.GetValue(null);
    fieldInfo.SetValue(null, value);
    return result;
  }

  public static void ExchangeField(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, ref object? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(type, true, name, memberAccessibility, true, true, typeArguments?.AsReadOnly(), TypedValue.GetType(value), TypeVariance.Contravariant)!;
    if (fieldInfo.DeclaringType is not null)
      SetArgumentTypes(fieldInfo.DeclaringType.GetGenericArguments(), typeArguments);
    var result = fieldInfo.GetValue(null);
    fieldInfo.SetValue(null, TypedValue.GetValue(value));
    value = result;
  }

  public static void ExchangeField<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, ref TValue? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(type, true, name, memberAccessibility, true, true, typeArguments?.AsReadOnly(), typeof(TValue?), TypeVariance.Invariant)!;
    if (fieldInfo.DeclaringType is not null)
      SetArgumentTypes(fieldInfo.DeclaringType.GetGenericArguments(), typeArguments);
    var result = (TValue?)fieldInfo.GetValue(null);
    fieldInfo.SetValue(null, value);
    value = result;
  }

  public static void ExchangeField<TSource>(string name, MemberAccessibility memberAccessibility, ref object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), true, name, memberAccessibility, true, true, null, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var result = fieldInfo.GetValue(null);
    fieldInfo.SetValue(null, TypedValue.GetValue(value));
    value = result;
  }

  public static void ExchangeField<TSource, TValue>(string name, MemberAccessibility memberAccessibility, ref TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var fieldInfo = MatchField(typeof(TSource), true, name, memberAccessibility, true, true, null, typeof(TValue?), TypeVariance.Invariant)!;
    var result = (TValue?)fieldInfo.GetValue(null);
    fieldInfo.SetValue(null, value);
    value = result;
  }

  #endregion
  #endregion
  #region Property internal methods

  private static IEnumerable<PropertyInfo> GetProperties(Type sourceType, bool staticMember, string name, MemberAccessibility memberAccessibility, bool shouldRead, bool shouldWrite)
  {
    var bindingFlags = GetBindingFlags(staticMember, memberAccessibility);
    var nameComparison = memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase) ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
    var predicate = (PropertyInfo propertyInfo) =>
      string.Equals(propertyInfo.Name, name, nameComparison) &&
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

  private static bool MatchProperty(PropertyInfo propertyInfo,
    IReadOnlyList<Type?>? sourceTypeArguments, IList<Type?>? resultTypeArguments, IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes,
    Type? valueType, TypeVariance variance, out SignatureWeight signatureWeight)
  {
    signatureWeight = default;
    var currentWeight = new SignatureWeight();
    var genericTypeArguments = propertyInfo.DeclaringType?.GetGenericArguments();
    if (!MatchArguments(genericTypeArguments, sourceTypeArguments, resultTypeArguments, null, null, null))
      return false;
    var propertyParameters = propertyInfo.GetIndexParameters();
    if (propertyParameters.Length < (positionalParameterTypes?.Count ?? 0))
        return false;
    if (!MatchParameters(propertyParameters, sourceTypeArguments, resultTypeArguments, null, null, positionalParameterTypes, namedParameterTypes, ref currentWeight))
        return false;
    var propertyType = propertyInfo.PropertyType;
    if (valueType is null)
    {
      if (!propertyType.IsNullAssignable())
        return false;
    }
    else
    {
      if (!MatchType(valueType, variance, propertyType, sourceTypeArguments, resultTypeArguments, null, null))
        return false;
    }
    signatureWeight = currentWeight;
    return true;
  }

  private static PropertyInfo? MatchProperty(Type sourceType,
    bool staticMember, string name, MemberAccessibility memberAccessibility, bool shouldRead, bool shouldWrite, bool required,
    IReadOnlyList<Type?>? sourceTypeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType, TypeVariance variance)
  {
    var positionalParameterTypes = positionalParameterValues?.Select(parameterValue => TypedValue.GetType(parameterValue)).ToList();
    var namedParameterTypes = namedParameterValues?.ToDictionary(parameterItem => parameterItem.Key, parameterItem => TypedValue.GetType(parameterItem.Value),
      memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase) ? StringComparer.InvariantCultureIgnoreCase : StringComparer.InvariantCulture);
    var propertyResult = GetProperties(sourceType, staticMember, name, memberAccessibility, shouldRead, shouldWrite)
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
      .TakeWhile((signatureWeight: default(SignatureWeight?), inheritanceLevel: -1), (state, item) => (item.propertyWeight, item.inheritanceLevel),
        (state, item) => state.signatureWeight is null || CompareSignatureWeight(item.propertyWeight, state.signatureWeight.Value) == 0 && state.inheritanceLevel == item.inheritanceLevel)
      .FirstOrDefault();
    var propertyInfo = propertyResult.propertyInfo;
    if (propertyInfo is not null)
    {
      if (sourceType.IsGenericType && !sourceType.IsConstructedGenericType && propertyResult.typeArguments is not null)
      {
        sourceType = sourceType.MakeGenericType(propertyResult.typeArguments);
        propertyInfo = MatchProperty(sourceType, staticMember, name, memberAccessibility, shouldRead, shouldWrite, required, null, positionalParameterValues, namedParameterValues, valueType, variance);
      }
    }
    if (required && propertyInfo is null)
      throw new InvalidOperationException(FormatMessage(ReflectionMessage.PropertyNotFound));
    return propertyInfo;
  }

  #endregion
  #region Instance property public methods
  #region Without parameters
  #region Try methods

  public static bool TryGetProperty(object source, string name, MemberAccessibility memberAccessibility, Type? valueType, out object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, true, false, false, null, null, null, valueType ?? typeof(object), TypeVariance.Covariant);
    if (propertyInfo is null)
    {
      value = default;
      return false;
    }
    value = propertyInfo.GetValue(sourceObject);
    return true;
  }

  public static bool TryGetProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility, out TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, true, false, false, null, null, null, typeof(TValue?), TypeVariance.Covariant);
    if (propertyInfo is null)
    {
      value = default;
      return false;
    }
    value = (TValue?)propertyInfo.GetValue(sourceObject);
    return true;
  }

  public static bool TryGetProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, Type? valueType, out object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, true, false, false, null, null, null, valueType ?? typeof(object), TypeVariance.Covariant);
    if (propertyInfo is null)
    {
      value = null;
      return false;
    }
    value = propertyInfo.GetValue(source);
    return true;
  }

  public static bool TryGetProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, out TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, true, false, false, null, null, null, typeof(TValue?), TypeVariance.Covariant);
    if (propertyInfo is null)
    {
      value = default;
      return false;
    }
    value = (TValue?)propertyInfo.GetValue(source);
    return true;
  }

  public static bool TrySetProperty(object source, string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, false, true, false, null, null, null, TypedValue.GetType(value), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    propertyInfo.SetValue(sourceObject, TypedValue.GetValue(value));
    return true;
  }

  public static bool TrySetProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, false, true, false, null, null, null, typeof(TValue?), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    propertyInfo.SetValue(sourceObject, value);
    return true;
  }

  public static bool TrySetProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, false, true, false, null, null, null, TypedValue.GetType(value), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    propertyInfo.SetValue(source, TypedValue.GetValue(value));
    return true;
  }

  public static bool TrySetProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, false, true, false, null, null, null, typeof(TValue?), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    propertyInfo.SetValue(source, value);
    return true;
  }

  public static bool TryReplaceProperty(object source, string name, MemberAccessibility memberAccessibility, object? newValue, out object? oldValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, true, true, false, null, null, null, TypedValue.GetType(newValue), TypeVariance.Contravariant);
    if (propertyInfo is null)
    {
      oldValue = default;
      return false;
    }
    oldValue = propertyInfo.GetValue(sourceObject);
    propertyInfo.SetValue(sourceObject, TypedValue.GetValue(newValue));
    return true;
  }

  public static bool TryReplaceProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility, TValue? newValue, out TValue? oldValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, true, true, false, null, null, null, typeof(TValue?), TypeVariance.Invariant);
    if (propertyInfo is null)
    {
      oldValue = default;
      return false;
    }
    oldValue = (TValue?)propertyInfo.GetValue(sourceObject);
    propertyInfo.SetValue(sourceObject, newValue);
    return true;
  }

  public static bool TryReplaceProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, object? newValue, out object? oldValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, true, true, false, null, null, null, TypedValue.GetType(newValue), TypeVariance.Contravariant);
    if (propertyInfo is null)
    {
      oldValue = default;
      return false;
    }
    oldValue = propertyInfo.GetValue(source);
    propertyInfo.SetValue(source, TypedValue.GetValue(newValue));
    return true;
  }

  public static bool TryReplaceProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, TValue? newValue, out TValue? oldValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, true, true, false, null, null, null, typeof(TValue?), TypeVariance.Invariant);
    if (propertyInfo is null)
    {
      oldValue = default;
      return false;
    }
    oldValue = (TValue?)propertyInfo.GetValue(source);
    propertyInfo.SetValue(source, newValue);
    return true;
  }

  public static bool TryExchangeProperty(object source, string name, MemberAccessibility memberAccessibility, ref object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, true, true, false, null, null, null, TypedValue.GetType(value), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    var result = propertyInfo.GetValue(sourceObject);
    propertyInfo.SetValue(sourceObject, TypedValue.GetValue(value));
    value = result;
    return true;
  }

  public static bool TryExchangeProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility, ref TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, true, true, false, null, null, null, typeof(TValue?), TypeVariance.Invariant);
    if (propertyInfo is null)
      return false;
    var result = (TValue?)propertyInfo.GetValue(sourceObject);
    propertyInfo.SetValue(sourceObject, value);
    value = result;
    return true;
  }

  public static bool TryExchangeProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, ref object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, true, true, false, null, null, null, TypedValue.GetType(value), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    var result = propertyInfo.GetValue(source);
    propertyInfo.SetValue(source, TypedValue.GetValue(value));
    value = result;
    return true;
  }

  public static bool TryExchangeProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, ref TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, true, true, false, null, null, null, typeof(TValue?), TypeVariance.Invariant);
    if (propertyInfo is null)
      return false;
    var result = (TValue?)propertyInfo.GetValue(source);
    propertyInfo.SetValue(source, value);
    value = result;
    return true;
  }

  #endregion
  #region Direct methods

  public static object? GetProperty(object source, string name, MemberAccessibility memberAccessibility, Type? valueType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, true, false, true, null, null, null, valueType ?? typeof(object), TypeVariance.Covariant)!;
    return propertyInfo.GetValue(sourceObject);
  }

  public static TValue? GetProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, true, false, true, null, null, null, typeof(TValue?), TypeVariance.Covariant)!;
    return (TValue?)propertyInfo.GetValue(sourceObject);
  }

  public static object? GetProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, Type? valueType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, true, false, true, null, null, null, valueType ?? typeof(object), TypeVariance.Covariant)!;
    return propertyInfo.GetValue(source);
  }

  public static TValue? GetProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, true, false, true, null, null, null, typeof(TValue?), TypeVariance.Covariant)!;
    return (TValue?)propertyInfo.GetValue(source);
  }

  public static void SetProperty(object source, string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, false, true, true, null, null, null, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    propertyInfo.SetValue(sourceObject, TypedValue.GetValue(value));
  }

  public static void SetProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, false, true, true, null, null, null, typeof(TValue?), TypeVariance.Contravariant)!;
    propertyInfo.SetValue(sourceObject, value);
  }

  public static void SetProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, false, true, true, null, null, null, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    propertyInfo.SetValue(source, TypedValue.GetValue(value));
  }

  public static void SetProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, false, true, true, null, null, null, typeof(TValue?), TypeVariance.Contravariant)!;
    propertyInfo.SetValue(source, value);
  }

  public static object? ReplaceProperty(object source, string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, true, true, true, null, null, null, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var result = propertyInfo.GetValue(sourceObject);
    propertyInfo.SetValue(sourceObject, TypedValue.GetValue(value));
    return result;
  }

  public static TValue? ReplaceProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, true, true, true, null, null, null, typeof(TValue?), TypeVariance.Invariant)!;
    var result = (TValue?)propertyInfo.GetValue(sourceObject);
    propertyInfo.SetValue(sourceObject, value);
    return result;
  }

  public static object? ReplaceProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, true, true, true, null, null, null, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var result = propertyInfo.GetValue(source);
    propertyInfo.SetValue(source, TypedValue.GetValue(value));
    return result;
  }

  public static TValue? ReplaceProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, true, true, true, null, null, null, typeof(TValue?), TypeVariance.Invariant)!;
    var result = (TValue?)propertyInfo.GetValue(source);
    propertyInfo.SetValue(source, value);
    return result;
  }

  public static void ExchangeProperty(object source, string name, MemberAccessibility memberAccessibility, ref object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, true, true, true, null, null, null, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var result = propertyInfo.GetValue(sourceObject);
    propertyInfo.SetValue(sourceObject, TypedValue.GetValue(value));
    value = result;
  }

  public static void ExchangeProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility, ref TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, true, true, true, null, null, null, typeof(TValue?), TypeVariance.Invariant)!;
    var result = (TValue?)propertyInfo.GetValue(sourceObject);
    propertyInfo.SetValue(sourceObject, value);
    value = result;
  }

  public static void ExchangeProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility, ref object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, true, true, true, null, null, null, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var result = propertyInfo.GetValue(source);
    propertyInfo.SetValue(source, TypedValue.GetValue(value));
    value = result;
  }

  public static void ExchangeProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility, ref TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, true, true, true, null, null, null, typeof(TValue?), TypeVariance.Invariant)!;
    var result = (TValue?)propertyInfo.GetValue(source);
    propertyInfo.SetValue(source, value);
    value = result;
  }

  #endregion
  #endregion
  #region With parameters
  #region Try methods

  public static bool TryGetProperty(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType, out object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, true, false, false, null, positionalParameterValues, namedParameterValues, valueType ?? typeof(object), TypeVariance.Covariant);
    if (propertyInfo is null)
    {
      value = default;
      return false;
    }
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    value = propertyInfo.GetValue(sourceObject, indices);
    return true;
  }

  public static bool TryGetProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, out TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, true, false, false, null, positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Covariant);
    if (propertyInfo is null)
    {
      value = default;
      return false;
    }
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    value = (TValue?)propertyInfo.GetValue(sourceObject, indices);
    return true;
  }

  public static bool TryGetProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType, out object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, true, false, false, null, positionalParameterValues, namedParameterValues, valueType ?? typeof(object), TypeVariance.Covariant);
    if (propertyInfo is null)
    {
      value = default;
      return false;
    }
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    value = propertyInfo.GetValue(source, indices);
    return true;
  }

  public static bool TryGetProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, out TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, true, false, false, null, positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Covariant);
    if (propertyInfo is null)
    {
      value = default;
      return false;
    }
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    value = (TValue?)propertyInfo.GetValue(source, indices);
    return true;
  }

  public static bool TrySetProperty(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, false, true, false, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    propertyInfo.SetValue(sourceObject, TypedValue.GetValue(value), indices);
    return true;
  }

  public static bool TrySetProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, false, true, false, null, positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    propertyInfo.SetValue(sourceObject, value, indices);
    return true;
  }

  public static bool TrySetProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, false, true, false, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    propertyInfo.SetValue(source, TypedValue.GetValue(value), indices);
    return true;
  }

  public static bool TrySetProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, false, true, false, null, positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    propertyInfo.SetValue(source, value, indices);
    return true;
  }

  public static bool TryReplaceProperty(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? newValue, out object? oldValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, true, true, false, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(newValue), TypeVariance.Contravariant);
    if (propertyInfo is null)
    {
      oldValue = default;
      return false;
    }
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    oldValue = propertyInfo.GetValue(sourceObject, indices);
    propertyInfo.SetValue(sourceObject, TypedValue.GetValue(newValue), indices);
    return true;
  }

  public static bool TryReplaceProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? newValue, out TValue? oldValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, true, true, false, null, positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Invariant);
    if (propertyInfo is null)
    {
      oldValue = default;
      return false;
    }
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    oldValue = (TValue?)propertyInfo.GetValue(sourceObject, indices);
    propertyInfo.SetValue(sourceObject, newValue, indices);
    return true;
  }

  public static bool TryReplaceProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? newValue, out object? oldValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, true, true, false, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(newValue), TypeVariance.Contravariant);
    if (propertyInfo is null)
    {
      oldValue = default;
      return false;
    }
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    oldValue = propertyInfo.GetValue(source, indices);
    propertyInfo.SetValue(source, TypedValue.GetValue(newValue), indices);
    return true;
  }

  public static bool TryReplaceProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? newValue, out TValue? oldValue)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, true, true, false, null, positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Invariant);
    if (propertyInfo is null)
    {
      oldValue = default;
      return false;
    }
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    oldValue = (TValue?)propertyInfo.GetValue(source, indices);
    propertyInfo.SetValue(source, newValue, indices);
    return true;
  }

  public static bool TryExchangeProperty(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, true, true, false, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = propertyInfo.GetValue(sourceObject, indices);
    propertyInfo.SetValue(sourceObject, TypedValue.GetValue(value), indices);
    value = result;
    return true;
  }

  public static bool TryExchangeProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, true, true, false, null, positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Invariant);
    if (propertyInfo is null)
      return false;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = (TValue?)propertyInfo.GetValue(sourceObject, indices);
    propertyInfo.SetValue(sourceObject, value, indices);
    value = result;
    return true;
  }

  public static bool TryExchangeProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, true, true, false, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = propertyInfo.GetValue(source, indices);
    propertyInfo.SetValue(source, TypedValue.GetValue(value), indices);
    value = result;
    return true;
  }

  public static bool TryExchangeProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, true, true, false, null, positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Invariant);
    if (propertyInfo is null)
      return false;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = (TValue?)propertyInfo.GetValue(source, indices);
    propertyInfo.SetValue(source, value, indices);
    value = result;
    return true;
  }

  #endregion
  #region Direct methods

  public static object? GetProperty(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, true, false, true, null, positionalParameterValues, namedParameterValues, valueType, TypeVariance.Covariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    return propertyInfo.GetValue(sourceObject, indices);
  }

  public static TValue? GetProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, true, false, true, null, positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Covariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    return (TValue?)propertyInfo.GetValue(sourceObject, indices);
  }

  public static object? GetProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, true, false, true, null, positionalParameterValues, namedParameterValues, valueType, TypeVariance.Covariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    return propertyInfo.GetValue(source, indices);
  }

  public static TValue? GetProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, true, false, true, null, positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Covariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    return (TValue?)propertyInfo.GetValue(source, indices);
  }

  public static void SetProperty(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, false, true, true, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    propertyInfo.SetValue(sourceObject, TypedValue.GetValue(value), indices);
  }

  public static void SetProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, false, true, true, null, positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Contravariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    propertyInfo.SetValue(sourceObject, value, indices);
  }

  public static void SetProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, false, true, true, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    propertyInfo.SetValue(source, TypedValue.GetValue(value), indices);
  }

  public static void SetProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, false, true, true, null, positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Contravariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    propertyInfo.SetValue(source, value, indices);
  }

  public static object? ReplaceProperty(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, true, true, true, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = propertyInfo.GetValue(sourceObject, indices);
    propertyInfo.SetValue(sourceObject, TypedValue.GetValue(value), indices);
    return result;
  }

  public static TValue? ReplaceProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, true, true, true, null, positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Invariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = (TValue?)propertyInfo.GetValue(sourceObject, indices);
    propertyInfo.SetValue(sourceObject, value, indices);
    return result;
  }

  public static object? ReplaceProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, true, true, true, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = propertyInfo.GetValue(source, indices);
    propertyInfo.SetValue(source, TypedValue.GetValue(value), indices);
    return result;
  }

  public static TValue? ReplaceProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, true, true, true, null, positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Invariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = (TValue?)propertyInfo.GetValue(source, indices);
    propertyInfo.SetValue(source, value, indices);
    return result;
  }

  public static void ExchangeProperty(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, true, true, true, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = propertyInfo.GetValue(sourceObject, indices);
    propertyInfo.SetValue(sourceObject, TypedValue.GetValue(value), indices);
    value = result;
  }

  public static void ExchangeProperty<TValue>(object source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var propertyInfo = MatchProperty(sourceType, false, name, memberAccessibility, true, true, true, null, positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Invariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = (TValue?)propertyInfo.GetValue(source, indices);
    propertyInfo.SetValue(source, value, indices);
    value = result;
  }

  public static void ExchangeProperty<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref object? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, true, true, true, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = propertyInfo.GetValue(source, indices);
    propertyInfo.SetValue(source, TypedValue.GetValue(value), indices);
    value = result;
  }

  public static void ExchangeProperty<TSource, TValue>(TSource source, string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref TValue? value)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), false, name, memberAccessibility, true, true, true, null, positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Invariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = (TValue?)propertyInfo.GetValue(source, indices);
    propertyInfo.SetValue(source, value, indices);
    value = result;
  }

  #endregion
  #endregion
  #endregion
  #region Static property public methods
  #region Without parameters
  #region Try methods

  public static bool TryGetProperty(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, Type? valueType, out object? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, true, false, false, typeArguments?.AsReadOnly(), null, null, valueType ?? typeof(object), TypeVariance.Covariant);
    if (propertyInfo is null)
    {
      value = default;
      return false;
    }
    value = propertyInfo.GetValue(null);
    return true;
  }

  public static bool TryGetProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, out TValue? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, true, false, false, typeArguments?.AsReadOnly(), null, null, typeof(TValue?), TypeVariance.Covariant);
    if (propertyInfo is null)
    {
      value = default;
      return false;
    }
    value = (TValue?)propertyInfo.GetValue(null);
    return true;
  }

  public static bool TryGetProperty<TSource>(string name, MemberAccessibility memberAccessibility, Type? valueType, out object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, true, false, false, null, null, null, valueType ?? typeof(object), TypeVariance.Covariant);
    if (propertyInfo is null)
    {
      value = default;
      return false;
    }
    value = propertyInfo.GetValue(null);
    return true;
  }

  public static bool TryGetProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility, out TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, true, false, false, null, null, null, typeof(TValue?), TypeVariance.Covariant);
    if (propertyInfo is null)
    {
      value = default;
      return false;
    }
    value = (TValue?)propertyInfo.GetValue(null);
    return true;
  }

  public static bool TrySetProperty(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, object? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, false, true, false, typeArguments?.AsReadOnly(), null, null, TypedValue.GetType(value), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    propertyInfo.SetValue(null, TypedValue.GetValue(value));
    return true;
  }

  public static bool TrySetProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, TValue? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, false, true, false, typeArguments?.AsReadOnly(), null, null, typeof(TValue?), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    propertyInfo.SetValue(null, value);
    return true;
  }

  public static bool TrySetProperty<TSource>(string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, false, true, false, null, null, null, TypedValue.GetType(value), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    propertyInfo.SetValue(null, TypedValue.GetValue(value));
    return true;
  }

  public static bool TrySetProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, false, true, false, null, null, null, typeof(TValue?), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    propertyInfo.SetValue(null, value);
    return true;
  }

  public static bool TryReplaceProperty(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, object? newValue, out object? oldValue)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, true, true, false, typeArguments?.AsReadOnly(), null, null, TypedValue.GetType(newValue), TypeVariance.Contravariant);
    if (propertyInfo is null)
    {
      oldValue = default;
      return false;
    }
    oldValue = propertyInfo.GetValue(null);
    propertyInfo.SetValue(null, TypedValue.GetValue(newValue));
    return true;
  }

  public static bool TryReplaceProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, TValue? newValue, out TValue? oldValue)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, true, true, false, typeArguments?.AsReadOnly(), null, null, typeof(TValue?), TypeVariance.Invariant);
    if (propertyInfo is null)
    {
      oldValue = default;
      return false;
    }
    oldValue = (TValue?)propertyInfo.GetValue(null);
    propertyInfo.SetValue(null, newValue);
    return true;
  }

  public static bool TryReplaceProperty<TSource>(string name, MemberAccessibility memberAccessibility, object? newValue, out object? oldValue)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, true, true, false, null, null, null, TypedValue.GetType(newValue), TypeVariance.Contravariant);
    if (propertyInfo is null)
    {
      oldValue = default;
      return false;
    }
    oldValue = propertyInfo.GetValue(null);
    propertyInfo.SetValue(null, TypedValue.GetValue(newValue));
    return true;
  }

  public static bool TryReplaceProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility, TValue? newValue, out TValue? oldValue)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, true, true, false, null, null, null, typeof(TValue?), TypeVariance.Invariant);
    if (propertyInfo is null)
    {
      oldValue = default;
      return false;
    }
    oldValue = (TValue?)propertyInfo.GetValue(null);
    propertyInfo.SetValue(null, newValue);
    return true;
  }

  public static bool TryExchangeProperty(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, ref object? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, true, true, false, typeArguments?.AsReadOnly(), null, null, TypedValue.GetType(value), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    var result = propertyInfo.GetValue(null);
    propertyInfo.SetValue(null, TypedValue.GetValue(value));
    value = result;
    return true;
  }

  public static bool TryExchangeProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, ref TValue? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, true, true, false, typeArguments?.AsReadOnly(), null, null, typeof(TValue?), TypeVariance.Invariant);
    if (propertyInfo is null)
      return false;
    var result = (TValue?)propertyInfo.GetValue(null);
    propertyInfo.SetValue(null, value);
    value = result;
    return true;
  }

  public static bool TryExchangeProperty<TSource>(string name, MemberAccessibility memberAccessibility, ref object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, true, true, false, null, null, null, TypedValue.GetType(value), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    var result = propertyInfo.GetValue(null);
    propertyInfo.SetValue(null, TypedValue.GetValue(value));
    value = result;
    return true;
  }

  public static bool TryExchangeProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility, ref TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, true, true, false, null, null, null, typeof(TValue?), TypeVariance.Invariant);
    if (propertyInfo is null)
      return false;
    var result = (TValue?)propertyInfo.GetValue(null);
    propertyInfo.SetValue(null, value);
    value = result;
    return true;
  }

  #endregion
  #region Direct methods

  public static object? GetProperty(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, Type? valueType)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, true, false, true, typeArguments?.AsReadOnly(), null, null, valueType ?? typeof(object), TypeVariance.Covariant)!;
    return propertyInfo.GetValue(null);
  }

  public static TValue? GetProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, true, false, true, typeArguments?.AsReadOnly(), null, null, typeof(TValue?), TypeVariance.Covariant)!;
    return (TValue?)propertyInfo.GetValue(null);
  }

  public static object? GetProperty<TSource>(string name, MemberAccessibility memberAccessibility, Type? valueType)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, true, false, true, null, null, null, valueType ?? typeof(object), TypeVariance.Covariant)!;
    return propertyInfo.GetValue(null);
  }

  public static TValue? GetProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, true, false, true, null, null, null, typeof(TValue?), TypeVariance.Covariant)!;
    return (TValue?)propertyInfo.GetValue(null);
  }

  public static void SetProperty(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, object? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, false, true, true, typeArguments?.AsReadOnly(), null, null, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    propertyInfo.SetValue(null, TypedValue.GetValue(value));
  }

  public static void SetProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, TValue? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, false, true, true, typeArguments?.AsReadOnly(), null, null, typeof(TValue?), TypeVariance.Contravariant)!;
    propertyInfo.SetValue(null, value);
  }

  public static void SetProperty<TSource>(string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, false, true, true, null, null, null, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    propertyInfo.SetValue(null, TypedValue.GetValue(value));
  }

  public static void SetProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, false, true, true, null, null, null, typeof(TValue?), TypeVariance.Contravariant)!;
    propertyInfo.SetValue(null, value);
  }

  public static object? ReplaceProperty(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, object? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, true, true, true, typeArguments?.AsReadOnly(), null, null, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var result = propertyInfo.GetValue(null);
    propertyInfo.SetValue(null, TypedValue.GetValue(value));
    return result;
  }

  public static TValue? ReplaceProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, TValue? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, true, true, true, typeArguments?.AsReadOnly(), null, null, typeof(TValue?), TypeVariance.Invariant)!;
    var result = (TValue?)propertyInfo.GetValue(null);
    propertyInfo.SetValue(null, value);
    return result;
  }

  public static object? ReplaceProperty<TSource>(string name, MemberAccessibility memberAccessibility, object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, true, true, true, null, null, null, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var result = propertyInfo.GetValue(null);
    propertyInfo.SetValue(null, TypedValue.GetValue(value));
    return result;
  }

  public static TValue? ReplaceProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility, TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, true, true, true, null, null, null, typeof(TValue?), TypeVariance.Invariant)!;
    var result = (TValue?)propertyInfo.GetValue(null);
    propertyInfo.SetValue(null, value);
    return result;
  }

  public static void ExchangeProperty(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, ref object? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, true, true, true, typeArguments?.AsReadOnly(), null, null, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var result = propertyInfo.GetValue(null);
    propertyInfo.SetValue(null, TypedValue.GetValue(value));
    value = result;
  }

  public static void ExchangeProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility, IList<Type?>? typeArguments, ref TValue? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, true, true, true, typeArguments?.AsReadOnly(), null, null, typeof(TValue?), TypeVariance.Invariant)!;
    var result = (TValue?)propertyInfo.GetValue(null);
    propertyInfo.SetValue(null, value);
    value = result;
  }

  public static void ExchangeProperty<TSource>(string name, MemberAccessibility memberAccessibility, ref object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, true, true, true, null, null, null, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var result = propertyInfo.GetValue(null);
    propertyInfo.SetValue(null, TypedValue.GetValue(value));
    value = result;
  }

  public static void ExchangeProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility, ref TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, true, true, true, null, null, null, typeof(TValue?), TypeVariance.Invariant)!;
    var result = (TValue?)propertyInfo.GetValue(null);
    propertyInfo.SetValue(null, value);
    value = result;
  }

  #endregion
  #endregion
  #region With parameters
  #region Try methods

  public static bool TryGetProperty(Type type, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType, out object? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, true, false, false, typeArguments?.AsReadOnly(), positionalParameterValues, namedParameterValues, valueType ?? typeof(object), TypeVariance.Covariant);
    if (propertyInfo is null)
    {
      value = default;
      return false;
    }
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    value = propertyInfo.GetValue(null, indices);
    return true;
  }

  public static bool TryGetProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, out TValue? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, true, false, false, typeArguments?.AsReadOnly(), positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Covariant);
    if (propertyInfo is null)
    {
      value = default;
      return false;
    }
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    value = (TValue?)propertyInfo.GetValue(null, indices);
    return true;
  }

  public static bool TryGetProperty<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType, out object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, true, false, false, null, positionalParameterValues, namedParameterValues, valueType ?? typeof(object), TypeVariance.Covariant);
    if (propertyInfo is null)
    {
      value = default;
      return false;
    }
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    value = propertyInfo.GetValue(null, indices);
    return true;
  }

  public static bool TryGetProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, out TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, true, false, false, null, positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Covariant);
    if (propertyInfo is null)
    {
      value = default;
      return false;
    }
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    value = (TValue?)propertyInfo.GetValue(null, indices);
    return true;
  }

  public static bool TrySetProperty(Type type, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, false, true, false, typeArguments?.AsReadOnly(), positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    propertyInfo.SetValue(null, TypedValue.GetValue(value), indices);
    return true;
  }

  public static bool TrySetProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, false, true, false, typeArguments?.AsReadOnly(), positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    propertyInfo.SetValue(null, value, indices);
    return true;
  }

  public static bool TrySetProperty<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, false, true, false, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    propertyInfo.SetValue(null, TypedValue.GetValue(value), indices);
    return true;
  }

  public static bool TrySetProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, false, true, false, null, positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    propertyInfo.SetValue(null, value, indices);
    return true;
  }

  public static bool TryReplaceProperty(Type type, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? newValue, out object? oldValue)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, true, true, false, typeArguments?.AsReadOnly(), positionalParameterValues, namedParameterValues, TypedValue.GetType(newValue), TypeVariance.Contravariant);
    if (propertyInfo is null)
    {
      oldValue = default;
      return false;
    }
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    oldValue = propertyInfo.GetValue(null, indices);
    propertyInfo.SetValue(null, TypedValue.GetValue(newValue), indices);
    return true;
  }

  public static bool TryReplaceProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? newValue, out TValue? oldValue)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, true, true, false, typeArguments?.AsReadOnly(), positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Invariant);
    if (propertyInfo is null)
    {
      oldValue = default;
      return false;
    }
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    oldValue = (TValue?)propertyInfo.GetValue(null, indices);
    propertyInfo.SetValue(null, newValue, indices);
    return true;
  }

  public static bool TryReplaceProperty<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? newValue, out object? oldValue)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, true, true, false, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(newValue), TypeVariance.Contravariant);
    if (propertyInfo is null)
    {
      oldValue = default;
      return false;
    }
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    oldValue = propertyInfo.GetValue(null, indices);
    propertyInfo.SetValue(null, TypedValue.GetValue(newValue), indices);
    return true;
  }

  public static bool TryReplaceProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? newValue, out TValue? oldValue)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, true, true, false, null, positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Invariant);
    if (propertyInfo is null)
    {
      oldValue = default;
      return false;
    }
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    oldValue = (TValue?)propertyInfo.GetValue(null, indices);
    propertyInfo.SetValue(null, newValue, indices);
    return true;
  }

  public static bool TryExchangeProperty(Type type, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref object? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, true, true, false, typeArguments?.AsReadOnly(), positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = propertyInfo.GetValue(null, indices);
    propertyInfo.SetValue(null, TypedValue.GetValue(value), indices);
    value = result;
    return true;
  }

  public static bool TryExchangeProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref TValue? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, true, true, false, typeArguments?.AsReadOnly(), positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Invariant);
    if (propertyInfo is null)
      return false;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = (TValue?)propertyInfo.GetValue(null, indices);
    propertyInfo.SetValue(null, value, indices);
    value = result;
    return true;
  }

  public static bool TryExchangeProperty<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, true, true, false, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypeVariance.Contravariant);
    if (propertyInfo is null)
      return false;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = propertyInfo.GetValue(null, indices);
    propertyInfo.SetValue(null, TypedValue.GetValue(value), indices);
    value = result;
    return true;
  }

  public static bool TryExchangeProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, true, true, false, null, positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Invariant);
    if (propertyInfo is null)
      return false;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = (TValue?)propertyInfo.GetValue(null, indices);
    propertyInfo.SetValue(null, value, indices);
    value = result;
    return true;
  }

  #endregion
  #region Direct methods

  public static object? GetProperty(Type type, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, true, false, true, typeArguments?.AsReadOnly(), positionalParameterValues, namedParameterValues, valueType ?? typeof(object), TypeVariance.Covariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    return propertyInfo.GetValue(null, indices);
  }

  public static TValue? GetProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, true, false, true, typeArguments?.AsReadOnly(), positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Covariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    return (TValue?)propertyInfo.GetValue(null, indices);
  }

  public static object? GetProperty<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? valueType)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, true, false, true, null, positionalParameterValues, namedParameterValues, valueType ?? typeof(object), TypeVariance.Covariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    return propertyInfo.GetValue(null, indices);
  }

  public static TValue? GetProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, true, false, true, null, positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Covariant);
    var indices = GetParameterValues(propertyInfo!.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    return (TValue?)propertyInfo.GetValue(null, indices);
  }

  public static void SetProperty(Type type, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, false, true, true, typeArguments?.AsReadOnly(), positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    propertyInfo.SetValue(null, TypedValue.GetValue(value), indices);
  }

  public static void SetProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, false, true, true, typeArguments?.AsReadOnly(), positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Contravariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    propertyInfo.SetValue(null, value, indices);
  }

  public static void SetProperty<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, false, true, true, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    propertyInfo.SetValue(null, TypedValue.GetValue(value), indices);
  }

  public static void SetProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, false, true, true, null, positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Contravariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    propertyInfo.SetValue(null, value, indices);
  }

  public static object? ReplaceProperty(Type type, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, true, true, true, typeArguments?.AsReadOnly(), positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = propertyInfo.GetValue(null, indices);
    propertyInfo.SetValue(null, TypedValue.GetValue(value), indices);
    return result;
  }

  public static TValue? ReplaceProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, true, true, true, typeArguments?.AsReadOnly(), positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Invariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = (TValue?)propertyInfo.GetValue(null, indices);
    propertyInfo.SetValue(null, value, indices);
    return result;
  }

  public static object? ReplaceProperty<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, true, true, true, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = propertyInfo.GetValue(null, indices);
    propertyInfo.SetValue(null, TypedValue.GetValue(value), indices);
    return result;
  }

  public static TValue? ReplaceProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, true, true, true, null, positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Invariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = (TValue?)propertyInfo.GetValue(null, indices);
    propertyInfo.SetValue(null, value, indices);
    return result;
  }

  public static void ExchangeProperty(Type type, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref object? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, true, true, true, typeArguments?.AsReadOnly(), positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = propertyInfo.GetValue(null, indices);
    propertyInfo.SetValue(null, TypedValue.GetValue(value), indices);
    value = result;
  }

  public static void ExchangeProperty<TValue>(Type type, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref TValue? value)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(type, true, name, memberAccessibility, true, true, true, typeArguments?.AsReadOnly(), positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Invariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = (TValue?)propertyInfo.GetValue(null, indices);
    propertyInfo.SetValue(null, value, indices);
    value = result;
  }

  public static void ExchangeProperty<TSource>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref object? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, true, true, true, null, positionalParameterValues, namedParameterValues, TypedValue.GetType(value), TypeVariance.Contravariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = propertyInfo.GetValue(null, indices);
    propertyInfo.SetValue(null, value, indices);
    value = result;
  }

  public static void ExchangeProperty<TSource, TValue>(string name, MemberAccessibility memberAccessibility,
    IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, ref TValue? value)
  {
    Argument.That.NotNullOrWhitespace(name);

    var propertyInfo = MatchProperty(typeof(TSource), true, name, memberAccessibility, true, true, true, null, positionalParameterValues, namedParameterValues, typeof(TValue?), TypeVariance.Invariant)!;
    var indices = GetParameterValues(propertyInfo.GetIndexParameters(), positionalParameterValues, namedParameterValues);
    var result = (TValue?)propertyInfo.GetValue(null, indices);
    propertyInfo.SetValue(null, value, indices);
    value = result;
  }

  #endregion
  #endregion
  #endregion
  #region Method internal methods

  private struct SignatureWeight
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
    var nameComparison = memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase) ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
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

  private static bool MatchMethod(MethodInfo methodInfo,
    IReadOnlyList<Type?>? sourceTypeArguments, IList<Type?>? resultTypeArguments, IReadOnlyList<Type?>? sourceMethodArguments, IList<Type?>? resultMethodArguments,
    IReadOnlyList<Type?>? positionalParameterTypes, IReadOnlyDictionary<string, Type?>? namedParameterTypes, Type? returnType,
    out SignatureWeight methodWeight)
    => MatchMethod(methodInfo, sourceTypeArguments, resultTypeArguments, sourceMethodArguments, resultMethodArguments, positionalParameterTypes, namedParameterTypes, out methodWeight) &&
      (returnType is null || MatchType(returnType, TypeVariance.Covariant, methodInfo.ReturnType, sourceTypeArguments, resultTypeArguments, sourceMethodArguments, resultMethodArguments));

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
    if (!methodBase.IsConstructor)
    {
      var genericArguments = methodBase.GetGenericArguments();
      MatchConstraints(genericArguments, ref currentWeight);
    }
    methodWeight = currentWeight;
    return true;
  }

  private static MethodInfo? MatchMethod(Type type, bool staticMember, string name, MemberAccessibility memberAccessibility, bool required,
    IReadOnlyList<Type?>? sourceTypeArguments, IReadOnlyList<Type?>? sourceMethodArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    var positionalParameterTypes = positionalParameterValues?.Select(parameterValue => TypedValue.GetType(parameterValue)).ToList();
    var namedParameterTypes = namedParameterValues?.ToDictionary(parameterItem => parameterItem.Key, parameterItem => TypedValue.GetType(parameterItem.Value),
      memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase) ? StringComparer.InvariantCultureIgnoreCase : StringComparer.InvariantCulture);
    var methodResults = GetMethods(type, staticMember, name, memberAccessibility)
      .Select(methodInfo =>
      {
        var inheritanceLevel = methodInfo.ReflectedType!.GetInheritanceLevel(methodInfo.DeclaringType!);
        var resultTypeArguments = new Type[type.GetGenericArguments().Length];
        var resultMethodArguments = new Type[methodInfo.GetGenericArguments().Length];
        var matched = MatchMethod(methodInfo, sourceTypeArguments, resultTypeArguments, sourceMethodArguments, resultMethodArguments, positionalParameterTypes, namedParameterTypes, returnType, out var methodWeight);
        return (methodInfo: matched ? methodInfo : null, inheritanceLevel, methodWeight, typeArguments: resultTypeArguments, methodArguments: resultMethodArguments);
      })
      .Where(tuple => tuple.methodInfo is not null)
      .OrderByDescending(tuple => tuple.methodWeight, SignatureWeightComparer.Value)
      .ThenBy(tuple => tuple.inheritanceLevel)
      .ToArray();
    var methodResult = methodResults
      .TakeWhile((signatureWeight: default(SignatureWeight?), inheritanceLevel: -1), (state, item) => (item.methodWeight, item.inheritanceLevel),
        (state, item) => state.signatureWeight is null || CompareSignatureWeight(item.methodWeight, state.signatureWeight.Value) == 0 && state.inheritanceLevel == item.inheritanceLevel)
      .SingleOrDefault();
    var methodInfo = methodResult.methodInfo;
    if (methodInfo is not null)
    {
      if (type.IsGenericType && !type.IsConstructedGenericType)
      {
        type = type.MakeGenericType(methodResult.typeArguments);
        methodInfo = MatchMethod(type, staticMember, name, memberAccessibility, required, methodResult.typeArguments, methodResult.methodArguments, positionalParameterValues, namedParameterValues, returnType);
      }
      if (methodInfo is not null && methodInfo.IsGenericMethod && methodInfo.IsGenericMethodDefinition)
      {
        methodInfo = methodInfo.MakeGenericMethod(methodResult.methodArguments);
      }
    }
    if (required && methodInfo is null)
      throw new InvalidOperationException(FormatMessage(ReflectionMessage.MethodNotFound));
    return methodInfo;
  }

  #endregion
  #region Instance method public methods
  #region Try methods

  public static bool TryCallMethod(object source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var methodInfo = MatchMethod(sourceType, false, name, memberAccessibility, false, null, methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), typeof(void));
    if (methodInfo is null)
      return false;
    var values = GetParameterValues(methodInfo.GetParameters(), positionalParameterValues, namedParameterValues);
    methodInfo.Invoke(sourceObject, values);
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    return true;
  }

  public static bool TryCallMethod<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var methodInfo = MatchMethod(source.GetType(), false, name, memberAccessibility, false, null, methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), typeof(void));
    if (methodInfo is null)
      return false;
    var values = GetParameterValues(methodInfo.GetParameters(), positionalParameterValues, namedParameterValues);
    methodInfo.Invoke(source, values);
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    return true;
  }

  public static bool TryCallMethod(object source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType, out object? result)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var methodInfo = MatchMethod(sourceType, false, name, memberAccessibility, false, null, methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), returnType);
    if (methodInfo is null)
    {
      result = null;
      return false;
    }
    var values = GetParameterValues(methodInfo.GetParameters(), positionalParameterValues, namedParameterValues);
    result = methodInfo.Invoke(sourceObject, values);
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    return true;
  }

  public static bool TryCallMethod<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType, out object? result)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var methodInfo = MatchMethod(typeof(TSource), false, name, memberAccessibility, false, null, methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), returnType);
    if (methodInfo is null)
    {
      result = null;
      return false;
    }
    var values = GetParameterValues(methodInfo.GetParameters(), positionalParameterValues, namedParameterValues);
    result = methodInfo.Invoke(source, values);
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    return true;
  }

  public static async Task<bool> TryCallMethodAsync(object source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var methodInfo = MatchMethod(sourceType, false, name, memberAccessibility, false, null, methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), null);
    if (methodInfo is null)
      return false;
    var values = GetParameterValues(methodInfo.GetParameters(), positionalParameterValues, namedParameterValues);
    var result = methodInfo.Invoke(sourceObject, values);
    dynamic awaitable = Argument.That.NotNull(result);
    await awaitable;
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    return true;
  }

  public static async Task<bool> TryCallMethodAsync<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var methodInfo = MatchMethod(typeof(TSource), false, name, memberAccessibility, false, null, methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), null);
    if (methodInfo is null)
      return false;
    var values = GetParameterValues(methodInfo.GetParameters(), positionalParameterValues, namedParameterValues);
    var result = methodInfo.Invoke(source, values);
    dynamic awaitable = Argument.That.NotNull(result);
    await awaitable;
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    return true;
  }

  public static async Task<TryOut<object>> TryCallMethodAsync(object source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var methodInfo = MatchMethod(sourceType, false, name, memberAccessibility, false, null, methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), returnType);
    if (methodInfo is null)
      return TryOut.Failure<object>();
    var values = GetParameterValues(methodInfo.GetParameters(), positionalParameterValues, namedParameterValues);
    var result = methodInfo.Invoke(sourceObject, values);
    dynamic awaitable = Argument.That.NotNull(result);
    await awaitable;
    var awaitableResult = GetAwaitableResult(awaitable.GetAwaiter());
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    return TryOut.Success(awaitableResult);
  }

  public static async Task<TryOut<object>> TryCallMethodAsync<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var methodInfo = MatchMethod(typeof(TSource), false, name, memberAccessibility, false, null, methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), returnType);
    if (methodInfo is null)
      return TryOut.Failure<object>();
    var values = GetParameterValues(methodInfo.GetParameters(), positionalParameterValues, namedParameterValues);
    var result = methodInfo.Invoke(source, values);
    dynamic awaitable = Argument.That.NotNull(result);
    await awaitable;
    var awaitableResult = GetAwaitableResult(awaitable.GetAwaiter());
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    return TryOut.Success(awaitableResult);
  }

  #endregion
  #region Direct methods

  public static void CallMethod(object source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var methodInfo = MatchMethod(sourceType, false, name, memberAccessibility, true, null, methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), typeof(void));
    var values = GetParameterValues(methodInfo!.GetParameters(), positionalParameterValues, namedParameterValues);
    methodInfo.Invoke(sourceObject, values);
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
  }

  public static void CallMethod<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var methodInfo = MatchMethod(typeof(TSource), false, name, memberAccessibility, true, null, methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), typeof(void));
    var values = GetParameterValues(methodInfo!.GetParameters(), positionalParameterValues, namedParameterValues);
    methodInfo.Invoke(source, values);
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
  }

  public static object? CallMethod(object source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var methodInfo = MatchMethod(sourceType, false, name, memberAccessibility, true, null, methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), returnType);
    var values = GetParameterValues(methodInfo!.GetParameters(), positionalParameterValues, namedParameterValues);
    var result = methodInfo.Invoke(sourceObject, values);
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    return result;
  }

  public static object? CallMethod<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var methodInfo = MatchMethod(typeof(TSource), false, name, memberAccessibility, true, null, methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), returnType);
    var values = GetParameterValues(methodInfo!.GetParameters(), positionalParameterValues, namedParameterValues);
    var result = methodInfo.Invoke(source, values);
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    return result;
  }

  public static async Task CallMethodAsync(object source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var methodInfo = MatchMethod(sourceType, false, name, memberAccessibility, true, null, methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), null);
    var values = GetParameterValues(methodInfo!.GetParameters(), positionalParameterValues, namedParameterValues);
    var result = methodInfo.Invoke(sourceObject, values);
    dynamic awaitable = Argument.That.NotNull(result);
    await awaitable;
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
  }

  public static async Task CallMethodAsync<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var methodInfo = MatchMethod(typeof(TSource), false, name, memberAccessibility, true, null, methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), null);
    var values = GetParameterValues(methodInfo!.GetParameters(), positionalParameterValues, namedParameterValues);
    var result = methodInfo.Invoke(source, values);
    dynamic awaitable = Argument.That.NotNull(result);
    await awaitable;
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
  }

  public static async Task<object?> CallMethodAsync(object source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);
    var sourceObject = TypedValue.GetValue(source);
    Argument.That.NotNull(sourceObject);
    var sourceType = TypedValue.GetType(source)!;

    var methodInfo = MatchMethod(sourceType, false, name, memberAccessibility, true, null, methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), returnType);
    var values = GetParameterValues(methodInfo!.GetParameters(), positionalParameterValues, namedParameterValues);
    var result = methodInfo.Invoke(sourceObject, values);

    dynamic awaitable = Argument.That.NotNull(result);
    await awaitable;
    var awaitableResult = GetAwaitableResult(awaitable.GetAwaiter());

    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    return awaitableResult;
  }

  public static async Task<object?> CallMethodAsync<TSource>(TSource source, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    Argument.That.NotNull(source);
    Argument.That.NotNullOrWhitespace(name);

    var methodInfo = MatchMethod(typeof(TSource), false, name, memberAccessibility, true, null, methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), returnType);
    var values = GetParameterValues(methodInfo!.GetParameters(), positionalParameterValues, namedParameterValues);
    var result = methodInfo.Invoke(source, values);
    dynamic awaitable = Argument.That.NotNull(result);
    await awaitable;
    var awaitableResult = GetAwaitableResult(awaitable.GetAwaiter());
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    return awaitableResult;
  }

  #endregion
  #endregion
  #region Static method public methods
  #region Try methods

  public static bool TryCallMethod(Type type, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var methodInfo = MatchMethod(type, true, name, memberAccessibility, false, typeArguments?.AsReadOnly(), methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), typeof(void));
    if (methodInfo is null)
      return false;
    var values = GetParameterValues(methodInfo.GetParameters(), positionalParameterValues, namedParameterValues);
    methodInfo.Invoke(null, values);
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    if (methodInfo.ReflectedType is not null)
      SetArgumentTypes(methodInfo.ReflectedType.GetGenericArguments(), typeArguments);
    return true;
  }

  public static bool TryCallMethod<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNullOrWhitespace(name);

    var methodInfo = MatchMethod(typeof(TSource), true, name, memberAccessibility, false, typeArguments?.AsReadOnly(), methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), typeof(void));
    if (methodInfo is null)
      return false;
    var values = GetParameterValues(methodInfo.GetParameters(), positionalParameterValues, namedParameterValues);
    methodInfo.Invoke(null, values);
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    if (methodInfo.ReflectedType is not null)
      SetArgumentTypes(methodInfo.ReflectedType.GetGenericArguments(), typeArguments);
    return true;
  }

  public static bool TryCallMethod(Type type, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType, out object? result)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var methodInfo = MatchMethod(type, true, name, memberAccessibility, false, typeArguments?.AsReadOnly(), methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), returnType);
    if (methodInfo is null)
    {
      result = null;
      return false;
    }
    var values = GetParameterValues(methodInfo.GetParameters(), positionalParameterValues, namedParameterValues);
    result = methodInfo.Invoke(null, values);
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    if (methodInfo.ReflectedType is not null)
      SetArgumentTypes(methodInfo.ReflectedType.GetGenericArguments(), typeArguments);
    return true;
  }

  public static bool TryCallMethod<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType, out object? result)
  {
    Argument.That.NotNullOrWhitespace(name);

    var methodInfo = MatchMethod(typeof(TSource), true, name, memberAccessibility, false, typeArguments?.AsReadOnly(), methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), returnType);
    if (methodInfo is null)
    {
      result = null;
      return false;
    }
    var values = GetParameterValues(methodInfo.GetParameters(), positionalParameterValues, namedParameterValues);
    result = methodInfo.Invoke(null, values);
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    if (methodInfo.ReflectedType is not null)
      SetArgumentTypes(methodInfo.ReflectedType.GetGenericArguments(), typeArguments);
    return true;
  }

  public static async Task<bool> TryCallMethodAsync(Type type, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var methodInfo = MatchMethod(type, true, name, memberAccessibility, false, typeArguments?.AsReadOnly(), methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), null);
    if (methodInfo is null)
      return false;
    var values = GetParameterValues(methodInfo.GetParameters(), positionalParameterValues, namedParameterValues);
    var result = methodInfo.Invoke(null, values);
    dynamic awaitable = Argument.That.NotNull(result);
    await awaitable;
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    if (methodInfo.ReflectedType is not null)
      SetArgumentTypes(methodInfo.ReflectedType.GetGenericArguments(), typeArguments);
    return true;
  }

  public static async Task<bool> TryCallMethodAsync<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNullOrWhitespace(name);

    var methodInfo = MatchMethod(typeof(TSource), true, name, memberAccessibility, false, typeArguments?.AsReadOnly(), methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), null);
    if (methodInfo is null)
      return false;
    var values = GetParameterValues(methodInfo.GetParameters(), positionalParameterValues, namedParameterValues);
    var result = methodInfo.Invoke(null, values);
    dynamic awaitable = Argument.That.NotNull(result);
    await awaitable;
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    if (methodInfo.ReflectedType is not null)
      SetArgumentTypes(methodInfo.ReflectedType.GetGenericArguments(), typeArguments);
    return true;
  }

  public static async Task<TryOut<object>> TryCallMethodAsync(Type type, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var methodInfo = MatchMethod(type, true, name, memberAccessibility, false, typeArguments?.AsReadOnly(), methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), returnType);
    if (methodInfo is null)
      return TryOut.Failure<object>();
    var values = GetParameterValues(methodInfo.GetParameters(), positionalParameterValues, namedParameterValues);
    var result = methodInfo.Invoke(null, values);
    dynamic awaitable = Argument.That.NotNull(result);
    await awaitable;
    var awaitableResult = GetAwaitableResult(awaitable.GetAwaiter());
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    if (methodInfo.ReflectedType is not null)
      SetArgumentTypes(methodInfo.ReflectedType.GetGenericArguments(), typeArguments);
    return TryOut.Success(awaitableResult);
  }

  public static async Task<TryOut<object>> TryCallMethodAsync<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    Argument.That.NotNullOrWhitespace(name);

    var methodInfo = MatchMethod(typeof(TSource), true, name, memberAccessibility, false, typeArguments?.AsReadOnly(), methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), returnType);
    if (methodInfo is null)
      return TryOut.Failure<object>();
    var values = GetParameterValues(methodInfo.GetParameters(), positionalParameterValues, namedParameterValues);
    var result = methodInfo.Invoke(null, values);
    dynamic awaitable = Argument.That.NotNull(result);
    await awaitable;
    var awaitableResult = GetAwaitableResult(awaitable.GetAwaiter());
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    if (methodInfo.ReflectedType is not null)
      SetArgumentTypes(methodInfo.ReflectedType.GetGenericArguments(), typeArguments);
    return TryOut.Success(awaitableResult);
  }

  #endregion
  #region Direct methods

  public static void CallMethod(Type type, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var methodInfo = MatchMethod(type, true, name, memberAccessibility, true, typeArguments?.AsReadOnly(), methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), typeof(void));
    var values = GetParameterValues(methodInfo!.GetParameters(), positionalParameterValues, namedParameterValues);
    methodInfo.Invoke(null, values);
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    if (methodInfo.ReflectedType is not null)
      SetArgumentTypes(methodInfo.ReflectedType.GetGenericArguments(), typeArguments);
  }

  public static void CallMethod<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNullOrWhitespace(name);

    var methodInfo = MatchMethod(typeof(TSource), true, name, memberAccessibility, true, typeArguments?.AsReadOnly(), methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), typeof(void));
    var values = GetParameterValues(methodInfo!.GetParameters(), positionalParameterValues, namedParameterValues);
    methodInfo.Invoke(null, values);
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    if (methodInfo.ReflectedType is not null)
      SetArgumentTypes(methodInfo.ReflectedType.GetGenericArguments(), typeArguments);
  }

  public static object? CallMethod(Type type, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var methodInfo = MatchMethod(type, true, name, memberAccessibility, true, typeArguments?.AsReadOnly(), methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), returnType);
    var values = GetParameterValues(methodInfo!.GetParameters(), positionalParameterValues, namedParameterValues);
    var result = methodInfo.Invoke(null, values);
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    if (methodInfo.ReflectedType is not null)
      SetArgumentTypes(methodInfo.ReflectedType.GetGenericArguments(), typeArguments);
    return result;
  }

  public static object? CallMethod<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    Argument.That.NotNullOrWhitespace(name);

    var methodInfo = MatchMethod(typeof(TSource), true, name, memberAccessibility, true, typeArguments?.AsReadOnly(), methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), returnType);
    var values = GetParameterValues(methodInfo!.GetParameters(), positionalParameterValues, namedParameterValues);
    var result = methodInfo.Invoke(null, values);
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    if (methodInfo.ReflectedType is not null)
      SetArgumentTypes(methodInfo.ReflectedType.GetGenericArguments(), typeArguments);
    return result;
  }

  public static async Task CallMethodAsync(Type type, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var methodInfo = MatchMethod(type, true, name, memberAccessibility, true, typeArguments?.AsReadOnly(), methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), null);
    var values = GetParameterValues(methodInfo!.GetParameters(), positionalParameterValues, namedParameterValues);
    var result = methodInfo.Invoke(null, values);
    dynamic awaitable = Argument.That.NotNull(result);
    await awaitable;
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    if (methodInfo.ReflectedType is not null)
      SetArgumentTypes(methodInfo.ReflectedType.GetGenericArguments(), typeArguments);
  }

  public static async Task CallMethodAsync<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNullOrWhitespace(name);

    var methodInfo = MatchMethod(typeof(TSource), true, name, memberAccessibility, true, typeArguments?.AsReadOnly(), methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), null);
    var values = GetParameterValues(methodInfo!.GetParameters(), positionalParameterValues, namedParameterValues);
    var result = methodInfo.Invoke(null, values);
    dynamic awaitable = Argument.That.NotNull(result);
    await awaitable;
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    if (methodInfo.ReflectedType is not null)
      SetArgumentTypes(methodInfo.ReflectedType.GetGenericArguments(), typeArguments);
  }

  public static async Task<object?> CallMethodAsync(Type type, string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNullOrWhitespace(name);

    var methodInfo = MatchMethod(type, true, name, memberAccessibility, true, typeArguments?.AsReadOnly(), methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), returnType);
    var values = GetParameterValues(methodInfo!.GetParameters(), positionalParameterValues, namedParameterValues);
    var result = methodInfo.Invoke(null, values);
    dynamic awaitable = Argument.That.NotNull(result);
    await awaitable;
    var awaitableResult = GetAwaitableResult(awaitable.GetAwaiter());
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    if (methodInfo.ReflectedType is not null)
      SetArgumentTypes(methodInfo.ReflectedType.GetGenericArguments(), typeArguments);
    return awaitableResult;
  }

  public static async Task<object?> CallMethodAsync<TSource>(string name, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IList<Type?>? methodArguments, IList<object?>? positionalParameterValues, IDictionary<string, object?>? namedParameterValues, Type? returnType)
  {
    Argument.That.NotNullOrWhitespace(name);

    var methodInfo = MatchMethod(typeof(TSource), true, name, memberAccessibility, true, typeArguments?.AsReadOnly(), methodArguments?.AsReadOnly(), positionalParameterValues?.AsReadOnly(), namedParameterValues?.AsReadOnly(), returnType);
    var values = GetParameterValues(methodInfo!.GetParameters(), positionalParameterValues, namedParameterValues);
    var result = methodInfo.Invoke(null, values);
    dynamic awaitable = Argument.That.NotNull(result);
    await awaitable;
    var awaitableResult = GetAwaitableResult(awaitable.GetAwaiter());
    SetParameterValues(methodInfo.GetParameters(), values, positionalParameterValues, namedParameterValues);
    SetArgumentTypes(methodInfo.GetGenericArguments(), methodArguments);
    if (methodInfo.ReflectedType is not null)
      SetArgumentTypes(methodInfo.ReflectedType.GetGenericArguments(), typeArguments);
    return awaitableResult;
  }

  #endregion
  #endregion
  #region Constructor internal methods

  private static IEnumerable<ConstructorInfo> GetConstructors(Type sourceType, bool staticMember, MemberAccessibility memberAccessibility)
  {
    var bindingFlags = (staticMember ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.DeclaredOnly
      | (memberAccessibility.IsFlagsSet(MemberAccessibility.Public) ? BindingFlags.Public : BindingFlags.Default)
      | (memberAccessibility.IsFlagsOverlapped(MemberAccessibility.NonPublic) ? BindingFlags.NonPublic : BindingFlags.Default);
    var nameComparison = memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase) ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
    foreach (var constructorInfo in sourceType.GetConstructors(bindingFlags).Where(constructorInfo =>
      (constructorInfo.Attributes & MethodAttributes.MemberAccessMask) switch
      {
        MethodAttributes.Private => memberAccessibility.IsFlagsSet(MemberAccessibility.Private),
        MethodAttributes.Public => memberAccessibility.IsFlagsSet(MemberAccessibility.Public),
        MethodAttributes.Assembly => memberAccessibility.IsFlagsSet(MemberAccessibility.Assembly),
        MethodAttributes.Family => memberAccessibility.IsFlagsSet(MemberAccessibility.Family),
        MethodAttributes.FamORAssem => memberAccessibility.IsFlagsSet(MemberAccessibility.FamilyOrAssembly),
        MethodAttributes.FamANDAssem => memberAccessibility.IsFlagsSet(MemberAccessibility.FamilyAndAssembly),
        _ => false
      }))
      yield return constructorInfo;
  }

  private static ConstructorInfo? MatchConstructor(Type sourceType, bool staticMember, MemberAccessibility memberAccessibility, bool required,
    IReadOnlyList<Type?>? sourceTypeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues)
  {
    var positionalParameterTypes = positionalParameterValues?.Select(parameterValue => parameterValue?.GetType()).ToList();
    var namedParameterTypes = namedParameterValues?.ToDictionary(parameterItem => parameterItem.Key, parameterItem => parameterItem.Value?.GetType(),
      memberAccessibility.IsFlagsSet(MemberAccessibility.IgnoreCase) ? StringComparer.InvariantCultureIgnoreCase : StringComparer.InvariantCulture);
    var constructorResult = GetConstructors(sourceType, staticMember, memberAccessibility)
      .Select(constructorInfo =>
      {
        var resultTypeArguments = new Type[sourceTypeArguments?.Count ?? constructorInfo.DeclaringType?.GetGenericArguments().Length ?? 0];
        var matched = MatchMethod(constructorInfo, sourceTypeArguments, resultTypeArguments, null, null, positionalParameterTypes, namedParameterTypes, out var constructorWeight);
        return (constructorInfo: matched ? constructorInfo : null, constructorWeight, typeArguments: resultTypeArguments);
      })
      .Where(tuple => tuple.constructorInfo is not null)
      .OrderByDescending(tuple => tuple.constructorWeight, SignatureWeightComparer.Value)
      .TakeWhile(default(SignatureWeight?), (state, item) => item.constructorWeight, (state, item) => state is null || CompareSignatureWeight(item.constructorWeight, state.Value) == 0)
      .SingleOrDefault();
    var constructorInfo = constructorResult.constructorInfo;
    if (constructorInfo is not null)
    {
      if (sourceType.IsGenericType && !sourceType.IsConstructedGenericType)
      {
        sourceType = sourceType.MakeGenericType(constructorResult.typeArguments);
        constructorInfo = MatchConstructor(sourceType, staticMember, memberAccessibility, required, constructorResult.typeArguments, positionalParameterValues, namedParameterValues);
      }
    }
    if (required && constructorInfo is null)
      throw new InvalidOperationException(FormatMessage(ReflectionMessage.ConstructorNotFound));
    return constructorInfo;
  }

  #endregion
  #region Constructor public methods
  #region Try methods

  public static bool TryConstruct(Type type, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues,
    out object? result)
  {
    Argument.That.NotNull(type);

    var constructorInfo = MatchConstructor(type, false, memberAccessibility, false, typeArguments?.AsReadOnly(), positionalParameterValues, namedParameterValues);
    if (constructorInfo is null)
    {
      result = null;
      return false;
    }
    var values = GetParameterValues(constructorInfo.GetParameters(), positionalParameterValues, namedParameterValues);
    result = constructorInfo.Invoke(values);
    if (constructorInfo.DeclaringType is not null)
      SetArgumentTypes(constructorInfo.DeclaringType.GetGenericArguments(), typeArguments);
    return true;
  }

  public static bool TryConstruct<TSource>(MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues,
    out object? result)
  {
    var constructorInfo = MatchConstructor(typeof(TSource), false, memberAccessibility, false, typeArguments?.AsReadOnly(), positionalParameterValues, namedParameterValues);
    if (constructorInfo is null)
    {
      result = null;
      return false;
    }
    var values = GetParameterValues(constructorInfo.GetParameters(), positionalParameterValues, namedParameterValues);
    result = constructorInfo.Invoke(values);
    if (constructorInfo.DeclaringType is not null)
      SetArgumentTypes(constructorInfo.DeclaringType.GetGenericArguments(), typeArguments);
    return true;
  }

  #endregion
  #region Direct methods

  public static object Construct(Type type, MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues)
  {
    Argument.That.NotNull(type);

    var constructorInfo = MatchConstructor(type, false, memberAccessibility, true, typeArguments?.AsReadOnly(), positionalParameterValues, namedParameterValues);
    var values = GetParameterValues(constructorInfo!.GetParameters(), positionalParameterValues, namedParameterValues);
    var result = constructorInfo.Invoke(values);
    if (constructorInfo.DeclaringType is not null)
      SetArgumentTypes(constructorInfo.DeclaringType.GetGenericArguments(), typeArguments);
    return result;
  }

  public static object Construct<TSource>(MemberAccessibility memberAccessibility,
    IList<Type?>? typeArguments, IReadOnlyList<object?>? positionalParameterValues, IReadOnlyDictionary<string, object?>? namedParameterValues)
  {
    var constructorInfo = MatchConstructor(typeof(TSource), false, memberAccessibility, true, typeArguments?.AsReadOnly(), positionalParameterValues, namedParameterValues);
    var values = GetParameterValues(constructorInfo!.GetParameters(), positionalParameterValues, namedParameterValues);
    var result = constructorInfo.Invoke(values);
    if (constructorInfo.DeclaringType is not null)
      SetArgumentTypes(constructorInfo.DeclaringType.GetGenericArguments(), typeArguments);
    return result;
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
  #region Methods filtering

  public static bool MatchParams(MethodBase method, bool exact, params Type[] argTypes)
  {
    Argument.That.NotNull(method);
    Argument.That.NotNull(argTypes);

    return method.GetParameters().All(pi => pi.Position >= argTypes.Length && pi.HasDefaultValue ||
      pi.Position < argTypes.Length && (argTypes[pi.Position] is null || exact && pi.ParameterType.Equals(argTypes[pi.Position]) || !exact && pi.ParameterType.IsAssignableFrom(argTypes[pi.Position])));
  }

  public static bool MatchParams(MethodBase method, params object?[] argValues)
  {
    Argument.That.NotNull(method);
    Argument.That.NotNull(argValues);

    return method.GetParameters().All(pi => pi.Position >= argValues.Length && pi.HasDefaultValue || pi.Position < argValues.Length && pi.ParameterType.IsInstanceOfType(argValues[pi.Position]));
  }

  public static bool MatchResult(MethodInfo method, bool exact, Type resType)
  {
    Argument.That.NotNull(method);
    Argument.That.NotNull(resType);

    return exact && method.ReturnType.Equals(resType) && !exact && resType.IsAssignableFrom(method.ReturnType);
  }

  public static int CompareParameters(MethodBase xMethod, MethodBase yMethod)
  {
    Argument.That.NotNull(xMethod);
    Argument.That.NotNull(yMethod);

    var xParameters = xMethod.GetParameters();
    var yParameters = yMethod.GetParameters();
    int result = 0;
    for (int i = 0, c = Comparable.Min(xParameters.Length, yParameters.Length); result == 0 && i < c; i++)
      result = xParameters[i].ParameterType.Equals(yParameters[i].ParameterType) ? 0 :
        xParameters[i].ParameterType.IsAssignableFrom(yParameters[i].ParameterType) ? 1 :
        yParameters[i].ParameterType.IsAssignableFrom(xParameters[i].ParameterType) ? -1 : 0;
    if (result == 0)
      result = Comparer<int>.Default.Compare(xParameters.Length, yParameters.Length);
    return result;
  }

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

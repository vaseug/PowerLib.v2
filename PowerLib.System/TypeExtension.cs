using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using PowerLib.System.Validation;

namespace PowerLib.System;

public static class TypeExtension
{
  public static bool IsNullable(this Type type)
  {
    Argument.That.NotNull(type);

    return type.IsValueType && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
  }

  public static bool IsNullAssignable(this Type type)
  {
    Argument.That.NotNull(type);

    return !type.IsValueType || type.IsNullable();
  }

  public static bool IsValueAssignable(this Type type, object? value)
  {
    Argument.That.NotNull(type);

    return value is null && type.IsNullAssignable() || value is not null && type.IsAssignableFrom(value.GetType());
  }

  public static bool IsValueAssignable<TValue>(this Type type, TValue? value)
  {
    Argument.That.NotNull(type);

    return value is null && type.IsNullAssignable() || value is not null && type.IsAssignableFrom(typeof(TValue));
  }

  public static bool IsAnonymous(this Type type)
  {
    Argument.That.NotNull(type);

    return type.IsGenericType && type.IsClass && type.IsSealed && type.IsNotPublic
      && Attribute.IsDefined(type.IsGenericTypeDefinition ? type : type.GetGenericTypeDefinition(), typeof(CompilerGeneratedAttribute), false);
  }

  public static bool IsMadeOf(this Type type, Type typeDefinition)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNull(typeDefinition);
    Argument.That.IsValid(type, !type.IsGenericTypeDefinition);
    Argument.That.IsValid(typeDefinition, typeDefinition.IsGenericTypeDefinition);

    if (type.IsInterface)
      return typeDefinition.IsInterface && typeDefinition == type.GetGenericTypeDefinition();
    if (typeDefinition.IsInterface)
      return type.GetInterfaces()
        .Any(iface => iface.IsGenericType && typeDefinition == iface.GetGenericTypeDefinition());
    for (var t = type; t is not null; t = t.BaseType)
      if (t.IsGenericType && typeDefinition == t.GetGenericTypeDefinition())
        return true;
    return false;
  }

  public static Type[][] GetMadeOfArguments(this Type type, Type typeDefinition)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNull(typeDefinition);

    List<Type[]> result = new();
    if (type.IsInterface)
    {
      if (typeDefinition.IsInterface && typeDefinition == type.GetGenericTypeDefinition())
        result.Add(type.GetGenericArguments());
    }
    else if (typeDefinition.IsInterface)
    {
      result.AddRange(type.GetInterfaces()
        .Where(iface => iface.IsGenericType && typeDefinition == iface.GetGenericTypeDefinition())
        .Select(iface => iface.GetGenericArguments()));
    }
    else
    {
      for (var t = type; t is not null; t = t.BaseType)
        if (t.IsGenericType && typeDefinition == t.GetGenericTypeDefinition())
        {
          result.Add(t.GetGenericArguments());
          break;
        }
    }
    return result.ToArray();
  }

  public static Type MakeNullable(this Type type)
  {
    Argument.That.NotNull(type);
    Argument.That.IsValid(type, type.IsValueType);

    return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) ? type : typeof(Nullable<>).MakeGenericType(type);
  }

  public static Type NullableArgument(this Type type)
  {
    Argument.That.NotNull(type);
    Argument.That.IsValid(type, type.IsGenericType && !type.IsGenericTypeDefinition && type.GetGenericTypeDefinition() == typeof(Nullable<>));

    return type.GetGenericArguments()[0];
  }

  public static int GetInheritanceLevel(this Type type, Type baseType)
  {
    Argument.That.NotNull(type);
    Argument.That.NotNull(baseType);

    var depth = 0;
    var currType = type;
    for (; currType != null && currType != baseType; depth++)
      currType = currType.BaseType;
    return currType != null ? depth : -1;
  }

  public static bool CanCreateParameterless(this Type type)
  {
    Argument.That.NotNull(type);

    return type.IsValueType || !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) is not null;
  }
}

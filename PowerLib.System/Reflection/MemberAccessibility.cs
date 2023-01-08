using System;

namespace PowerLib.System.Reflection;

[Flags]
public enum MemberAccessibility
{
  None = 0x0,
  IgnoreCase = 0x1,
  DeclaredOnly = 0x2,
  FlattenHierarchy = 0x4,
  Family = 0x10,
  Assembly = 0x20,
  FamilyOrAssembly = 0x40,
  FamilyAndAssembly = 0x80,
  Private = 0x100,
  Public = 0x200,
  NonPublic = 0x1F0,
  AnyAccess = 0x3F0,
  MemberAccessMask = 0x3F0,
}
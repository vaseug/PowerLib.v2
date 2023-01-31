
namespace PowerLib.System.Reflection;

internal enum ReflectionMessage
{
  No = 0,
  FieldNotFound,
  PropertyNotFound,
  MethodNotFound,
  ConstructorNotFound,
  EventNotFound,
  FieldAmbiguousMatch,
  PropertyAmbiguousMatch,
  MethodAmbiguousMatch,
  ConstructorAmbiguousMatch,
  EventAmbiguousMatch,
  NotAllTypeGenericArgumentsAreDefined,
  NotAllMethodGenericArgumentsAreDefined,
}

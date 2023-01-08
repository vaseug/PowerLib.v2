using System;

namespace PowerLib.System.ComponentModel
{
	[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	public sealed class AtomicEnumAttribute : Attribute
	{ }
}

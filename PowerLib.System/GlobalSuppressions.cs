// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Naming", "CA1721:Property names should not match get methods", Justification = "By design", Scope = "module")]
[assembly: SuppressMessage("Design", "CA1043:Use integral or string argument for indexers", Justification = "By design", Scope = "namespaceanddescendants", Target = "~N:PowerLib.System.Accessors")]
[assembly: SuppressMessage("Design", "CA1044:Properties should not be write only", Justification = "By design", Scope = "namespaceanddescendants", Target = "~N:PowerLib.System.Accessors")]
[assembly: SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "By design", Scope = "module")]
[assembly: SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "By design", Scope = "module")]
[assembly: SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "By design", Scope = "module")]
[assembly: SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "By design", Scope = "module")]
[assembly: SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "By design", Scope = "module")]
[assembly: SuppressMessage("Performance", "CA1813:Avoid unsealed attributes", Justification = "By design", Scope = "module")]
[assembly: SuppressMessage("Design", "CA1019:Define accessors for attribute arguments", Justification = "By design", Scope = "module")]
[assembly: SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "By design", Scope = "module")]
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "By design", Scope = "module")]
[assembly: SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "By design", Scope = "module")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity", Justification = "By design", Scope = "module")]
[assembly: SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "By design", Scope = "module")]
[assembly: SuppressMessage("Reliability", "CA2008:Do not create tasks without passing a TaskScheduler", Justification = "By design", Scope = "module")]

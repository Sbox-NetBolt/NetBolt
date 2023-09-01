using System;

namespace NetBolt.Attributes;

[AttributeUsage( AttributeTargets.All, AllowMultiple = false, Inherited = true )]
public sealed class ServerOnlyAttribute : Attribute
{
}

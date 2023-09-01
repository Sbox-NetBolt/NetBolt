using System;

namespace NetBolt.Attributes;

[AttributeUsage( AttributeTargets.Constructor, AllowMultiple = false, Inherited = true )]
public sealed class ForReplicationAttribute : Attribute
{
}

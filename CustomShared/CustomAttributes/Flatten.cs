using System;

namespace CustomShared.CustomAttributes;

/// signal to unnest when serializing
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class Flatten : Attribute
{
}
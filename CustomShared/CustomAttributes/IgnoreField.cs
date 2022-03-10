using System;

namespace CustomShared.CustomAttributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class IgnoreField : Attribute
{
}
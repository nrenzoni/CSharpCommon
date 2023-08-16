using System;
using System.Collections;
using System.Collections.Generic;

namespace CustomShared;

public static class TypeExtensions
{
    public static bool IsDictionaryType(this Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);

    public static bool IsCollectionType(this Type type)
    {
        return (type.GetInterface(nameof(ICollection)) != null);
    }
    
    public static bool IsSimple(this Type type)
    {
        return type.IsPrimitive 
               || type == typeof(string);
    }

}
using System;
using System.Collections.Generic;
using System.Linq;

namespace CustomShared;

public static class EnumExtensions
{
    public static List<string> GetEnumTypesAsStrList(
        this Type enumType)
    {
        if (!enumType.IsEnum)
            throw new Exception(
                $"Cannot run {nameof(GetEnumTypesAsStrList)} on type {enumType} since it's not an enum.");

        return Enum.GetNames(enumType).ToList();
    }


    public static List<string> GetEnumTypesAsStrList<T>()
        where T : Enum
        => Enum.GetNames(typeof(T)).ToList();
}
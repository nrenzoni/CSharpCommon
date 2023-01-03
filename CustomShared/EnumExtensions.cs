using System;
using System.Collections.Generic;
using System.Linq;

namespace CustomShared;

public class EnumExtensions
{
    public static List<string> GetEnumTypesAsStrList<T>()
        where T : Enum
        => Enum.GetNames(typeof(T)).ToList();
}

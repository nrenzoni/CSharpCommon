using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using CustomShared.CustomAttributes;
using log4net;

namespace CustomShared;

public static class ReflectionUtils
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(ReflectionUtils));

    public static IEnumerable<string> GetPropertyNamesNonRecursive(
        Type type,
        bool withGetter = true,
        bool withSetter = true)
    {
        if (!withGetter
            && !withSetter)
            throw new ArgumentException();

        var props =
            type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .OrderBy(p => p.Name)
                .Where(
                    x =>
                    {
                        if (withGetter ^ withSetter)
                        {
                            return withGetter
                                ? x.CanRead
                                : x.CanWrite;
                        }

                        return x.CanRead && x.CanWrite;
                    });

        foreach (var propertyInfo in props)
        {
            yield return propertyInfo.Name;
        }
    }

    // only used in tests
    public static IEnumerable<PropertyInfo> GetPropertyInfoEnumerable(
        Type type,
        bool returnFlattened = false)
    {
        var props = type.GetProperties()
            .OrderBy(p => p.Name);

        List<PropertyInfo> returnCollection = new List<PropertyInfo>();

        foreach (var propertyInfo in props)
        {
            if (Attribute.IsDefined(
                    propertyInfo,
                    typeof(CustomAttributes.IgnoreField)))
                continue;

            if (returnFlattened
                && Attribute.IsDefined(
                    propertyInfo,
                    typeof(CustomAttributes.Flatten)))
            {
                var inner =
                    GetPropertyInfoEnumerable(
                        propertyInfo.PropertyType,
                        true);
                returnCollection.AddRange(inner);
                continue;
            }

            returnCollection.Add(propertyInfo);
        }

        return returnCollection;
    }

    public static (IReadOnlyList<string> matchingValid, IReadOnlyList<string> notMatched) GetValidColumnNames(
        this IReadOnlyList<string> columnsToVerify,
        IReadOnlyList<string> validColumnNames)
    {
        var validColumnsHashSet = new HashSet<string>(validColumnNames);

        HashSet<string> matchingValid = new();
        HashSet<string> notMatched = new();

        foreach (var s in columnsToVerify)
        {
            if (validColumnsHashSet.Contains(s))
            {
                matchingValid.Add(s);
                continue;
            }

            var colNameWithLeadingC = $"c_{s}";

            if (validColumnsHashSet.Contains(colNameWithLeadingC))
            {
                matchingValid.Add(colNameWithLeadingC);
                continue;
            }

            var underscoreCase = s.CamelCaseToUnderscoreCase();

            if (validColumnsHashSet.Contains(underscoreCase))
            {
                matchingValid.Add(underscoreCase);
                continue;
            }

            notMatched.Add(s);
        }

        return (matchingValid.ToList(), notMatched.ToList());
    }

    public static IReadOnlyList<string> GetColumnNames(
        this Type type,
        HashSet<string> skipFields = null,
        bool ignoreNested = false)
    {
        var inst = Activator.CreateInstance(type);

        var returnColumnNames = new List<string>();

        var fieldNames = inst.GetFields(
                skipFields: skipFields,
                ignoreNested: ignoreNested)
            .Keys;

        foreach (var rawColumn in fieldNames)
        {
            var column = rawColumn;
            if (column.StartsWith("c_"))
            {
                returnColumnNames.Add(column);
                continue;
            }

            returnColumnNames.Add(column.CamelCaseToUnderscoreCase());
        }

        return returnColumnNames;
    }

    public static object[] GetValues(
        this object obj)
        => obj.GetFields().Cast<DictionaryEntry>().Select(x => x.Value).ToArray();

    /// obj can be either a class instance or a Type
    public static SortedDictionary<string, object> GetFields(
        this object obj,
        bool useCustomDbColumnName = false,
        HashSet<string> skipFields = null,
        bool ignoreNested = false)
    {
        var type =
            obj as Type ?? obj.GetType();

        var props = type.GetProperties()
            .OrderBy(p => p.Name); // ordering for consistency

        SortedDictionary<string, object> returnCollection = new();

        foreach (var propertyInfo in props)
        {
            if (skipFields?.Contains(propertyInfo.Name) is true)
                continue;

            if (Attribute.IsDefined(
                    propertyInfo,
                    typeof(CustomAttributes.IgnoreField)))
                continue;

            if (Attribute.IsDefined(
                    propertyInfo,
                    typeof(CustomAttributes.Flatten)))
            {
                if (ignoreNested)
                    continue;

                // check if inner value is null
                var valToRecurse =
                    obj is Type
                        ? propertyInfo.PropertyType
                        : propertyInfo.GetValue(obj) ?? propertyInfo.PropertyType;
                var inner =
                    GetFields(
                        valToRecurse,
                        useCustomDbColumnName);
                foreach (var (key, value) in inner)
                {
                    if (returnCollection.ContainsKey(key))
                        throw new Exception("Outer object contains a field with same name as a nested field!");
                    returnCollection[key] = value;
                }

                continue;
            }

            string propertyName;
            if (useCustomDbColumnName &&
                Attribute.IsDefined(
                    propertyInfo,
                    typeof(CustomAttributes.CustomDbColumnName)))
            {
                var customDbColumnNameAttribute =
                    (CustomAttributes.CustomDbColumnName)propertyInfo.GetCustomAttribute(
                        typeof(CustomAttributes.CustomDbColumnName));
                propertyName = customDbColumnNameAttribute.ColumnName;
            }
            else
            {
                propertyName = propertyInfo.Name;
            }

            returnCollection.Add(
                propertyName,
                obj is Type
                    ? null
                    : propertyInfo.GetValue(obj));
        }

        return returnCollection;
    }

    public static List<PropertyInfo> GetFlattenedProperties(
        this object obj)
    {
        var type = obj is Type
            ? (Type)obj
            : obj.GetType();
        var props = type.GetProperties()
            .OrderBy(p => p.Name); // ordering for consistency

        List<PropertyInfo> returnProperties = new();

        foreach (var propertyInfo in props)
        {
            if (Attribute.IsDefined(
                    propertyInfo,
                    typeof(CustomAttributes.IgnoreField)))
                continue;

            if (Attribute.IsDefined(
                    propertyInfo,
                    typeof(CustomAttributes.Flatten)))
            {
                // check if inner value is null
                var valToRecurse =
                    obj is Type
                        ? propertyInfo.PropertyType
                        : propertyInfo.GetValue(obj) ?? propertyInfo.PropertyType;

                var innerProperties =
                    GetFlattenedProperties(valToRecurse);

                returnProperties.AddRange(innerProperties);

                continue;
            }

            returnProperties.Add(propertyInfo);
        }

        return returnProperties;
    }

    public static string GetCustomDbColumnNameIfDefined(
        this PropertyInfo propertyInfo)
    {
        var customDbColumnName = propertyInfo.GetAttribute<CustomDbColumnName>();
        if (customDbColumnName == null)
            return propertyInfo.Name;
        return customDbColumnName.ColumnName;
    }

    public static TAttr GetAttribute<TAttr>(
        this PropertyInfo propertyInfo) where TAttr : Attribute
    {
        return (TAttr)propertyInfo.GetCustomAttribute(typeof(TAttr));
    }

    public static string GetRealTypeName(
        this Type t)
    {
        if (!t.IsGenericType)
            return t.Name;

        StringBuilder sb = new StringBuilder();
        sb.Append(
            t.Name.Substring(
                0,
                t.Name.IndexOf('`')));
        sb.Append('<');
        bool appendComma = false;
        foreach (Type arg in t.GetGenericArguments())
        {
            if (appendComma) sb.Append(',');
            sb.Append(GetRealTypeName(arg));
            appendComma = true;
        }

        sb.Append('>');
        return sb.ToString();
    }

    public static Boolean IsAnonymousType(
        this Type type)
    {
        Boolean hasCompilerGeneratedAttribute = type.GetCustomAttributes(
                typeof(CompilerGeneratedAttribute),
                false)
            .Any();
        Boolean nameContainsAnonymousType = type.FullName.Contains("AnonymousType");
        Boolean isAnonymousType = hasCompilerGeneratedAttribute && nameContainsAnonymousType;

        return isAnonymousType;
    }

    public static Dictionary<string, object> AnonymousToDictionary(
        this object obj)
    {
        var type = obj.GetType();
        if (!type.IsAnonymousType())
            throw new Exception("AnonymousToDictionary() only works on anonymous objects.");

        var props = type.GetProperties();

        var dict = new Dictionary<string, object>();

        foreach (var propertyInfo in props)
        {
            var value = propertyInfo.GetValue(
                obj,
                null);

            if (value?.GetType().IsAnonymousType() == true)
            {
                value = value.AnonymousToDictionary();
            }

            dict.Add(
                propertyInfo.Name,
                value);
        }

        return dict;
    }

    // https://stackoverflow.com/a/2473675/3262950
    public static Type GetNullableType(
        this Type sourceType)
    {
        if (sourceType == null)
        {
            // Throw System.ArgumentNullException or return null, your preference
        }
        else if (sourceType == typeof(void))
        {
            // Special Handling - known cases where Exceptions would be thrown
            return null; // There is no Nullable version of void
        }

        return !sourceType.IsValueType
               || (sourceType.IsGenericType
                   && sourceType.GetGenericTypeDefinition() == typeof(Nullable<>))
            ? sourceType
            : typeof(Nullable<>).MakeGenericType(sourceType);
    }

    private static IEnumerable<Type> GetLoadableTypes(
        this Assembly assembly)
    {
        if (assembly == null) throw new ArgumentNullException(nameof(assembly));
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t != null);
        }
    }

    public static IEnumerable<Type> GetTypesWithInterface(
        Type type)
    {
        var loadableTypes =
            Assembly.GetAssembly(type).GetLoadableTypes();

        return loadableTypes.Where(type.IsAssignableFrom).ToList();
    }

    public static IEnumerable<Type> GetTypesWithInterfaceMultiType(
        Type candidateType,
        IEnumerable<Assembly> assemblies)
    {
        var loadableTypes =
            assemblies.SelectMany(a => a.GetLoadableTypes());

        return loadableTypes.Where(candidateType.IsAssignableFrom)
            .ToList();
    }

    public static HashSet<string> GetPropertyNames(
        Type type,
        HashSet<Type> recursiveIgnoreTypes = null)
    {
        recursiveIgnoreTypes ??= DefaultRecursiveIgnoreTypes;

        var props = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(p => p.Name);

        var propNames = new HashSet<string>();

        foreach (var propertyInfo in props)
        {
            if (recursiveIgnoreTypes?.Contains(propertyInfo.PropertyType) is true)
            {
                propNames.Add(propertyInfo.Name);
                continue;
            }

            var propertyNamesAndValues =
                GetPropertyNames(
                    propertyInfo.PropertyType);

            if (propertyNamesAndValues.Any())
                foreach (var value in propertyNamesAndValues)
                {
                    propNames.Add(value);
                }
            else
            {
                propNames.Add(propertyInfo.Name);
            }
        }

        return propNames;
    }

    private static readonly HashSet<Type> DefaultRecursiveIgnoreTypes = new()
    {
        typeof(decimal),
        typeof(DecimalWithInf)
    };

    public static Dictionary<string, object>
        GetPropertyNamesAndValues(
            object inObj,
            bool ignoreNull,
            HashSet<Type> recursiveIgnoreTypes = null,
            bool withSetter = true,
            bool withGetter = true)
    {
        if (!withGetter
            && !withSetter)
            throw new ArgumentException();

        recursiveIgnoreTypes ??= DefaultRecursiveIgnoreTypes;

        var type =
            inObj as Type ?? inObj.GetType();

        var props1 = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .AsEnumerable();

        if (withGetter ^ withSetter)
        {
            props1 =
                withGetter
                    ? props1.Where(x => x.CanRead)
                    : props1.Where(x => x.CanWrite);
        }

        var props = props1
            .OrderBy(p => p.Name);

        var propNamesAndVals = new Dictionary<string, object>();

        foreach (var propertyInfo in props)
        {
            object val;
            if (inObj is Type)
                val = propertyInfo.PropertyType;
            else
            {
                if (propertyInfo.CanRead)
                    val = propertyInfo.GetValue(inObj);
                else
                    continue;
            }

            if (recursiveIgnoreTypes?.Contains(propertyInfo.PropertyType) is true)
            {
                AddPropertyToDictHelper(
                    val,
                    propertyInfo.Name,
                    ignoreNull,
                    propNamesAndVals);
                continue;
            }

            var propertyNamesAndValues =
                GetPropertyNamesAndValues(
                    val,
                    ignoreNull,
                    recursiveIgnoreTypes);

            if (propertyNamesAndValues.Any())
                foreach (var (key, value) in propertyNamesAndValues)
                {
                    propNamesAndVals[key] = value;
                }
            else
            {
                AddPropertyToDictHelper(
                    val,
                    propertyInfo.Name,
                    ignoreNull,
                    propNamesAndVals);
            }
        }

        return propNamesAndVals;
    }

    private static void AddPropertyToDictHelper(
        object val,
        string propertyName,
        bool ignoreNull,
        Dictionary<string, object> propNamesAndVals)
    {
        if (val is null)
        {
            if (ignoreNull)
                return;
            val = "null";
        }

        propNamesAndVals[propertyName] = val;
    }
}

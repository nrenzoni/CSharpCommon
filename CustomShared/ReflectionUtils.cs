using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CustomShared.CustomAttributes;
using log4net;

namespace CustomShared
{
    public static class ReflectionUtils
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ReflectionUtils));

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
                if (Attribute.IsDefined(propertyInfo, typeof(CustomAttributes.IgnoreField)))
                    continue;

                if (returnFlattened
                    && Attribute.IsDefined(propertyInfo, typeof(CustomAttributes.Flatten)))
                {
                    var inner =
                        GetPropertyInfoEnumerable(propertyInfo.PropertyType, true);
                    returnCollection.AddRange(inner);
                    continue;
                }

                returnCollection.Add(propertyInfo);
            }

            return returnCollection;
        }

        public static IReadOnlyList<string> GetColumnNames(this Type type)
        {
            var inst = Activator.CreateInstance(type);

            var returnColumnNames = new List<string>();

            foreach (var rawColumn in inst.GetFields().Keys)
            {
                var column = ((string)rawColumn);
                if (column.StartsWith("c_"))
                {
                    returnColumnNames.Add(column);
                    continue;
                }

                returnColumnNames.Add(column.CamelCaseToUnderscoreCase());
            }

            return returnColumnNames;
        }


        public static object[] GetValues(this object obj)
            => obj.GetFields().Cast<DictionaryEntry>().Select(x => x.Value).ToArray();
        
        /// obj can be either a class instance or a Type
        public static SortedDictionary<string,object> GetFields(
            this object obj,
            bool useCustomDbColumnName = false)
        {
            var type = obj is Type ? (Type)obj : obj.GetType();
            var props = type.GetProperties()
                .OrderBy(p => p.Name); // ordering for consistency

            SortedDictionary<string,object> returnCollection = new();

            foreach (var propertyInfo in props)
            {
                if (Attribute.IsDefined(propertyInfo, typeof(CustomAttributes.IgnoreField)))
                    continue;

                if (Attribute.IsDefined(propertyInfo, typeof(CustomAttributes.Flatten)))
                {
                    // check if inner value is null
                    var valToRecurse =
                        obj is Type
                            ? propertyInfo.PropertyType
                            : propertyInfo.GetValue(obj) ?? propertyInfo.PropertyType;
                    var inner =
                        GetFields(valToRecurse, useCustomDbColumnName);
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
                    Attribute.IsDefined(propertyInfo, typeof(CustomAttributes.CustomDbColumnName)))
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

                returnCollection.Add(propertyName, obj is Type ? null : propertyInfo.GetValue(obj));
            }

            return returnCollection;
        }

        public static List<PropertyInfo> GetFlattenedProperties(this object obj)
        {
            var type = obj is Type ? (Type)obj : obj.GetType();
            var props = type.GetProperties()
                .OrderBy(p => p.Name); // ordering for consistency

            List<PropertyInfo> returnProperties = new();

            foreach (var propertyInfo in props)
            {
                if (Attribute.IsDefined(propertyInfo, typeof(CustomAttributes.IgnoreField)))
                    continue;

                if (Attribute.IsDefined(propertyInfo, typeof(CustomAttributes.Flatten)))
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

        public static string GetCustomDbColumnNameIfDefined(this PropertyInfo propertyInfo)
        {
            var customDbColumnName = propertyInfo.GetAttribute<CustomDbColumnName>();
            if (customDbColumnName == null)
                return propertyInfo.Name;
            return customDbColumnName.ColumnName;
        }
        
        public static TAttr GetAttribute<TAttr>(this PropertyInfo propertyInfo) where TAttr : Attribute
        {
            return (TAttr)propertyInfo.GetCustomAttribute(typeof(TAttr));
        }
    }
}
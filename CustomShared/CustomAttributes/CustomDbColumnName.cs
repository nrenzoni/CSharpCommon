using System;

namespace CustomShared.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class CustomDbColumnName : Attribute
    {
        public string ColumnName { get; }
        
        public CustomDbColumnName(string columnName)
        {
            ColumnName = columnName;
        }
    }
}
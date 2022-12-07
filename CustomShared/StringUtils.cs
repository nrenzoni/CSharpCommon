using System.Linq;

namespace CustomShared;

public static class StringUtils
{
    public static string CamelCaseToUnderscoreCase(this string inStr)
    {
        return string.Concat(inStr.Select(
            (x, i) => i > 0 && char.IsUpper(x)
                ? "_" + char.ToLower(x)
                : char.ToLower(x).ToString()));
    }

    public static string UnderscoreCaseToCamelCase(this string inStr)
    {
        return string.Concat(inStr.Select(
            (x, i) => i == 0 || inStr[i - 1] == '_'
                ? char.ToUpper(x)
                : x)).Replace("_", "");
    }
    
    public static string SurroundWithSingleQuotes(
        this string str)
        => $"'{str}'";
}
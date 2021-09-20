using System.Linq;

namespace CustomShared
{
    public static class StringUtils
    {
        public static string CamelCaseToUnderscoreCase(this string inStr)
        {
            return string.Concat(inStr.Select(
                (x, i) => i > 0 && char.IsUpper(x)
                ? "_" + char.ToLower(x).ToString()
                : char.ToLower(x).ToString()));
        }
    }
}

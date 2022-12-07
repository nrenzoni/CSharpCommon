using System;
using System.Collections.Generic;

namespace CustomShared;

public static class CommandLineParsingExtensions
{
    public static Dictionary<string, string> ParseToDictionary(
        this string str)
    {
        Dictionary<string, string> returnDict = new();

        foreach (var keyValSet in str.Split(new[] {',',';'}))
        {
            var keyVal = keyValSet.Split(
                "=",
                2);

            if (keyVal.Length != 2)
                throw new Exception();

            returnDict[keyVal[0]] = keyVal[1];
        }

        return returnDict;
    }
}

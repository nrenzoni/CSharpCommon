using System;
using System.IO;

namespace CustomShared;

public static class DotEnvLoader
{
    public static bool Load(string envFileName = ".env")
    {
        string filePath;

        try
        {
            filePath = PathUtils.FindPath(envFileName);
        }
        catch
        {
            Console.WriteLine(
                $"Could not find any matching {envFileName} files starting in path {Directory.GetParent(envFileName)}!");
            return false;
        }


        Console.WriteLine($"Loading Env file from {Path.GetFullPath(filePath)}.");

        foreach (var line in File.ReadAllLines(filePath))
        {
            var lineTrimmed = line.Trim();
            if (lineTrimmed.StartsWith('#'))
                continue;

            var parts = lineTrimmed.Split(
                '=',
                2,
                StringSplitOptions.RemoveEmptyEntries);

            switch (parts.Length)
            {
                case < 2:
                    continue;
                case > 2:
                    throw new Exception($"Env line contains more parts than just key=value: [{lineTrimmed}]");
            }

            var key = parts[0];
            var val = parts[1];
            val = val.Trim('"');

            Environment.SetEnvironmentVariable(key, val);
        }

        return true;
    }
}
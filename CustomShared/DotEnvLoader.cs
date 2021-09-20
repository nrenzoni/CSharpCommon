using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomShared
{
    public static class DotEnvLoader
    {
        public static bool Load(string envFileName = ".env")
        {
            string filePath = Path.GetFullPath(envFileName);
            for (int i = 0; i < 5; i++)
            {
                if (File.Exists(filePath))
                    break;

                string parent = Directory.GetParent(filePath).Parent.ToString();
                filePath = Path.Combine(parent, envFileName);
            }

            if (!File.Exists(filePath))
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
}
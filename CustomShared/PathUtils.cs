using System;
using System.IO;

namespace CustomShared;

public class PathUtils
{
    public static string FindPath(string filename)
    {
        var filePath = Path.GetFullPath(filename);
        for (int i = 0; i < 5; i++)
        {
            if (File.Exists(filePath))
                break;

            string parent = Directory.GetParent(filePath).Parent.ToString();
            filePath = Path.Combine(parent, filename);
        }

        if (!File.Exists(filePath))
        {
            throw new Exception(
                $"Could not find any matching {filename} files starting in path {Directory.GetParent(filename)}.");
        }

        return filePath;
    }
}
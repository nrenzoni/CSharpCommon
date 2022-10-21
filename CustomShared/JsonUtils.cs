using log4net;
using Newtonsoft.Json;
using System.IO;

namespace CustomShared;

public class JsonUtils
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(JsonUtils));

    public static void SerializeToJsonFile(object obj, string filename)
    {
        filename += ".json";

        var serialized = JsonConvert.SerializeObject(obj, Formatting.Indented);

        Log.Info("Writing serialized Json to file: " + Path.GetFullPath(filename));
        File.WriteAllText(filename, serialized);
    }

    public static T DeserializeJson<T>(string serialized)
    {
        var settings = new JsonSerializerSettings()
        {
            MissingMemberHandling = MissingMemberHandling.Error
        };
        return JsonConvert.DeserializeObject<T>(serialized, settings);
    }
}
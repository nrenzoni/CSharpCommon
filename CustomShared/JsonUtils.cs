using log4net;
using Newtonsoft.Json;
using System.IO;
using NodaTime;
using NodaTime.Serialization.JsonNet;
using NodaTime.TimeZones;

namespace CustomShared;

public class JsonUtils
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(JsonUtils));

    private const string jsonExt = ".json";

    private static readonly JsonSerializerSettings JsonSerializerSettings;

    static JsonUtils()
    {
        JsonSerializerSettings = new();
        JsonSerializerSettings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        JsonSerializerSettings.MissingMemberHandling = MissingMemberHandling.Error;
    }
    
    public static string Serialize(
        object obj)
    {
        return JsonConvert.SerializeObject(
            obj,
            Formatting.Indented,
            JsonSerializerSettings);
    }

    public static void SerializeToJsonFile(
        object obj,
        string filename)
    {
        var serialized = Serialize(obj);

        WriteJson(
            serialized,
            filename);
    }

    public static void WriteJson(
        string serializedJson,
        string filename)
    {
        if (!filename.EndsWith(jsonExt))
            filename += jsonExt;

        Log.Info("Writing serialized Json to file: " + Path.GetFullPath(filename));

        File.WriteAllText(
            filename,
            serializedJson);
    }

    public static T DeserializeJson<T>(
        string serialized)
    {
        return JsonConvert.DeserializeObject<T>(
            serialized,
            JsonSerializerSettings);
    }
}

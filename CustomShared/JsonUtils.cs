using System;
using System.Collections;
using System.Collections.Generic;
using log4net;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Serialization.JsonNet;

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
        JsonSerializerSettings.TypeNameHandling = TypeNameHandling.Auto;

        JsonSerializerSettings.Converters.Add(new ComplexDictionaryKeyCustomConverter());

        JsonSerializerSettings.Converters.Add(new NestedDictionaryConverter());
    }

    private static JsonSerializerSettings GetJsonSerializerSettings(
        params JsonConverter[] extraConverters)
    {
        if (extraConverters == null)
            return JsonSerializerSettings;

        var jsonSerializerSettings = new JsonSerializerSettings(JsonSerializerSettings);
        foreach (var jsonConverter in extraConverters)
        {
            jsonSerializerSettings.Converters.Add(jsonConverter);
        }

        return jsonSerializerSettings;
    }

    public static string Serialize(
        object obj,
        params JsonConverter[] extraConverters)
    {
        var jsonSerializerSettings = GetJsonSerializerSettings(extraConverters);

        return JsonConvert.SerializeObject(
            obj,
            Formatting.Indented,
            jsonSerializerSettings);
    }

    public static void SerializeToJsonFile(
        object obj,
        string filename,
        params JsonConverter[] extraConverters)
    {
        var serialized = Serialize(
            obj,
            extraConverters);

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
        string serialized,
        params JsonConverter[] extraConverters)
    {
        var jsonSerializerSettings = GetJsonSerializerSettings(extraConverters);

        return JsonConvert.DeserializeObject<T>(
            serialized,
            jsonSerializerSettings);
    }
}

// json dictionary key must be string. this class solves issue of complex key type by storing dictionary as list of Key/Value.
public class ComplexDictionaryKeyCustomConverter
    : JsonConverter
{
    public override void WriteJson(
        JsonWriter writer,
        object value,
        JsonSerializer serializer)
    {
        if (value is not IDictionary dict)
            throw new Exception();

        var array = new JArray();

        foreach (DictionaryEntry o in dict)
        {
            var fromObject = JObject.FromObject(
                o,
                serializer);
            array.Add(fromObject);
        }

        array.WriteTo(writer);
    }

    public override object ReadJson(
        JsonReader reader,
        Type objectType,
        object existingValue,
        JsonSerializer serializer)
    {
        var dict = (IDictionary)Activator.CreateInstance(objectType);

        var keyType = objectType.GenericTypeArguments[0];
        var valueType = objectType.GenericTypeArguments[1];

        var readFrom = JToken.ReadFrom(reader);

        if (readFrom is not JArray array)
            throw new Exception();

        foreach (var jToken in array)
        {
            var key = jToken["Key"].ToString();
            var value = jToken["Value"];

            var keyVal = serializer.Deserialize(
                new StringReader(key),
                keyType);

            var valVal = serializer.Deserialize(
                new JTokenReader(value),
                valueType);

            if (keyVal == null)
                throw new Exception();

            dict[keyVal] = valVal;
        }

        /*foreach (var (key, value) in jObject)
        {
            var keyVal = serializer.Deserialize(
                new StringReader(key),
                keyType);

            var valVal = serializer.Deserialize(new JTokenReader(value));

            if (keyVal == null)
                throw new Exception();

            dict[keyVal] = valVal;
        }*/

        return dict;
    }

    // only handle dictionaries with complex key type, otherwise uses default dictionary converter 
    public override bool CanConvert(
        Type objectType)
    {
        return objectType.IsDictionaryType()
                // check key type
               && !objectType.GenericTypeArguments[0].IsSimple();
    }
}

// allow nested conversion to dictionary
class NestedDictionaryConverter : CustomCreationConverter<IDictionary<string, object>>
{
    public override IDictionary<string, object> Create(
        Type objectType)
    {
        return new Dictionary<string, object>();
    }

    public override bool CanConvert(
        Type objectType)
    {
        // in addition to handling IDictionary<string, object>
        // we want to handle the deserialization of dict value
        // which is of type object
        return objectType == typeof(object) || base.CanConvert(objectType);
    }

    public override object ReadJson(
        JsonReader reader,
        Type objectType,
        object existingValue,
        JsonSerializer serializer)
    {
        return reader.TokenType switch
        {
            JsonToken.StartObject or JsonToken.Null
                => base.ReadJson(
                    reader,
                    objectType,
                    existingValue,
                    serializer),

            //if it's an array serialize it as a list of dictionaries
            JsonToken.StartArray
                => serializer.Deserialize(
                    reader,
                    typeof(List<Dictionary<string, object>>)),

            // if the next token is not an object
            // then fall back on standard deserializer (strings, numbers etc.)
            _ => serializer.Deserialize(reader)
        };
    }
}
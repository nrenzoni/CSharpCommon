using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CustomShared;

public static class ConfigVariableUtils
{
    public static T LoadFromEnv<T>() where T : IConfigVariables, new()
    {
        var builtEnvironmentVariables = new T();

        var propertyInfos =
            typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var propertyInfo in propertyInfos)
        {
            var envName = ConvertPropertyNameToEnvName(propertyInfo.Name);
            var value = GetEnvironmentVariable(envName);
            object parsedVal;
            if (propertyInfo.PropertyType == typeof(string))
                parsedVal = value;
            else if (propertyInfo.PropertyType == typeof(uint))
                parsedVal = uint.Parse(value);
            else if (propertyInfo.PropertyType == typeof(string[]))
                parsedVal = value.Split(";",
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            else
                throw new Exception(
                    $"EnvironmentVariables property type {propertyInfo.PropertyType} " +
                    $"for property {propertyInfo.Name} is not supported. " +
                    $"Implement here in code!");

            propertyInfo.SetValue(builtEnvironmentVariables, parsedVal);
        }

        return builtEnvironmentVariables;
    }

    public static string ConvertPropertyNameToEnvName(
        string propertyName)
    {
        var val = propertyName.CamelCaseToUnderscoreCase();
        return val.ToUpper();
    }

    public static string GetEnvironmentVariable(
        string name,
        bool throwExpIfNull = true)
    {
        var val = Environment.GetEnvironmentVariable(name);
        if (throwExpIfNull && val == null)
            throw new Exception($"Env variable {name} is not assigned. (Perhaps env file was not loaded.");
        return val;
    }
}

public interface IConfigVariables
{
}

// ReSharper disable UnassignedGetOnlyAutoProperty UnusedMember.Global UnusedAutoPropertyAccessor.Local AutoPropertyCanBeMadeGetOnly.Local
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public class ConfigVariables : IConfigVariables
{
    private static readonly ConfigVariables _prodInstance = ConfigVariableUtils.LoadFromEnv<ConfigVariables>();
    private static readonly ConfigVariables _testInstance = MakeTestConfigVariables<ConfigVariables>();

    public static ConfigVariables Instance => IsTestGlobalChecker.IsTest
        ? _testInstance
        : _prodInstance;

    public static ConfigVariables TestInstance => _testInstance;

    public string MarketDayClosedListDir { get; protected set; }
    public string ProjectBaseDir { get; protected set; }
    public string LogConfigFile { get; protected set; }
    public string TestDataDirectory { get; protected set; }


    public string MongoConn { get; protected set; } = "mongodb://localhost:27017/?compressors=zstd";
    public string MongoTradeIdeasDb { get; protected set; } = "trade_ideas";

    public string MongoLogEntryDb { get; protected set; } = "logs";
    public string MongoTradeIdeasAlertsCollection { get; protected set; } = "trade_ideas_alerts";

    public string MongoLogEntryCollection { get; protected set; } = "ti_retriever";
    public uint MongoSaverBatchSize { get; protected set; } = 10000;
    public uint NMongoSavers { get; protected set; }
    public uint NMongoLogFlushSize { get; protected set; } = 1000;

    public string ClickhouseHost { get; protected set; }
    public string ClickhouseUser { get; protected set; }
    public string ClickhousePassword { get; protected set; }
    public string ClickhouseAlertsTable { get; protected set; } = "ti.alerts";
    public string ClickhouseTopListTable { get; protected set; } = "ti.top_list";

    public string ClickhouseSchemaDirectory { get; protected set; }

    public uint NClickhouseConverters { get; protected set; } = 4;

    public uint ClickhouseSaverBatchSize { get; protected set; } = 10000;

    public string[] RepoBackends { get; protected set; } = { "CLICKHOUSE" };

    protected static T MakeTestConfigVariables<T>() where T : ConfigVariables, new()
    {
        var configVariables = ConfigVariableUtils.LoadFromEnv<T>();
        configVariables.MongoTradeIdeasDb = "trade_ideas_test";
        configVariables.ClickhouseAlertsTable = "ti_test.alerts";
        configVariables.ClickhouseTopListTable = "ti_test.top_list";
        configVariables.MongoLogEntryDb = "logs_test";

        return configVariables;
    }
}

public static class IsTestGlobalChecker
{
    public static bool IsTest { get; set; }
}

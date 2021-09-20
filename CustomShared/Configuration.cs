using System;
using System.Reflection;

namespace CustomShared
{
    // ReSharper disable UnassignedGetOnlyAutoProperty UnusedMember.Global UnusedAutoPropertyAccessor.Local AutoPropertyCanBeMadeGetOnly.Local
    public class ConfigVariables
    {
        private static readonly ConfigVariables _prodInstance = LoadFromEnv();
        private static readonly ConfigVariables _testInstance = MakeTestConfigVariables();
        public static ConfigVariables Instance => IsTestGlobalChecker.IsTest ? _testInstance : _prodInstance;

        public static ConfigVariables TestInstance => _testInstance;

        public string MarketDayClosedListDir { get; init; }
        public string ProjectBaseDir { get; private set; }
        public string LogConfigFile { get; private set; }
        public string TestDataDirectory { get; private set; }
        public string TopListDownloadConfigFile { get; private set; }
        public string TopListSymbolOutputDir { get; private set; }

        public string TradeIdeasUsername { get; private set; }
        public string TradeIdeasPassword { get; private set; }

        public uint MaxParallelAlertRequestDays { get; private set; } = 3;
        public uint NRowDataParsers { get; private set; } = 2;

        public uint MaxParallelTopListRequests { get; private set; } = 10;

        public string MongoConn { get; private set; } = "mongodb://localhost:27017/?compressors=zstd";
        public string MongoTradeIdeasDb { get; private set; } = "trade_ideas";

        public string MongoLogEntryDb { get; private set; } = "logs";
        public string MongoTradeIdeasAlertsCollection { get; private set; } = "trade_ideas_alerts";

        public string MongoLogEntryCollection { get; private set; } = "ti_retriever";
        public uint MongoSaverBatchSize { get; private set; } = 10000;
        public uint NMongoSavers { get; private set; }

        public string ClickhouseHost { get; private set; }
        public string ClickhouseUser { get; private set; }
        public string ClickhousePassword { get; private set; }
        public uint NClickhouseConverters { get; private set; } = 4;
        public string ClickhouseAlertsTable { get; private set; } = "ti.alerts";
        public string ClickhouseTopListTable { get; private set; } = "ti.top_list";
        public string ClickhouseSchemaDirectory { get; private set; }
        public uint ClickhouseSaverBatchSize { get; private set; } = 10000;

        public string[] RepoBackends { get; private set; } = { "CLICKHOUSE" };

        private static ConfigVariables LoadFromEnv()
        {
            var builtEnvironmentVariables = new ConfigVariables();

            var propertyInfos =
                typeof(ConfigVariables).GetProperties(BindingFlags.Public | BindingFlags.Instance);

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

        static string ConvertPropertyNameToEnvName(string propertyName)
        {
            var val = propertyName.CamelCaseToUnderscoreCase();
            return val.ToUpper();
        }

        public static string GetEnvironmentVariable(string name, bool throwExpIfNull = true)
        {
            var val = Environment.GetEnvironmentVariable(name);
            if (throwExpIfNull && val == null)
                throw new Exception($"Env variable {name} is not assigned.");
            return val;
        }

        private static ConfigVariables MakeTestConfigVariables()
        {
            var configVariables = LoadFromEnv();
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
}
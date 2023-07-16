using System;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace CustomShared;

public class MicrosoftConfigurationLoader
{
    public static object GetConfiguration(
        IConfiguration configuration,
        Type type,
        string sectionName = null)
    {
        IConfiguration configurationSection;

        if (sectionName is not null)
        {
            var configSection = configuration.GetSection(sectionName);

            if (!configSection.Exists())
                throw new Exception($"Section {sectionName} of type {type} is not defined in the configuration.");

            configurationSection = configSection;
        }
        else
            configurationSection = configuration;

        return configurationSection.Get(type);
    }

    public static T GetConfiguration<T>(
        IConfiguration configuration,
        string sectionName = null)
    {
        var configurationSection =
            sectionName is not null
                ? configuration.GetSection(sectionName)
                : configuration;

        return configurationSection.Get<T>();
    }

    public static IConfiguration LoadMicrosoftConfiguration(
        params string[] appSettingsFiles)
    {
        var configurationBuilder = new ConfigurationBuilder();

        if (!appSettingsFiles.Any())
            throw new Exception($"Must contain at least 1 {nameof(appSettingsFiles)}.");

        foreach (var s in appSettingsFiles)
        {
            configurationBuilder
                .AddJsonFile(s);
        }

        return configurationBuilder
            .Build();
    }

    public static string FindAppSettingsFile()
    {
        return PathUtils.FindPath("appsettings.json");
    }
}

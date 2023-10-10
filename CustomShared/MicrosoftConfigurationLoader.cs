using System;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace CustomShared;

public static class MicrosoftConfigurationLoader
{
    public static object GetConfiguration(
        IConfiguration configuration,
        Type type,
        string? sectionName,
        bool allowDefault)
    {
        if (sectionName is null)
            return configuration.Get(type);
        var configSection = configuration.GetSection(sectionName);

        if (configSection.Exists())
            return configSection.Get(type);

        if (!allowDefault)
            throw new Exception(
                $"Section {sectionName} of type {type} is not defined or is blank in the configuration.");

        return Activator.CreateInstance(type);
    }

    public static T GetConfiguration<T>(
        this IConfiguration configuration,
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

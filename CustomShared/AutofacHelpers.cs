using System;
using System.Collections.Generic;
using Autofac;
using Microsoft.Extensions.Configuration;

namespace CustomShared;

public static class AutofacHelpers
{
    public record ConfigSection(
        Type ConfigType,
        string? SectionName = null);

    public static void RegisterConfigs(
        ContainerBuilder builder,
        IConfiguration configuration,
        ICollection<ConfigSection> configSectionsToLoad)
    {
        foreach (var configSection in configSectionsToLoad)
        {
            var configSectionName = configSection.SectionName;

            if (configSection.SectionName is null)
            {
                var configTypeName = configSection.ConfigType.Name;
                var lastIndexOfConfig
                    = configTypeName.ToLower()
                        .LastIndexOf(
                            "config",
                            StringComparison.Ordinal);
                configSectionName = configTypeName.Substring(
                    0,
                    lastIndexOfConfig);
            }

            AutofacHelpers.LoadConfigurationsHelper(
                builder,
                configuration,
                configSection.ConfigType,
                configSectionName);
        }
    }

    public static void LoadConfigurationsHelper<T>(
        ContainerBuilder containerBuilder,
        IConfiguration configuration,
        string sectionName)
        where T : class, new()
    {
        var config =
            MicrosoftConfigurationLoader.GetConfiguration<T>(
                configuration,
                sectionName);

        containerBuilder.RegisterInstance(config)
            .As<T>()
            .SingleInstance();
    }

    public static void LoadConfigurationsHelper(
        ContainerBuilder containerBuilder,
        IConfiguration configuration,
        Type configType,
        string sectionName)
    {
        var config =
            MicrosoftConfigurationLoader.GetConfiguration(
                configuration,
                configType,
                sectionName);

        containerBuilder.RegisterInstance(config)
            .As(configType)
            .SingleInstance();
    }
}

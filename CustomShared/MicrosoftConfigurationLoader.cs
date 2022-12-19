using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace CustomShared;

public class MicrosoftConfigurationLoader
{
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
}

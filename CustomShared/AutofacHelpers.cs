using Autofac;
using Microsoft.Extensions.Configuration;

namespace CustomShared;

public static class AutofacHelpers
{
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
}

using System;
using System.IO;
using log4net;
using log4net.Config;
using log4net.Core;

namespace CustomShared
{
    public static class LogConfig
    {
        public static void Load(string logFile)
        {
            // var logConfig = ConfigVariables.Instance.LogConfigFile;
            var logConfig = logFile;
            if (logConfig == null)
            {
                throw new Exception("Log not configured! Set path to config file in env variable: LOG_CONFIG_FILE.");
            }

            var fileConfig = new FileInfo(logConfig);
            XmlConfigurator.Configure(fileConfig);
        }

        public static void LogToConsoleForDebug()
        {
            BasicConfigurator.Configure();
            ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root.Level = Level.Info;
        }
    }
}
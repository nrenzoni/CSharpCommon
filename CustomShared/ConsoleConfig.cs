using System;
using System.Threading;
using log4net;

namespace CustomShared;

public static class ConsoleConfig
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(ConsoleConfig));

    private static readonly CancellationTokenSource _cancelKeyPressCts;
        
    public static CancellationToken CancelKeyPressCt => _cancelKeyPressCts.Token;

    static ConsoleConfig()
    {
        _cancelKeyPressCts = ConfigureCancelKeyPress();
    }

    private static CancellationTokenSource ConfigureCancelKeyPress()
    {
        var cancelKeyPressCts = new CancellationTokenSource();

        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            Log.Info("Caught Ctrl-C, gracefully shutting down...");
            cancelKeyPressCts.Cancel();
        };

        return cancelKeyPressCts;
    }
}
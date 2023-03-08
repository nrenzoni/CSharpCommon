using System;
using System.Threading.Tasks;

namespace CustomShared;

public class ExceptionFuncs
{
    public static async Task<T> RunWithRetries<T>(
        Func<Task<T>> requestFunc,
        string requestFuncName,
        uint? retryCount,
        TimeSpan delayBetweenRetries)
    {
        var remainingTries = retryCount + 1;

        while (remainingTries is null or > 0)
        {
            try
            {
                return await requestFunc();
            }
            catch (Exception _)
            {
                // ignored
            }

            await Task.Delay(delayBetweenRetries);

            remainingTries--;
        }

        throw new Exception($"Max retries made for {requestFuncName}.");
    }

    public static T RunWithRetries<T>(
        Func<T> requestFunc,
        string requestFuncName,
        uint? retryCount,
        TimeSpan delayBetweenRetries)
    {
        var remainingTries = retryCount + 1;

        while (remainingTries is null or > 0)
        {
            try
            {
                return requestFunc();
            }
            catch (Exception _)
            {
                // ignored
            }

            Task.Delay(delayBetweenRetries).Wait();

            remainingTries--;
        }

        throw new Exception($"Max retries made for {requestFuncName}.");
    }
}
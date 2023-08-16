using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace CustomShared;

public class ExceptionFuncs
{
    public static async Task<T> RunWithRetries<T>(
        Func<Task<T>> requestFunc,
        string requestFuncName,
        uint? retryCount = null,
        TimeSpan? delayBetweenRetries = null)
    {
        var remainingTries = retryCount + 1;

        Exception e = null;

        while (remainingTries is null or > 0)
        {
            try
            {
                return await requestFunc();
            }
            catch (Exception caught)
            {
                e = caught;
                // ignored
            }

            if (delayBetweenRetries != null)
                Task.Delay(delayBetweenRetries.Value).Wait();

            remainingTries--;
        }

        throw new Exception(
            $"Max retries made for {requestFuncName}.",
            e);
    }

    public static T RunWithRetries<T>(
        Func<T> requestFunc,
        [CanBeNull] string requestFuncName = null,
        uint? retryCount = null,
        TimeSpan? delayBetweenRetries = null)
    {
        var remainingTries = retryCount + 1;

        Exception e = null;

        while (remainingTries is null or > 0)
        {
            try
            {
                return requestFunc();
            }
            catch (Exception caught)
            {
                e = caught;
                // ignored
            }

            if (delayBetweenRetries != null)
                Task.Delay(delayBetweenRetries.Value).Wait();

            remainingTries--;
        }

        var funcName = requestFuncName ?? "function";
        throw new Exception(
            $"Max retries made for {funcName}.",
            e);
    }

    public static void RunWithRetries(
        Action requestFunc,
        string requestFuncName,
        uint? retryCount = null,
        TimeSpan? delayBetweenRetries = null)
    {
        RunWithRetries(
            () => {
                requestFunc();
                return (int?)null;
            },
            requestFuncName,
            retryCount,
            delayBetweenRetries);
    }
}
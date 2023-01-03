using System;
using log4net;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CustomShared;

public class TasksRunner
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(TasksRunner));

    // custom wrapper func since Task.WaitAll doesn't elegantly handle thrown exceptions
    public static void WaitForAllTasksToComplete<T>(
        IList<T> tasks) where T : Task
    {
        var tempTasksList = new List<T>(tasks);

        while (tempTasksList.Count > 0)
        {
            PopFinishedTaskAndRecordErrors(tempTasksList);
        }
    }

    // returns finished task id if no exception thrown 
    public static int PopFinishedTaskAndRecordErrors<T>(
        IList<T> runningTasks,
        bool rethrowException = true)
        where T : Task
    {
        int finishedIndex = Task.WaitAny(runningTasks.ToArray());

        // if (finishedIndex < 0)
        // return null;

        var finishedTask = runningTasks[finishedIndex];
        runningTasks.Remove(finishedTask);

        if (finishedTask.Status != TaskStatus.Faulted)
            return finishedTask.Id;

        if (rethrowException)
            throw new Exception(
                "Caught exception in task",
                finishedTask.Exception.InnerException);

        Log.Error($"Caught exception(s) in task:");
        int exceptionCounter = 1;
        foreach (var ex in finishedTask.Exception.InnerExceptions)
        {
            Log.Error($"\t{exceptionCounter++}: {ex.Message}");
        }

        return finishedTask.Id;
    }

    /// <summary>
    /// Starts the given tasks and waits for them to complete. This will run, at most, the specified number of tasks in parallel.
    /// <para>NOTE: If one of the given tasks has already been started, an exception will be thrown.</para>
    /// </summary>
    /// <param name="tasksToRun">The tasks to run.</param>
    /// <param name="maxTasksToRunInParallel">The maximum number of tasks to run in parallel.</param>
    /// <param name="timeoutInMilliseconds">The maximum milliseconds we should allow the max tasks to run in parallel before allowing another task to start. Specify -1 to wait indefinitely.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// https://stackoverflow.com/a/36932845/3262950
    public static void StartAndWaitTasksWithConcurrentLimiter(
        IEnumerable<Task> tasksToRun,
        uint? maxTasksToRunInParallel = null,
        int timeoutInMilliseconds = -1,
        CancellationToken cancellationToken = new())
    {
        // Convert to a list of tasks so that we don't enumerate over it multiple times needlessly.
        var tasks = tasksToRun.ToList();

        using var limiter = new SemaphoreSlim((int)(maxTasksToRunInParallel ?? 100));
        
        var postTaskTasks = new List<Task>();

        // Have each task notify the throttler when it completes so that it decrements the number of tasks currently running.
        tasks.ForEach(t => postTaskTasks.Add(t.ContinueWith(tsk => limiter.Release())));

        // Start running each task.
        foreach (var task in tasks)
        {
            // Increment the number of tasks currently running and wait if too many are running.
            limiter.Wait(
                timeoutInMilliseconds,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            task.Start();
        }

        // Wait for all of the provided tasks to complete.
        // We wait on the list of "post" tasks instead of the original tasks, otherwise there is a potential race condition where the throttler&#39;s using block is exited before some Tasks have had their "post" action completed, which references the throttler, resulting in an exception due to accessing a disposed object.
        Task.WaitAll(
            postTaskTasks.ToArray(),
            cancellationToken);
    }
}

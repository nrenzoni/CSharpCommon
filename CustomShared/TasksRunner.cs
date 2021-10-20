using System;
using log4net;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomShared
{
    public class TasksRunner
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TasksRunner));

        public static void WaitForAllTasksToComplete(IList<Task> tasks)
        {
            while (tasks.Count > 0)
            {
                PopFinishedTaskAndRecordErrors(tasks);
            }
        }

        // returns finished task id
        public static int? PopFinishedTaskAndRecordErrors(IList<Task> runningTasks)
        {
            int finishedIndex = Task.WaitAny(runningTasks.ToArray());

            if (finishedIndex < 0)
                return null;

            var finishedTask = runningTasks[finishedIndex];
            runningTasks.Remove(finishedTask);

            if (finishedTask.Status != TaskStatus.Faulted)
                return finishedTask.Id;

            if (IsTestGlobalChecker.IsTest)
                throw new Exception("Caught exception in task", finishedTask.Exception.InnerException);

            Log.Error($"Caught exception(s) in task:");
            int exceptionCounter = 1;
            foreach (var ex in finishedTask.Exception.InnerExceptions)
            {
                Log.Error($"\t{exceptionCounter++}: {ex.Message}");
            }

            return finishedTask.Id;
        }
    }
}
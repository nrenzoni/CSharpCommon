using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using CustomShared;

namespace Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TasksRunner_TasksRunner()
    {
        var t = Task.Run(async () => throw new Exception("inner exception"));

        TasksRunner.WaitForAllTasksToComplete(new List<Task> { t });
    }

    [Test]
    public void BlockingQueue_test()
    {
        // Increase or decrease this value as desired.
        int itemsToAdd = 500;

        // A bounded collection. Increase, decrease, or remove the
        // maximum capacity argument to see how it impacts behavior.
        var numbers = new BlockingCollection<int>(50);

        // A simple blocking consumer with no cancellation.
        Task.Run(
            () =>
            {
                int i;
                while (!numbers.IsCompleted)
                {
                    try
                    {
                        i = numbers.Take();
                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine("Adding was completed!");
                        break;
                    }

                    Console.WriteLine(
                        "Take:{0} ",
                        i);

                    // Simulate a slow consumer. This will cause
                    // collection to fill up fast and thus Adds wil block.
                    Thread.SpinWait(100000);
                }

                Console.WriteLine("\r\nNo more items to take. Press the Enter key to exit.");
            });

        // A simple blocking producer with no cancellation.
        Task.Run(
            () =>
            {
                for (int i = 0; i < itemsToAdd; i++)
                {
                    numbers.Add(i);
                    Console.WriteLine(
                        "Add:{0} Count={1}",
                        i,
                        numbers.Count);
                }

                // See documentation for this method.
                numbers.CompleteAdding();
            });

        // Keep the console display open in debug mode.
        Console.ReadLine();
    }
}

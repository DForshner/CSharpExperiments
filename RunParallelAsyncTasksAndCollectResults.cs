using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

// RunParallelAsyncTasksAndCollectResults.cs
// Starts a async tasks that then runs two parallel async tasks
// and waits to collect the results.

public class Program
{
    static void Main(string[] args)
    {
        // Do some work asynchronously 
        DisplayImportantResults();

        // Do other meaningful work like reading keys while
        // we wait for results.
        ConsoleKeyInfo key;
        do
        {
            key = Console.ReadKey(true);
            Console.WriteLine("You pressed {0}", key.KeyChar);
        }
        while (key.Key != ConsoleKey.Escape);
    }

    /// <summary>
    /// Makes an asynchronous call that spawns two parallel tasks, 
	/// puts the thread to sleep while waiting for both to complete
	/// , and finally displays the results.
    /// </summary>
    public static async void DisplayImportantResults()
    {
        var taskA = Worker("Task A", 6000);
        var taskB = Worker("Task B", 3000);

        Console.WriteLine("Both Task A and Task B have been started.");

        // .WhenAll can be used with async/await because it returns a task that 
        // represents the action of waiting until everything has completed.  
        // This is opposed to .WaitAll which just blocks the thread until 
        // everything is completed.

        // await causes the thread to suspend until t1 and t2 have completed
        // at which point it will resume and method will continue.
        string[] results = await Task.WhenAll(taskA, taskB);

        Console.WriteLine("Both Task A and Task B are complete.");

        foreach (var result in results)
            Console.WriteLine("Result: {0}", result);
    }

    // async indicates to the compiler that await will be used inside the method.
    // the thread can suspend at the await point and be resumed 
    private async static Task<string> Worker(string name, int msDelay)
    {
        Console.WriteLine("Starting Task {0}", name);

        // The thread can suspend at the await point and it will be resumed 
        // asynchronously when the awaited instance (Task.Delay) completes.
        await Task.Delay(msDelay);

        Console.WriteLine("Completing Task {0}", name);

        return "Some important result";
    }
}
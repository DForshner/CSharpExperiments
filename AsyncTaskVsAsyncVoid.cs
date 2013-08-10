using System;
using System.Threading.Tasks;

namespace TestConsole
{
    public class AsyncTaskVsAsyncVoid
    {
        // Ways to author async methods:
        //
        // async Task Method() { } - Creates a method that can be awaited but returns nothing.
        // async Task<T> Method() { return default(T); } - Creates method that can be awaited and returns
        // a value of type T.
        // async void Method() - Creates method that cannot be awaited.  Used to create "Fire & Forget"
        // event handlers.
        //
        // async methods that return a task are managed by the AsyncTaskMethodBuilder class.
        // async methods that return void are managed by the AsyncVoidMethodBuilder class.
        // Both builders handle reuse the ambient context to execute each block of code
        // between the awaits of an async method, but they handle exceptions differently.

        static void Main(string[] args)
        {
            Console.WriteLine("Example 1");

            AsyncTaskTest(); // Gives warning about not awaiting the result of call.

            // Code will continue waiting until a key is pressed and the
            // The exception will never been shown.
            Console.ReadLine();

            Console.WriteLine("Example 2");
            
            // The TPL handles exceptions differently and throws unobserved 
            // exceptions to the UnobservedTaskException event.

            TaskScheduler.UnobservedTaskException += UnobservedTaskExceptionHandler;
            AsyncTaskTest();

            // The task is faulted but the exception won't be raised until the 
            // task instance is collected.
            Console.ReadLine();

            Console.WriteLine("Example 2 - Forcing GC");            
            // Forcing garbage collection will cause the task instance to be collected
            // and the exception event to be raised.
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Console.ReadLine();

            Console.WriteLine("Example 3");

            AsyncVoidTest();
           
            // The process will crash at this point because the AsyncVoidMethodBuilder 
            // has no ambient synchronization context so any exception
            // that isn't handled by the body of the async void method will
            // be re-thrown on the ThreadPool.
            //
            // One of the few places you can use an async void method without a try/catch
            // is UI event handlers (button clicks ect.) because exceptions are thrown
            // on the Dispatcher.

            Console.ReadLine();
        
            // (lambda version) that also crashes
            // test.ForEach(async x => { throw new Exception(); });
        }

        public static async Task AsyncTaskTest()
        {
            throw new Exception();
            // The exception get silently stored in an anonymous Task instance because we aren't
            // awaiting the return value or storing it in a variable.
        }

        static void UnobservedTaskExceptionHandler(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Console.WriteLine("Caught unobserved exception!");
            Console.WriteLine(e.Exception);
        }

        public static async void AsyncVoidTest()
        {
            throw new Exception();
        }
    }
}

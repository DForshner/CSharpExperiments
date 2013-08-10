using System;
using System.Runtime.CompilerServices;

// Example of using conditional weak table.
// Visual Studio 2012 + .Net 4.5

namespace ConditionalWeakTableExample
{
    class Program
    {
        public static void Main()
        {
            ConditionalWeakTable<object, object> table = 
                new ConditionalWeakTable<object, object>();

            var key = new object();
            var val = new object();

            // Weak reference to value so we can check if it's in memory or not
            var weakRefToValue = new WeakReference(val);

            // Key is stored as weak reference so the value 
            // is held as a hard references as long as the key lives.
            table.Add(key, val);

            // Release the reference to the value.
            // not the only reference is from the table
            val = null;

            // Force garbage collection from generation 0 to 2.
            GC.Collect(2);

            // Check that value is still in memory
            Console.WriteLine("Is value still in memory: {0}", weakRefToValue.IsAlive);

            // Try to get the value from the table
            new Action(() =>
            {
                var returnObject = new Object();
                if (table.TryGetValue(key, out returnObject))
                    Console.WriteLine("Value exists in table.");
            })();

            // Release the key instance.
            key = null;

            // Now the value is available for garbage collection as long as it's not
            // references anywhere else.
            GC.Collect(2);

            // Check that value has been garbage collected.
            Console.WriteLine("Is value still in memory: {0}", weakRefToValue.IsAlive);
            
            Console.ReadLine();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Collections.Concurrent;

/// Demo of using concurrent queue collection

namespace ConcurrentQueueCollectionDemo
{
    class Program
    {
        static void Main()
        {
            var toDisplay = new ConcurrentQueue<string>();
            Task task1 = Task.Run(() => CreateMessages(toDisplay, "ID1"));
            Task task2 = Task.Run(() => CreateMessages(toDisplay, "ID2"));
            Task task3 = Task.Run(() => CreateMessages(toDisplay, "ID3"));
            Task.WaitAll(task1, task2);

            Task producer = Task.Run(() => ProduceMessages(toDisplay, "ID4"));
            Task consumer = Task.Run(() => ConsumeMessages(toDisplay, "ID4"));
            Task.WaitAll(producer, consumer);
        }

        static void CreateMessages(ConcurrentQueue<string> toDisplay, string id)
        {
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(5);
                toDisplay.Enqueue(String.Format("{0}:{1}", id, i));
            }
        }

        static void ProduceMessages(ConcurrentQueue<string> toDisplay, string id)
        {
            int i = 0;
            while(true)
            {
                Thread.Sleep(5);
                toDisplay.Enqueue(String.Format("{0}:{1}", id, i));
                i += 1;
            }
        }

        static void ConsumeMessages(ConcurrentQueue<string> toDisplay, string id)
        {
            while(true)
            {
                Thread.Sleep(1);
                string msg;
                if (toDisplay.TryDequeue(out msg))
                {
                    Console.WriteLine(msg);
                }
            }
        }
    }

}
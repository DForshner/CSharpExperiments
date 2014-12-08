using Akka.Actor;
using Akka.Routing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// Distribute work items from a blocking queue to a pool of consumers.
// TODO: Implement throttling to provide back pressure on the producer by limiting the total number of unprocessed work items at any one point.
// Compiled: Visual Studio 2013
// Requires: Akka.NET Nuget package

namespace AkkaDistributeQueueToConsumers
{
    public static class FakeQueue
    {
        private static BlockingCollection<UInt32> numbers = new BlockingCollection<UInt32>(100);

        public static UInt32 Take()
        {
            return numbers.Take();
        }

        public static void Add(UInt32 number)
        {
            numbers.Add(number);
        }
    }

    public class StartWork
    {
        public UInt32 Workload { get; private set; }

        public StartWork(UInt32 workload)
        {
            Workload = workload;
        }
    }

    public class WorkCompleted
    {
    }

    public class StartConsuming 
    {
    }

    public class StopConsuming
    {
    }

    public class WorkDistributor : TypedActor, IHandle<StartConsuming>, IHandle<StopConsuming>
    {
        private ActorRef _router;
        private CancellationTokenSource _token;

        public WorkDistributor()
        {
        }

        public void Handle(StartConsuming msg)
        {
            // Setup a round robin pool of consumers
            var consumers = Enumerable.Range(0, 5)
                .Select(x => { return Context.ActorOf<WorkConsumer>("worker" + x); });
            var props = Props.Empty.WithRouter(new RoundRobinGroup(consumers));
            _router = Context.ActorOf(props);

            var tokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                while (!tokenSource.IsCancellationRequested)
                {
                    var number = FakeQueue.Take();
                    _router.Tell(new StartWork(number));
                }
            }, tokenSource.Token);

            _token = tokenSource;
        }

        public void Handle(StopConsuming msg)
        {
            // Stop consuming from queue;
            _token.Cancel();

            // Broadcast poison pill to children before stopping
            _router.GracefulStop(new TimeSpan(0, 1, 0), new Broadcast(PoisonPill.Instance)).Wait();
        }

        protected override void PostStop()
        {
            Console.WriteLine("Stopping distribution");
            base.PostStop();
        }
    }

    public class WorkConsumer: TypedActor, IHandle<StartWork>
    {
        ActorRef _output;

        public WorkConsumer()
        {
            _output = Context.ActorSelection("/user/output").ResolveOne(new TimeSpan(0, 0, 10)).Result;
        }

        public void Handle(StartWork msg)
        {
            _output.Tell(new Output(Context.Self.Path + " completed work item " + msg.Workload));
        }

        protected override void PostStop()
        {
            Console.WriteLine("Stopping worker");
            base.PostStop();
        }
    }

    public class Output 
    {
        public string Message { get; private set; }

        public Output(string message)
        {
            Message = message;
        }
    }

    public class OutputWriter : ReceiveActor
    {
        public OutputWriter()
        {
            Receive<Output>(x => { Console.WriteLine(x.Message); });
        }
    }

    public class AkkaDistributeQueueToConsumers
    {
        public static void Main()
        {
            var system = ActorSystem.Create("WorkSystem");

            var output = system.ActorOf<OutputWriter>("output");
            output.Tell(new Output("Starting ..."));

            var distributor = system.ActorOf<WorkDistributor>("distro1");
            distributor.Tell(new StartConsuming());

            var cancelToken = ProduceIncomingQueueData();

            output.Tell(new Output("Press Enter to stop"));
            Console.ReadKey(true);

            output.Tell(new Output("Stopping ..."));
            distributor.Tell(new StopConsuming());

            cancelToken.Cancel();
            //distributor.GracefulStop(new TimeSpan(0, 1, 0));

            output.Tell(new Output("Press Enter to exit"));
            Console.ReadKey(true);
        }

        private static CancellationTokenSource ProduceIncomingQueueData()
        {
            var tokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                UInt32 count = 0;
                while (!tokenSource.IsCancellationRequested)
                {
                    Thread.Sleep(10);
                    FakeQueue.Add(count++);
                }
            }, tokenSource.Token);
            return tokenSource;
        }
    }
}
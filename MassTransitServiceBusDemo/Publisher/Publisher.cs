using Common;
using MassTransit;
using System;

// Simulates a publisher that can either publish key press events 
// (pub/sub) or produce workloads to be consumed (producer/consumer).

namespace Publisher
{
    public class Publisher 
    {
        static TimeSpan _duration = TimeSpan.FromSeconds(1);

        static void Main(string[] args)
        {
            var publisher = ServiceBusFactory.New(sbc =>
            {
                sbc.UseRabbitMq();
                sbc.ReceiveFrom("rabbitmq://localhost/publisher");
            });

            Console.WriteLine("Press key(s) to publish or press 'Enter' to produce workload.");

            while (true)
            {
                var key = Console.ReadKey().Key;
                if (key == ConsoleKey.Enter)
                {
                    _duration += TimeSpan.FromSeconds(1);
                    publisher.Publish(new WorkLoad() { Duration = _duration });
                }
                else
                {
                    publisher.Publish(new KeyPressedEvent() { Key = key });
                }
            }
        }
    }
}
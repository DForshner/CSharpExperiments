using Common;
using MassTransit;
using System;
using System.Threading;

namespace Subscriber2
{
    public class Subscriber2
    {
        static void Main(string[] args)
        {
            // Pub - Sub
            SubscribeToKeyPresses();

            // Producer - Consumer
            SubscribeToWorkload();

            Console.WriteLine("Subscriber2 Ready");
        }

        class WorkLoadConsumer : Consumes<WorkLoad>.Context
        {
            public void Consume(IConsumeContext<WorkLoad> msg)
            {
                var id = Thread.CurrentThread.ManagedThreadId;
                Console.WriteLine("Consumer - Start - [{0}] - {1}", id, msg.Message.Duration);
                Thread.Sleep(msg.Message.Duration);
                Console.WriteLine("Consumer - End - [{0}] - {1}", id, msg.Message.Duration);
            }
        }

        private static void SubscribeToWorkload()
        {
            var workLoadConsumer = ServiceBusFactory.New(sbc =>
            {
                sbc.UseRabbitMq();
                sbc.SetConcurrentConsumerLimit(3);
                sbc.ReceiveFrom("rabbitmq://localhost/consumer?prefetch=3");

                sbc.Subscribe(s => { s.Consumer<WorkLoadConsumer>().Permanent(); });
            });
        }

        private static void SubscribeToKeyPresses()
        {
            var keyPressSubscriber = ServiceBusFactory.New(sbc =>
                {
                    sbc.UseRabbitMq();
                    sbc.ReceiveFrom("rabbitmq://localhost/subscriber2");
                });
            keyPressSubscriber.SubscribeHandler<KeyPressedEvent>(msg =>
                {
                    Console.WriteLine("Subscriber - Key {0} was pressed", msg.Key);
                });
        }
    }
}
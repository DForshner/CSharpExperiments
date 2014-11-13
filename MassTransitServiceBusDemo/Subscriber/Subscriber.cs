using Common;
using MassTransit;
using System;
using System.Threading;

namespace Subscriber
{
    public class Subscriber 
    {
        static void Main(string[] args)
        {
            // Pub - Sub
            SubscribeToKeyPresses();

            // Producer - Consumer
            SubscribeToWorkload();

            Console.WriteLine("Subscriber Ready");
        }

        private class WorkLoadConsumer : Consumes<WorkLoad>.Context
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
                sbc.SetConcurrentConsumerLimit(2);
                sbc.ReceiveFrom("rabbitmq://localhost/consumer?prefetch=2");

                sbc.Subscribe(s => { s.Consumer<WorkLoadConsumer>().Permanent(); });
            });
        }

        private static void SubscribeToKeyPresses()
        {
            var keyPressSubscriberA = ServiceBusFactory.New(sbc =>
                {
                    sbc.UseRabbitMq();
                    sbc.ReceiveFrom("rabbitmq://localhost/subscribera");

                    sbc.Subscribe(s => s.Handler<KeyPressedEvent>(msg =>
                        {
                            Console.WriteLine("SubscriberA - Key {0} was pressed", msg.Key);
                        }));
                });

            var keyPressSubscriberB = ServiceBusFactory.New(sbc =>
                {
                    sbc.UseRabbitMq();
                    sbc.ReceiveFrom("rabbitmq://localhost/subscriberb");

                    sbc.Subscribe(s => s.Handler<KeyPressedEvent>(msg =>
                        {
                            Console.WriteLine("SubscriberB - Key {0} was pressed", msg.Key);
                        }));
                });
        }
    }
}
using MassTransit;
using MassTransit.BusConfigurators;
using System;

namespace Infrastructure
{
    public static class Bus
    {
        public static IServiceBus CreateBus(string queueName, Action<ServiceBusConfigurator> initialize)
        {
            return ServiceBusFactory.New(x =>
            {
                x.UseRabbitMq();
                x.ReceiveFrom("rabbitmq://localhost/" + queueName);
                initialize(x);
            });
        }
    }
}

﻿using Newtonsoft.Json;
using RabbitMQ.Client;
using Rabbit.Common.Display;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Collections.Immutable;
using System.Threading.Tasks;
using DotnetCrawler.Data.Constants;
using DotnetCrawler.Data.Model;
using System;

namespace DotnetCrawler.Core.RabitMQ
{
    public interface IRabitMQProducer
    {
        void SendMessage<T>(string queue, T message);
    }

    public class RabitMQProducer : IRabitMQProducer
    {
        private IConnection _connection;
        private ConnectionFactory _connectionFactory;

        public RabitMQProducer(IRabitMQSettings rabitMQSettings)
        {
            _connectionFactory = new ConnectionFactory
            {
                HostName = rabitMQSettings.HostName,
                UserName = rabitMQSettings.UserName,
                Password = rabitMQSettings.Password,
            };

            _connection = _connectionFactory.CreateConnection();
        }
     
        public void SendMessage<T>(string queue, T message)
        {
            var exchangeName = "";
            var channel = _connection.CreateModel();

            channel.QueueDeclare(
                queue,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: ImmutableDictionary<string, object>.Empty);
            var json = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(json);
            channel.BasicPublish(exchange: exchangeName, routingKey: queue, body: body);

            DisplayInfo<T>
                .For(message)
                .SetExchange(exchangeName)
                .SetQueue(queue)
                .SetRoutingKey(queue)
                .SetVirtualHost(_connectionFactory.VirtualHost)
                .Display(Color.Cyan);
        }
    }
}

using System.Text.Json;
using RabbitMQ.Client;
using Rabbit.Common.Display;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client.Events;
using System.Threading;
using System;
using DotnetCrawler.Data.Constants;
using DotnetCrawler.Data.Model;
using DotnetCrawler.Data.Setting;

namespace DotnetCrawler.Core.RabitMQ
{
    public class RabitMQConsumer : IHostedService
    {
        private IConnection _connection;
        private ConnectionFactory _connectionFactory;
        private readonly ICrawlerCore<CategorySetting> _crawlerCore;

        public RabitMQConsumer(
            IRabitMQSettings rabitMQSettings,
            ICrawlerCore<CategorySetting> dotnetCrawlerCore
            )
        {
            _connectionFactory = new ConnectionFactory
            {
                HostName = rabitMQSettings.HostName,
                UserName = rabitMQSettings.UserName,
                Password = rabitMQSettings.Password,
            };

            _connection = _connectionFactory.CreateConnection();
            _crawlerCore = dotnetCrawlerCore;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // listener post
            var channelPost = _connection.CreateModel();
            channelPost.QueueDeclare(
                QueueName.QueuePostName,
                durable: false,
                exclusive: false,
                autoDelete: false
            );
            var consumer = new EventingBasicConsumer(channelPost);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var postmessage = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<PostMessage>(postmessage);

                _crawlerCore.JobPost(
                    message.LinkReader, 
                    message.HtmlDocumentCategory, 
                    message.Category, 
                    message.IsReCrawleSmall
                );

                DisplayInfo<PostMessage>
                .For(message)
                .SetExchange("")
                .SetQueue(QueueName.QueuePostName)
                .SetRoutingKey(QueueName.QueuePostName)
                .SetVirtualHost(_connectionFactory.VirtualHost)
                .Display(Color.Yellow);
            };
            channelPost.BasicConsume(queue: QueueName.QueuePostName, autoAck: true, consumer: consumer);

            // listener chap
            var channelChap = _connection.CreateModel();
            channelChap.QueueDeclare(QueueName.QueueChapName, durable: false,
                exclusive: false,
                autoDelete: false);
            var consumer = new EventingBasicConsumer(channelChap);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine("Received message: {0}", message);
                DisplayInfo<string>
                .For(message)
                .SetExchange("")
                .SetQueue(QueueName.QueueChapName)
                .SetRoutingKey(QueueName.QueueChapName)
                .SetVirtualHost(_connectionFactory.VirtualHost)
                .Display(Color.Yellow);
            };
            channelChap.BasicConsume(queue: QueueName.QueueChapName, autoAck: true, consumer: consumer);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _connection.Close();
            return Task.CompletedTask;
        }
    }
}

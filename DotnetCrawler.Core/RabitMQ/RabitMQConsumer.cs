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
using Hangfire;
using Newtonsoft.Json.Linq;

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
            ConsumerCategory();
            ConsumerPost();
            ConsumerPostDetail();
            ConsumerChap();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            _connection.Close();
            return Task.CompletedTask;
        }

        private void ConsumerCategory() {
            var channel = _connection.CreateModel();
            channel.QueueDeclare(
                QueueName.QueueCategoryName,
                durable: false,
                exclusive: false,
                autoDelete: false
            );
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) => {
                var body = ea.Body.ToArray();
                var bodyString = Encoding.UTF8.GetString(body);
                if (!string.IsNullOrEmpty(bodyString)) {
                    var message = JsonSerializer.Deserialize<CategoryMessage>(bodyString);

                    BackgroundJob.Enqueue(() => _crawlerCore.JobCategory(message));

                    DisplayInfo<CategoryMessage>
                    .For(message)
                    .SetExchange("")
                    .SetQueue(QueueName.QueueCategoryName)
                    .SetRoutingKey(QueueName.QueueCategoryName)
                    .SetVirtualHost(_connectionFactory.VirtualHost)
                    .Display(Color.Yellow);
                }
            };
            channel.BasicConsume(queue: QueueName.QueueCategoryName, autoAck: true, consumer: consumer);
        }

        private void ConsumerPost() {
            var channel = _connection.CreateModel();
            channel.QueueDeclare(
                QueueName.QueuePostName,
                durable: false,
                exclusive: false,
                autoDelete: false
            );
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) => {
                var body = ea.Body.ToArray();
                var bodyString = Encoding.UTF8.GetString(body);

                if(!string.IsNullOrEmpty(bodyString)) {
                    var message = JsonSerializer.Deserialize<PostMessage>(bodyString);
                    BackgroundJob.Enqueue(() => _crawlerCore.JobPost(message));

                    DisplayInfo<PostMessage>
                    .For(message)
                    .SetExchange("")
                    .SetQueue(QueueName.QueuePostName)
                    .SetRoutingKey(QueueName.QueuePostName)
                    .SetVirtualHost(_connectionFactory.VirtualHost)
                    .Display(Color.Yellow);
                }
            };
            channel.BasicConsume(queue: QueueName.QueuePostName, autoAck: true, consumer: consumer);
        }

        private void ConsumerPostDetail() {
            var channel = _connection.CreateModel();
            channel.QueueDeclare(
                QueueName.QueuePostDetailName,
                durable: false,
                exclusive: false,
                autoDelete: false
            );
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) => {
                var body = ea.Body.ToArray();
                var bodyString = Encoding.UTF8.GetString(body);

                if(!string.IsNullOrEmpty(bodyString)) {
                    var message = JsonSerializer.Deserialize<PostDetailMessage>(bodyString);
                    BackgroundJob.Enqueue(() => _crawlerCore.JobPostDetail(message));

                    DisplayInfo<PostDetailMessage>
                    .For(message)
                    .SetExchange("")
                    .SetQueue(QueueName.QueuePostDetailName)
                    .SetRoutingKey(QueueName.QueuePostDetailName)
                    .SetVirtualHost(_connectionFactory.VirtualHost)
                    .Display(Color.Yellow);
                }
            };
            channel.BasicConsume(queue: QueueName.QueuePostDetailName, autoAck: true, consumer: consumer);
        }


        private void ConsumerChap() {
            var channel = _connection.CreateModel();
            channel.QueueDeclare(
                QueueName.QueueChapName,
                durable: false,
                exclusive: false,
                autoDelete: false
            );
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) => {
                var body = ea.Body.ToArray();
                var bodyString = Encoding.UTF8.GetString(body);

                if(!string.IsNullOrEmpty(bodyString)) {
                    var message = JsonSerializer.Deserialize<ChapMessage>(bodyString);
                    BackgroundJob.Enqueue(() => _crawlerCore.JobChap(message));

                    DisplayInfo<ChapMessage>
                    .For(message)
                    .SetExchange("")
                    .SetQueue(QueueName.QueueChapName)
                    .SetRoutingKey(QueueName.QueueChapName)
                    .SetVirtualHost(_connectionFactory.VirtualHost)
                    .Display(Color.Yellow);
                }
            };
            channel.BasicConsume(queue: QueueName.QueueChapName, autoAck: true, consumer: consumer);
        }
    }
}

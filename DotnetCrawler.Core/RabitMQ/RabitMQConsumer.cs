using System.Text.Json;
using RabbitMQ.Client;
using Rabbit.Common.Display;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client.Events;
using System.Threading;
using DotnetCrawler.Data.Constants;
using DotnetCrawler.Data.Model;
using Hangfire;
using DotnetCrawler.Data.ModelDb;
using System;

namespace DotnetCrawler.Core.RabitMQ
{
    public class RabitMQConsumer : IHostedService
    {
        private IConnection _connection;
        private IModel _modelChannel;
        private ConnectionFactory _connectionFactory;
        private readonly ICrawlerCore<SiteConfigDb> _crawlerCore;
        private readonly IWordpressSyncCore _wordpressSyncCore;

        delegate void ConsumerCrawleDelegate(string queueName);

        public RabitMQConsumer(
            IRabitMQSettings rabitMQSettings,
            ICrawlerCore<SiteConfigDb> dotnetCrawlerCore,
            IWordpressSyncCore wordpressSyncCore
            )
        {

            _connectionFactory = new ConnectionFactory
            {
                HostName = rabitMQSettings.HostName,
                UserName = rabitMQSettings.UserName,
                Password = rabitMQSettings.Password,
            };

            _connection = _connectionFactory.CreateConnection();
            _modelChannel = _connection.CreateModel();
            _crawlerCore = dotnetCrawlerCore;
            _wordpressSyncCore = wordpressSyncCore;

            QueueDeclare(QueueName.QueueCrawleCategory);
            QueueDeclare(QueueName.QueueCrawlePost);
            QueueDeclare(QueueName.QueueCrawlePostDetail);
            QueueDeclare(QueueName.QueueCrawleChap);

            QueueDeclare(QueueName.QueueSyncCategory);
            QueueDeclare(QueueName.QueueSyncPost);
            QueueDeclare(QueueName.QueueSyncChap);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            ConsumerCrawleCategory();
            ConsumerCrawlePost();
            ConsumerCrawlePostDetail();
            ConsumerCrawleChap();

            ConsumerSyncCategory();
            ConsumerSyncPost();
            ConsumerSyncChap();
            return Task.CompletedTask;
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _connection.Close();
            return Task.CompletedTask;
        }

        private void QueueDeclare(string QueueName)
        {
            _modelChannel.QueueDeclare(
                QueueName,
                durable: false,
                exclusive: false,
                autoDelete: false
            );
        }
       
        private void ConsumerCrawleCategory()
        {
            var consumer = new EventingBasicConsumer(_modelChannel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var bodyString = Encoding.UTF8.GetString(body);
                if (!string.IsNullOrEmpty(bodyString))
                {
                    var message = JsonSerializer.Deserialize<CategoryMessage>(bodyString);
                    BackgroundJob.Enqueue(() => _crawlerCore.JobCategory(message));

                    DisplayInfo<string>
                    .For("Received Category")
                    .SetExchange("")
                    .SetQueue(QueueName.QueueCrawleCategory)
                    .SetRoutingKey(QueueName.QueueCrawleCategory)
                    .SetVirtualHost(_connectionFactory.VirtualHost)
                    .Display(Color.Yellow);
                }
            };
            _modelChannel.BasicConsume(queue: QueueName.QueueCrawleCategory, autoAck: true, consumer: consumer);
        }

        private void ConsumerCrawlePost()
        {

            var consumer = new EventingBasicConsumer(_modelChannel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var bodyString = Encoding.UTF8.GetString(body);

                if (!string.IsNullOrEmpty(bodyString))
                {
                    var message = JsonSerializer.Deserialize<PostMessage>(bodyString);
                    BackgroundJob.Enqueue(() => _crawlerCore.JobPost(message));

                    DisplayInfo<string>
                    .For("Received Post")
                    .SetExchange("")
                    .SetQueue(QueueName.QueueCrawlePost)
                    .SetRoutingKey(QueueName.QueueCrawlePost)
                    .SetVirtualHost(_connectionFactory.VirtualHost)
                    .Display(Color.Yellow);
                }
            };
            _modelChannel.BasicConsume(queue: QueueName.QueueCrawlePost, autoAck: true, consumer: consumer);
        }

        private void ConsumerCrawlePostDetail()
        {
            var consumer = new EventingBasicConsumer(_modelChannel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var bodyString = Encoding.UTF8.GetString(body);

                if (!string.IsNullOrEmpty(bodyString))
                {
                    var message = JsonSerializer.Deserialize<PostDetailMessage>(bodyString);
                    BackgroundJob.Enqueue(() => _crawlerCore.JobPostDetail(message));

                    DisplayInfo<string>
                    .For("Received Post Detail")
                    .SetExchange("")
                    .SetQueue(QueueName.QueueCrawlePostDetail)
                    .SetRoutingKey(QueueName.QueueCrawlePostDetail)
                    .SetVirtualHost(_connectionFactory.VirtualHost)
                    .Display(Color.Yellow);
                }
            };
            _modelChannel.BasicConsume(queue: QueueName.QueueCrawlePostDetail, autoAck: true, consumer: consumer);
        }

        private void ConsumerCrawleChap()
        {
            var consumer = new EventingBasicConsumer(_modelChannel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var bodyString = Encoding.UTF8.GetString(body);

                if (!string.IsNullOrEmpty(bodyString))
                {
                    var message = JsonSerializer.Deserialize<ChapMessage>(bodyString);
                    BackgroundJob.Enqueue(() => _crawlerCore.JobChap(message));

                    DisplayInfo<string>
                    .For("Received Chap")
                    .SetExchange("")
                    .SetQueue(QueueName.QueueCrawleChap)
                    .SetRoutingKey(QueueName.QueueCrawleChap)
                    .SetVirtualHost(_connectionFactory.VirtualHost)
                    .Display(Color.Yellow);
                }
            };
            _modelChannel.BasicConsume(queue: QueueName.QueueCrawleChap, autoAck: true, consumer: consumer);
        }

        private void ConsumerSyncCategory()
        {
            var consumer = new EventingBasicConsumer(_modelChannel);
            consumer.Received += (model, ea) => {
                var body = ea.Body.ToArray();
                var bodyString = Encoding.UTF8.GetString(body);

                if (!string.IsNullOrEmpty(bodyString))
                {
                    var message = JsonSerializer.Deserialize<CategorySyncMessage>(bodyString);
                    BackgroundJob.Enqueue(() => _wordpressSyncCore.JobSyncCategory(message));

                    DisplayInfo<string>
                    .For("Received Sync Category")
                    .SetExchange("")
                    .SetQueue(QueueName.QueueSyncCategory)
                    .SetRoutingKey(QueueName.QueueSyncCategory)
                    .SetVirtualHost(_connectionFactory.VirtualHost)
                    .Display(Color.Yellow);
                }
            };
            _modelChannel.BasicConsume(queue: QueueName.QueueSyncCategory, autoAck: true, consumer: consumer);
        }

        private void ConsumerSyncPost() {
            var consumer = new EventingBasicConsumer(_modelChannel);
            consumer.Received += (model, ea) => {
                var body = ea.Body.ToArray();
                var bodyString = Encoding.UTF8.GetString(body);

                if(!string.IsNullOrEmpty(bodyString)) {
                    var message = JsonSerializer.Deserialize<PostSyncMessage>(bodyString);
                    BackgroundJob.Enqueue(() => _wordpressSyncCore.JobSyncPost(message));

                    DisplayInfo<string>
                    .For("Received Sync Post")
                    .SetExchange("")
                    .SetQueue(QueueName.QueueSyncPost)
                    .SetRoutingKey(QueueName.QueueSyncPost)
                    .SetVirtualHost(_connectionFactory.VirtualHost)
                    .Display(Color.Yellow);
                }
            };
            _modelChannel.BasicConsume(queue: QueueName.QueueSyncPost, autoAck: true, consumer: consumer);
        }

        private void ConsumerSyncChap() {
            var consumer = new EventingBasicConsumer(_modelChannel);
            consumer.Received += (model, ea) => {
                var body = ea.Body.ToArray();
                var bodyString = Encoding.UTF8.GetString(body);

                if(!string.IsNullOrEmpty(bodyString)) {
                    var message = JsonSerializer.Deserialize<ChapSyncMessage>(bodyString);
                    BackgroundJob.Enqueue(() => _wordpressSyncCore.JobSyncChap(message));

                    DisplayInfo<string>
                    .For("Received Sync Chap")
                    .SetExchange("")
                    .SetQueue(QueueName.QueueSyncChap)
                    .SetRoutingKey(QueueName.QueueSyncChap)
                    .SetVirtualHost(_connectionFactory.VirtualHost)
                    .Display(Color.Yellow);
                }
            };
            _modelChannel.BasicConsume(queue: QueueName.QueueSyncChap, autoAck: true, consumer: consumer);
        }
    }
}

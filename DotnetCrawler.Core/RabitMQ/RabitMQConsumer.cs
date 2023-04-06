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

namespace DotnetCrawler.Core.RabitMQ
{
    public class RabitMQConsumer : IHostedService
    {
        private IConnection _connection;
        private IModel _modelChannel;
        private ConnectionFactory _connectionFactory;
        private readonly ICrawlerCore<SiteConfigDb> _crawlerCore;

        public RabitMQConsumer(
            IRabitMQSettings rabitMQSettings,
            ICrawlerCore<SiteConfigDb> dotnetCrawlerCore
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

            _modelChannel.QueueDeclare(
                QueueName.QueueCategoryName,
                durable: false,
                exclusive: false,
                autoDelete: false
            );
            _modelChannel.QueueDeclare(
                QueueName.QueuePostName,
                durable: false,
                exclusive: false,
                autoDelete: false
            );
            _modelChannel.QueueDeclare(
                QueueName.QueuePostDetailName,
                durable: false,
                exclusive: false,
                autoDelete: false
            );
            _modelChannel.QueueDeclare(
                QueueName.QueueChapName,
                durable: false,
                exclusive: false,
                autoDelete: false
            );
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            ConsumerCategory();
            ConsumerPost();
            ConsumerPostDetail();
            ConsumerChap();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _connection.Close();
            return Task.CompletedTask;
        }

        private void ConsumerCategory()
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
                    .SetQueue(QueueName.QueueCategoryName)
                    .SetRoutingKey(QueueName.QueueCategoryName)
                    .SetVirtualHost(_connectionFactory.VirtualHost)
                    .Display(Color.Yellow);
                }
            };
            _modelChannel.BasicConsume(queue: QueueName.QueueCategoryName, autoAck: true, consumer: consumer);
        }

        private void ConsumerPost()
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
                    .SetQueue(QueueName.QueuePostName)
                    .SetRoutingKey(QueueName.QueuePostName)
                    .SetVirtualHost(_connectionFactory.VirtualHost)
                    .Display(Color.Yellow);
                }
            };
            _modelChannel.BasicConsume(queue: QueueName.QueuePostName, autoAck: true, consumer: consumer);
        }

        private void ConsumerPostDetail()
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
                    .SetQueue(QueueName.QueuePostDetailName)
                    .SetRoutingKey(QueueName.QueuePostDetailName)
                    .SetVirtualHost(_connectionFactory.VirtualHost)
                    .Display(Color.Yellow);
                }
            };
            _modelChannel.BasicConsume(queue: QueueName.QueuePostDetailName, autoAck: true, consumer: consumer);
        }

        private void ConsumerChap()
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
                    .SetQueue(QueueName.QueueChapName)
                    .SetRoutingKey(QueueName.QueueChapName)
                    .SetVirtualHost(_connectionFactory.VirtualHost)
                    .Display(Color.Yellow);
                }
            };
            _modelChannel.BasicConsume(queue: QueueName.QueueChapName, autoAck: true, consumer: consumer);
        }
    }
}

using System.Text.Json;
using RabbitMQ.Client;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client.Events;
using System.Threading;
using DotnetCrawler.Data.Constants;
using DotnetCrawler.Data.Model;
using Hangfire;
using DotnetCrawler.Data.Entity;
using DotnetCrawler.Base.Extension;

namespace DotnetCrawler.Core.RabitMQ
{
    public class RabitMQConsumer : IHostedService
    {
        private IConnection _connection;
        private IModel _modelChannel;
        private ConnectionFactory _connectionFactory;
        private readonly ICrawlerCore<SiteConfigDb> _crawlerCore;

        delegate void ConsumerCrawleDelegate(string queueName);

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
                QueueName.QueueCrawlePost,
                durable: false,
                exclusive: false,
                autoDelete: false
            );

            _modelChannel.QueueDeclare(
                QueueName.QueueCrawlePostDetail,
                durable: false,
                exclusive: false,
                autoDelete: false
            );
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var consumerPost = new EventingBasicConsumer(_modelChannel);
            consumerPost.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var bodyString = Encoding.UTF8.GetString(body);

                if (!string.IsNullOrEmpty(bodyString))
                {
                    var message = JsonSerializer.Deserialize<PostMessage>(bodyString);
                    BackgroundJob.Enqueue(() => _crawlerCore.JobPostData(message));
                    Helper.Display("Received Post", Extension.MessageType.SystemInfo);
                }
            };
            _modelChannel.BasicConsume(queue: QueueName.QueueCrawlePost, autoAck: true, consumer: consumerPost);

            var consumerPostDetail = new EventingBasicConsumer(_modelChannel);
            consumerPostDetail.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var bodyString = Encoding.UTF8.GetString(body);

                if (!string.IsNullOrEmpty(bodyString))
                {
                    var message = JsonSerializer.Deserialize<PostDetailMessage>(bodyString);
                    BackgroundJob.Enqueue(() => _crawlerCore.JobPostDetail(message));
                    Helper.Display("Received Post Detail", Extension.MessageType.SystemInfo);
                }
            };
            _modelChannel.BasicConsume(queue: QueueName.QueueCrawlePostDetail, autoAck: true, consumer: consumerPostDetail);

            return Task.CompletedTask;
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _connection.Close();
            return Task.CompletedTask;
        }
    }
}

using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Drawing;
using System.Text;
using System.Collections.Immutable;
using DotnetCrawler.Data.Model;
using DotnetCrawler.Base.Extension;

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
            // create channel
            using (var channel = _connection.CreateModel())
            {
                var exchangeName = "";

                channel.QueueDeclare(
                    queue,
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: ImmutableDictionary<string, object>.Empty);
                var json = JsonConvert.SerializeObject(message);
                var body = Encoding.UTF8.GetBytes(json);
                channel.BasicPublish(exchange: exchangeName, routingKey: queue, body: body);
                Helper.Display(string.Format("Send queue {0}", queue), Extension.MessageType.SystemInfo);
            }
        }
    }
}

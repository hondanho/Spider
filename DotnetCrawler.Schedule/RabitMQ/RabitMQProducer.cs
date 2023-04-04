using DotnetCrawler.Data.Model.RabitMQ;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Rabbit.Common.Display;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Collections.Immutable;
using System.Threading.Tasks;
using DotnetCrawler.Data.Constants;

namespace DotnetCrawler.API.RabitMQ
{
    public interface IRabitMQProducer
    {
        void SendCategoryMessage<T>(T message);
        void SendPostMessage<T>(T message);
        void SendChapMessage<T>(T message);
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
        public void SendCategoryMessage<T>(T message)
        {

        }
        public void SendPostMessage<T>(T message)
        {
          
        }
        public void SendChapMessage<T>(T message)
        {
            var exchangeName = "";
            var channel = _connection.CreateModel();
            channel.QueueDeclare(
                QueueName.QueueChapName, 
                durable: false, 
                exclusive: false, 
                autoDelete: false, 
                arguments: ImmutableDictionary<string, object>.Empty);
            var json = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(json);
            channel.BasicPublish(exchange: exchangeName, routingKey: QueueName.QueueChapName, body: body);

            DisplayInfo<T>
                .For(message)
                .SetExchange(exchangeName)
                .SetQueue(QueueName.QueueChapName)
                .SetRoutingKey(QueueName.QueueChapName)
                .SetVirtualHost(_connectionFactory.VirtualHost)
                .Display(Color.Cyan);
        }
    }
}

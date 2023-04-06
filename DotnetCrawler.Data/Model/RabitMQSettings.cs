
namespace DotnetCrawler.Data.Model
{
    public interface IRabitMQSettings
    {
        public string HostName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class RabitMQSettings : IRabitMQSettings
    {
        public string HostName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}

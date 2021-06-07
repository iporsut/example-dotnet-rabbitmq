using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

namespace RabbitMQWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IConnection>((serviceProvider) =>
                    {
                        var factory = new ConnectionFactory() { HostName = "localhost" };
                        factory.UserName = "admin";
                        factory.Password = "Admin@123";
                        factory.DispatchConsumersAsync = true;
                        return factory.CreateConnection();
                    });
                    services.AddSingleton<IModel>((serviceProvider) =>
                    {
                        var connection = serviceProvider.GetService<IConnection>();
                        return connection.CreateModel();
                    });
                    services.AddTransient<Channel<string>>(serviceProvider =>
                    {
                        return Channel.CreateUnbounded<string>();
                    });
                    services.AddHostedService<Worker>();
                });
    }
}

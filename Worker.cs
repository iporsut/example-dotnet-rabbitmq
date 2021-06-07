using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly Channel<string> _channel;
        private readonly IModel _rabbitChannel;

        public Worker(ILogger<Worker> logger, Channel<string> channel, IModel rabbitChannel)
        {
            _logger = logger;
            _channel = channel;
            _rabbitChannel = rabbitChannel;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            FetchMessageAndWriteToChannel(stoppingToken);
            await InvokeExternalService(stoppingToken);
        }

        private void FetchMessageAndWriteToChannel(CancellationToken stoppingToken)
        {
            _rabbitChannel.QueueDeclare("two.port", true, false, false, null);

            var consumer = new AsyncEventingBasicConsumer(_rabbitChannel);

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation($"From rabbit to channel->{message}");
                await _channel.Writer.WriteAsync(message, stoppingToken);
            };

            _rabbitChannel.BasicConsume("two.port", true, consumer);
        }

        private async Task InvokeExternalService(CancellationToken stoppingToken)
        {
            await Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var item = await _channel.Reader.ReadAsync(stoppingToken);
                    _logger.LogInformation($"From channel t0 somewhere->{item}");
                }
            }, stoppingToken);
        }
    }
}

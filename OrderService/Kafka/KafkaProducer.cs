using Confluent.Kafka;
using System.Text.Json;

namespace OrderService.Kafka
{
    public class KafkaProducer
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<KafkaProducer> _logger;

        public KafkaProducer(IConfiguration config, ILogger<KafkaProducer> logger)
        {
            _logger = logger;
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = config["Kafka:BootstrapServers"]
            };
            _producer = new ProducerBuilder<string, string>(producerConfig).Build();
        }

        public async Task PublishAsync(string topic, object message)
        {
            var json = JsonSerializer.Serialize(message);
            try
            {
                await _producer.ProduceAsync(topic, new Message<string, string>
                {
                    Key = Guid.NewGuid().ToString(),
                    Value = json
                });
                _logger.LogInformation("[Kafka] Published to {Topic}: {Message}", topic, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Kafka] Failed to publish to {Topic}", topic);
            }
        }

        public void Dispose() => _producer.Dispose();
    }
}

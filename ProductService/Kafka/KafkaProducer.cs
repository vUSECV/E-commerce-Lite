using Confluent.Kafka;
using System.Text.Json;

namespace ProductService.Kafka
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
                _logger.LogInformation("[Kafka] Отправлен в {Topic}: {Message}", topic, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Kafka] Ошибка отправки в  {Topic}", topic);
            }
        }

        public void Dispose() => _producer.Dispose();
    }
}

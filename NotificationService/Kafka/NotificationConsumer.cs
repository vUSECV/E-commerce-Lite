using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NotificationService.Kafka
{
    public class NotificationConsumer : BackgroundService
    {
        private readonly IConfiguration _config;
        private readonly string[] _topics = { "product-created", "order-created" };

        public NotificationConsumer(IConfiguration config, ILogger<NotificationConsumer> logger)
        {
            _config = config;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() => Consume(stoppingToken), stoppingToken);
        }

        private void Consume(CancellationToken stoppingToken)
        {
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _config["Kafka:BootstrapServers"],
                GroupId = _config["Kafka:GroupId"],
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            };

            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            consumer.Subscribe(_topics);

            while (!stoppingToken.IsCancellationRequested)
            {
                var result = consumer.Consume(stoppingToken);
                HandleMessage(result.Topic, result.Message.Value);
            }

            consumer.Close();
        }

        private void HandleMessage(string topic, string json)
        {
            var doc = JsonSerializer.Deserialize<JsonElement>(json);

            if (topic == "product-created")
            {
                var id = doc.GetProperty("ProductId").GetInt32();
                var name = doc.GetProperty("Name").GetString();
                var price = doc.GetProperty("Price").GetDecimal();

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("┌─────────────────────────────────────────┐");
                Console.WriteLine("│         НОВЫЙ ПРОДУКТ СОЗДАН            │");
                Console.WriteLine("├─────────────────────────────────────────┤");
                Console.WriteLine($"│  ID:       {id,-30}│");
                Console.WriteLine($"│  Название: {name,-30}│");
                Console.WriteLine($"│  Цена:     {price,-30}│");
                Console.WriteLine($"│  Время:    {DateTime.Now:dd.MM.yyyy HH:mm:ss}               │");
                Console.WriteLine("└─────────────────────────────────────────┘");
                Console.ResetColor();
            }
            else if (topic == "order-created")
            {
                var id = doc.GetProperty("OrderId").GetInt32();
                var productName = doc.GetProperty("ProductName").GetString();
                var quantity = doc.GetProperty("Quantity").GetInt32();
                var total = doc.GetProperty("TotalAmount").GetDecimal();
                var status = doc.GetProperty("Status").GetString();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("┌─────────────────────────────────────────┐");
                Console.WriteLine("│           НОВЫЙ ЗАКАЗ СОЗДАН            │");
                Console.WriteLine("├─────────────────────────────────────────┤");
                Console.WriteLine($"│  ID заказа: {id,-29}│");
                Console.WriteLine($"│  Товар:     {productName,-29}│");
                Console.WriteLine($"│  Кол-во:    {quantity,-29}│");
                Console.WriteLine($"│  Сумма:     {total,-29}│");
                Console.WriteLine($"│  Статус:    {status,-29}│");
                Console.WriteLine($"│  Время:     {DateTime.Now:dd.MM.yyyy HH:mm:ss}              │");
                Console.WriteLine("└─────────────────────────────────────────┘");
                Console.ResetColor();
            }
        }
    }
}

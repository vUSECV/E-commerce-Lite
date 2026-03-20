using NotificationService;
using NotificationService.Kafka;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<NotificationConsumer>();

var host = builder.Build();
host.Run();

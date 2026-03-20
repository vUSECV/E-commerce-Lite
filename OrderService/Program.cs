using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.HttpClients;
using OrderService.Kafka;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<KafkaProducer>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var handler = new HttpClientHandler
{
    UseProxy = false
};

builder.Services.AddHttpClient<ProductHttpClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:ProductService"]!);
}).ConfigurePrimaryHttpMessageHandler(() => handler);

builder.Services.AddHttpClient<InventoryHttpClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:InventoryService"]!);
}).ConfigurePrimaryHttpMessageHandler(() => handler);

builder.Services.AddHttpClient<PaymentHttpClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:PaymentService"]!);
}).ConfigurePrimaryHttpMessageHandler(() => handler);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();

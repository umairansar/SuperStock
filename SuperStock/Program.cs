using SuperStock;
using SuperStock.Cache;
using SuperStock.Infrastructure.MessageBus;
using SuperStock.Infrastructure.Persistence;
using SuperStock.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.RegisterTimeoutPolicies();

builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("Database:MongoDb"));
builder.Services.AddSingleton<Database>();
builder.Services.AddHostedService<DatabaseInitializer>();

var hostId = Environment.GetEnvironmentVariable("STOCK_HOST_ID") ?? Environment.MachineName;
builder.Services.AddSingleton(new HostInfo(hostId));

builder.Services.Configure<MessageBusSettings>(builder.Configuration.GetSection("MessageBus:Redis"));
builder.Services.AddSingleton<MessageBus>();
builder.Services.AddHostedService<CacheKeyUpdateConsumer>();

builder.Services.AddScoped<IOneStockService, OneStockService>();
builder.Services.AddScoped<IManyStockService, ManyStockService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();
app.UseRequestTimeouts();           
app.MapControllers();

app.Run();
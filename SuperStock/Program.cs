using SuperStock;
using SuperStock.Repository;
using SuperStock.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.RegisterTimeoutPolicies();

builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection("SuperStockDatabase"));

builder.Services.AddSingleton<Database>();
builder.Services.AddHostedService<DatabaseInitializer>();
builder.Services.AddScoped<IOneStockService, OneStockService>();

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
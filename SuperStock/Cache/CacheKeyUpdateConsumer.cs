using System.Text.Json;
using StackExchange.Redis;
using SuperStock.Infrastructure.MessageBus;
using SuperStock.Models;

namespace SuperStock.Cache;

public class CacheKeyUpdateConsumer(MessageBus messageBus, HostInfo hostInfo) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var oneStockChannel = RedisChannel.Literal(messageBus.OneStockChannel);
        await messageBus.OneStockSubscriber.SubscribeAsync(oneStockChannel, (_, message) =>
        {
            var eventDto = JsonSerializer.Deserialize<StockUpdateEventDto>(message!);

            if (eventDto?.Host == hostInfo.Id)
            {
                Console.WriteLine("Ignoring echo message: {0} - {1}", oneStockChannel, JsonSerializer.Serialize(eventDto));
                return;
            }

            Console.WriteLine("Received message: {0} - {1}", oneStockChannel, JsonSerializer.Serialize(eventDto));
        });
        
        var multiStockChannel = RedisChannel.Literal(messageBus.MultiStockChannel);
        await messageBus.MultiStockSubscriber.SubscribeAsync(multiStockChannel, (_, message) =>
        {
            var eventDto = JsonSerializer.Deserialize<StockUpdateEventDto>(message!);

            Console.WriteLine("Received message: {0} - {1}", multiStockChannel, JsonSerializer.Serialize(eventDto));
        });
    }
}
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace SuperStock.Infrastructure.MessageBus;

public class MessageBus
{
    public readonly ConnectionMultiplexer Connection;
    public readonly string OneStockChannel;
    public readonly ISubscriber OneStockSubscriber;
    public readonly string MultiStockChannel;
    public readonly ISubscriber MultiStockSubscriber;
    
    public MessageBus(IOptions<MessageBusSettings> messageBusSettings)
    {
        var connectionString = messageBusSettings.Value.ConnectionString;
        Connection = ConnectionMultiplexer.Connect(connectionString);
        
        OneStockChannel = messageBusSettings.Value.OneStockChannel;
        OneStockSubscriber = Connection.GetSubscriber(OneStockChannel);
        
        MultiStockChannel = messageBusSettings.Value.MultiStockChannel;
        MultiStockSubscriber = Connection.GetSubscriber(MultiStockChannel);
    }
}
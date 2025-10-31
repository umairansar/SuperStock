namespace SuperStock.Infrastructure.MessageBus;

public class MessageBusSettings
{
    public string ConnectionString { get; set; }
    public string OneStockChannel { get; set; }
    public string MultiStockChannel { get; set; }
}
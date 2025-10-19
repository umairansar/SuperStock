using MongoDB.Driver;
using SuperStock.Repository;

namespace SuperStock.Services;

public class OneStockService(Database database)
{
    private static int s_InMemoryStock = 5000;
    private static int s_FastSold = 0;
    private static int s_SafeSold = 0;
    
    public async Task<List<Ticket>> PeekViaDb()
    {
        return await database.TicketCollection.Find(_ => true).ToListAsync();
    }

    //https://www.mongodb.com/docs/manual/tutorial/model-data-for-atomic-operations/?utm_source=chatgpt.com#pattern
    public async Task<int> BuySafe(string traceId)
    {
        var filter = Builders<Ticket>.Filter.And(
            Builders<Ticket>.Filter.Eq(t => t.Id, "dcbc9f373e7c96cae045a587"),
            Builders<Ticket>.Filter.Gte(t => t.Stock, 1)
        );
        var update = Builders<Ticket>.Update.Inc(t => t.Stock, -1);
        var options = new FindOneAndUpdateOptions<Ticket>() { ReturnDocument = ReturnDocument.After };
        var res = await database.TicketCollection.FindOneAndUpdateAsync(filter, update, options);
        
        Console.WriteLine("{0} > Remaining safe stock: {1}", traceId, res?.Stock ?? 0);
        if (res == null)
        {
            Console.WriteLine("{0} > Safe Tickets Sold: {1}", traceId, s_SafeSold);
        }
        else
        {
            Interlocked.Increment(ref s_SafeSold);
        }
        
        return res?.Stock ?? 0;
    }
    
    
    public async Task<List<Ticket>> PeekViaCache()
    {
        return [new Ticket { Id = "", Stock = s_InMemoryStock }];
    }

    //Modified Source: https://stackoverflow.com/a/13056904/30097388
    public async Task<int> BuyFast(string traceId)
    {
        int originalValue = 0, newValue = 0;
        do
        {
            originalValue = s_InMemoryStock;
            if (originalValue <= 0) break;
            newValue = originalValue - 1;
        } while (Interlocked.CompareExchange(ref s_InMemoryStock, newValue, originalValue)  != originalValue);
        
        Console.WriteLine("{0} > Remaining fast stock: {1}", traceId, newValue);
        if (newValue == 0 && originalValue != 1)
        {
            Console.WriteLine("{0} > Fast Tickets Sold: {1}", traceId, s_FastSold);
        }
        else
        {
            Interlocked.Increment(ref s_FastSold);
        }
        
        return newValue;
    }
}
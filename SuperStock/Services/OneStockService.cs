using MongoDB.Driver;
using SuperStock.Repository;

namespace SuperStock.Services;

public class OneStockService(Database database)
{
    public async Task<List<Ticket>> Peek()
    {
        return await database.TicketCollection.Find(_ => true).ToListAsync();
    }

    public async Task<int> BuySafe()
    {
        var filter = Builders<Ticket>.Filter.And(
            Builders<Ticket>.Filter.Eq(t => t.Id, "dcbc9f373e7c96cae045a587"),
            Builders<Ticket>.Filter.Gte(t => t.Stock, 1)
        );
        var update = Builders<Ticket>.Update.Inc(t => t.Stock, -1);
        var options = new FindOneAndUpdateOptions<Ticket>() { ReturnDocument = ReturnDocument.After };
        
        //https://www.mongodb.com/docs/manual/tutorial/model-data-for-atomic-operations/?utm_source=chatgpt.com#pattern
        var res = await database.TicketCollection.FindOneAndUpdateAsync(filter, update, options);
        Console.WriteLine("Remaining stock: {0}", res?.Stock);
        return res.Stock;
    }
}
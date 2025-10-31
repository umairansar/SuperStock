using MongoDB.Driver;
using SuperStock.Cache;
using SuperStock.Domain;
using SuperStock.Infrastructure.Persistence;

namespace SuperStock.Services;

public interface IOneStockService
{
    Task<Ticket> PeekSafe();
    Task<bool> BuySafe(string traceId);
    Ticket PeekFastAtomic();
    Task<bool> BuyFastAtomic(string traceId);
    Ticket PeekFastSignal();
    bool BuyFastSignal(string traceId);
    Ticket PeekFastLocking();
    bool BuyFastLocking(string traceId);
}

public class OneStockService(Database database) : IOneStockService
{
    private static AutoResetEvent _resetEvent = new (true);
    private static readonly object _locker = new();
    
    public async Task<Ticket> PeekSafe()
    {
        return await database.TicketCollection.Find(_ => true).FirstOrDefaultAsync();
    }

    //https://www.mongodb.com/docs/manual/tutorial/model-data-for-atomic-operations/?utm_source=chatgpt.com#pattern
    public async Task<bool> BuySafe(string traceId)
    {
        var bought = false;
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
            Console.WriteLine("{0} > Safe Tickets Sold: {1}", traceId, MemoryCache.SafeSold);
        }
        else
        {
            bought = true;
            Interlocked.Increment(ref MemoryCache.SafeSold);
        }

        return bought;
    }
    
    
    public Ticket PeekFastAtomic()
    {
        return new Ticket { Id = "", Stock = MemoryCache.InMemoryAtomicStock };
    }

    //Modified Source: https://stackoverflow.com/a/13056904/30097388
    public async Task<bool> BuyFastAtomic(string traceId)
    {
        var bought = false;
        int originalValue = 0, newValue = 0;
        do
        {
            originalValue = MemoryCache.InMemoryAtomicStock;
            if (originalValue <= 0) break;
            newValue = originalValue - 1;
        } while (Interlocked.CompareExchange(ref MemoryCache.InMemoryAtomicStock, newValue, originalValue)  != originalValue);
        
        Console.WriteLine("{0} > Remaining fast stock: {1}", traceId, newValue);
        if (newValue == 0 && originalValue != 1)
        {
            Console.WriteLine("{0} > Fast Tickets Sold: {1}", traceId, MemoryCache.FastAtomicSold);
        }
        else
        {
            bought = true;
            Interlocked.Increment(ref MemoryCache.FastAtomicSold);
        }
        
        return bought;
    }
    
    public Ticket PeekFastSignal()
    {
        return new Ticket { Id = "", Stock = MemoryCache.InMemorySignalStock };
    }

    public bool BuyFastSignal(string traceId)
    {
        _resetEvent.WaitOne();
        if (MemoryCache.InMemorySignalStock == 0)
        {
            _resetEvent.Set();
            return false;
        }
        
        MemoryCache.InMemorySignalStock -= 1;
        MemoryCache.FastSignalSold += 1;
        
        var capturedInMemorySignalStock = MemoryCache.InMemorySignalStock;
        var capturedFastSignalSold = MemoryCache.FastSignalSold;
        _resetEvent.Set();
        
        Console.WriteLine("{0} > Remaining fast stock: {1}", traceId, capturedInMemorySignalStock);
        if (capturedInMemorySignalStock == 0)
        {
            Console.WriteLine("{0} > Fast Tickets Sold: {1}", traceId, capturedFastSignalSold);
        }

        return true;
    }

    public Ticket PeekFastLocking()
    {
        return new Ticket { Id = "", Stock = MemoryCache.InMemoryLockingStock };
    }

    public bool BuyFastLocking(string traceId)
    {
        int capturedInMemoryLockingStock, capturedFastLockingSold;
        lock (_locker)
        {
            if (MemoryCache.InMemoryLockingStock == 0)
            {
                return false;
            }
        
            MemoryCache.InMemoryLockingStock -= 1;
            MemoryCache.FastLockingSold += 1;
        
            capturedInMemoryLockingStock = MemoryCache.InMemoryLockingStock;
            capturedFastLockingSold = MemoryCache.FastLockingSold;
        }
        
        Console.WriteLine("{0} > Remaining fast stock: {1}", traceId, capturedInMemoryLockingStock);
        if (capturedInMemoryLockingStock == 0)
        {
            Console.WriteLine("{0} > Fast Tickets Sold: {1}", traceId, capturedFastLockingSold);
        }

        return true;
    }
}
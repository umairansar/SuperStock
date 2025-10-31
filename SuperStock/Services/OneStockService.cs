using MongoDB.Driver;
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
    private static int s_InMemoryAtomicStock = 15000;
    private static int s_InMemorySignalStock = 15000;
    private static int s_InMemoryLockingStock = 15000;
    private static int s_FastAtomicSold = 0;
    private static int s_FastSignalSold = 0;
    private static int s_FastLockingSold = 0;
    private static int s_SafeSold = 0;
    
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
            Console.WriteLine("{0} > Safe Tickets Sold: {1}", traceId, s_SafeSold);
        }
        else
        {
            bought = true;
            Interlocked.Increment(ref s_SafeSold);
        }

        return bought;
    }
    
    
    public Ticket PeekFastAtomic()
    {
        return new Ticket { Id = "", Stock = s_InMemoryAtomicStock };
    }

    //Modified Source: https://stackoverflow.com/a/13056904/30097388
    public async Task<bool> BuyFastAtomic(string traceId)
    {
        var bought = false;
        int originalValue = 0, newValue = 0;
        do
        {
            originalValue = s_InMemoryAtomicStock;
            if (originalValue <= 0) break;
            newValue = originalValue - 1;
        } while (Interlocked.CompareExchange(ref s_InMemoryAtomicStock, newValue, originalValue)  != originalValue);
        
        Console.WriteLine("{0} > Remaining fast stock: {1}", traceId, newValue);
        if (newValue == 0 && originalValue != 1)
        {
            Console.WriteLine("{0} > Fast Tickets Sold: {1}", traceId, s_FastAtomicSold);
        }
        else
        {
            bought = true;
            Interlocked.Increment(ref s_FastAtomicSold);
        }
        
        return bought;
    }
    
    public Ticket PeekFastSignal()
    {
        return new Ticket { Id = "", Stock = s_InMemorySignalStock };
    }

    public bool BuyFastSignal(string traceId)
    {
        _resetEvent.WaitOne();
        if (s_InMemorySignalStock == 0)
        {
            _resetEvent.Set();
            return false;
        }
        
        s_InMemorySignalStock -= 1;
        s_FastSignalSold += 1;
        
        var capturedInMemorySignalStock = s_InMemorySignalStock;
        var capturedFastSignalSold =  s_FastSignalSold;
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
        return new Ticket { Id = "", Stock = s_InMemoryLockingStock };
    }

    public bool BuyFastLocking(string traceId)
    {
        int capturedInMemoryLockingStock, capturedFastLockingSold;
        lock (_locker)
        {
            if (s_InMemoryLockingStock == 0)
            {
                return false;
            }
        
            s_InMemoryLockingStock -= 1;
            s_FastLockingSold += 1;
        
            capturedInMemoryLockingStock = s_InMemoryLockingStock;
            capturedFastLockingSold =  s_FastLockingSold;
        }
        
        Console.WriteLine("{0} > Remaining fast stock: {1}", traceId, capturedInMemoryLockingStock);
        if (capturedInMemoryLockingStock == 0)
        {
            Console.WriteLine("{0} > Fast Tickets Sold: {1}", traceId, capturedFastLockingSold);
        }

        return true;
    }
}
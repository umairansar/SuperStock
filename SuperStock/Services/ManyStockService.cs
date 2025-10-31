using SuperStock.Cache;
using SuperStock.Domain;
using SuperStock.Models;

namespace SuperStock.Services;

public interface IManyStockService
{
    ProductDto PeekFastAtomic(string id);
    bool BuyFastAtomic(string id, string traceId);
}

public class ManyStockService : IManyStockService
{
    public ProductDto PeekFastAtomic(string id)
    {
        var product = MemoryCache.InMemoryStockRecord[id];
        return new ProductDto(product.Id, product.Stock, MemoryCache.InMemorySoldRecord[id].Value);
    }

    // https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Collections/Concurrent/ConcurrentDictionary.cs
    // All public and protected members of are thread-safe and may be used concurrently from multiple threads.
    public bool BuyFastAtomic(string id, string traceId)
    {
        Product? existingProduct;
        Product newProduct = null!;
        var bought = true;
        do
        {
            var success = MemoryCache.InMemoryStockRecord.TryGetValue(id, out existingProduct); //thread safe
            if (!success)
            {
                Console.WriteLine("{0} > Failed > Product id {1} does not exist.", traceId, id);
                break;
            }
            
            if (existingProduct!.Stock == 0)
            {
                bought = false; 
                break;
            }
            
            newProduct = new Product { Id = existingProduct.Id, Stock = existingProduct.Stock - 1 };
        } while (!MemoryCache.InMemoryStockRecord.TryUpdate(id, newProduct,existingProduct)); // atomic
        
        if (bought)
        {
            Interlocked.Increment(ref MemoryCache.InMemorySoldRecord[id].Value);
            Console.WriteLine("{0} > Success > Remaining {1} fast stock: {2}", traceId, newProduct?.Id, newProduct?.Stock);
        }
        else
        {
            Console.WriteLine("{0} > Success > Remaining {1} fast stock: {2}", traceId, existingProduct?.Id, existingProduct?.Stock);
        }

        return bought; 
    }
}
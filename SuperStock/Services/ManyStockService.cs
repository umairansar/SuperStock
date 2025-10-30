using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using SuperStock.Models;
using SuperStock.Repository;
using SuperStock.Utils;

namespace SuperStock.Services;

public interface IManyStockService
{
    ProductDto PeekFastAtomic(string id);
    bool BuyFastAtomic(string id, string traceId);
}

public class ManyStockService : IManyStockService
{
    private static readonly string[] products = ["NVDA", "TSLA", "AMD"];
    private static ConcurrentDictionary<string, Product> s_inMemoryStockRecord = new ()
    {
        [products[0].ToProductId()] = new Product{Id = products[0].ToProductId(), Stock = 15000},
        [products[1].ToProductId()] = new Product{Id = products[1].ToProductId(), Stock = 15000},
        [products[2].ToProductId()] = new Product{Id = products[2].ToProductId(), Stock = 15000}
    };
    
    // https://stackoverflow.com/a/33779778/30097388
    private static ConcurrentDictionary<string, StrongBox<int>> s_inMemorySoldRecord = new ()
    {
        [products[0].ToProductId()] =  new StrongBox<int>(0),
        [products[1].ToProductId()] =  new StrongBox<int>(0),
        [products[2].ToProductId()] =  new StrongBox<int>(0)
    };
    
    public ProductDto PeekFastAtomic(string id)
    {
        var product = s_inMemoryStockRecord[id];
        return new ProductDto(product.Id, product.Stock, s_inMemorySoldRecord[id].Value);
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
            var success = s_inMemoryStockRecord.TryGetValue(id, out existingProduct); //thread safe
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
        } while (!s_inMemoryStockRecord.TryUpdate(id, newProduct,existingProduct)); // atomic
        
        if (bought)
        {
            Interlocked.Increment(ref s_inMemorySoldRecord[id].Value);
            Console.WriteLine("{0} > Success > Remaining {1} fast stock: {2}", traceId, newProduct?.Id, newProduct?.Stock);
        }
        else
        {
            Console.WriteLine("{0} > Success > Remaining {1} fast stock: {2}", traceId, existingProduct?.Id, existingProduct?.Stock);
        }

        return bought; 
    }
}
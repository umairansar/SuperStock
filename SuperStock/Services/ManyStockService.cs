using System.Collections.Concurrent;
using SuperStock.Repository;
using SuperStock.Utils;

namespace SuperStock.Services;

public interface IManyStockService
{
    Product PeekFastAtomic(string id);
    bool BuyFastAtomic(string id, string traceId);
}

public class ManyStockService : IManyStockService
{
    private static readonly string[] products = ["NVDA", "TSLA", "AMD"];
    private static ConcurrentDictionary<string, Product> s_manyStockService = new ()
    {
        [products[0].ToProductId()] = new Product{Id = products[0].ToProductId(), Stock = 5000},
        [products[1].ToProductId()] = new Product{Id = products[1].ToProductId(), Stock = 5000},
        [products[2].ToProductId()] = new Product{Id = products[2].ToProductId(), Stock = 5000}
    };
    
    public Product PeekFastAtomic(string id)
    {
        return s_manyStockService[id];
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
            var success = s_manyStockService.TryGetValue(id, out existingProduct); //thread safe
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
        } while (!s_manyStockService.TryUpdate(id, newProduct,existingProduct)); // atomic
        
        if (bought)
        {
            Console.WriteLine("{0} > Success > Remaining {1} fast stock: {2}", traceId, newProduct?.Id, newProduct?.Stock);
        }
        else
        {
            Console.WriteLine("{0} > Success > Remaining {1} fast stock: {2}", traceId, existingProduct?.Id, existingProduct?.Stock);
        }

        return bought;
    }
}
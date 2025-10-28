using System.Collections.Concurrent;
using SuperStock.Repository;

namespace SuperStock.Services;

public interface IManyStockService
{
    Product PeekFastAtomic(string id);
    bool BuyFastAtomic(string id, string traceId);
}

public class ManyStockService : IManyStockService
{
    private static readonly string[] s_productIds = ["dcbc9f373e7c96cae045a589", "dcbc9f373e7c96cae045a590", "dcbc9f373e7c96cae045a591"];
    private static ConcurrentDictionary<string, Product> s_manyStockService = new () //https://learn.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2.getoradd?view=net-9.0#system-collections-concurrent-concurrentdictionary-2-getoradd(-0-system-func((-0-1)))
    {
        [s_productIds[0]] = new Product{Id = s_productIds[0], Stock = 5000},
        [s_productIds[1]] = new Product{Id = s_productIds[1], Stock = 5000},
        [s_productIds[2]] = new Product{Id = s_productIds[2], Stock = 5000}
    };
    
    public Product PeekFastAtomic(string id)
    {
        return s_manyStockService[id];
    }

    public bool BuyFastAtomic(string id, string traceId)
    {
        Product existingProduct;
        Product newProduct = null!;
        var bought = true;
        do
        {
            existingProduct = s_manyStockService[id];
            if (existingProduct.Stock == 0)
            {
                bought = false; 
                break;
            }
            newProduct = new Product { Id = existingProduct.Id, Stock = existingProduct.Stock - 1 };;
        } while (s_manyStockService.TryUpdate(id, newProduct,existingProduct)); // atomic
        
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
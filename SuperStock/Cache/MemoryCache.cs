using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using SuperStock.Domain;
using SuperStock.Utils;

namespace SuperStock.Cache;

public static class MemoryCache
{
    // One Stock Cache
    public static int InMemoryAtomicStock = 15000;
    public static int InMemorySignalStock = 15000;
    public static int InMemoryLockingStock = 15000;
    public static int FastAtomicSold = 0;
    public static int FastSignalSold = 0;
    public static int FastLockingSold = 0;
    public static int SafeSold = 0;
    
    // Multi Stock Cache - https://stackoverflow.com/a/33779778/30097388
    private static readonly string[] products = ["NVDA", "TSLA", "AMD"];
    public static ConcurrentDictionary<string, Product> InMemoryStockRecord = new ()
    {
        [products[0].ToProductId()] = new Product{Id = products[0].ToProductId(), Stock = 15000},
        [products[1].ToProductId()] = new Product{Id = products[1].ToProductId(), Stock = 15000},
        [products[2].ToProductId()] = new Product{Id = products[2].ToProductId(), Stock = 15000}
    };
    public static ConcurrentDictionary<string, StrongBox<int>> InMemorySoldRecord = new ()
    {
        [products[0].ToProductId()] =  new StrongBox<int>(0),
        [products[1].ToProductId()] =  new StrongBox<int>(0),
        [products[2].ToProductId()] =  new StrongBox<int>(0)
    };
}
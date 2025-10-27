using SuperStock.Repository;

namespace SuperStock.Services;

public interface IManyStockService
{
    Product PeekFastAtomic();
    Task<bool> BuyFastAtomic(string traceId);
}

public class ManyStockService : IManyStockService
{
    public Product PeekFastAtomic()
    {
        throw new NotImplementedException();
    }

    public Task<bool> BuyFastAtomic(string traceId)
    {
        throw new NotImplementedException();
    }
}
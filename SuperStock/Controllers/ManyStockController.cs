using Microsoft.AspNetCore.Mvc;
using SuperStock.Services;
using SuperStock.Utils;

namespace SuperStock.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ManyStockController(IManyStockService manyStockService) : ControllerBase
{
    [HttpGet("Cache/PeekFastAtomic/{product}")]
    public IActionResult PeekFastAtomic(string product)
    {
        var productId = product.ToProductId();
        var res =  manyStockService.PeekFastAtomic(productId);
        return Ok(res);
    }
    
    [HttpPost("Cache/PeekFastAtomic/{product}")]
    public IActionResult BuyFastAtomic(string product)
    {
        var productId = product.ToProductId();
        var traceId = HttpContext.TraceIdentifier;
        var res =  manyStockService.BuyFastAtomic(productId, traceId);
        return Ok(res);
    }
}
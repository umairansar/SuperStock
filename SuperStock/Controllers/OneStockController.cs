using Microsoft.AspNetCore.Mvc;
using SuperStock.Services;

namespace SuperStock.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OneStockController(OneStockService oneStockService) : ControllerBase
{
    [HttpGet("Db/Peek")]
    public async Task<IActionResult> PeekViaDb()
    {
        var res = await oneStockService.PeekViaDb();
        return Ok(res);
    }
    
    [HttpPost("Db/Buy")]
    public async Task<IActionResult> BuyViaDb()
    {
        var traceId= HttpContext.TraceIdentifier;      
        var res = await oneStockService.BuySafe(traceId); //Something with siege undercounting successful requests, appears underselling
        return Ok($"Remaining stock: {res}");
    }
    
    [HttpGet("Cache/Peek")]
    public async Task<IActionResult> PeekViaCache()
    {
        var res = await oneStockService.PeekViaCache();
        return Ok(res);
    }
    
    [HttpPost("Cache/Buy")]
    public async Task<IActionResult> BuyViaCache()
    {
        var traceId = HttpContext.TraceIdentifier;
        var res = await oneStockService.BuyFast(traceId);
        return Ok($"Remaining stock: {res}");
    }
}
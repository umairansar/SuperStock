using Microsoft.AspNetCore.Mvc;
using SuperStock.Services;

namespace SuperStock.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OneStockController(IOneStockService oneStockService) : ControllerBase
{
    [HttpGet("Db/Peek")]
    public async Task<IActionResult> PeekSafe()
    {
        var res = await oneStockService.PeekSafe();
        return Ok(res);
    }
    
    [HttpPost("Db/Buy")]
    public async Task<IActionResult> BuySafe()
    {
        var traceId= HttpContext.TraceIdentifier;      
        var res = await oneStockService.BuySafe(traceId); //Something with siege undercounting successful requests, appears underselling
        var remaining = await oneStockService.PeekSafe();
        return res ? 
            Ok($"Bought successfully. Remaining stock: {remaining}") :
            BadRequest($"Already sold out. Stock: {remaining}");
    }
    
    [HttpGet("Cache/Atomic/Peek")]
    public IActionResult PeekFastAtomic()
    {
        var res = oneStockService.PeekFastAtomic();
        return Ok(res);
    }
    
    [HttpPost("Cache/Atomic/Buy")]
    public async Task<IActionResult> BuyFastAtomic()
    {
        var traceId = HttpContext.TraceIdentifier;
        var res = await oneStockService.BuyFastAtomic(traceId);
        var remaining = oneStockService.PeekFastAtomic();
        return res ? 
            Ok($"Bought successfully. Remaining stock: {remaining}") :
            BadRequest($"Already sold out. Stock: {remaining}");
    }
    
    [HttpGet("Cache/Signal/Peek")]
    public IActionResult PeekFastSignal()
    {
        var res = oneStockService.PeekFastSignal();
        return Ok(res);
    }
    
    [HttpPost("Cache/Signal/Buy")]
    public IActionResult BuyFastSignal()
    {
        var traceId = HttpContext.TraceIdentifier;
        var res = oneStockService.BuyFastSignal(traceId);
        var remaining = oneStockService.PeekFastSignal();
        return res ? 
            Ok($"Bought successfully. Remaining stock: {remaining}") :
            BadRequest($"Already sold out. Stock: {remaining}");
    }
    
    [HttpGet("Cache/Lock/Peek")]
    public IActionResult PeekFastLocking()
    {
        var res =  oneStockService.PeekFastLocking();
        return Ok(res);
    }
    
    [HttpPost("Cache/Lock/Buy")]
    public IActionResult BuyFastLocking()
    {
        var traceId = HttpContext.TraceIdentifier;
        var res = oneStockService.BuyFastLocking(traceId);
        var remaining = oneStockService.PeekFastLocking();
        return res ? 
            Ok($"Bought successfully. Remaining stock: {remaining}") :
            BadRequest($"Already sold out. Stock: {remaining}");
    }
}
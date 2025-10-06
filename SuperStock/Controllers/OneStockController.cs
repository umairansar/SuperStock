using Microsoft.AspNetCore.Mvc;
using SuperStock.Services;

namespace SuperStock.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OneStockController(OneStockService oneStockService) : ControllerBase
{
    [HttpGet("Db/Peek")]
    public async Task<IActionResult> Peek()
    {
        var res = await oneStockService.Peek();
        return Ok(res);
    }
    
    [HttpPost("Db/Buy")]
    public async Task<IActionResult> ViaDb()
    {
        var res = await oneStockService.BuySafe();
        return Ok($"Remaining stock: {res}");
    }
    
    [HttpPost("Cache/Buy")]
    public IActionResult ViaCache()
    {
        return Ok();
    }
}
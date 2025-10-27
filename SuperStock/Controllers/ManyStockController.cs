using Microsoft.AspNetCore.Mvc;
using SuperStock.Services;

namespace SuperStock.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ManyStockController(IManyStockService manyStockService) : ControllerBase
{
    [HttpGet("Db/PeekFastAtomic")]
    public async Task<IActionResult> PeekFastAtomic()
    {
        var res =  manyStockService.PeekFastAtomic();
        
        return Ok(res);
    }
}
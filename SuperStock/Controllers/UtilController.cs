using Microsoft.AspNetCore.Mvc;
using SuperStock.Utils;

namespace SuperStock.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class UtilController : ControllerBase
{
    [HttpPost("Open")]
    public IActionResult Set()
    {
        Gatekeeper.Reset.Set();
        return Ok();
    }
    
    [HttpPost("Close")]
    public IActionResult Reset()
    {
        Gatekeeper.Reset.Reset();
        return Ok();
    }
}
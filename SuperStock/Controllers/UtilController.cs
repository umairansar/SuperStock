using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using SuperStock.Cache;
using SuperStock.Infrastructure.MessageBus;
using SuperStock.Models;
using SuperStock.Utils;

namespace SuperStock.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class UtilController(MessageBus messageBus, HostInfo hostInfo) : ControllerBase
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

    [HttpPost("Publish")]
    public async Task<IActionResult> Publish()
    {
        var res = await messageBus.OneStockSubscriber.PublishAsync(
            RedisChannel.Literal(messageBus.OneStockChannel),
            JsonSerializer.Serialize(
                new StockUpdateEventDto{Host = hostInfo.Id, Key = "dcbc9f373e7c96cae045a587", Value = "4999"}));
        return Ok(res);
    }
}
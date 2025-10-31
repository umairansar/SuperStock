using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using SuperStock.Utils;

namespace SuperStock.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class WeatherController : ControllerBase
{
    [HttpGet("Throttle")]
    [RequestTimeout("ThrottledPolicy")]
    public async Task<IActionResult> ThrottledWeather(CancellationToken ct)
    {
        var result = await Throttler.Run(GetWeather, ct);
        
        return Ok(result);
    }
    
    [HttpGet("Throttle/Try")]
    [RequestTimeout("ThrottledPolicy")]
    public async Task<IActionResult> TryThrottledWeather(CancellationToken ct)
    {
        var (isSuccess, result) = await Throttler.TryRun(GetWeather, ct);
        
        return !isSuccess ? 
            StatusCode(503, "Throttle Capacity Reached!") : 
            Ok(result);
    }
    
    [HttpGet("Gatekeep")]
    [RequestTimeout("GatedPolicy")]
    public async Task<IActionResult> GatedWeather(CancellationToken ct)
    {
        Gatekeeper.Reset.Wait(ct);
        
        var result = await GetWeather(ct);
        
        return Ok(result);
    }
    
    [HttpGet("Gatekeep/Async")]
    [RequestTimeout("GatedPolicy")]
    public async Task<IActionResult> GatedWeatherAsync(CancellationToken ct)
    {
        await Gatekeeper.Reset.WaitAsync(ct);
        
        var result = await GetWeather(ct);
        
        return Ok(result);
    }

    private async Task<WeatherDto[]> GetWeather(CancellationToken ct)
    {
        var summaries = new[] { "Freezing", "Chilly", "Cool", "Mild", "Warm", "Hot", "Hell" };
        
        var forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherDto(summaries[Random.Shared.Next(summaries.Length)])).ToArray();

        await Task.Delay(1000, ct);

        return forecast;
    }
}
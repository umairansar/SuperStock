using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using SuperStock.Utils;

namespace SuperStock.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class WeatherController : ControllerBase
{
    [HttpGet("Throttle")]
    [RequestTimeout("ThrottlePolicy")]
    public async Task<IActionResult> ThrottledWeather()
    {
        var result = await Throttler.Run(GetWeather);
        
        return Ok(result);
    }
    
    [HttpGet("Gatekeep")]
    [RequestTimeout("GatedPolicy")]
    public async Task<IActionResult> GatedWeather(CancellationToken ct)
    {
        Gatekeeper.Reset.Wait(ct);
        
        var result = await GetWeather();
        
        return Ok(result);
    }
    
    [HttpGet("Gatekeep/Async")]
    [RequestTimeout("GatedPolicy")]
    public async Task<IActionResult> GatedWeatherAsync(CancellationToken ct)
    {
        await Gatekeeper.Reset.WaitAsync(ct);
        
        var result = await GetWeather();
        
        return Ok(result);
    }

    private async Task<WeatherForecast[]> GetWeather()
    {
        var summaries = new[] { "Freezing", "Chilly", "Cool", "Mild", "Warm", "Hot", "Hell" };
        
        var forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast(summaries[Random.Shared.Next(summaries.Length)])).ToArray();

        await Task.Delay(1000);

        return forecast;
    }
}
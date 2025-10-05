using Microsoft.AspNetCore.Http.Timeouts;

namespace SuperStock;

public static class StartupExtensions
{
    //https://learn.microsoft.com/en-us/aspnet/core/performance/timeouts?view=aspnetcore-9.0#specify-the-status-code-in-a-policy
    public static void RegisterTimeoutPolicies(this IServiceCollection services)
    {
        services.AddRequestTimeouts(options =>
        {
            options.AddPolicy("ThrottlePolicy", new RequestTimeoutPolicy()
            {
                Timeout = TimeSpan.FromSeconds(10),
                WriteTimeoutResponse = async context =>
                {
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync("Timeout: throttle capacity reached.");
                }
            });
            options.AddPolicy("GatedPolicy", new RequestTimeoutPolicy
            {
                Timeout = TimeSpan.FromSeconds(10),
                WriteTimeoutResponse = async context =>
                {
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync("Timeout: gate is closed.");
                }
            });
        });
    }
}
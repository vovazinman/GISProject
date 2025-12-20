using Microsoft.Extensions.Configuration;

namespace GIS3DEngine.Demo;

public static class AppConfig
{
    private static IConfiguration? _configuration;

    public static IConfiguration Configuration
    {
        get
        {
            if (_configuration == null)
            {
                _configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddJsonFile("appsettings.local.json", optional: true) 
                    .Build();
            }
            return _configuration;
        }
    }

 
    public static string AnthropicApiKey => Configuration["Anthropic:ApiKey"] ?? "";
    public static string AnthropicModel => Configuration["Anthropic:Model"] ?? "claude-sonnet-4-20250514";
    public static double DefaultAltitude => double.Parse(Configuration["Drone:DefaultAltitude"] ?? "50");
    public static double DefaultSpeed => double.Parse(Configuration["Drone:DefaultSpeed"] ?? "10");
}
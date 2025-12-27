using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// ========== Read Secrets ==========
var anthropicKey = builder.Configuration["Anthropic:ApiKey"];
var hasAnthropicKey = !string.IsNullOrEmpty(anthropicKey);

if (hasAnthropicKey)
{
    Console.WriteLine(" Anthropic API key found");
}
else
{
    Console.WriteLine(" Anthropic API key not found - AI features disabled");
    Console.WriteLine(" Run: dotnet user-secrets set \"Anthropic:ApiKey\" \"your-key\"");
}

// Infrastructure
var redis = builder.AddRedis("redis");

// Log server
var seq = builder.AddContainer("seq", "datalust/seq")
    .WithEnvironment("ACCEPT_EULA", "Y")
    .WithEnvironment("SEQ_FIRSTRUN_NOAUTHENTICATION", "true")
    .WithHttpEndpoint(port: 5341, targetPort: 80, name: "seq-ui");

// Backend API
var api = builder.AddProject<Projects.GIS3DEngine_WebApi>("api")
    .WithReference(redis)
    .WithEnvironment("SEQ_URL", "http://localhost:5341")
    .WithExternalHttpEndpoints();

// Inject Anthropic key if exists
if (hasAnthropicKey)
{
    api.WithEnvironment("Anthropic__ApiKey", anthropicKey);
    api.WithEnvironment("Anthropic__Model", "claude-sonnet-4-20250514");
}

var frontendPath = Path.GetFullPath(
    Path.Combine(builder.AppHostDirectory, "../../../drone-control-app"));

// React Frontend (npm)
var frontend = builder.AddNpmApp("frontend", frontendPath, "dev")
    .WithReference(api)
    .WithHttpEndpoint(port: 5173, env: "PORT")
    .WithExternalHttpEndpoints();
    //.PublishAsDockerFile();



builder.Build().Run();

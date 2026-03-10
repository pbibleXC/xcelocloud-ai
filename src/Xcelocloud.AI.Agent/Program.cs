using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xcelocloud.AI.Agent;
using Xcelocloud.AI.Agent.Models;
using Xcelocloud.AI.Agent.Orchestrators;

var appConfigConnectionString = Environment.GetEnvironmentVariable("XC_AppConfiguration_ConnectionString") ??
        throw new ArgumentNullException("XC_AppConfiguration_ConnectionString");
var appConfigLabel = Environment.GetEnvironmentVariable("XC_AppConfiguration_Label") ??
        throw new ArgumentNullException("XC_AppConfiguration_Label");

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .AddAzureAppConfiguration(options =>
    {
        options.Connect(appConfigConnectionString)
            .UseFeatureFlags()
            .Select(KeyFilter.Any, appConfigLabel);
    })
    .Build();

var allowedCorsOrigins = (configuration["XC_Xcelohub_AI_AgentServer_AllowedCorsOrigins"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

var builder = FunctionsApplication.CreateBuilder(args);

// CORS for local development is configured via the "Host.CORS" key in local.settings.json.
// For deployed environments, configure CORS origins in the Azure Portal
// (Function App > Settings > CORS) to match your Blazor app URL.
builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.Configure<McpServerOptions>(options =>
{
    options.BaseUrl = configuration["XC_Xcelohub_AI_McpServer_BaseUrl"] ?? string.Empty;
    options.FunctionKey = configuration["XC_Xcelohub_AI_McpServer_FunctionKey"] ?? string.Empty;
});

builder.Services.Configure<OpenRouterOptions>(options =>
{
    options.ApiKey = configuration["XC_Xcelohub_AI_OpenRouter_ApiKey"] ?? string.Empty;
    options.ModelId = configuration["XC_Xcelohub_AI_OpenRouter_ModelId"] ?? string.Empty;
});

builder.Services.AddSingleton<IHaloReportingOrchestrator, HaloReportingOrchestrator>();
builder.Services.AddSingleton<IHaloChatOrchestrator, HaloChatOrchestrator>();

builder.Build().Run();

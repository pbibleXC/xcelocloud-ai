using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xcelocloud.AI.Mcp;
using Xcelocloud.SDK.Halo.Models;
using Xcelocloud.SDK.Halo.Services;
using Xcelocloud.Service.Shared.Helpers;

var appConfigConnectionString = Environment.GetEnvironmentVariable("XC_AppConfiguration_ConnectionString") ??
        throw new ArgumentNullException("XC_AppConfiguration_ConnectionString");
var appConfigLabel = Environment.GetEnvironmentVariable("XC_AppConfiguration_Label") ??
        throw new ArgumentNullException("XC_AppConfiguration_Label");

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile($"appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .AddAzureAppConfiguration(options =>
    {
        options.Connect(appConfigConnectionString)
            .UseFeatureFlags()
            .Select(KeyFilter.Any, appConfigLabel);
    })
    .Build();

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddSingleton<IHaloApiFactory>(s =>
{
    return new HaloApiFactory(new HaloCredentials
    {
        TokenUrl = configuration["XC_Xcelohub_Portal_Internal_Halo_TokenUrl"] ?? string.Empty,
        BaseUrl = configuration["XC_Xcelohub_Portal_Internal_Halo_BaseUrl"] ?? string.Empty,
        ClientId = configuration["XC_Xcelohub_Portal_Internal_Halo_ClientId"] ?? string.Empty,
        ClientSecret = configuration["XC_Xcelohub_Portal_Internal_Halo_ClientSecret"] ?? string.Empty,
        Scope = configuration["XC_Xcelohub_Portal_Internal_Halo_Scope"] ?? string.Empty
    });
});
builder.Services.AddSingleton<IHaloQueryHelper, HaloQueryHelper>();

builder
    .ConfigureMcpTool(ToolsInformation.GetAgentUtilization.ToolName)
    .WithProperty(ToolsInformation.GetAgentUtilization.AgentIdsPropertyName, McpToolPropertyType.String, ToolsInformation.GetAgentUtilization.AgentIdsPropertyDescription)
    .WithProperty(ToolsInformation.GetAgentUtilization.StartDatePropertyName, McpToolPropertyType.String, ToolsInformation.GetAgentUtilization.StartDatePropertyDescription)
    .WithProperty(ToolsInformation.GetAgentUtilization.EndDatePropertyName, McpToolPropertyType.String, ToolsInformation.GetAgentUtilization.EndDatePropertyDescription);

builder.Build().Run();

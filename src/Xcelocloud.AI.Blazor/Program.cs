using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Xcelocloud.AI.Blazor.Models;
using Xcelocloud.AI.Blazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ── Configuration ────────────────────────────────────────────────────────────
// Blazor WebAssembly loads configuration from wwwroot/appsettings.json
// and wwwroot/appsettings.{Environment}.json files.
//
// For local development, use wwwroot/appsettings.Development.json
// For production, settings should be provided via the hosting environment
// or a backend API endpoint that fetches from Azure App Configuration.
//
// NOTE: Azure App Configuration connection string-based authentication 
// cannot be used directly from browser due to HMAC header restrictions.

// ── Root components ──────────────────────────────────────────────────────────
builder.RootComponents.Add<Xcelocloud.AI.Blazor.App>("#app");
builder.RootComponents.Add<Microsoft.AspNetCore.Components.Web.HeadOutlet>("head::after");

// ── Agent client options ─────────────────────────────────────────────────────
builder.Services.Configure<AgentClientOptions>(options =>
{
    options.BaseUrl = builder.Configuration["XC_Xcelohub_AI_AgentServer_BaseUrl"] ?? string.Empty;
    options.FunctionKey = builder.Configuration["XC_Xcelohub_AI_AgentServer_FunctionKey"] ?? string.Empty;
});

// ── HTTP client for the Halo Agent function app ──────────────────────────────
builder.Services.AddHttpClient<IHaloAgentClient, HaloAgentClient>((sp, client) =>
{
    var baseUrl = builder.Configuration["XC_Xcelohub_AI_AgentServer_BaseUrl"] ?? string.Empty;
    if (!string.IsNullOrWhiteSpace(baseUrl))
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

    var functionKey = builder.Configuration["XC_Xcelohub_AI_AgentServer_FunctionKey"] ?? string.Empty;
    if (!string.IsNullOrWhiteSpace(functionKey))
        client.DefaultRequestHeaders.Add("x-functions-key", functionKey);
});

// ── FluentUI Blazor ──────────────────────────────────────────────────────────
builder.Services.AddFluentUIComponents();

await builder.Build().RunAsync();

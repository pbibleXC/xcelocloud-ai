using Xcelocloud.AI.Blazor.Models;

namespace Xcelocloud.AI.Blazor.Services;

/// <summary>
/// Provides access to the Halo Reporting Agent API hosted in Xcelocloud.AI.Agent.
/// Configure the base URL and function key via Azure App Configuration:
///   XC_Xcelohub_AI_AgentServer_BaseUrl
///   XC_Xcelohub_AI_AgentServer_FunctionKey
/// </summary>
public interface IHaloAgentClient
{
    Task<HaloChatResponse> ChatAsync(HaloChatRequest request, CancellationToken cancellationToken = default);
}

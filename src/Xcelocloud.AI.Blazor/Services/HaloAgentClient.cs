using System.Net.Http.Json;
using Xcelocloud.AI.Blazor.Models;

namespace Xcelocloud.AI.Blazor.Services;

public class HaloAgentClient : IHaloAgentClient
{
    private readonly HttpClient _httpClient;

    public HaloAgentClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HaloChatResponse> ChatAsync(HaloChatRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/halo/chat", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Halo Agent returned {(int)response.StatusCode}: {error}",
                inner: null,
                statusCode: response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<HaloChatResponse>(cancellationToken)
            ?? new HaloChatResponse { Message = "No response received from the agent." };
    }
}

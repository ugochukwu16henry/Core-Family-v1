using System.Net.Http.Headers;
using System.Net.Http.Json;
using CoreFamily.Admin.Models;

namespace CoreFamily.Admin.Services;

public class AdminApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AdminSessionState _sessionState;

    public AdminApiClient(IHttpClientFactory httpClientFactory, AdminSessionState sessionState)
    {
        _httpClientFactory = httpClientFactory;
        _sessionState = sessionState;
    }

    public void SetToken(string token) => _sessionState.AccessToken = token.Trim();
    public string GetToken() => _sessionState.AccessToken;

    public async Task<IReadOnlyList<AdminUserSummary>> GetUsersAsync()
    {
        var client = CreateClient();
        return await client.GetFromJsonAsync<IReadOnlyList<AdminUserSummary>>("admin/users") ?? [];
    }

    public async Task<AdminUserSummary?> SetUserActiveStatusAsync(Guid userId, bool isActive)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync($"admin/users/{userId}/status", new SetUserActiveStatusRequest(isActive));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AdminUserSummary>();
    }

    public async Task<IReadOnlyList<AdminReviewSummary>> GetFlaggedReviewsAsync()
    {
        var client = CreateClient();
        return await client.GetFromJsonAsync<IReadOnlyList<AdminReviewSummary>>("admin/reviews/flagged") ?? [];
    }

    public async Task<AdminReviewSummary?> SetReviewFlagStatusAsync(Guid reviewId, bool isFlagged)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync($"admin/reviews/{reviewId}/flag", new SetReviewFlagStatusRequest(isFlagged));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AdminReviewSummary>();
    }

    private HttpClient CreateClient()
    {
        var token = _sessionState.AccessToken;
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Admin access token is required. Paste an admin JWT on the Home page first.");

        var client = _httpClientFactory.CreateClient("CoreFamilyApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

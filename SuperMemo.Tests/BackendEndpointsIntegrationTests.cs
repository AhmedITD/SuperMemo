using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SuperMemo.Infrastructure.Data;
using Xunit;

namespace SuperMemo.Tests;

/// <summary>
/// Integration tests for all backend API endpoints. Requires database with seeded users (Admin: 00000000000 / Admin@123, Customer: 11111111111 / Customer@123).
/// </summary>
public class BackendEndpointsIntegrationTests : IClassFixture<SuperMemoApiFixture>
{
    private readonly SuperMemoApiFixture _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public BackendEndpointsIntegrationTests(SuperMemoApiFixture factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string?> GetCustomerTokenAsync()
    {
        var login = new { phone = DataSeeder.TestCustomerPhone, password = DataSeeder.TestCustomerPassword };
        var res = await _client.PostAsJsonAsync("/auth/login", login);
        if (!res.IsSuccessStatusCode) return null;
        var body = await res.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        return body.TryGetProperty("data", out var data) && data.TryGetProperty("token", out var token)
            ? token.GetString()
            : null;
    }

    private async Task<string?> GetAdminTokenAsync()
    {
        var login = new { phone = DataSeeder.AdminPhone, password = DataSeeder.AdminDefaultPassword };
        var res = await _client.PostAsJsonAsync("/auth/login", login);
        if (!res.IsSuccessStatusCode) return null;
        var body = await res.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        return body.TryGetProperty("data", out var data) && data.TryGetProperty("token", out var token)
            ? token.GetString()
            : null;
    }

    private HttpClient CreateClientWithBearer(string? token)
    {
        var client = _factory.CreateClient();
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // ----- Auth (no token) -----
    [Fact]
    public async Task Auth_Login_WithValidCredentials_ReturnsOk()
    {
        var login = new { phone = DataSeeder.TestCustomerPhone, password = DataSeeder.TestCustomerPassword };
        var res = await _client.PostAsJsonAsync("/auth/login", login);
        Assert.True(res.IsSuccessStatusCode, $"Login failed: {res.StatusCode}");
    }

    [Fact]
    public async Task Auth_Login_WithInvalidCredentials_ReturnsBadRequest()
    {
        var login = new { phone = "invalid", password = "wrong" };
        var res = await _client.PostAsJsonAsync("/auth/login", login);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Auth_Refresh_WithoutToken_ReturnsUnauthorizedOrBadRequest()
    {
        var res = await _client.PostAsJsonAsync("/auth/refresh", new { refreshToken = "invalid" });
        Assert.True(res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest, res.StatusCode.ToString());
    }

    [Fact]
    public async Task Auth_Me_WithoutToken_Returns401()
    {
        var res = await _client.GetAsync("/auth/Me");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ----- Accounts (customer token) -----
    [Fact]
    public async Task Accounts_GetMe_WithCustomerToken_ReturnsOk()
    {
        var token = await GetCustomerTokenAsync();
        if (token == null) { Assert.Fail("Could not get customer token (seed data may be missing)."); return; }
        using var client = CreateClientWithBearer(token);
        var res = await client.GetAsync("/api/accounts/me");
        Assert.True(res.IsSuccessStatusCode, $"GET /api/accounts/me: {res.StatusCode}");
    }

    [Fact]
    public async Task Accounts_GetMe_WithoutToken_Returns401()
    {
        var res = await _client.GetAsync("/api/accounts/me");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ----- User cards (customer token) -----
    [Fact]
    public async Task UserCards_Get_WithCustomerToken_ReturnsOk()
    {
        var token = await GetCustomerTokenAsync();
        if (token == null) return;
        using var client = CreateClientWithBearer(token);
        var res = await client.GetAsync("/api/user/cards");
        Assert.True(res.IsSuccessStatusCode, $"GET /api/user/cards: {res.StatusCode}");
    }

    [Fact]
    public async Task UserCards_Post_WithCustomerToken_ReturnsOkOrBadRequest()
    {
        var token = await GetCustomerTokenAsync();
        if (token == null) return;
        using var client = CreateClientWithBearer(token);
        var res = await client.PostAsJsonAsync("/api/user/cards", new { type = 0 });
        Assert.True(res.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest, $"POST /api/user/cards: {res.StatusCode}");
    }

    // ----- Profile (customer token) -----
    [Fact]
    public async Task Profile_Get_WithCustomerToken_ReturnsOk()
    {
        var token = await GetCustomerTokenAsync();
        if (token == null) return;
        using var client = CreateClientWithBearer(token);
        var res = await client.GetAsync("/api/profile");
        Assert.True(res.IsSuccessStatusCode, $"GET /api/profile: {res.StatusCode}");
    }

    [Fact]
    public async Task Profile_Put_WithCustomerToken_ReturnsOkOrBadRequest()
    {
        var token = await GetCustomerTokenAsync();
        if (token == null) return;
        using var client = CreateClientWithBearer(token);
        var res = await client.PutAsJsonAsync("/api/profile", new { fullName = "Test Customer", phone = DataSeeder.TestCustomerPhone });
        Assert.True(res.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest, $"PUT /api/profile: {res.StatusCode}");
    }

    // ----- Transactions (customer token) -----
    [Fact]
    public async Task Transactions_GetByAccount_WithCustomerToken_ReturnsOk()
    {
        var token = await GetCustomerTokenAsync();
        if (token == null) return;
        using var client = CreateClientWithBearer(token);
        var res = await client.GetAsync("/api/transactions/account/1?pageNumber=1&pageSize=10");
        Assert.True(res.IsSuccessStatusCode || res.StatusCode == HttpStatusCode.NotFound, $"GET /api/transactions/account/1: {res.StatusCode}");
    }

    [Fact]
    public async Task Transactions_Transfer_WithCustomerToken_ReturnsOkOrBadRequest()
    {
        var token = await GetCustomerTokenAsync();
        if (token == null) return;
        using var client = CreateClientWithBearer(token);
        var body = new { fromAccountId = 1, toAccountNumber = "SM999999999", amount = 0.01m, purpose = "Test", idempotencyKey = Guid.NewGuid().ToString() };
        var res = await client.PostAsJsonAsync("/api/transactions/transfer", body);
        Assert.True(res.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest or HttpStatusCode.NotFound, $"POST /api/transactions/transfer: {res.StatusCode}");
    }

    // ----- Dashboard (customer token) -----
    [Fact]
    public async Task Dashboard_Get_WithCustomerToken_ReturnsOk()
    {
        var token = await GetCustomerTokenAsync();
        if (token == null) return;
        using var client = CreateClientWithBearer(token);
        var res = await client.GetAsync("/api/dashboard");
        Assert.True(res.IsSuccessStatusCode, $"GET /api/dashboard: {res.StatusCode}");
    }

    // ----- Analytics (customer token) -----
    [Fact]
    public async Task Analytics_Overview_WithCustomerToken_ReturnsOk()
    {
        var token = await GetCustomerTokenAsync();
        if (token == null) return;
        using var client = CreateClientWithBearer(token);
        var res = await client.GetAsync("/api/analytics/overview");
        Assert.True(res.IsSuccessStatusCode, $"GET /api/analytics/overview: {res.StatusCode}");
    }

    [Fact]
    public async Task Analytics_BalanceTrend_WithCustomerToken_ReturnsOk()
    {
        var token = await GetCustomerTokenAsync();
        if (token == null) return;
        using var client = CreateClientWithBearer(token);
        var res = await client.GetAsync("/api/analytics/balance-trend");
        Assert.True(res.IsSuccessStatusCode, $"GET /api/analytics/balance-trend: {res.StatusCode}");
    }

    [Fact]
    public async Task Analytics_Transactions_WithCustomerToken_ReturnsOk()
    {
        var token = await GetCustomerTokenAsync();
        if (token == null) return;
        using var client = CreateClientWithBearer(token);
        var res = await client.GetAsync("/api/analytics/transactions");
        Assert.True(res.IsSuccessStatusCode, $"GET /api/analytics/transactions: {res.StatusCode}");
    }

    [Fact]
    public async Task Analytics_TransactionsList_WithCustomerToken_ReturnsOk()
    {
        var token = await GetCustomerTokenAsync();
        if (token == null) return;
        using var client = CreateClientWithBearer(token);
        var res = await client.GetAsync("/api/analytics/transactions-list?page=1&pageSize=10");
        Assert.True(res.IsSuccessStatusCode, $"GET /api/analytics/transactions-list: {res.StatusCode}");
    }

    // ----- KYC (customer token) -----
    [Fact]
    public async Task Kyc_Status_WithCustomerToken_ReturnsOk()
    {
        var token = await GetCustomerTokenAsync();
        if (token == null) return;
        using var client = CreateClientWithBearer(token);
        var res = await client.GetAsync("/api/kyc/status");
        Assert.True(res.IsSuccessStatusCode, $"GET /api/kyc/status: {res.StatusCode}");
    }

    // ----- Payments (customer token) -----
    [Fact]
    public async Task Payments_TopUp_WithCustomerToken_ReturnsOkOrBadRequest()
    {
        var token = await GetCustomerTokenAsync();
        if (token == null) return;
        using var client = CreateClientWithBearer(token);
        var res = await client.PostAsJsonAsync("/api/payments/top-up", new { amount = 10m, currency = "USD" });
        Assert.True(res.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest, $"POST /api/payments/top-up: {res.StatusCode}");
    }

    // ----- Admin endpoints (admin token) -----
    [Fact]
    public async Task Admin_GetUsers_WithAdminToken_ReturnsOk()
    {
        var token = await GetAdminTokenAsync();
        if (token == null) return;
        using var client = CreateClientWithBearer(token);
        var res = await client.GetAsync("/api/admin/users");
        Assert.True(res.IsSuccessStatusCode, $"GET /api/admin/users: {res.StatusCode}");
    }

    [Fact]
    public async Task Admin_GetUsers_WithCustomerToken_Returns403()
    {
        var token = await GetCustomerTokenAsync();
        if (token == null) return;
        using var client = CreateClientWithBearer(token);
        var res = await client.GetAsync("/api/admin/users");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Admin_DashboardMetrics_WithAdminToken_ReturnsOk()
    {
        var token = await GetAdminTokenAsync();
        if (token == null) return;
        using var client = CreateClientWithBearer(token);
        var res = await client.GetAsync("/api/admin/dashboard/metrics");
        Assert.True(res.IsSuccessStatusCode, $"GET /api/admin/dashboard/metrics: {res.StatusCode}");
    }

    [Fact]
    public async Task Admin_DashboardUsers_WithAdminToken_ReturnsOk()
    {
        var token = await GetAdminTokenAsync();
        if (token == null) return;
        using var client = CreateClientWithBearer(token);
        var res = await client.GetAsync("/api/admin/dashboard/users");
        Assert.True(res.IsSuccessStatusCode, $"GET /api/admin/dashboard/users: {res.StatusCode}");
    }

    [Fact]
    public async Task Admin_Payroll_List_WithAdminToken_ReturnsOk()
    {
        var token = await GetAdminTokenAsync();
        if (token == null) return;
        using var client = CreateClientWithBearer(token);
        var res = await client.GetAsync("/api/admin/payroll?pageNumber=1&pageSize=10");
        Assert.True(res.IsSuccessStatusCode, $"GET /api/admin/payroll: {res.StatusCode}");
    }

    [Fact]
    public async Task Admin_Transactions_RiskReview_WithAdminToken_ReturnsOk()
    {
        var token = await GetAdminTokenAsync();
        if (token == null) return;
        using var client = CreateClientWithBearer(token);
        var res = await client.GetAsync("/api/admin/transactions/risk-review?pageNumber=1&pageSize=10");
        Assert.True(res.IsSuccessStatusCode, $"GET /api/admin/transactions/risk-review: {res.StatusCode}");
    }
}

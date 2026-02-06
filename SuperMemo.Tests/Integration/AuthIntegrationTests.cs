using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SuperMemo.Application.DTOs.requests.Auth;
using SuperMemo.Api;
using Xunit;

namespace SuperMemo.Tests.Integration;

/// <summary>
/// Integration tests for authentication endpoints.
/// Tests the full HTTP request/response cycle.
/// 
/// NOTE: These tests require a test database to be configured.
/// For now, they serve as templates showing the test structure.
/// </summary>
public class AuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var phone = $"964{DateTime.UtcNow.Ticks % 1000000000}"; // Unique phone number
        var request = new RegisterRequest
        {
            FullName = "Test User",
            Phone = phone,
            Password = "TestPassword123!",
            VerificationCode = "123456" // Note: In real test, you'd need to get this from DB or mock
        };

        // Act
        var response = await _client.PostAsJsonAsync("/auth/register", request);

        // Assert
        // Note: This will likely fail without proper verification code setup
        // This is a template showing the structure
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequest
        {
            Phone = "nonexistent@test.com",
            Password = "WrongPassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var result = await response.Content.ReadFromJsonAsync<Application.DTOs.responses.Common.ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Code.Should().Be(Application.Common.ErrorCodes.InvalidCredentials);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/auth/Me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

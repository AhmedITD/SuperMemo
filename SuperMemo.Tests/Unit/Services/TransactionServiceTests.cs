using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Services;
using SuperMemo.Domain.Entities;
using SuperMemo.Domain.Enums;
using SuperMemo.Infrastructure.Data;
using Xunit;

namespace SuperMemo.Tests.Unit.Services;

/// <summary>
/// Unit tests for TransactionService - testing idempotency, validation, and business logic.
/// </summary>
public class TransactionServiceTests
{
    [Fact]
    public async Task CreateTransferAsync_WithIdempotencyKey_ReturnsExistingTransaction_WhenKeyExists()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SuperMemoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new SuperMemoDbContext(options);
        
        var account = new Account
        {
            Id = 1,
            UserId = 1,
            Balance = 1000,
            Currency = "IQD",
            Status = AccountStatus.Active,
            AccountNumber = "ACC001"
        };
        context.Accounts.Add(account);

        var existingTransaction = new Transaction
        {
            Id = 1,
            FromAccountId = 1,
            ToAccountNumber = "ACC002",
            Amount = 100,
            Status = TransactionStatus.Created,
            IdempotencyKey = "test-key-123",
            TransactionType = TransactionType.Debit,
            Category = TransactionCategory.Transfer
        };
        context.Transactions.Add(existingTransaction);
        await context.SaveChangesAsync();

        var transactionService = new TransactionService(context, Mock.Of<Application.Interfaces.Auth.ICurrentUser>());

        var request = new Application.DTOs.requests.Transactions.CreateTransferRequest
        {
            FromAccountId = 1,
            ToAccountNumber = "ACC002",
            Amount = 100,
            Purpose = "Test",
            IdempotencyKey = "test-key-123"
        };

        // Act
        var result = await transactionService.CreateTransferAsync(request, userId: 1, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(1); // Should return existing transaction
        result.Data.IdempotencyKey.Should().Be("test-key-123");
    }

    [Fact]
    public async Task CreateTransferAsync_WithInsufficientFunds_ReturnsError()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SuperMemoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new SuperMemoDbContext(options);
        
        var account = new Account
        {
            Id = 1,
            UserId = 1,
            Balance = 50, // Less than transfer amount
            Currency = "IQD",
            Status = AccountStatus.Active,
            AccountNumber = "ACC001"
        };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var transactionService = new TransactionService(context, Mock.Of<Application.Interfaces.Auth.ICurrentUser>());

        var request = new Application.DTOs.requests.Transactions.CreateTransferRequest
        {
            FromAccountId = 1,
            ToAccountNumber = "ACC002",
            Amount = 100, // More than balance
            Purpose = "Test"
        };

        // Act
        var result = await transactionService.CreateTransferAsync(request, userId: 1, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Code.Should().Be(Application.Common.ErrorCodes.InsufficientFunds);
    }
}

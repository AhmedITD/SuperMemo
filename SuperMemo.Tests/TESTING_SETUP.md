# Testing Setup Guide

## Current Status

✅ **Test project structure created**
- Test project: `SuperMemo.Tests`
- Test framework: xUnit
- Mocking: Moq
- Assertions: FluentAssertions
- Integration testing: WebApplicationFactory

⚠️ **Tests need to be expanded and configured**

## Next Steps

### 1. Fix Integration Tests

The integration tests currently reference `Program` which may not be accessible. Options:

**Option A: Make Program class accessible**
```csharp
// In Program.cs, change:
var builder = WebApplication.CreateBuilder(args);
// To:
public partial class Program { }
```

**Option B: Use a custom WebApplicationFactory**
Create a custom factory that sets up the test environment properly.

### 2. Configure Test Database

For integration tests, you need a test database:

**Option A: Use TestContainers (Recommended)**
```bash
dotnet add SuperMemo.Tests package Testcontainers.PostgreSql
```

**Option B: Use In-Memory Database (Limited)**
- Good for unit tests
- Limited for integration tests (no real SQL features)

**Option C: Use Local Test Database**
- Create a separate test database
- Run migrations before tests
- Clean up after tests

### 3. Add Test Data Seeding

Create a test data seeder:
```csharp
public class TestDataSeeder
{
    public static async Task SeedTestData(SuperMemoDbContext context)
    {
        // Add test users, accounts, etc.
    }
}
```

### 4. Expand Test Coverage

**Priority Tests to Add:**

1. **TransactionService Tests:**
   - ✅ Idempotency (already added)
   - ✅ Insufficient funds (already added)
   - [ ] Daily spending limit validation
   - [ ] Account status validation
   - [ ] User approval status validation
   - [ ] KYC status validation

2. **AuthService Tests:**
   - [ ] Registration with valid data
   - [ ] Registration with duplicate phone
   - [ ] Login with valid credentials
   - [ ] Login with invalid credentials
   - [ ] Password reset flow

3. **KYC Service Tests:**
   - [ ] IC document submission
   - [ ] Passport document submission
   - [ ] Living identity submission
   - [ ] Status updates

4. **Admin Service Tests:**
   - [ ] User approval
   - [ ] User rejection
   - [ ] KYC verification

5. **Integration Tests:**
   - [ ] Full registration → login → KYC → approval flow
   - [ ] Transaction creation and processing
   - [ ] Card issuance flow

### 5. Run Tests

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~TransactionServiceTests"
```

### 6. CI/CD Integration

Add to your CI/CD pipeline:
```yaml
- name: Run Tests
  run: dotnet test --no-build --verbosity normal
```

## Test Structure

```
SuperMemo.Tests/
├── Unit/
│   └── Services/
│       └── TransactionServiceTests.cs
├── Integration/
│   └── AuthIntegrationTests.cs
├── Helpers/
│   └── TestDataSeeder.cs (to be created)
└── SuperMemo.Tests.csproj
```

## Notes

- Unit tests use in-memory database (fast, isolated)
- Integration tests need real/test database (slower, more realistic)
- All tests should be independent (no shared state)
- Use unique data per test (unique phone numbers, emails, etc.)

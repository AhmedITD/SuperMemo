# SuperMemo Tests

This project contains unit and integration tests for the SuperMemo Virtual Banking API.

## Test Structure

- **Unit Tests** (`Unit/`): Test individual services and business logic in isolation
- **Integration Tests** (`Integration/`): Test full HTTP request/response cycles with test database

## Running Tests

### Run all tests
```bash
dotnet test
```

### Run specific test project
```bash
dotnet test SuperMemo.Tests/SuperMemo.Tests.csproj
```

### Run with coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Test Categories

- **Unit Tests**: Fast, isolated tests using mocks and in-memory database
- **Integration Tests**: Full API tests using WebApplicationFactory and test database

## Setup

1. Ensure test database is configured (or use in-memory for unit tests)
2. Run migrations on test database if needed
3. Configure test appsettings if required

## Notes

- Integration tests require a test database (PostgreSQL or in-memory)
- Some tests may need test data seeding
- Authentication tests require proper JWT setup

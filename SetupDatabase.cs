using System;
using System.IO;
using System.Threading.Tasks;
using Npgsql;

class Program
{
    static async Task Main(string[] args)
    {
        var adminConnectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=M10m10m10;Pooling=true;CommandTimeout=30;";
        var targetDatabase = "SuperMemo";
        var connectionString = $"Host=localhost;Port=5432;Database={targetDatabase};Username=postgres;Password=M10m10m10;Pooling=true;MinPoolSize=5;MaxPoolSize=100;CommandTimeout=30;";
        var advancedFeaturesSql = Path.Combine("SuperMemo.Infrastructure", "Migrations", "20250207000000_AdvancedFeatures.sql");

        Console.WriteLine("========================================");
        Console.WriteLine("Complete Database Setup Tool");
        Console.WriteLine("========================================");
        Console.WriteLine();

        try
        {
            // Step 1: Create database if it doesn't exist
            Console.WriteLine("Step 1: Ensuring database exists...");
            await using var adminConnection = new NpgsqlConnection(adminConnectionString);
            await adminConnection.OpenAsync();

            var checkDbCmd = $"SELECT 1 FROM pg_database WHERE datname = '{targetDatabase}';";
            await using var checkCmd = new NpgsqlCommand(checkDbCmd, adminConnection);
            var dbExists = await checkCmd.ExecuteScalarAsync() != null;

            if (!dbExists)
            {
                Console.WriteLine($"Creating database '{targetDatabase}'...");
                var terminateConnections = $@"
                    SELECT pg_terminate_backend(pg_stat_activity.pid)
                    FROM pg_stat_activity
                    WHERE pg_stat_activity.datname = '{targetDatabase}'
                    AND pid <> pg_backend_pid();";
                
                try
                {
                    await using var termCmd = new NpgsqlCommand(terminateConnections, adminConnection);
                    await termCmd.ExecuteNonQueryAsync();
                }
                catch { }

                var createDbCmd = $"CREATE DATABASE \"{targetDatabase}\";";
                await using var createCmd = new NpgsqlCommand(createDbCmd, adminConnection);
                await createCmd.ExecuteNonQueryAsync();
                Console.WriteLine($"✓ Database '{targetDatabase}' created");
            }
            else
            {
                Console.WriteLine($"✓ Database '{targetDatabase}' already exists");
            }

            await adminConnection.CloseAsync();

            // Step 2: Connect to SuperMemo and check if base tables exist
            Console.WriteLine();
            Console.WriteLine("Step 2: Checking base schema...");
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            var checkTablesCmd = @"
                SELECT COUNT(*) 
                FROM information_schema.tables 
                WHERE table_schema = 'public' 
                AND table_name IN ('Users', 'Accounts', 'Transactions', 'Cards');";
            
            await using var checkTables = new NpgsqlCommand(checkTablesCmd, connection);
            var tableCount = Convert.ToInt32(await checkTables.ExecuteScalarAsync() ?? 0);

            if (tableCount < 4)
            {
                Console.WriteLine("⚠ Base tables don't exist. You need to run EF Core migrations first.");
                Console.WriteLine("   Run: cd SuperMemo.Api && dotnet ef database update --project ..\\SuperMemo.Infrastructure\\SuperMemo.Infrastructure.csproj");
                Console.WriteLine();
                Console.WriteLine("   Or the base schema will be created when you first run the API.");
                Console.WriteLine();
                Console.WriteLine("   After base tables exist, run this tool again to apply advanced features migration.");
                return;
            }

            Console.WriteLine($"✓ Base schema exists ({tableCount} core tables found)");

            // Step 3: Check if advanced features are already applied
            Console.WriteLine();
            Console.WriteLine("Step 3: Checking advanced features migration...");
            var checkAdvancedCmd = @"
                SELECT COUNT(*) 
                FROM information_schema.columns 
                WHERE table_schema = 'public' 
                AND table_name = 'Transactions' 
                AND column_name IN ('FailureReason', 'RetryCount', 'RiskScore', 'RiskLevel', 'StatusChangedAt');";
            
            await using var checkAdvanced = new NpgsqlCommand(checkAdvancedCmd, connection);
            var advancedColumnCount = Convert.ToInt32(await checkAdvanced.ExecuteScalarAsync() ?? 0);

            if (advancedColumnCount >= 5)
            {
                Console.WriteLine("✓ Advanced features migration already applied");
                Console.WriteLine();
                Console.WriteLine("Database is fully set up and ready!");
                return;
            }

            // Step 4: Apply advanced features migration
            Console.WriteLine();
            Console.WriteLine("Step 4: Applying advanced features migration...");
            
            if (!File.Exists(advancedFeaturesSql))
            {
                Console.WriteLine($"✗ Error: SQL file not found at {advancedFeaturesSql}");
                return;
            }

            var sql = await File.ReadAllTextAsync(advancedFeaturesSql);
            await using var command = new NpgsqlCommand(sql, connection);
            command.CommandTimeout = 60;
            
            try
            {
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("✓ Advanced features migration applied successfully!");
                Console.WriteLine();
                Console.WriteLine("New features added:");
                Console.WriteLine("  - Transactions: FailureReason, RetryCount, RiskScore, RiskLevel, StatusChangedAt");
                Console.WriteLine("  - MerchantAccounts table");
                Console.WriteLine("  - FraudDetectionRules table");
                Console.WriteLine("  - TransactionStatusHistory table");
                Console.WriteLine("  - Performance indexes");
            }
            catch (PostgresException ex)
            {
                if (ex.SqlState == "42P07" || ex.Message.Contains("already exists"))
                {
                    Console.WriteLine("⚠ Some objects may already exist (safe to ignore)");
                }
                else
                {
                    Console.WriteLine($"✗ Error: {ex.Message}");
                    throw;
                }
            }

            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("✓ Database setup complete!");
            Console.WriteLine("========================================");
        }
        catch (NpgsqlException ex)
        {
            Console.WriteLine($"✗ Database error: {ex.Message}");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}

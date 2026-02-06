using System;
using System.Diagnostics;
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
        var completeSetupSql = Path.Combine("SuperMemo.Infrastructure", "Migrations", "CompleteDatabaseSetup.sql");
        var advancedFeaturesSql = Path.Combine("SuperMemo.Infrastructure", "Migrations", "20250207000000_AdvancedFeatures.sql");

        Console.WriteLine("========================================");
        Console.WriteLine("Complete Database Setup & Migration");
        Console.WriteLine("========================================");
        Console.WriteLine();

        try
        {
            // Step 1: Create database
            Console.WriteLine("Step 1: Ensuring database exists...");
            await using var adminConnection = new NpgsqlConnection(adminConnectionString);
            await adminConnection.OpenAsync();

            var checkDbCmd = $"SELECT 1 FROM pg_database WHERE datname = '{targetDatabase}';";
            await using var checkCmd = new NpgsqlCommand(checkDbCmd, adminConnection);
            var dbExists = await checkCmd.ExecuteScalarAsync() != null;

            if (!dbExists)
            {
                Console.WriteLine($"Creating database '{targetDatabase}'...");
                var createDbCmd = $"CREATE DATABASE \"{targetDatabase}\";";
                await using var createCmd = new NpgsqlCommand(createDbCmd, adminConnection);
                await createCmd.ExecuteNonQueryAsync();
                Console.WriteLine($"✓ Database '{targetDatabase}' created");
            }
            else
            {
                Console.WriteLine($"✓ Database '{targetDatabase}' exists");
            }
            await adminConnection.CloseAsync();

            // Step 2: Run EF Core migrations to create base schema
            Console.WriteLine();
            Console.WriteLine("Step 2: Running EF Core migrations for base schema...");
            var apiPath = Path.Combine("SuperMemo.Api", "SuperMemo.Api.csproj");
            var infraPath = Path.Combine("SuperMemo.Infrastructure", "SuperMemo.Infrastructure.csproj");

            if (File.Exists(apiPath) && File.Exists(infraPath))
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"ef database update --project \"{infraPath}\" --startup-project \"{apiPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine("✓ Base schema migrations applied");
                    }
                    else
                    {
                        Console.WriteLine($"⚠ Migration output: {output}");
                        if (!string.IsNullOrEmpty(error))
                            Console.WriteLine($"⚠ Errors: {error}");
                    }
                }
            }
            else
            {
                Console.WriteLine("⚠ Could not find project files. Skipping EF migrations.");
                Console.WriteLine("   Base schema will be created when API runs for the first time.");
            }

            // Step 3: Apply complete database setup
            Console.WriteLine();
            Console.WriteLine("Step 3: Applying complete database schema...");
            
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            // Check if complete setup SQL exists, otherwise use advanced features only
            string sqlFile;
            if (File.Exists(completeSetupSql))
            {
                sqlFile = completeSetupSql;
                Console.WriteLine("Using complete setup script (base + advanced features)");
            }
            else if (File.Exists(advancedFeaturesSql))
            {
                sqlFile = advancedFeaturesSql;
                Console.WriteLine("Using advanced features migration only (base schema should exist)");
                
                // Check if base tables exist
                var checkTableCmd = @"
                    SELECT COUNT(*) 
                    FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_name = 'Transactions';";
                
                await using var checkTable = new NpgsqlCommand(checkTableCmd, connection);
                var tableExists = Convert.ToInt32(await checkTable.ExecuteScalarAsync() ?? 0) > 0;

                if (!tableExists)
                {
                    Console.WriteLine("⚠ Base tables don't exist. Please run complete setup script or create base schema first.");
                    return;
                }
            }
            else
            {
                Console.WriteLine($"✗ Error: No SQL migration files found");
                return;
            }

            var sql = await File.ReadAllTextAsync(sqlFile);
            await using var command = new NpgsqlCommand(sql, connection);
            command.CommandTimeout = 120; // 2 minutes for complete setup
            
            try
            {
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("✓ Database schema applied successfully!");
                Console.WriteLine();
                Console.WriteLine("Schema includes:");
                Console.WriteLine("  - Base tables: Users, Accounts, Cards, Transactions, KYC docs, etc.");
                Console.WriteLine("  - Advanced features: FailureReason, RetryCount, RiskScore, RiskLevel, StatusChangedAt");
                Console.WriteLine("  - New tables: MerchantAccounts, FraudDetectionRules, TransactionStatusHistory");
                Console.WriteLine("  - All indexes and constraints");
            }
            catch (PostgresException ex)
            {
                if (ex.SqlState == "42P07" || ex.Message.Contains("already exists"))
                {
                    Console.WriteLine("⚠ Some objects already exist (safe to ignore)");
                    Console.WriteLine("✓ Database is ready!");
                }
                else
                {
                    Console.WriteLine($"✗ Error: {ex.Message}");
                    Console.WriteLine($"SQL State: {ex.SqlState}");
                    throw;
                }
            }

            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("✓ Setup Complete! Database is ready.");
            Console.WriteLine("========================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}

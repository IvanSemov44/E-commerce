using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ECommerce.API.Common.Extensions;

/// <summary>
/// Validates database schema consistency with EF Core model.
/// Detects issues where migration history is out of sync with actual schema.
/// </summary>
public static class DatabaseSchemaValidator
{
    /// <summary>
    /// Tables that should NOT have RowVersion column (no optimistic concurrency needed).
    /// </summary>
    private static readonly string[] TablesWithoutRowVersion =
    [
        "RefreshTokens", "Categories", "ProductImages", "Addresses",
        "CartItems", "OrderItems", "Reviews", "Wishlists", "InventoryLogs"
    ];

    /// <summary>
    /// Tables that SHOULD have RowVersion column (optimistic concurrency required).
    /// </summary>
    private static readonly string[] TablesWithRowVersion =
    [
        "Users", "Products", "Carts", "Orders", "PromoCodes"
    ];

    /// <summary>
    /// Critical tables that must exist for the application to function.
    /// </summary>
    private static readonly string[] RequiredTables =
    [
        "Users", "Products", "Orders", "RefreshTokens", "Categories"
    ];

    /// <summary>
    /// Critical columns that must exist for authentication to work.
    /// </summary>
    private static readonly (string Table, string Column)[] CriticalColumns =
    [
        ("RefreshTokens", "Token"),
        ("RefreshTokens", "UserId"),
        ("RefreshTokens", "ExpiresAt")
    ];

    /// <summary>
    /// Validates the database schema matches the expected EF Core model.
    /// </summary>
    public static async Task ValidateAsync(DbContext context)
    {
        try
        {
            var providerName = context.Database.ProviderName ?? string.Empty;
            if (providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase) ||
                !context.Database.IsRelational())
            {
                Log.Information("Skipping schema validation for non-relational provider: {Provider}", providerName);
                return;
            }

            var connection = context.Database.GetDbConnection();
            await connection.OpenAsync();

            using var command = connection.CreateCommand();

            await ValidateRequiredTablesAsync(command);
            await ValidateRowVersionColumnsAsync(command);
            await ValidateCriticalColumnsAsync(command);

            await connection.CloseAsync();

            Log.Information("Database schema validation passed. All critical tables and columns verified.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            Log.Warning(ex, "Could not validate database schema (non-fatal). This may indicate a connection issue.");
        }
    }

    private static async Task ValidateRequiredTablesAsync(System.Data.Common.DbCommand command)
    {
        var missingTables = new List<string>();

        foreach (var tableName in RequiredTables)
        {
            if (!await TableExistsAsync(command, tableName))
            {
                missingTables.Add(tableName);
            }
        }

        if (missingTables.Count != 0)
        {
            var errorMessage = $"Database schema validation failed. Missing required tables: {string.Join(", ", missingTables)}. " +
                               "The database may not be properly initialized or migrations may have failed.";
            Log.Error(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }
    }

    private static async Task ValidateRowVersionColumnsAsync(System.Data.Common.DbCommand command)
    {
        var schemaIssues = new List<string>();

        // Check tables that should NOT have RowVersion
        foreach (var tableName in TablesWithoutRowVersion)
        {
            if (await ColumnExistsAsync(command, tableName, "RowVersion"))
            {
                schemaIssues.Add($"Table '{tableName}' has RowVersion column which should not exist. " +
                                "Run migration to remove it.");
            }
        }

        // Check tables that SHOULD have RowVersion
        foreach (var tableName in TablesWithRowVersion)
        {
            if (!await ColumnExistsAsync(command, tableName, "RowVersion"))
            {
                schemaIssues.Add($"Table '{tableName}' is missing RowVersion column required for optimistic concurrency. " +
                                "Run migration to add it.");
            }
        }

        if (schemaIssues.Count != 0)
        {
            var errorMessage = $"Database schema validation failed. Issues found:\n{string.Join("\n", schemaIssues)}";
            Log.Error(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }
    }

    private static async Task ValidateCriticalColumnsAsync(System.Data.Common.DbCommand command)
    {
        var missingColumns = new List<string>();

        foreach (var (table, column) in CriticalColumns)
        {
            if (!await ColumnExistsAsync(command, table, column))
            {
                missingColumns.Add($"{table}.{column}");
            }
        }

        if (missingColumns.Count != 0)
        {
            var errorMessage = $"Database schema validation failed. Missing critical columns: {string.Join(", ", missingColumns)}. " +
                               "Authentication may not work correctly.";
            Log.Error(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }
    }

    private static async Task<bool> TableExistsAsync(System.Data.Common.DbCommand command, string tableName)
    {
        command.CommandText = $@"
            SELECT EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = 'public'
                AND table_name = '{tableName}'
            )";

        var result = await command.ExecuteScalarAsync();
        return result != null && (bool)result;
    }

    private static async Task<bool> ColumnExistsAsync(System.Data.Common.DbCommand command, string tableName, string columnName)
    {
        command.CommandText = $@"
            SELECT EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'public'
                AND table_name = '{tableName}'
                AND column_name = '{columnName}'
            )";

        var result = await command.ExecuteScalarAsync();
        return result != null && (bool)result;
    }
}



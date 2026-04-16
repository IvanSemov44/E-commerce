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
    /// Platform tables that must exist for the application runtime to function.
    /// </summary>
    private static readonly string[] RequiredPublicTables =
    [
        "DataProtectionKeys"
    ];

    /// <summary>
    /// Integration reliability tables that must exist in the integration schema.
    /// </summary>
    private static readonly string[] RequiredIntegrationTables =
    [
        "outbox_messages",
        "inbox_messages",
        "dead_letter_messages",
        "order_fulfillment_saga_states"
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

            await ValidateRequiredTablesAsync(command, "public", RequiredPublicTables);
            await ValidateRequiredTablesAsync(command, "integration", RequiredIntegrationTables);

            await connection.CloseAsync();

            Log.Information("Database schema validation passed. Platform and integration tables verified.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            Log.Warning(ex, "Could not validate database schema (non-fatal). This may indicate a connection issue.");
        }
    }

    private static async Task ValidateRequiredTablesAsync(
        System.Data.Common.DbCommand command,
        string schemaName,
        IEnumerable<string> requiredTables)
    {
        var missingTables = new List<string>();

        foreach (var tableName in requiredTables)
        {
            if (!await TableExistsAsync(command, schemaName, tableName))
            {
                missingTables.Add($"{schemaName}.{tableName}");
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

    private static async Task<bool> TableExistsAsync(System.Data.Common.DbCommand command, string schemaName, string tableName)
    {
        command.CommandText = $@"
            SELECT EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = '{schemaName}'
                AND table_name = '{tableName}'
            )";

        var result = await command.ExecuteScalarAsync();
        return result != null && (bool)result;
    }
}



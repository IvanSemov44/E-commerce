using Microsoft.EntityFrameworkCore.Migrations;

namespace ECommerce.Infrastructure.Migrations;

/// <summary>
/// Optimizes RowVersion columns for optimistic concurrency.
/// 
/// This migration ensures:
/// 1. Tables that NEED optimistic concurrency (Products, Orders, PromoCodes) have RowVersion
/// 2. Tables that DON'T need optimistic concurrency have RowVersion removed
/// 
/// The migration is IDEMPOTENT - it safely handles cases where columns may or may not exist.
/// This is critical because the AddRowVersionToAllTables migration was recorded as applied
/// but the actual columns may not have been created in the database.
/// </summary>
public partial class OptimizeRowVersionColumns : Migration
{
    /// <summary>
    /// Tables that REQUIRE RowVersion for optimistic concurrency.
    /// These entities have concurrent modifications from multiple sources.
    /// </summary>
    private static readonly string[] TablesWithRowVersion =
    [
        "Products",   // Inventory/pricing updates from multiple sources
        "Orders",     // Status/payment updates from multiple systems
        "PromoCodes"  // Usage count updates from concurrent requests
    ];

    /// <summary>
    /// Tables that DO NOT need RowVersion (no optimistic concurrency required).
    /// These entities are typically updated by a single user at a time.
    /// </summary>
    private static readonly string[] TablesWithoutRowVersion =
    [
        "Addresses",
        "CartItems",
        "Carts",
        "Categories",
        "InventoryLogs",
        "OrderItems",
        "ProductImages",
        "RefreshTokens",
        "Reviews",
        "Users",
        "Wishlists"
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // First, ensure tables that NEED RowVersion have it
        foreach (var tableName in TablesWithRowVersion)
        {
            AddRowVersionColumnIfNotExists(migrationBuilder, tableName);
        }

        // Then, remove RowVersion from tables that don't need it
        foreach (var tableName in TablesWithoutRowVersion)
        {
            DropRowVersionColumnIfExists(migrationBuilder, tableName);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reverse: add RowVersion back to tables that had it removed
        foreach (var tableName in TablesWithoutRowVersion)
        {
            AddRowVersionColumnIfNotExists(migrationBuilder, tableName);
        }

        // Remove RowVersion from tables that had it added
        foreach (var tableName in TablesWithRowVersion)
        {
            DropRowVersionColumnIfExists(migrationBuilder, tableName);
        }
    }

    private static void AddRowVersionColumnIfNotExists(MigrationBuilder migrationBuilder, string tableName)
    {
        migrationBuilder.Sql($@"
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_schema = 'public' 
                    AND table_name = '{tableName}' 
                    AND column_name = 'RowVersion'
                ) THEN
                    ALTER TABLE ""{tableName}"" ADD COLUMN ""RowVersion"" bytea;
                    RAISE NOTICE 'Added RowVersion column to {tableName}';
                ELSE
                    RAISE NOTICE 'RowVersion column already exists in {tableName}, skipping';
                END IF;
            END $$;");
    }

    private static void DropRowVersionColumnIfExists(MigrationBuilder migrationBuilder, string tableName)
    {
        migrationBuilder.Sql($@"
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_schema = 'public' 
                    AND table_name = '{tableName}' 
                    AND column_name = 'RowVersion'
                ) THEN
                    ALTER TABLE ""{tableName}"" DROP COLUMN ""RowVersion"";
                    RAISE NOTICE 'Dropped RowVersion column from {tableName}';
                ELSE
                    RAISE NOTICE 'RowVersion column does not exist in {tableName}, skipping';
                END IF;
            END $$;");
    }
}

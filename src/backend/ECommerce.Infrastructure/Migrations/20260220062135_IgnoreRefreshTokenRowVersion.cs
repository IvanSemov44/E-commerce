using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IgnoreRefreshTokenRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop RowVersion column from RefreshTokens if it exists
            // This migration is idempotent to handle cases where the column may not exist
            // due to migration history being out of sync with the actual schema
            // 
            // Scenarios handled:
            // 1. Fresh database: Column doesn't exist, nothing to drop
            // 2. Database where AddRowVersionToAllTables was applied: Column exists, will be dropped
            // 3. Database where migration history shows applied but column missing: No error
            migrationBuilder.Sql(
                @"DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_schema = 'public'
                        AND table_name = 'RefreshTokens' 
                        AND column_name = 'RowVersion'
                    ) THEN
                        ALTER TABLE ""RefreshTokens"" DROP COLUMN ""RowVersion"";
                        RAISE NOTICE 'Dropped RowVersion column from RefreshTokens table';
                    ELSE
                        RAISE NOTICE 'RowVersion column does not exist in RefreshTokens table, skipping drop';
                    END IF;
                END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only add the column if it doesn't already exist (idempotent rollback)
            migrationBuilder.Sql(
                @"DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_schema = 'public'
                        AND table_name = 'RefreshTokens' 
                        AND column_name = 'RowVersion'
                    ) THEN
                        ALTER TABLE ""RefreshTokens"" ADD COLUMN ""RowVersion"" bytea;
                        RAISE NOTICE 'Added RowVersion column to RefreshTokens table';
                    END IF;
                END $$;");
        }
    }
}

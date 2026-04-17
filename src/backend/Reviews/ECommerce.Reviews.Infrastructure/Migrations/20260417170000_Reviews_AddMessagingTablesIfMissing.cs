using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Reviews.Infrastructure.Migrations;

[DbContext(typeof(Persistence.ReviewsDbContext))]
[Migration("20260417170000_Reviews_AddMessagingTablesIfMissing")]
public partial class Reviews_AddMessagingTablesIfMissing : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Intentionally left empty.
        // This migration previously contained local environment repair SQL.
        // Reviews messaging tables are now handled by regular EF migrations.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally left empty.
    }
}

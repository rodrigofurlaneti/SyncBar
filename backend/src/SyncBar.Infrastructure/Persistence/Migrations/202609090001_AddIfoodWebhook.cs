using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SyncBar.Infrastructure.Persistence.Migrations;

// The existing schema is managed by SQL scripts. This additive migration also supports
// installations where the webhook script was already applied manually.
[DbContext(typeof(AppDbContext))]
[Migration("202609090001_AddIfoodWebhook")]
public sealed class AddIfoodWebhook : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        using var stream = typeof(AddIfoodWebhook).Assembly.GetManifestResourceStream("SyncBar.IfoodWebhook.sql")
            ?? throw new InvalidOperationException("Missing iFood webhook schema resource.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd(), suppressTransaction: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => throw new NotSupportedException("Preserve webhook events and credentials; rollback application code only.");
}

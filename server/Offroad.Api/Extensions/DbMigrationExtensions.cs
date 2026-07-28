using Microsoft.EntityFrameworkCore;
using Routing.Infrastructure.Data;

namespace Offroad.Api.Extensions;

public static class DbMigrationExtensions
{
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Offroad.Api.Program>>();

        var dbContext = services.GetRequiredService<ApplicationDbContext>();

        try
        {
            logger.LogInformation("Applying pending migrations to PostgreSQL...");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "FATAL ERROR: Database migration failed. Check connection string, PostgreSQL container, and PostGIS extension.");
            throw;
        }
    }
}

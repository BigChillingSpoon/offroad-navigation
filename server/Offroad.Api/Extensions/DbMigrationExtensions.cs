using Microsoft.EntityFrameworkCore;
using Routing.Domain.Models;
using Routing.Infrastructure.Data;
using Routing.Infrastructure.Persistance;

namespace Offroad.Api.Extensions;

public static class DbMigrationExtensions
{
    public static async Task UseGisDataSeedingAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<GisDataSeeder>>();

        logger.LogInformation("Starting database initialization process...");

        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        var seeder = services.GetRequiredService<GisDataSeeder>();

        // --- DB MIGRATION ---
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

        // --- DATA SEEDING---
        try
        {
            // // 1. Offroad zones
            var offroadPath = app.Configuration["Routing:FilteredNatureGeoJsonPath"];
            if (!string.IsNullOrEmpty(offroadPath))
            {
                var fullPath = Path.Combine(app.Environment.ContentRootPath, offroadPath);
                logger.LogInformation("Seeding Offroad zones...");
                await seeder.SeedZonesAsync(fullPath, ZoneType.OffroadArea, "Offroad Zone");
            }

            // 2. Restricted Areas
            var parksPath = app.Configuration["Routing:ParksGeoJsonPath"];
            if (!string.IsNullOrEmpty(parksPath))
            {
                var fullPath = Path.Combine(app.Environment.ContentRootPath, parksPath);
                logger.LogInformation("Seeding Restricted zones from: {Path}", fullPath);
                await seeder.SeedZonesAsync(fullPath, ZoneType.RestrictedArea, "National Parks and Reservations");
            }

            logger.LogInformation("All GIS data seeded successfully.");
        }
        catch (FileNotFoundException ex)
        {
            logger.LogError(ex, "ERROR: GeoJSON file is missing. Expected path: {FilePath}", ex.FileName);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ERROR: GIS data seeding failed. The file might be corrupted or too large.");
            throw;
        }
    }
}
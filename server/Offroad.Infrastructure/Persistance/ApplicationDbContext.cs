using Microsoft.EntityFrameworkCore;
using Routing.Domain.Models;
using Routing.Infrastructure.Persistence.Entities;

namespace Routing.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<GeoZone> GeoZones { get; set; }
    public DbSet<HookpointEntity> Hookpoints { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("public");

        // GEOZONE
        modelBuilder.Entity<GeoZone>(entity =>
        {
            entity.ToTable("geo_zones", schema: "gis");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Geometry)
                  .HasMethod("gist");
        });

        // HOOKPOINTS
        modelBuilder.Entity<HookpointEntity>(entity =>
        {
            entity.ToTable("offroad_hookpoints", schema: "gis");
            entity.HasNoKey();
            entity.Property(e => e.Geom)
                  .HasColumnName("geom")
                  .HasColumnType("geography(Point, 4326)");

            entity.Property(e => e.PocetPrujezdu)
                  .HasColumnName("pocet_prujezdu");

            entity.Property(e => e.GradesMask)
                  .HasColumnName("grades_mask");
            entity.Property(e => e.IsStrictlyInForest).HasColumnName("is_strictly_in_forest");
            entity.HasIndex(e => e.Geom)
                  .HasMethod("gist");
        });
    }
}
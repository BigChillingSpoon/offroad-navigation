using Microsoft.EntityFrameworkCore;
using Routing.Domain.Models; 

namespace Routing.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<GeoZone> GeoZones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("public");

        //GIS config
        modelBuilder.Entity<GeoZone>(entity =>
        {
            entity.ToTable("geo_zones", schema: "gis");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Geometry)
                  .HasMethod("gist");
        });
    }
}
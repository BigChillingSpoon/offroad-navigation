using Microsoft.EntityFrameworkCore;
using Routing.Infrastructure.Persistence.Entities;

namespace Routing.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<GeometricZoneEntity> GeometricZones { get; set; }
    public DbSet<NodeEntity> Nodes { get; set; }
    public DbSet<EdgeEntity> Edges { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("public");

        // GEOMETRIC ZONES (gis.geometric_zones - natural-area/restricted-area polygons owned by
        // the OSM import pipeline, not by EF migrations - same as Nodes/Edges below.)
        modelBuilder.Entity<GeometricZoneEntity>(entity =>
        {
            entity.ToTable("geometric_zones", schema: "gis");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("zone_id");
            entity.Property(e => e.ZoneType).HasColumnName("zone_type");
            entity.Property(e => e.Geometry)
                  .HasColumnName("geom")
                  .HasColumnType("geometry(MultiPolygon, 4326)");
            entity.Property(e => e.AreaSqm).HasColumnName("area_sqm");

            entity.HasIndex(e => e.Geometry)
                  .HasDatabaseName("idx_gis_zones_geom")
                  .HasMethod("gist");
        });

        // NODES (gis.nodes - strict intersections in the pre-processed offroad topology graph)
        modelBuilder.Entity<NodeEntity>(entity =>
        {
            entity.ToTable("nodes", schema: "gis");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("node_id");
            entity.Property(e => e.Geom)
                  .HasColumnName("geom")
                  .HasColumnType("geometry(Point, 4326)");

            entity.Property(e => e.IsEntryPoint).HasColumnName("is_entry_point");
            entity.Property(e => e.Altitude).HasColumnName("altitude");
            entity.HasIndex(e => e.Geom)
                  .HasDatabaseName("idx_gis_nodes_geom")
                  .HasMethod("gist");
        });

        // EDGES (gis.edges - routable segments connecting Nodes)
        modelBuilder.Entity<EdgeEntity>(entity =>
        {
            entity.ToTable("edges", schema: "gis");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("edge_id");

            entity.Property(e => e.SourceNodeId).HasColumnName("source_node");
            entity.Property(e => e.TargetNodeId).HasColumnName("target_node");
            entity.Property(e => e.LengthMeters).HasColumnName("length_m");
            entity.Property(e => e.HasBarrier).HasColumnName("has_barrier");
            entity.Property(e => e.HasNoEntry).HasColumnName("has_no_entry");
            entity.Property(e => e.IsRestricted).HasColumnName("is_restricted");
            entity.Property(e => e.ElevationGainMeters).HasColumnName("elevation_gain");
            entity.Property(e => e.IsOffroad).HasColumnName("is_offroad");
            entity.Property(e => e.Geom)
                  .HasColumnName("geom")
                  .HasColumnType("geometry(LineString, 4326)");

            entity.HasIndex(e => e.SourceNodeId)
                  .HasDatabaseName("idx_gis_edges_source_node");
            entity.HasIndex(e => e.TargetNodeId)
                  .HasDatabaseName("idx_gis_edges_target_node");
            entity.HasIndex(e => e.Geom)
                  .HasDatabaseName("idx_gis_edges_geom")
                  .HasMethod("gist");
        });
    }
}
using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Routing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncGisSchemaWithImportPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "geo_zones",
                schema: "gis");

            // gis.nodes/gis.edges/gis.geometric_zones may already exist, created and populated by
            // the OSM import pipeline (routing/scripts/database/build_routing_graph.sh)
            // independently of EF migrations - written as idempotent raw SQL (matching the
            // script's own CREATE TABLE/INDEX IF NOT EXISTS style) so this migration is safe to
            // run against both a fresh database and one the script has already populated. Only the
            // columns the app actually reads are declared here; the script owns adding its own
            // extra columns via its own ALTER TABLE ADD COLUMN IF NOT EXISTS statements.
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS gis.edges (
                    edge_id serial PRIMARY KEY,
                    source_node bigint NOT NULL,
                    target_node bigint NOT NULL,
                    geom geometry(LineString, 4326) NOT NULL,
                    length_m double precision NOT NULL,
                    has_barrier boolean NOT NULL DEFAULT false,
                    has_no_entry boolean NOT NULL DEFAULT false,
                    is_restricted boolean NOT NULL DEFAULT false,
                    elevation_gain double precision NOT NULL DEFAULT 0,
                    is_offroad boolean NOT NULL DEFAULT false
                );

                CREATE TABLE IF NOT EXISTS gis.geometric_zones (
                    zone_id serial PRIMARY KEY,
                    zone_type integer NOT NULL,
                    geom geometry(MultiPolygon, 4326) NOT NULL,
                    area_sqm double precision
                );

                CREATE TABLE IF NOT EXISTS gis.nodes (
                    node_id bigint PRIMARY KEY,
                    geom geometry(Point, 4326) NOT NULL,
                    is_entry_point boolean NOT NULL DEFAULT false,
                    altitude double precision
                );

                CREATE INDEX IF NOT EXISTS idx_gis_edges_geom ON gis.edges USING GIST (geom);
                CREATE INDEX IF NOT EXISTS idx_gis_edges_source_node ON gis.edges (source_node);
                CREATE INDEX IF NOT EXISTS idx_gis_edges_target_node ON gis.edges (target_node);
                CREATE INDEX IF NOT EXISTS idx_gis_zones_geom ON gis.geometric_zones USING GIST (geom);
                CREATE INDEX IF NOT EXISTS idx_gis_nodes_geom ON gis.nodes USING GIST (geom);
                """);

            // gis.heatmap: not an EF entity - not queried by the app yet, but kept here (unlike
            // the import pipeline's other script-only columns/tables) because a heatmap-display
            // feature is planned to read it soon.
            migrationBuilder.Sql("""
                CREATE MATERIALIZED VIEW IF NOT EXISTS gis.heatmap AS
                SELECT edge_id, geom
                FROM gis.edges
                WHERE is_offroad = true;

                CREATE INDEX IF NOT EXISTS idx_gis_heatmap_geom ON gis.heatmap USING GIST (geom);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Must drop before "edges" - the materialized view depends on that table.
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS gis.heatmap;");

            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS gis.edges;
                DROP TABLE IF EXISTS gis.geometric_zones;
                DROP TABLE IF EXISTS gis.nodes;
                """);

            migrationBuilder.CreateTable(
                name: "geo_zones",
                schema: "gis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Geometry = table.Column<Polygon>(type: "geometry", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_geo_zones", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_geo_zones_Geometry",
                schema: "gis",
                table: "geo_zones",
                column: "Geometry")
                .Annotation("Npgsql:IndexMethod", "gist");
        }
    }
}

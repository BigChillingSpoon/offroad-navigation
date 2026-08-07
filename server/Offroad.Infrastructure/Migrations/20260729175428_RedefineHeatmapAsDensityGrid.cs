using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Routing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RedefineHeatmapAsDensityGrid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Replace the raw-offroad-edge heatmap with a pre-aggregated density grid so arena
            // selection sums a handful of ~1km cells within a loop's reach instead of scanning the
            // 246k raw offroad LineStrings. Kept in sync with the import pipeline's own definition in
            // routing/scripts/database/build_routing_graph.sh (section "B. HEATMAP DENSITY GRID").
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS gis.heatmap;");
            migrationBuilder.Sql("""
                CREATE MATERIALIZED VIEW gis.heatmap AS
                SELECT
                    ST_SnapToGrid(ST_Centroid(geom), 0.01) AS cell_geom,
                    SUM(length_m)                          AS offroad_len_m,
                    COUNT(*)                               AS edge_count
                FROM gis.edges
                WHERE is_offroad = true
                GROUP BY ST_SnapToGrid(ST_Centroid(geom), 0.01);

                CREATE INDEX IF NOT EXISTS idx_gis_heatmap_geom ON gis.heatmap USING GIST (cell_geom);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the previous raw-offroad-edge definition.
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS gis.heatmap;");
            migrationBuilder.Sql("""
                CREATE MATERIALIZED VIEW gis.heatmap AS
                SELECT edge_id, geom
                FROM gis.edges
                WHERE is_offroad = true;

                CREATE INDEX IF NOT EXISTS idx_gis_heatmap_geom ON gis.heatmap USING GIST (geom);
                """);
        }
    }
}

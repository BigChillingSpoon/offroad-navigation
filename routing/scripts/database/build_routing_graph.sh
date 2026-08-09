1#!/bin/bash
# ==============================================================================
# OFFROAD ROUTING GRAPH BUILDER (GIS SCHEME)
# Features: Topology Noding, Infection, Recursive Edge Contraction, 3D Elevation
# ==============================================================================

set -e

DB_HOST="localhost"
DB_PORT="5433"
DB_NAME="offroad"
DB_USER="offroad"
export PGPASSWORD="offroad"

RAW_PBF="../../graphhopper/data/czech-republic-latest.osm.pbf"
FILTERED_PBF="../../graphhopper/data/filtered.osm.pbf"
LUA_SCRIPT="import_rules.lua"
ELEVATION_ZIPS_DIR="../../graphhopper/data/srtm"
BACKUP_DIR="../../graphhopper/data/backups"

echo "========================================================"
echo "STARTING 3D TOPOLOGY PIPELINE (GIS SCHEME)..."
echo "========================================================"
echo "Step 1/7: Extracting and Filtering OSM Data..."
osmium tags-filter $RAW_PBF \
    nwr/highway nwr/barrier nwr/boundary=national_park nwr/boundary=protected_area nwr/leisure=nature_reserve nwr/landuse=forest nwr/landuse=meadow \
    -o $FILTERED_PBF --overwrite

echo "Step 2/7: Creating Schemas & Wiping Old Data..."
psql -v ON_ERROR_STOP=1 -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME << 'EOF'

    -- 0. TURN ON THE SPATIAL AND ROUTING ENGINES
    CREATE EXTENSION IF NOT EXISTS postgis;
    CREATE EXTENSION IF NOT EXISTS pgrouting;
    CREATE EXTENSION IF NOT EXISTS postgis_raster;

    -- 1. Reset the raw staging area
    DROP SCHEMA IF EXISTS staging CASCADE;
    CREATE SCHEMA staging;

    -- 2. Ensure the master GIS schema exists
    CREATE SCHEMA IF NOT EXISTS gis;

    -- 3. Explicitly define the Nodes table
    CREATE TABLE IF NOT EXISTS gis.nodes (
        node_id bigint PRIMARY KEY,
        geom geometry(Point, 4326),
        altitude real,
        is_entry_point boolean DEFAULT false
    );

    -- 4. Explicitly define the Edges table
    CREATE TABLE IF NOT EXISTS gis.edges (
        edge_id serial PRIMARY KEY,
        source_node bigint,
        target_node bigint,
        geom geometry(LineString, 4326),
        length_m real,
        surface text,
        has_no_entry boolean DEFAULT false,
        tracktype text,
        highway text,
        is_offroad boolean,
        has_barrier boolean DEFAULT false,
        is_restricted boolean DEFAULT false,
        elevation_gain real
    );

    -- 5. Explicitly define the Zones table
    CREATE TABLE IF NOT EXISTS gis.geometric_zones (
        zone_id serial PRIMARY KEY,
        zone_type int,
        geom geometry(MultiPolygon, 4326),
        area_sqm real
    );

    CREATE TABLE IF NOT EXISTS public.srtm (
    rid serial PRIMARY KEY, 
    rast raster
    );

    -- 6. Wipe the tables clean for the new run
    TRUNCATE TABLE gis.edges CASCADE;
    TRUNCATE TABLE gis.nodes CASCADE;
    TRUNCATE TABLE gis.geometric_zones CASCADE;
EOF
osm2pgsql -d $DB_NAME -H $DB_HOST -P $DB_PORT -U $DB_USER -O flex -S $LUA_SCRIPT $FILTERED_PBF

echo "Step 3/7: Noding & Business Logic Infection..."
psql -v ON_ERROR_STOP=1 -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME << 'EOF'

    -- A. MATHEMATICAL NODING
    SELECT pgr_nodeNetwork('staging.raw_lines', 0.00001, 'way_id', 'geom');
    SELECT pgr_createTopology('staging.raw_lines_noded', 0.00001, 'geom', 'id');

    -- B. POPULATE FINAL GIS SCHEMA
    INSERT INTO gis.geometric_zones (zone_type, geom, area_sqm)
    SELECT zone_type, ST_Multi(ST_CollectionExtract(ST_MakeValid(geom), 3)), ST_Area(geom::geography)::real
    FROM staging.raw_polygons
    WHERE geom IS NOT NULL AND ST_IsEmpty(geom) = FALSE;

    INSERT INTO gis.nodes (node_id, geom)
    SELECT id, the_geom FROM staging.raw_lines_noded_vertices_pgr;

   ALTER TABLE gis.edges ADD COLUMN IF NOT EXISTS highway text;
    ALTER TABLE gis.edges ADD COLUMN IF NOT EXISTS is_offroad boolean;

    INSERT INTO gis.edges (source_node, target_node, geom, length_m, surface, has_no_entry, tracktype, highway, is_offroad)
    SELECT 
        n.source, n.target, n.geom, ST_Length(n.geom::geography)::real,
        r.surface, r.has_no_entry, r.tracktype::text, r.highway,
        
        -- IsOffroad should be the same as in the navigation rules
        CASE 
            WHEN r.surface IN ('unpaved', 'gravel', 'fine_gravel', 'compacted', 'dirt', 'ground', 'sand', 'grass', 'wood') THEN true
            WHEN r.tracktype IN ('grade2', 'grade3', 'grade4', 'grade5') THEN true
            WHEN r.highway = 'track' 
                 AND (r.surface IS NULL OR r.surface NOT IN ('paved', 'asphalt', 'concrete', 'paving_stones', 'cobblestone'))
                 AND (r.tracktype IS NULL OR r.tracktype != 'grade1') THEN true
            ELSE false
        END
    FROM staging.raw_lines_noded n
    JOIN staging.raw_lines r ON n.old_id = r.way_id;

    -- Create temporary spatial indexes to make intersections lightning fast
    CREATE INDEX IF NOT EXISTS idx_staging_barriers_geom ON staging.barrier_nodes USING GIST (geom);
    CREATE INDEX IF NOT EXISTS idx_gis_nodes_geom ON gis.nodes USING GIST (geom);
    CREATE INDEX IF NOT EXISTS idx_gis_edges_geom ON gis.edges USING GIST (geom);
    CREATE INDEX IF NOT EXISTS idx_gis_zones_geom ON gis.geometric_zones USING GIST (geom);
    
    -- Tell the database to calculate statistics so it uses the indexes properly
    ANALYZE staging.barrier_nodes;
    ANALYZE gis.nodes;
    ANALYZE gis.edges;
    ANALYZE gis.geometric_zones;
    
    -- C. THE INFECTION RULES
    -- Checked against the edge's full linestring (e.geom), not just its
    -- endpoint nodes - a barrier is typically a shared vertex somewhere along
    -- the way's interior, not necessarily at an intersection/topology node,
    -- so a node-only check misses most real barriers regardless of tolerance.
    UPDATE gis.edges e SET has_barrier = TRUE
    FROM staging.barrier_nodes b
    WHERE ST_DWithin(e.geom, b.geom, 0.00001);

    UPDATE gis.edges e SET is_restricted = TRUE
    FROM gis.geometric_zones z
    WHERE z.zone_type = 2 AND ST_Intersects(e.geom, z.geom);
EOF

echo " Step 4/7: The Great Topological Optimizer (DEBUG MODE)..."
psql -v ON_ERROR_STOP=1 -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME << 'EOF'
    -- Create the permanent debug log table
    DROP TABLE IF EXISTS gis.debug_log;
    CREATE TABLE gis.debug_log (
        log_id serial PRIMARY KEY,
        pass_count int,
        action text,
        detail text,
        ts timestamp DEFAULT clock_timestamp()
    );

    DO $$
    DECLARE
        rec RECORD; edge1 RECORD; edge2 RECORD;
        new_geom geometry; new_source bigint; new_target bigint;
        deleted_dead_ends int := 1; merged_count int := 1;
        deleted_loops int := 1; deleted_dupes int := 1;
        pass_count int := 0;
        _deg2_count int := 0;
        _geom_type text;
        _edge1_is_offroad boolean;
        _edge2_is_offroad boolean;
    BEGIN
        WHILE deleted_dead_ends > 0 OR merged_count > 0 OR deleted_loops > 0 OR deleted_dupes > 0 LOOP
            pass_count := pass_count + 1;
            INSERT INTO gis.debug_log (pass_count, action, detail) VALUES (pass_count, 'START_PASS', 'Beginning optimization pass');

            -- A. NUKE SELF-LOOPS
            DELETE FROM gis.edges WHERE source_node = target_node;
            GET DIAGNOSTICS deleted_loops = ROW_COUNT;

            -- B. PRUNE DEAD ENDS
            WITH node_degrees AS (
                SELECT node_id FROM (
                    SELECT source_node AS node_id FROM gis.edges
                    UNION ALL SELECT target_node FROM gis.edges
                ) n GROUP BY node_id HAVING COUNT(*) = 1
            )
            DELETE FROM gis.edges
            WHERE source_node IN (SELECT node_id FROM node_degrees) 
               OR target_node IN (SELECT node_id FROM node_degrees);
            GET DIAGNOSTICS deleted_dead_ends = ROW_COUNT;

            -- C. MERGE PASS-THROUGH NODES (Degree-2 Nodes)
            merged_count := 0;
            
            -- Count how many degree 2 nodes exist before trying to merge
            WITH edge_ends AS (
                SELECT source_node AS node_id, edge_id FROM gis.edges
                UNION ALL SELECT target_node AS node_id, edge_id FROM gis.edges
            )
            SELECT COUNT(*) INTO _deg2_count FROM (
                SELECT node_id FROM edge_ends GROUP BY node_id HAVING COUNT(*) = 2 AND COUNT(DISTINCT edge_id) = 2
            ) sub;
            
            INSERT INTO gis.debug_log (pass_count, action, detail) VALUES (pass_count, 'FOUND_DEG2', 'Found ' || _deg2_count || ' nodes with exactly 2 edges.');

            FOR rec IN (
                WITH edge_ends AS (
                    SELECT source_node AS node_id, edge_id FROM gis.edges
                    UNION ALL SELECT target_node AS node_id, edge_id FROM gis.edges
                )
                SELECT node_id, array_agg(edge_id) as edges FROM edge_ends
                GROUP BY node_id HAVING COUNT(*) = 2 AND COUNT(DISTINCT edge_id) = 2
            ) LOOP
                INSERT INTO gis.debug_log (pass_count, action, detail) VALUES (pass_count, 'TRY_MERGE', 'Attempting merge on Node ' || rec.node_id || ' between edges ' || rec.edges[1] || ' and ' || rec.edges[2]);

                SELECT * INTO edge1 FROM gis.edges WHERE edge_id = rec.edges[1];
                SELECT * INTO edge2 FROM gis.edges WHERE edge_id = rec.edges[2];

                IF edge1 IS NULL OR edge2 IS NULL THEN
                    INSERT INTO gis.debug_log (pass_count, action, detail) VALUES (pass_count, 'MERGE_FAIL', 'Could not find one or both edges in the database.');
                    CONTINUE;
                END IF;

                -- Preserve real offroad<->paved transitions as actual graph nodes. Merging across
                -- this boundary would blend two segments with very different offroad character into
                -- one edge, forcing an arbitrary COALESCE to pick a single surface/highway for the
                -- whole thing and hiding exactly where the pavement ends and the dirt begins - the one
                -- distinction that matters for routing (e.g. a 1km grade3 track that ends in a 20m
                -- grade1 asphalt link would otherwise be recorded as entirely one or the other).
                -- All other degree-2 transitions (e.g. grade2->grade3, both offroad) keep merging
                -- exactly as before, so the loop-finding graph's node budget isn't spent on
                -- differences nobody cares about. is_offroad is always set by the initial insert
                -- above and carried through every merge via OR, so it should never actually be NULL
                -- here - the CASE fallback (identical to that original rule) is defensive insurance
                -- only, recomputed from this edge's own surface/tracktype/highway if it ever is.
                _edge1_is_offroad := COALESCE(edge1.is_offroad,
                    CASE
                        WHEN edge1.surface IN ('unpaved', 'gravel', 'fine_gravel', 'compacted', 'dirt', 'ground', 'sand', 'grass', 'wood') THEN true
                        WHEN edge1.tracktype IN ('grade2', 'grade3', 'grade4', 'grade5') THEN true
                        WHEN edge1.highway = 'track'
                             AND (edge1.surface IS NULL OR edge1.surface NOT IN ('paved', 'asphalt', 'concrete', 'paving_stones', 'cobblestone'))
                             AND (edge1.tracktype IS NULL OR edge1.tracktype != 'grade1') THEN true
                        ELSE false
                    END);
                _edge2_is_offroad := COALESCE(edge2.is_offroad,
                    CASE
                        WHEN edge2.surface IN ('unpaved', 'gravel', 'fine_gravel', 'compacted', 'dirt', 'ground', 'sand', 'grass', 'wood') THEN true
                        WHEN edge2.tracktype IN ('grade2', 'grade3', 'grade4', 'grade5') THEN true
                        WHEN edge2.highway = 'track'
                             AND (edge2.surface IS NULL OR edge2.surface NOT IN ('paved', 'asphalt', 'concrete', 'paving_stones', 'cobblestone'))
                             AND (edge2.tracktype IS NULL OR edge2.tracktype != 'grade1') THEN true
                        ELSE false
                    END);

                IF _edge1_is_offroad IS DISTINCT FROM _edge2_is_offroad THEN
                    INSERT INTO gis.debug_log (pass_count, action, detail) VALUES (pass_count, 'MERGE_SKIP_TRANSITION', 'Node ' || rec.node_id || ' preserved: is_offroad differs between edges ' || rec.edges[1] || ' and ' || rec.edges[2] || '.');
                    CONTINUE;
                END IF;

                new_source := CASE WHEN edge1.source_node = rec.node_id THEN edge1.target_node ELSE edge1.source_node END;
                new_target := CASE WHEN edge2.source_node = rec.node_id THEN edge2.target_node ELSE edge2.source_node END;

                -- Orient edges
                IF edge1.source_node = rec.node_id THEN edge1.geom := ST_Reverse(edge1.geom); END IF;
                IF edge2.target_node = rec.node_id THEN edge2.geom := ST_Reverse(edge2.geom); END IF;

                -- Build line
                new_geom := ST_RemoveRepeatedPoints(ST_MakeLine(edge1.geom, edge2.geom));
                _geom_type := GeometryType(new_geom);
                
                INSERT INTO gis.debug_log (pass_count, action, detail) VALUES (pass_count, 'GEOMETRY_CHECK', 'ST_MakeLine produced geometry type: ' || _geom_type);

                IF _geom_type = 'LINESTRING' THEN
                    INSERT INTO gis.edges (
                        source_node, target_node, geom, length_m,
                        surface, has_no_entry, tracktype, has_barrier, is_restricted, highway, is_offroad
                    ) VALUES (
                        new_source, new_target, new_geom, ST_Length(new_geom::geography)::real,
                        COALESCE(edge1.surface, edge2.surface),
                        edge1.has_no_entry OR edge2.has_no_entry,
                        GREATEST(edge1.tracktype, edge2.tracktype), 
                        edge1.has_barrier OR edge2.has_barrier,     
                        edge1.is_restricted OR edge2.is_restricted,
                        COALESCE(edge1.highway, edge2.highway),
                        edge1.is_offroad OR edge2.is_offroad
                    );
                    DELETE FROM gis.edges WHERE edge_id IN (edge1.edge_id, edge2.edge_id);
                    merged_count := merged_count + 1;
                    INSERT INTO gis.debug_log (pass_count, action, detail) VALUES (pass_count, 'MERGE_SUCCESS', 'Successfully merged and replaced edges.');
                ELSE
                    INSERT INTO gis.debug_log (pass_count, action, detail) VALUES (pass_count, 'MERGE_ABORT', 'Aborted because geometry was not a LINESTRING.');
                END IF;
            END LOOP;

            -- D. NUKE DUPLICATE EDGES
            WITH duplicates AS (
                SELECT edge_id, ROW_NUMBER() OVER(PARTITION BY LEAST(source_node, target_node), GREATEST(source_node, target_node) ORDER BY length_m ASC) as rn
                FROM gis.edges
            )
            DELETE FROM gis.edges WHERE edge_id IN (SELECT edge_id FROM duplicates WHERE rn > 1);
            GET DIAGNOSTICS deleted_dupes = ROW_COUNT;
            
            INSERT INTO gis.debug_log (pass_count, action, detail) VALUES (pass_count, 'PASS_END', 'Deleted ' || deleted_dead_ends || ' dead ends, ' || deleted_loops || ' loops, ' || deleted_dupes || ' dupes. Merged ' || merged_count);

        END LOOP;
    END $$;

    -- Phase 3: Garbage Collect Orphan Nodes
    -- NOT IN is not null-safe (source_node/target_node are nullable), so Postgres
    -- can't prove a hash anti join is safe here and falls back to a per-row linear
    -- re-scan of both subqueries - catastrophically slow at country scale.
    -- NOT EXISTS has no such restriction and gets planned as a cheap Hash Anti Join.
    DELETE FROM gis.nodes n
    WHERE NOT EXISTS (SELECT 1 FROM gis.edges e WHERE e.source_node = n.node_id)
      AND NOT EXISTS (SELECT 1 FROM gis.edges e WHERE e.target_node = n.node_id);
EOF

echo "Step 5/7: Unzipping and Importing SRTM Elevation Data..."

# 1. Create a secure, temporary directory in Linux
TEMP_HGT_DIR=$(mktemp -d)

# 2. Extract ONLY the .hgt files from the zips into the temp folder
echo " -> Extracting zipped HGT files to temporary memory..."
unzip -q -j "$ELEVATION_ZIPS_DIR/*.zip" "*.hgt" -d "$TEMP_HGT_DIR"

# 3. Drop any broken/old SRTM table to ensure a clean slate
psql -v ON_ERROR_STOP=1 -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "DROP TABLE IF EXISTS public.srtm CASCADE;"

# 4. Process rasters ONE BY ONE to prevent memory crashes
echo " -> Processing rasters file by file..."
FIRST_FILE=true
for hgt_file in "$TEMP_HGT_DIR"/*.hgt; do
    filename=$(basename "$hgt_file")
    echo "    Loading tile: $filename ..."
    
    if [ "$FIRST_FILE" = true ]; then
        # First file: Create table, chop into 100x100 tiles, add index and constraints.
        # -x skips the max-extent constraint here: applying it against only the first
        # tile would reject every other (geographically distant) tile appended below.
        raster2pgsql -s 4326 -t 100x100 -c -I -C -x -M "$hgt_file" public.srtm | psql -q -v ON_ERROR_STOP=1 -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME
        FIRST_FILE=false
    else
        # Remaining files: Append data, chop into 100x100 tiles
        raster2pgsql -s 4326 -t 100x100 -a -M "$hgt_file" public.srtm | psql -q -v ON_ERROR_STOP=1 -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME
    fi
done

# 4.5 TELL POSTGRESQL TO INDEX THE NEW 3D MAP
echo " -> Analyzing spatial statistics for lightning-fast intersections..."
# The max-extent constraint was skipped per-file above, so add it now that all
# tiles (covering the full country) are loaded and its true extent is known.
psql -v ON_ERROR_STOP=1 -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "SELECT AddRasterConstraints('public'::name, 'srtm'::name, 'rast'::name, 'extent');"
psql -v ON_ERROR_STOP=1 -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "ANALYZE public.srtm;"

# 5. Nuke the extracted files to save your hard drive space
echo " -> Deleting unzipped temporary files..."
rm -rf "$TEMP_HGT_DIR"

echo "Step 6/7: 3D Elevation Gain & Heatmap Generation..."
psql -v ON_ERROR_STOP=1 -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME << 'EOF'
    -- A. ELEVATION GAIN
    ALTER TABLE gis.nodes ADD COLUMN IF NOT EXISTS altitude real;
    
    -- The raster GiST index (from raster2pgsql -I) is built on ST_ConvexHull(rast),
    -- so the filter must use that same expression for the planner to match it -
    -- "s.rast && n.geom" alone doesn't hit the index and forces a full scan.
    UPDATE gis.nodes n SET altitude = ST_Value(s.rast, n.geom)
    FROM public.srtm s
    WHERE ST_ConvexHull(s.rast) && n.geom
      AND ST_Intersects(s.rast, n.geom);

    UPDATE gis.edges e SET elevation_gain = ABS(t.altitude - s.altitude)
    FROM gis.nodes s, gis.nodes t
    WHERE e.source_node = s.node_id 
      AND e.target_node = t.node_id
      AND s.altitude IS NOT NULL AND t.altitude IS NOT NULL;

    -- B. HEATMAP DENSITY GRID
    -- Pre-aggregated offroad density: each ~1km grid cell (0.01deg ~ 1.1km lon x 0.7km lat @49N)
    -- carries the total offroad edge length and edge count binned into it. Arena selection sums a
    -- handful of these cells within a loop's reach instead of scanning the raw offroad geometry.
    DROP MATERIALIZED VIEW IF EXISTS gis.heatmap;
    CREATE MATERIALIZED VIEW gis.heatmap AS
    SELECT
        ST_SnapToGrid(ST_Centroid(geom), 0.01) AS cell_geom,
        SUM(length_m)                          AS offroad_len_m,
        COUNT(*)                               AS edge_count
    FROM gis.edges
    WHERE is_offroad = true
    GROUP BY ST_SnapToGrid(ST_Centroid(geom), 0.01);

    CREATE INDEX IF NOT EXISTS idx_gis_heatmap_geom ON gis.heatmap USING GIST (cell_geom);

    -- C. FLAG OFFROAD LOOP ENTRY POINTS
    ALTER TABLE gis.nodes ADD COLUMN IF NOT EXISTS is_entry_point boolean DEFAULT false;
    UPDATE gis.nodes SET is_entry_point = false;

    WITH offroad_node_counts AS (
        SELECT node_id
        FROM (
            SELECT source_node AS node_id FROM gis.edges WHERE is_offroad = true
            UNION ALL 
            SELECT target_node AS node_id FROM gis.edges WHERE is_offroad = true
        ) sub
        GROUP BY node_id
        HAVING COUNT(*) >= 2
    ),
    valid_trailheads AS (
        SELECT DISTINCT onc.node_id
        FROM offroad_node_counts onc
        JOIN gis.nodes n ON onc.node_id = n.node_id
        -- The geography-cast ST_DWithin alone can't use the plain-geometry GiST
        -- index on raw_lines.geom, which forces a brute-force scan of every
        -- candidate-node x raw_line pair. The "&&"/ST_Expand check is an
        -- index-accelerated bounding-box pre-filter (0.001 deg is a safe
        -- upper bound for 50m at CZ latitudes); ST_DWithin still makes the
        -- exact geodesic decision, so results are unchanged.
        -- ST_Expand must wrap the outer/search geometry (n.geom), not r.geom -
        -- expanding the indexed inner column makes the condition non-sargable
        -- and the planner falls back to scanning every filtered raw_lines row
        -- per candidate node instead of using raw_lines_geom_idx.
        JOIN staging.raw_lines r ON r.geom && ST_Expand(n.geom, 0.001)
                                 AND ST_DWithin(n.geom::geography, r.geom::geography, 50)
        WHERE r.surface IN ('asphalt', 'paved', 'concrete')
           OR r.highway IN ('primary', 'secondary', 'tertiary', 'residential')
    )
    UPDATE gis.nodes SET is_entry_point = TRUE
    WHERE node_id IN (SELECT node_id FROM valid_trailheads);
EOF

echo "Step 6.5/7: Syncing restricted areas (national parks) between DB and GraphHopper..."
# Load the curated park polygons into gis.geometric_zones, re-flag edge.is_restricted,
# and export the DB's zones back out to GH's custom area, so the skeleton search and GH
# block the exact same parks (otherwise GH rejects park loops with ConnectionNotFound).
DB_HOST="$DB_HOST" DB_PORT="$DB_PORT" DB_NAME="$DB_NAME" DB_USER="$DB_USER" \
    bash "$(dirname "$0")/sync_restricted_areas.sh"

echo "Step 7/7: Backing Up Database..."
# This lives on the host filesystem, outside the Docker volume, so it survives
# a "docker compose down -v" (which deletes the pgdata volume completely -
# that's what wiped this database the last time and cost hours to rebuild).
mkdir -p "$BACKUP_DIR"
BACKUP_FILE="$BACKUP_DIR/offroad_$(date +%Y%m%d_%H%M%S).dump"
pg_dump -h $DB_HOST -p $DB_PORT -U $DB_USER -Fc -f "$BACKUP_FILE" $DB_NAME
cp -f "$BACKUP_FILE" "$BACKUP_DIR/offroad_latest.dump"
echo " -> Backup written to $BACKUP_FILE"

echo "========================================================"
echo " PIPELINE COMPLETE!"
echo "========================================================"

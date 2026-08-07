1#!/bin/bash
# ==============================================================================
# OFFROAD ROUTING GRAPH BUILDER (PALKOVICE TEST AREA)
# Features: Topology Noding, Infection, Recursive Edge Contraction, 3D Elevation
# ==============================================================================

set -e

DB_HOST="localhost"
DB_PORT="5433"
DB_NAME="offroad"
DB_USER="offroad"
export PGPASSWORD="offroad"

RAW_PBF="../../graphhopper/data/czech-republic-latest.osm.pbf"
BBOX_PBF="../../graphhopper/data/palkovice_raw.osm.pbf"
FILTERED_PBF="../../graphhopper/data/filtered_palkovice.osm.pbf"
LUA_SCRIPT="import_rules.lua"
BBOX="18.1343,49.5502,18.4117,49.7298"

echo "========================================================"
echo "STARTING 3D TOPOLOGY PIPELINE (PALKOVICE)..."
echo "========================================================"

echo "Step 1/5: Extracting and Filtering OSM Data..."
osmium extract -b $BBOX $RAW_PBF -o $BBOX_PBF --overwrite
osmium tags-filter $BBOX_PBF \
    nwr/highway nwr/barrier nwr/boundary=national_park nwr/boundary=protected_area nwr/leisure=nature_reserve nwr/landuse=forest nwr/landuse=meadow \
    -o $FILTERED_PBF --overwrite

echo "Step 2/5: Creating Schemas & Wiping Old Data..."
psql -v ON_ERROR_STOP=1 -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME << 'EOF'

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
        has_no_entry text,
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

    -- 6. Wipe the tables clean for the new run
    TRUNCATE TABLE gis.edges CASCADE;
    TRUNCATE TABLE gis.nodes CASCADE;
    TRUNCATE TABLE gis.geometric_zones CASCADE;
EOF

osm2pgsql -d $DB_NAME -H $DB_HOST -P $DB_PORT -U $DB_USER -O flex -S $LUA_SCRIPT $FILTERED_PBF

echo "Step 3/5: Noding & Business Logic Infection..."
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

    -- C. THE INFECTION RULES
    WITH infected_nodes AS (
        SELECT n.node_id FROM gis.nodes n
        JOIN staging.barrier_nodes b ON ST_DWithin(n.geom, b.geom, 0.00001)
    )
    UPDATE gis.edges e SET has_barrier = TRUE
    WHERE source_node IN (SELECT node_id FROM infected_nodes) OR target_node IN (SELECT node_id FROM infected_nodes);

    UPDATE gis.edges e SET is_restricted = TRUE
    FROM gis.geometric_zones z
    WHERE z.zone_type = 2 AND ST_Intersects(e.geom, z.geom);
EOF

echo " Step 4/5: The Great Topological Optimizer (DEBUG MODE)..."
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
    DELETE FROM gis.nodes
    WHERE node_id NOT IN (SELECT source_node FROM gis.edges)
      AND node_id NOT IN (SELECT target_node FROM gis.edges);
EOF

echo "Step 5/5: 3D Elevation Gain & Heatmap Generation..."
psql -v ON_ERROR_STOP=1 -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME << 'EOF'
    -- A. ELEVATION GAIN
    ALTER TABLE gis.nodes ADD COLUMN IF NOT EXISTS altitude real;
    
    UPDATE gis.nodes n SET altitude = ST_Value(s.rast, n.geom)
    FROM public.srtm s WHERE ST_Intersects(s.rast, n.geom);

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
        JOIN staging.raw_lines r ON ST_DWithin(n.geom::geography, r.geom::geography, 50) 
        WHERE r.surface IN ('asphalt', 'paved', 'concrete') 
           OR r.highway IN ('primary', 'secondary', 'tertiary', 'residential')
    )
    UPDATE gis.nodes SET is_entry_point = TRUE 
    WHERE node_id IN (SELECT node_id FROM valid_trailheads);
EOF

echo "========================================================"
echo " PIPELINE COMPLETE! Topologically sound and fully 3D."
echo "========================================================"

#!/bin/bash
# ==============================================================================
# SYNC RESTRICTED AREAS (national parks) - single source of truth = the DB.
#
# Keeps the skeleton search (Postgres gis.edges.is_restricted) and GraphHopper
# (its cz_parks custom area) blocking the EXACT same park polygons, so a loop the
# skeleton proposes is never rejected by GH with ConnectionNotFound.
#
#   1. LOAD  the curated cz_national_parks GeoJSON into gis.geometric_zones (type 2).
#   2. FLAG  gis.edges.is_restricted for every edge intersecting a type-2 zone.
#   3. EXPORT gis.geometric_zones (type 2) back out to GH's custom_areas GeoJSON,
#            so GH consumes the DB's zones (DB -> GH). Restart GH to pick it up.
#
# Called at the end of build_routing_graph.sh; also runnable standalone after a
# manual park-data change. Requires python3 (to turn the GeoJSON into SQL).
# ==============================================================================
set -e

DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5433}"
DB_NAME="${DB_NAME:-offroad}"
DB_USER="${DB_USER:-offroad}"
export PGPASSWORD="${PGPASSWORD:-offroad}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PARKS_GEOJSON="${PARKS_GEOJSON:-$SCRIPT_DIR/../../graphhopper/data/restricted_areas/cz_national_parks.geojson.json}"
GH_CUSTOM_AREA="${GH_CUSTOM_AREA:-$SCRIPT_DIR/../../graphhopper/data/restricted_areas/cz_national_parks.geojson.json}"

PSQL="psql -v ON_ERROR_STOP=1 -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME"

echo "[sync] 1/3 Loading park polygons from $PARKS_GEOJSON ..."
# Emit one INSERT per GeoJSON feature (ST_GeomFromGeoJSON needs a geometry, not a
# FeatureCollection), tagged zone_type = 2 (restricted).
python3 - "$PARKS_GEOJSON" <<'PY' | $PSQL -q
import json, sys
features = json.load(open(sys.argv[1]))["features"]
for f in features:
    g = json.dumps(f["geometry"]).replace("'", "''")
    print(f"""INSERT INTO gis.geometric_zones (zone_type, geom, area_sqm)
SELECT 2,
       ST_Multi(ST_CollectionExtract(ST_MakeValid(ST_SetSRID(ST_GeomFromGeoJSON('{g}'),4326)),3)),
       ST_Area(ST_SetSRID(ST_GeomFromGeoJSON('{g}'),4326)::geography)::real;""")
PY

echo "[sync] 2/3 Flagging edges inside restricted zones ..."
$PSQL -q -c "
    UPDATE gis.edges e SET is_restricted = TRUE
    FROM gis.geometric_zones z
    WHERE z.zone_type = 2 AND ST_Intersects(e.geom, z.geom) AND e.is_restricted = FALSE;"

echo "[sync] 3/3 Exporting DB zones -> GraphHopper custom area $GH_CUSTOM_AREA ..."
# GH's custom area 'cz_parks' = the union of all restricted zones in the DB.
$PSQL -tA -c "
    COPY (
        SELECT json_build_object(
            'type','FeatureCollection',
            'features', json_build_array(json_build_object(
                'type','Feature',
                'properties', json_build_object('id','cz_parks'),
                'geometry', ST_AsGeoJSON(ST_Union(geom))::json))
        )::text
        FROM gis.geometric_zones WHERE zone_type = 2
    ) TO STDOUT" > "$GH_CUSTOM_AREA"

echo "[sync] Done. Restart GraphHopper to load the updated custom area."

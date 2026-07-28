-- ==========================================================
-- osm2pgsql Flex Import Script for Offroad Routing Database
-- Target EPSG: 4326
-- ==========================================================

local tables = {}

-- 1. STAGING: BARRIER NODES
-- We only save nodes that represent a physical barrier. 
-- We will use these later to "infect" the intersecting edges.
tables.barrier_nodes = osm2pgsql.define_table({
    name = 'barrier_nodes',
    schema = 'staging',
    ids = { type = 'node', id_column = 'node_id' },
    columns = {
        { column = 'geom', type = 'point', projection = 4326 },
        { column = 'barrier_type', type = 'text' }
    }
})

-- 2. STAGING: RAW LINES
-- Raw OSM ways representing roads, tracks, and paths. 
-- These will be shattered into topology edges in Phase 2.
tables.raw_lines = osm2pgsql.define_table({
    name = 'raw_lines',
    schema = 'staging',
    ids = { type = 'way', id_column = 'way_id' },
    columns = {
        { column = 'geom', type = 'linestring', projection = 4326 },
        { column = 'highway', type = 'text' },
        { column = 'tracktype', type = 'text' },
        { column = 'surface', type = 'text' },
        { column = 'has_no_entry', type = 'boolean' }
    }
})

-- 3. STAGING: RAW POLYGONS
-- Natural areas and restricted zones.
tables.raw_polygons = osm2pgsql.define_table({
    name = 'raw_polygons',
    schema = 'staging',
    ids = { type = 'any', type_column = 'osm_type', id_column = 'osm_id' },
    columns = {
        { column = 'geom', type = 'multipolygon', projection = 4326 },
        { column = 'zone_type', type = 'int2' } -- 1: Nature, 2: Restricted
    }
})

-- ==========================================================
-- PROCESSING FUNCTIONS
-- ==========================================================

-- Helper function to check if access is restricted
local function check_no_entry(tags)
    local access = tags.access
    local motor = tags.motor_vehicle
    local vehicle = tags.vehicle

    -- Standard strict bans in OSM
    if access == 'private' or access == 'no' or access == 'forestry' or access == 'agricultural' then return true end
    if motor == 'no' or motor == 'private' or motor == 'forestry' or motor == 'agricultural' then return true end
    if vehicle == 'no' or vehicle == 'private' then return true end

    return false
end

-- Process Points (Nodes)
function osm2pgsql.process_node(object)
    if object.tags.barrier then
        local b = object.tags.barrier
        -- Only care about barriers that actually block a vehicle
        if b == 'gate' or b == 'lift_gate' or b == 'chain' or b == 'block' or b == 'bollard' then
            tables.barrier_nodes:insert({
                geom = object:as_point(),
                barrier_type = b
            })
        end
    end
end

-- Process Lines (Ways)
function osm2pgsql.process_way(object)
    -- 1. Process Routable Highways
    if object.tags.highway then
        local hw = object.tags.highway
        
        -- IMPASSABLE FOR CARS: Drop these immediately.
        if hw == 'path' or hw == 'footway' or hw == 'cycleway' or hw == 'bridleway' or hw == 'steps' or hw == 'pedestrian' or hw == 'corridor' then
            return -- Exit the function, do not save to database
        end
            
        -- For everything else (track, unclassified, tertiary, etc.), force LineString output
        if not object.tags.area or object.tags.area ~= 'yes' then
            tables.raw_lines:insert({
                geom = object:as_linestring(),
                highway = hw,
                tracktype = object.tags.tracktype,
                surface = object.tags.surface,
                has_no_entry = check_no_entry(object.tags)
            })
        end
    end

    -- 2. Process simple closed ways as Polygons
    if object.is_closed then
        local z_type = nil
        
        -- Check for Nature Zones (Type 1)
        if object.tags.landuse == 'forest' or object.tags.natural == 'wood' or object.tags.natural == 'scrub' or object.tags.landuse == 'meadow' then
            z_type = 1
        -- Check for Restricted Zones (Type 2)
        elseif object.tags.boundary == 'national_park' or object.tags.leisure == 'nature_reserve' or object.tags.boundary == 'protected_area' then
            z_type = 2
        end

        if z_type then
            tables.raw_polygons:insert({
                geom = object:as_polygon(),
                zone_type = z_type
            })
        end
    end
end
-- Process MultiPolygons (Relations)
function osm2pgsql.process_relation(object)
    -- Only process multipolygon relations
    if object.tags.type == 'multipolygon' or object.tags.type == 'boundary' then
        local z_type = nil
        
        if object.tags.landuse == 'forest' or object.tags.natural == 'wood' or object.tags.natural == 'scrub' then
            z_type = 1
        elseif object.tags.boundary == 'national_park' or object.tags.leisure == 'nature_reserve' then
            z_type = 2
        end

        if z_type then
            tables.raw_polygons:insert({
                geom = object:as_multipolygon(),
                zone_type = z_type
            })
        end
    end
end

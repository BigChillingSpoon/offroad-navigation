import psycopg2
from psycopg2.extras import execute_values
import rasterio
import math
import os

# --- KONFIGURACE ---
# Cesta ke složce, kde má GraphHopper ty .hgt soubory (z tvého obrázku)
SRTM_CACHE_DIR = r"C:\Users\vikslajvant\source\repos\offroad-navigation\routing\graphhopper\data\srtm"

DB_PARAMS = {
    "dbname": "offroad",
    "user": "offroad",
    "password": "offroad",
    "host": "localhost",
    "port": "5433"
}
# -------------------

def get_hgt_filename(lon, lat):
    """Vypočítá název SRTM dlaždice podle souřadnic (např. N49E018)."""
    lat_int = math.floor(lat)
    lon_int = math.floor(lon)
    
    lat_prefix = 'N' if lat_int >= 0 else 'S'
    lon_prefix = 'E' if lon_int >= 0 else 'W'
    
    # Formát je vždy NXXEYYY (např. N49E018)
    return f"{lat_prefix}{abs(lat_int):02d}{lon_prefix}{abs(lon_int):03d}"


def main():
    print("Připojuji se k databázi...")
    conn = psycopg2.connect(**DB_PARAMS)
    cur = conn.cursor()

    # 1. Vytáhneme body, které ještě nemají elevaci
    print("Stahuji body bez elevace...")
    cur.execute("""
        SELECT id, ST_X(geom) as lon, ST_Y(geom) as lat 
        FROM gis.offroad_hookpoints 
        WHERE elevation IS NULL
    """)
    points = cur.fetchall()
    
    if not points:
        print("Všechny body už mají elevaci. Konec.")
        return

    # 2. Seskupíme body podle HGT dlaždice
    # Struktura: {'N49E018': [(id1, lon1, lat1), (id2, lon2, lat2), ...]}
    tiles = {}
    for pt in points:
        pt_id, lon, lat = pt
        tile_name = get_hgt_filename(lon, lat)
        if tile_name not in tiles:
            tiles[tile_name] = []
        tiles[tile_name].append(pt)

    print(f"Body rozděleny do {len(tiles)} SRTM dlaždic.")

    # 3. Zpracování každé dlaždice
    updates = []
    
    for tile_name, pts in tiles.items():
        # Zkusíme najít soubor. Windows je často ukazují jako 'N49E018.hgt' ale reálně to je .zip
        zip_path = os.path.join(SRTM_CACHE_DIR, f"{tile_name}.hgt.zip")
        raw_path = os.path.join(SRTM_CACHE_DIR, f"{tile_name}.hgt")
        
        raster_path = None
        if os.path.exists(zip_path):
            # Kouzlo rasteria: umí číst přímo zevnitř zipu bez rozbalování!
            raster_path = f"zip://{zip_path}!/{tile_name}.hgt"
        elif os.path.exists(raw_path):
            raster_path = raw_path
        
        if not raster_path:
            print(f"[VAROVÁNÍ] Soubor pro {tile_name} nenalezen. Přeskakuji {len(pts)} bodů.")
            continue

        print(f"Zpracovávám dlaždici {tile_name} ({len(pts)} bodů)...")
        
        try:
            with rasterio.open(raster_path) as src:
                # Připravíme seznam souřadnic [(lon, lat), ...] pro rasterio
                coords = [(lon, lat) for _, lon, lat in pts]
                
                # Rasterio.sample přečte výšky pro všechny body naráz (brutálně rychlé)
                elevations = [val[0] for val in src.sample(coords)]
                
                # Spárujeme ID s novou výškou pro databázový update
                for pt, elev in zip(pts, elevations):
                    pt_id = pt[0]
                    # Ošetření: SRTM má někdy pro chybějící data hodnotu -32768
                    if elev > -10000: 
                        updates.append((float(elev), pt_id))
        except Exception as e:
            print(f"[CHYBA] Nelze přečíst {raster_path}: {e}")

    # 4. Hromadný (batch) update zpět do Postgresu
    if updates:
        print(f"Zapisuji {len(updates)} výškových bodů do databáze...")
        update_query = """
            UPDATE gis.offroad_hookpoints AS o
            SET elevation = data.elev
            FROM (VALUES %s) AS data(elev, id)
            WHERE o.id = data.id
        """
        # execute_values je asi 100x rychlejší než psát cur.execute() v cyklu
        execute_values(cur, update_query, updates)
        conn.commit()
        print("Hotovo! Elevace byla úspěšně doplněna.")
    else:
        print("Nebyly nalezeny žádné nové výšky k aktualizaci.")

    cur.close()
    conn.close()

if __name__ == "__main__":
    main()
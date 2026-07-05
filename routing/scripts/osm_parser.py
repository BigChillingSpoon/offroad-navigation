import osmium
import math
import sys
import shutil
import os
import random
if len(sys.argv) < 2:
    print("Pouziti: python osm_parser.py <cesta_k_souboru.pbf>")
    sys.exit(1)

PBF_FILE = sys.argv[1]
OUTPUT_FILE = "realny_les_osm.txt"

# ---------------------------------------------------------
# FÁZE 1: Nalezení všech relevantních cest a uzlů
# ---------------------------------------------------------
class WayHandler(osmium.SimpleHandler):
    def __init__(self):
        super().__init__()
        self.used_nodes = set()
        self.ways = []
        self.valid_highways = {'track', 'path', 'footway', 'cycleway', 'unclassified', 'dirt'}

    def way(self, w):
        highway = w.tags.get('highway')
        if highway in self.valid_highways:
            # Urceni narocnosti (grade)
            grade = 3 # Vychozi stred
            tracktype = w.tags.get('tracktype')
            if tracktype == 'grade1': grade = 1 # Pevna cesta
            elif tracktype == 'grade2': grade = 2
            elif tracktype == 'grade3': grade = 3
            elif tracktype == 'grade4': grade = 4
            elif tracktype == 'grade5': grade = 5 # Rozbahnena/zarostla pesina

            nodes = [n.ref for n in w.nodes]
            self.used_nodes.update(nodes)
            self.ways.append({'nodes': nodes, 'grade': grade})

# ---------------------------------------------------------
# FÁZE 2: Načtení souřadnic pro použité uzly
# ---------------------------------------------------------
class NodeHandler(osmium.SimpleHandler):
    def __init__(self, used_nodes):
        super().__init__()
        self.used_nodes = used_nodes
        self.nodes_data = {}
        self.min_lat, self.max_lat = 90.0, -90.0
        self.min_lon, self.max_lon = 180.0, -180.0

    def node(self, n):
        if n.id in self.used_nodes:
            lat, lon = n.location.lat, n.location.lon
            self.nodes_data[n.id] = {'lat': lat, 'lon': lon}
            
            # Hledani hranic mapy (Bounding box)
            if lat < self.min_lat: self.min_lat = lat
            if lat > self.max_lat: self.max_lat = lat
            if lon < self.min_lon: self.min_lon = lon
            if lon > self.max_lon: self.max_lon = lon


print(f"Ctu cesty ze souboru {PBF_FILE}...")
way_handler = WayHandler()
way_handler.apply_file(PBF_FILE, locations=False)

print(f"Nalezeno {len(way_handler.ways)} lesnich cest.")
print("Ctu souradnice uzlu...")
node_handler = NodeHandler(way_handler.used_nodes)
node_handler.apply_file(PBF_FILE, locations=False)

# ---------------------------------------------------------
# FÁZE 3: Zpracování a přepočet do metrického systému X/Y
# ---------------------------------------------------------
print("Prepočitávám GPS na X/Y metry a generuji graf...")

# Konverzni konstanty
MEAN_LAT = (node_handler.min_lat + node_handler.max_lat) / 2.0
METERS_PER_DEG_LAT = 111320.0
METERS_PER_DEG_LON = 111320.0 * math.cos(math.radians(MEAN_LAT))

mapped_nodes = {}
edges = []
edge_counter = 0
osm_to_local_id = {}

# 1. Priprava uzlu (Krizovatek)
for i, (osm_id, data) in enumerate(node_handler.nodes_data.items()):
    osm_to_local_id[osm_id] = i
    
    # Prevod na metry (relativne k levemu dolnimu rohu)
    x = int((data['lon'] - node_handler.min_lon) * METERS_PER_DEG_LON)
    # Zapadoklademe Y odspodu nahoru, ale SDL ho ma odshora dolu, 
    # pro kompatibilitu proste odecitame od minima
    y = int((node_handler.max_lat - data['lat']) * METERS_PER_DEG_LAT)

    # Detekce okraju (treba do 15% kraje mapy)
    x_max = (node_handler.max_lon - node_handler.min_lon) * METERS_PER_DEG_LON
    y_max = (node_handler.max_lat - node_handler.min_lat) * METERS_PER_DEG_LAT
    
    is_entry = (x < x_max*0.05 or x > x_max*0.95 or y < y_max*0.05 or y > y_max*0.95)
    is_close = (x < x_max*0.15 or x > x_max*0.15 or y < y_max*0.15 or y > y_max*0.85)

    mapped_nodes[i] = {
        'id': i, 'x': x, 'y': y,
        'is_close': 1 if is_close else 0,
        'is_entry': 1 if is_entry else 0,
        'cesty_ids': []
    }

# 2. Priprava cest (Hran)
for way in way_handler.ways:
    grade = way['grade']
    nodes = way['nodes']
    
    for i in range(len(nodes) - 1):
        n1_osm = nodes[i]
        n2_osm = nodes[i+1]
        
        # Validace (obcas v PBF chybi uzly pokud je vyrez spatne oriznuty)
        if n1_osm not in osm_to_local_id or n2_osm not in osm_to_local_id:
            continue
            
        n1 = osm_to_local_id[n1_osm]
        n2 = osm_to_local_id[n2_osm]
        
        edges.append({'id': edge_counter, 'grade': grade, 'A': n1, 'B': n2})
        mapped_nodes[n1]['cesty_ids'].append(edge_counter)
        mapped_nodes[n2]['cesty_ids'].append(edge_counter)
        edge_counter += 1

# ---------------------------------------------------------
# FÁZE 3.5: Očištění grafu a přidání atributů
# ---------------------------------------------------------
print("Hledam nejvetsi souvislou sit (odstranuji ostrovy)...")

# 1. Rychlé sestavení grafu pro vyhledávání
adj = {i: [] for i in mapped_nodes}
for e in edges:
    adj[e['A']].append(e['B'])
    adj[e['B']].append(e['A'])

# 2. BFS algoritmus pro nalezení komponent
visited = set()
largest_component = []

for start_node in adj:
    if start_node not in visited:
        queue = [start_node]
        visited.add(start_node)
        current_component = []
        
        while queue:
            curr = queue.pop(0)
            current_component.append(curr)
            for neighbor in adj[curr]:
                if neighbor not in visited:
                    visited.add(neighbor)
                    queue.append(neighbor)
                    
        if len(current_component) > len(largest_component):
            largest_component = current_component

print(f"Puvodni pocet uzlu: {len(mapped_nodes)}. Uzlu v hlavni siti: {len(largest_component)}.")

# 3. Přečíslování ID uzlů
old_to_new_id = {}
new_mapped_nodes = {}

for new_id, old_id in enumerate(largest_component):
    old_to_new_id[old_id] = new_id
    node_data = mapped_nodes[old_id].copy()
    node_data['id'] = new_id
    node_data['cesty_ids'] = []
    new_mapped_nodes[new_id] = node_data

# 4. Filtrace cest, přidání GRADIENTU a VZDÁLENOSTI
new_edges = []
new_edge_counter = 0

for e in edges:
    if e['A'] in old_to_new_id and e['B'] in old_to_new_id:
        new_a = old_to_new_id[e['A']]
        new_b = old_to_new_id[e['B']]
        
        # Simulace gradientu
        simulated_gradient = round(random.uniform(-15.0, 15.0), 1)
        
        # VÝPOČET VZDÁLENOSTI (v metrech)
        node_a_data = new_mapped_nodes[new_a]
        node_b_data = new_mapped_nodes[new_b]
        dx = node_a_data['x'] - node_b_data['x']
        dy = node_a_data['y'] - node_b_data['y']
        distance = round(math.hypot(dx, dy), 1) # Zaokrouhlíme na 1 desetinné místo
        
        new_edges.append({
            'id': new_edge_counter,
            'grade': e['grade'],
            'gradient': simulated_gradient,
            'length': distance, # <-- PŘIDANÁ DÉLKA
            'A': new_a,
            'B': new_b
        })
        
        new_mapped_nodes[new_a]['cesty_ids'].append(new_edge_counter)
        new_mapped_nodes[new_b]['cesty_ids'].append(new_edge_counter)
        new_edge_counter += 1

mapped_nodes = new_mapped_nodes
edges = new_edges

# ---------------------------------------------------------
# FÁZE 4: Zápis do TXT souboru
# ---------------------------------------------------------
with open(OUTPUT_FILE, 'w', encoding='utf-8') as f:
    f.write("[KRIZOVATKY]\n")
    for n_id in range(len(mapped_nodes)):
        n = mapped_nodes[n_id]
        is_shape_point = 1 if len(n['cesty_ids']) == 2 else 0
        is_dead_end = 1 if len(n['cesty_ids']) == 1 else 0
        cesty_str = ",".join(map(str, n['cesty_ids']))
        
        f.write(f"{n['id']};{n['x']};{n['y']};{n['is_close']};{n['is_entry']};{is_shape_point};{is_dead_end};{cesty_str}\n")
        
    f.write("\n[CESTY]\n")
    for e in edges:
        # PŘIDÁNÍ délky na konec řádku cesty: id;grade;A;B;gradient;length
        f.write(f"{e['id']};{e['grade']};{e['A']};{e['B']};{e['gradient']};{e['length']}\n")

print(f"HOTOVO! Vygenerovan soubor '{OUTPUT_FILE}'.")

# ---------------------------------------------------------
# FÁZE 5: Automatické zkopírování do CLion projektu
# ---------------------------------------------------------
DESTINATION_DIR = r"C:\Users\vikslajvant\Documents\Personal\grafy-c\cmake-build-debug"

print(f"\nKopiruji '{OUTPUT_FILE}' do CLion slozky...")

try:
    os.makedirs(DESTINATION_DIR, exist_ok=True)
    destination_path = os.path.join(DESTINATION_DIR, OUTPUT_FILE)
    shutil.copy2(OUTPUT_FILE, destination_path)
    print("✅ Kopirovani uspesne! Muzes rovnou spustit program v CLionu.")
except Exception as e:
    print(f"❌ Chyba pri kopirovani: {e}")
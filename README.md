# Offroad Navigation

A navigation backend for people who leave the asphalt. Ordinary map apps optimize for the fastest way from A to B, this one optimizes for the *drive itself*: forest tracks, dirt roads, elevation, and how much of the trip is actually off-road.

The project is currently **API-only**. Clients (native mobile apps and a web client) are planned but not started yet.

---

## Core features

### Routes (A → B)

Point-to-point navigation with an off-road bias. You give a start and an end, plus your preferences, and the planner generates several route candidates, scores them, and returns the best match.

- `RouteBalance` — `Shortest`, `Balanced`, or `MaxOffroad` — controls how much detour you're willing to trade for unpaved kilometers.
- Access preferences: `AllowPrivateRoads`, `AllowGates`.
- Every result carries metrics: total distance, **off-road distance**, duration, elevation gain and loss.

### Loops (circular routes)

Give a single starting point and a preferred length, and the app builds a circular routes in the given radious -> that means all possible loops made on the spot based on users data. This is the interesting part.

Instead of firing hundreds of speculative requests at a routing engine, loops are built from a **Loop Skeleton** — a small, curated chain of off-road intersections that defines the loop's shape *before* any routing call happens:

1. **Arena selection** — a pre-computed off-road density grid picks a high-quality "arena" near the user, rather than relying on static forest polygons.
2. **Single spatial fetch** — all nodes and edges inside the target radius are loaded in one query.
3. **Constrained search** — an in-memory branch-and-bound / DFS walks the real graph edges, tracking the chain in lightweight `SkeletonVector` structs. Because it traverses actual edges, distance, elevation, and surface grade are exact — no Euclidean guesswork.
4. **Aggressive pruning** — barriers, no-entry ways and restricted zones are dropped according to user preferences; angle rules, dynamic minimum distance, and hard cuts keep the shape genuinely circular instead of a there-and-back.
5. **One provider call per skeleton** — the ordered nodes go to GraphHopper with `pass_through: true`, producing a single fluid turn-by-turn route through the validated skeleton.

The resulting candidates then run through the same scoring pipeline as routes.

### Saved trips

Both routes and loops can be saved, listed, fetched by id, and deleted.

---

## The GIS graph

The off-road quality comes from a pre-processed PostGIS graph built from OSM data, not from live queries:

| Table | What it holds |
|---|---|
| `gis.nodes` | Strict intersections (degree ≥ 3, or degree-2 where a critical property changes). Flags for `is_entry_point` (where pavement enters the forest) and `is_restricted` (national parks / strict reserves). |
| `gis.edges` | Routable segments with `length_m`, `grade`/`tracktype`, `surface`, `elevation_gain`. Dead ends are recursively pruned. |
| `gis.heatmap` | ~1 km off-road density grid used for arena selection. |
| `gis.geo_zones` | Cleaned nature and restricted-area polygons for spatial filtering. |

Two rules worth knowing:

- **The Infection Rule** — when several raw OSM ways merge into one edge, a barrier or ban on *any* sub-part infects the whole edge (`has_barrier`, `has_no_entry`, `is_restricted`).
- **Grade mask** — path difficulty (grade 1–5) is stored as a `grades_mask` integer and decoded with bitwise operations.

The graph is built by `routing/scripts/database/build_routing_graph.sh` (osmium filter → topology noding → infection → edge contraction → SRTM elevation). The same access rules are mirrored in the GraphHopper custom models, so the skeleton search and the routing engine block exactly the same things.

---

## Architecture

.NET 8 / C#, Clean Architecture with strict CQRS.

```
Offroad.Api            ASP.NET controllers, DI, Swagger
Routing.Application    planning pipeline, CQRS handlers, contracts
Routing.Domain         entities and business rules (Trip, TripPlan)
Offroad.Infrastructure GraphHopper client, PostgreSQL/PostGIS access
```

Every request — route or loop — flows through the same `PlanningPipeline<TIntent, TCandidate>`:

**Intent** → **Generator** (calls the routing provider) → **Goal** (does it satisfy the request?) → **Scorer** (how well does it match preferences?) → **Mapper** (→ `TripPlan`).

Dependencies point inward. The Application layer talks to abstractions (`IRoutingProvider`, `ITripRepository`); GraphHopper and PostgreSQL live behind them in Infrastructure.

---

## API

| Method | Endpoint | Purpose |
|---|---|---|
| `POST` | `/api/routes/plan` | Plan an A → B route |
| `GET` | `/api/routes` · `/api/routes/{id}` | List / fetch saved routes |
| `POST` · `DELETE` | `/api/routes` | Save / delete a route |
| `POST` | `/api/loops/find` | Find circular routes from a start point |
| `GET` | `/api/loops` · `/api/loops/{id}` | List / fetch saved loops |
| `POST` · `DELETE` | `/api/loops` | Save / delete a loop |

Swagger is available in development. Postman collections live in `server/postman`.

---

## Running it

```bash
cd docker
docker compose up -d          # postgres+postgis+pgrouting, graphhopper, seq
```

| Service | Port |
|---|---|
| PostgreSQL / PostGIS | `5433` |
| GraphHopper | `8989` |
| Seq (structured logs) | `5341` |

Then build the routing graph once (`routing/scripts/database/build_routing_graph.sh` — it needs an OSM `.pbf` extract and SRTM elevation data in `routing/graphhopper/data`) and run the API:

```bash
cd server
dotnet run --project Offroad.Api
```

---

## Roadmap

- **Native mobile apps** (iOS / Android) — the primary target; offline-capable turn-by-turn for places without signal.
- **Web client** — planning and trip management in the browser.
- Per-user trip ownership and soft deletes.
- Expeditions - multiple day trips.
- Continued tuning of the loop skeleton search and off-road scoring.
- For what I am currenty building see open issues.
---


\# Domain Knowledge

This document details the business logic and processes specific to the two main features of the Offroad Navigation app: **Routes** and **Loops**.

## 1. Domain 1: Routes (A -\> B Navigation)

This is the core point-to-point navigation system. It involves generating and scoring route candidates from a start to an end point. The term **Path** is reserved for the final geometry returned by the routing provider (e.g., GraphHopper), and **Segment** is reserved for the output of our `SegmentBuilder` logic.

## 2. Domain 2: Loops (Circular Routes)

This feature generates custom-tailored circular routes from a single starting point. The core of this process is the generation of a "Loop Skeleton"—a set of highly curated elite nodes (intersections) that define the loop's shape before making any calls to the routing engine.

### Pre-calculated Data

- **Hookpoints**: We maintain a `gis.nodes` table in PostgreSQL. These are pre-filtered, high-quality off-road intersections.

- **Grade Mask**: Path difficulty (Grade 1-5) is stored as a `grades\_mask` integer, decoded in C\# using bitwise operations.

### 2.1. Pre-calculated Database Architecture (The GIS Graph)

We no longer rely on a simple point cloud of hookpoints. Our PostgreSQL/PostGIS database contains a fully pre-processed, routable topological graph designed specifically for offroad skeleton generation.

- **Nodes (`gis.nodes`):** Strict intersections. A node exists ONLY if it connects 3 or more edges.

  - *Exception:* A degree-2 node is kept only if it represents a change in critical properties (e.g., transition from asphalt to dirt/grade change).

  - *Entry Points:* Nodes marked with `is\_entry\_point = true` signify the boundary of an offroad zone where a paved road enters the forest. We want to plan the loop from that point if the user is not inside of a forest, if yes we want also to find close to user loops without need for entry point node.

  - *Restrictions:* Nodes marked with `is\_restricted = true` lie directly inside a National Park or strict reserve.

- **Edges (`gis.edges`):** The actual routable segments connecting the Nodes. Dead-ends (dangling edges) are recursively pruned from the DB.

  - Attributes include `length\_m`, `grade`/`tracktype`, `surface`, and `elevation\_gain`.

  - *The Infection Rule:* Edges contain boolean flags (`has\_barrier`, `has\_no\_entry`, `is\_restricted`). If multiple raw OSM ways were merged into a single edge, and *any* sub-part had a barrier or ban, the entire resulting edge inherits (is "infected" by) this flag.

- **Offroad Arenas (gis.heatmap):** A spatial grid (e.g., materialized view of hexagons/squares) that calculates the density of offroad edges. Empty cells are discarded. This is used to find optimal starting locations (Arenas) dynamically, rather than relying on static forest polygons.

- **Geometric Zones (`gis.geo\_zones`):** Cleaned, valid polygons of nature (`Type = 1`) and restricted areas (`Type = 2`) used purely for spatial context and filtering.


### Loop Skeleton Generation: In-Memory DFS

Instead of random searches or excessive provider calls, we use a deterministic, in-memory algorithm traversing our predefined DB graph to find the best node combinations.

1. **Arena Selection & Single DB Fetch**: The process begins by identifying a high-density "Offroad Arena" from the Heatmap. We perform a single spatial query to fetch all relevant `Nodes` and `Edges` within the target radius. All data needed for connectivity, distance, and difficulty (grade) is retrieved at once.

2. **Constrained DFS with `SkeletonVector`**: A Constrained Depth-First Search (DFS) algorithm explores potential loops by traversing the exact `Edges` between `Nodes`. The state of the search is tracked in a `List\<SkeletonVector\>`, referred to as the **VectorChain**.

   - A **`SkeletonVector`** is a lightweight `readonly struct` containing `\{FromNode, ToNode, EdgeLength, AccumulatedElevation, HeadingAngle\}`. Using a struct minimizes GC pressure.

   - Because we traverse actual Edges, `TraveledDistance` and `Grade` are exact, eliminating the need for heuristic Euclidean distance math for connectivity.

3. **Logical Pruning & Constraints**: The search is aggressively pruned using:

   - *Barrier/Restriction Avoidance:* Dropping paths traversing edges with `has\_barrier` or `has\_no\_entry` based on user preferences.

   - *Geometric Rules:* Angle Pruning, Dynamic Minimum Distance, and Hard-Cuts to ensure a circular shape.

4. **Output**: The result is a list of `SkeletonCandidate` objects, each containing the ordered Nodes from a valid, highly scored `VectorChain`.

### 2.3. Final Route Assembly: Single Provider Call

- For each `SkeletonCandidate`, the `LoopCandidateGenerator` makes a **single call** to the `IRoutingProvider` (GraphHopper).

- This call includes the full list of Nodes from the skeleton and the parameter **`pass\_through: true`**.

- This instructs the routing engine to generate one continuous, fluid route (turn-by-turn navigation path) that passes exactly through our mathematically validated skeleton without stopping or making U-turns.

- The resulting `ProviderRoute` is mapped to a `LoopTripCandidate` and proceeds through the standard scoring pipeline.



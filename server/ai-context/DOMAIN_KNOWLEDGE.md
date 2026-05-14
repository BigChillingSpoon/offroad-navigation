# Domain Knowledge

This document details the business logic and processes specific to the two main features of the Offroad Navigation app: **Routes** and **Loops**.

## 1. Domain 1: Routes (A -> B Navigation)
This is the core point-to-point navigation system. It involves generating and scoring route candidates from a start to an end point. The term **Path** is reserved for the final geometry returned by the routing provider (e.g., GraphHopper), and **Segment** is reserved for the output of our `SegmentBuilder` logic.

## 2. Domain 2: Loops (Circular Routes)
This feature generates custom-tailored circular routes from a single starting point. The core of this process is the generation of a "Loop Skeleton"—a set of 3-5 elite hookpoints that define the loop's shape before any calls to the routing engine.

### Pre-calculated Data
- **Hookpoints**: We maintain a `gis.offroad_hookpoints` table in PostgreSQL. These are pre-filtered, high-quality off-road intersections.
- **Grade Mask**: Path difficulty (Grade 1-5) is stored as a `grades_mask` integer, decoded in C# using bitwise operations.

### Loop Skeleton Generation: In-Memory DFS
Instead of random searches or excessive provider calls, we use a deterministic, in-memory algorithm to find the best hookpoint combinations.

1.  **Single DB Fetch & JIT Math**: The process begins with a single spatial query (`ST_DWithin`) to fetch all relevant hookpoints. To conserve memory and CPU, we **do not** pre-calculate an all-to-all distance matrix. All geometric calculations (distance, heading) are performed **Just-In-Time (JIT)** during the search.

2.  **Constrained DFS with `SkeletonVector`**: A Constrained Depth-First Search (DFS) algorithm explores potential connections. The state of the search is tracked in a `List<SkeletonVector>`, referred to as the **VectorChain**.
    - A **`SkeletonVector`** is a lightweight `readonly struct` containing `{From, To, EuclideanDistance, HeadingAngle}`. Using a struct minimizes GC pressure.
    - This `VectorChain` gives us the `TraveledDistance` and `CurrentHeading` with O(1) lookup time, avoiding recalculations.

3.  **Geometrical Pruning & Rules**: The search is aggressively pruned using a set of rules (Angle Pruning, Dynamic Minimum Distance, Hard-Cut, Branching Limit) to ensure efficiency.

4.  **Output**: The result is a list of `SkeletonCandidate` objects, each containing the ordered hookpoints from a valid `VectorChain`.

### Final Route Assembly: Single Provider Call
- For each `SkeletonCandidate`, the `LoopCandidateGenerator` makes a **single call** to the `IRoutingProvider`.
- This call includes the full list of hookpoints from the skeleton and the parameter **`pass_through: true`**.
- This instructs the routing engine (GraphHopper) to generate one continuous, fluid route that passes through the intermediate hookpoints without stopping or making U-turns. This is far more efficient than making multiple separate routing requests and stitching them together.
- The resulting `ProviderRoute` is then mapped to a `LoopTripCandidate` and proceeds through the standard scoring pipeline.

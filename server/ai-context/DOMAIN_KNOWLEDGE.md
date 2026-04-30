# Domain Knowledge

This document details the business logic and processes specific to the two main features of the Offroad Navigation app: **Routes** and **Loops**.

## 1. Domain 1: Routes (A -> B Navigation)

This is the core point-to-point navigation system.

### Pipeline & Candidate Generation
- A `PlanRouteRequest` is mapped to a `RouteIntent`.
- The `RouteCandidateGenerator` calls the `IRoutingProvider` (GraphHopper) to generate multiple route candidates using different routing profiles.
- **Profiles**:
    - `shortest`: The fastest path, allowing some off-road.
    - `balanced`: Prioritizes off-road segments but avoids excessively long detours.
    - `hardcore_offroad`: Maximizes off-road travel, even if it means significant detours.

### Scoring & Goals
- The `SegmentBuilder` processes each candidate's geometry, slicing it into segments to precisely calculate the total off-road distance.
- The `RouteGoal` ensures the candidate is valid (e.g., it reaches the destination).
- The `RouteScorer` scores candidates based on user preferences, primarily the ratio of off-road to total distance, aligned with the chosen profile.
- The highest-scoring candidate is selected as the final `TripPlan`.

### Constraints & Edge Cases
- **Dynamic Constraints**: User preferences like `AllowPrivateRoads` or `AllowGates` are translated into a GraphHopper `CustomModel` at runtime by the `GraphHopperProfileBuilder`. This modifies the graph traversal penalties on the fly.
- **Restricted Areas**: We globally avoid areas tagged `car=no`, and national parks/reserves through penalties in the base GraphHopper profiles, user can define it through the request body(AllowGates and AllowPrivateRoads properties).
- **Gate Handling**:
    - **Trapped User**: If the only path from the start point is through a gate, we permit it but attach a "Gate Encountered" event to the response.
    - **Last-Mile Barrier**: Gates within 50 meters of the destination are ignored to prevent routing failures when a destination is just behind a barrier.

## 2. Domain 2: Loops (Circular Routes)

This feature generates custom-tailored circular routes from a single starting point.

### Pre-calculated Data
- **Hookpoints**: We maintain a `gis.offroad_hookpoints` table in PostgreSQL. These are pre-filtered, high-quality off-road intersections (3+ branches) that serve as the building blocks for loops. They are clustered at a 40m radius.
- **Grade Mask**: Path difficulty (Grade 1-5) is stored as a `grades_mask` integer. This **must** be decoded in C# using bitwise operations (e.g., `(mask & 1) != 0` for Grade 1, `(mask & 16) != 0` for Grade 5).

### Three-Phase Routing ("The Prague Scenario")

Due to the complexity of finding a good off-road area near a user who might be in an urban environment, we use a multi-phase approach:

- **Phase 1 (Macro Search - In C#)**:
    - The system queries the database to find "Arenas"—clusters of `offroad_hookpoints`—that are within the user's specified maximum transit distance.

- **Phase 2 (Portal Search - GH Matrix API)**:
    - The edge hookpoints of the identified Arenas are treated as potential entry points ("portals") to the off-road network.
    - We use the GraphHopper Matrix API to find the travel time from the user's starting location to each of these portals.
    - Because asphalt is "fast" and off-road is "slow" (due to GH profiles), the Matrix API naturally finds the most efficient portal for the user to enter the off-road arena. **We never perform complex asphalt routing searches in our own database.**

- **Phase 3 (The Loop - GH Round Trip API)**:
    - The best portal identified in Phase 2 becomes the starting point for a call to the GraphHopper Round Trip API (`algorithm=round_trip`).
    - The API generates a loop of the desired length (`RoundTripDistance`) using the nearby hookpoints, and this forms the final `TripPlan`.

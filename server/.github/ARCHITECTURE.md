# Architecture

This document outlines the software architecture of the Offroad Navigation backend. The system is built using .NET and C# following Clean Architecture principles with a strict implementation of CQRS.

## 1. Core Principles

- **Clean Architecture**: The solution is divided into layers (Core, Application, Infrastructure, Api). Dependencies flow inwards, with the Application layer orchestrating the core business logic and the Infrastructure layer handling external concerns like databases and third-party services.
- **CQRS (Command Query Responsibility Segregation)**: We strictly separate read operations (Queries) from write operations (Commands). This simplifies the models, improves performance, and enhances security. Command handlers are located in `Application/{Context}/Commands` and query handlers in `Application/{Context}/Queries`.
- **Dependency Inversion**: The Application layer depends on abstractions (`IRoutingProvider`, `ITripRepository`), not concrete implementations. Infrastructure layers implement these interfaces. This keeps our core logic independent of external services like GraphHopper or PostgreSQL.

## 2. Architectural Layers

```
/-------------------\
|   Api (ASP.NET)   |  <-- Presentation Layer (Controllers, Program.cs)
\-------------------/
         |
         v
/-------------------\
|  Application      |  <-- Business Logic (Commands, Queries, Pipelines)
\-------------------/
         |
         v
/-------------------\
|   Domain          |  <-- Core Entities & Business Rules (Trip, TripPlan)
\-------------------/
         |
         v
/-------------------\
|  Infrastructure   |  <-- External Services (GraphHopper, PostgreSQL)
\-------------------/
```

- **Domain**: Contains the core enterprise logic. These are the business objects (Entities, Value Objects) and rules that are central to the application (e.g., `Trip`, `TripPlan`). This layer has zero dependencies on other layers.
- **Application**: Orchestrates the data flow and use cases. It contains the CQRS Commands, Queries, and the `PlanningPipeline`. It defines the interfaces that the Infrastructure layer must implement. **This layer must not contain any infrastructure-specific code or dependencies.**
- **Infrastructure**: Provides concrete implementations for the interfaces defined in the Application layer. This is where all external service details (GraphHopper API calls, database access) reside. It is responsible for mapping external exceptions (e.g., `HttpRequestException`) to application-level exceptions (e.g., `RoutingProviderException`).
- **Api**: The entry point to the application. This layer contains the ASP.NET Core controllers, middleware, and service configuration (`Program.cs`). It is responsible for translating HTTP requests into Commands and Queries and mapping the results back to HTTP responses.

## 3. The Planning Pipeline

The core of our routing logic resides in the `PlanningPipeline<TIntent, TCandidate>`. Every routing request, whether for a standard A->B route or a circular loop, is processed through a pipeline instance.

- **Intent**: An `ITripIntent` object (`RouteIntent`, `LoopIntent`) is created from the incoming request. It represents the user's goal in a structured way.
- **Generator (`ICandidateGenerator`)**: Generates one or more potential `TripCandidate` objects based on the intent. This is typically where the primary interaction with the `IRoutingProvider` (GraphHopper) happens.
- **Goal (`ITripGoal`)**: Filters the generated candidates to ensure they meet the fundamental requirements of the intent (e.g., the route actually reaches the destination).
- **Scorer (`ITripCandidateScorer`)**: Evaluates and scores the valid candidates based on how well they match the user's preferences (e.g., maximizing off-road travel).
- **Mapper (`ITripMapper`)**: Converts the final, highest-scoring candidate into a `TripPlan` that can be saved and returned to the user.

This pipeline ensures a consistent, extensible, and testable process for all routing calculations.

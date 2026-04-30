# C# Coding Standards & Rules

This document defines the strict coding standards and patterns to be used across the entire C#/.NET backend. Consistency and adherence to these rules are mandatory to maintain code quality, readability, and architectural integrity.

## 1. Clean Architecture & Dependencies

- **Strict Adherence**: Infrastructure details **MUST NOT** leak into the Application or Domain layers.
- **Example (Prohibited)**: The Application layer should never catch a `GraphHopperException` or `HttpRequestException`.
- **Example (Correct)**: The `GraphHopperService` (Infrastructure) must catch all provider-specific exceptions and re-throw them as an application-specific `RoutingProviderException`. The `RoutesCommands` (Application) class will then catch this generic exception.

```csharp
// In GraphHopperService (Infrastructure) - CORRECT
catch (HttpRequestException ex)
{
    throw new RoutingProviderException(RoutingProviderErrorCategory.Unavailable, "GraphHopper is unreachable.", ex);
}

// In RoutesCommands (Application) - CORRECT
catch (RoutingProviderException ex)
{
    _logger.LogWarning(ex, "Routing provider failed during route planning");
    return PlanningErrorMappings.MapRoutingProviderError(ex);
}
```

## 2. CQRS Pattern

- **Strict Separation**: Every action must be clearly defined as either a Command (writes/mutates state) or a Query (reads state). Do not mix read and write operations in the same class or method.
- **Implementation**: We use custom command/query interfaces (e.g., `IRoutesCommands`). Handlers receive a request object (e.g., `PlanRouteRequest`) and return a `Result<T>`.

## 3. Result Pattern (`Result<T>`)

- **Purpose**: To handle success and failure scenarios without resorting to exceptions for predictable errors (e.g., validation, not found).
- **Pragmatic Use**: Use the `Result<T>` pattern reasonably. It is a tool for clear, predictable error flow, not for turning C# into a functional language.
- **Prohibited**: Avoid excessive, deeply nested `Bind` or `Match` calls that mimic F# or Haskell. If your logic becomes hard to read, prefer a more traditional `if (result.IsFailure) return result.Error;` style check.
- **Correct Usage**:
    - Method signatures should return `Task<Result<T>>`.
    - Use implicit conversion from `Error` for clean return statements.
    - Use `Match` for branching at the end of a flow (e.g., in the API layer).

```csharp
// In a Command Handler - CORRECT
public async Task<Result<TripResult>> PlanAsync(PlanRouteRequest request, CancellationToken ct)
{
    var validationResult = await _planValidator.ValidateAsync(request, ct);
    if (!validationResult.IsValid)
    {
        return Error.Validation("Invalid request"); // Implicit conversion
    }

    var result = await _planningPipeline.PlanAsync(...);
    if (result.IsFailure)
    {
        return result; // Propagate failure
    }

    return result.Value.ToTripResult();
}
```

## 4. Design Patterns

- **Factory Pattern**: Heavily used for creating complex objects. For example, `Trip.Create(...)` encapsulates the logic for creating a new trip entity.
- **Builder Pattern**: Used for constructing complex API requests, such as `GraphHopperProfileBuilder`, which dynamically creates the `CustomModel` for GraphHopper requests.
- **Strategy Pattern**: The `PlanningPipeline` itself is a form of the Strategy pattern, where different strategies for generation (`ICandidateGenerator`) and scoring (`ITripCandidateScorer`) can be injected for different use cases (Routes vs. Loops).

## 5. Resilience

- **Retry Logic**: All external HTTP requests, especially to GraphHopper, **must** be resilient.
- **Implementation**: Retry logic (e.g., using Polly) for `HttpClient` **MUST** be configured in the `Infrastructure` module's `DependencyInjection.cs` file via `IHttpClientFactory`. The main `Program.cs` should remain clean of such infrastructure-specific configuration.

## 6. Bitwise Operations

- **Grade Masks**: When decoding the `grades_mask` integer from the database, bitwise operators **MUST** be used. This is a performance-critical and standardized operation.

```csharp
// CORRECT way to check for Grade 5
bool isGrade5 = (grades_mask & 16) != 0; // 2^(5-1) = 16
```

## 7. Code Documentation

Code should be as self-documenting as possible, but where necessary, comments should be clear, concise, and meaningful.

### XML Documentation Comments (`///`)
- **Public Members**: All `public` classes and methods **MUST** have an XML documentation summary.
- **Clarity Over Complexity**: The `<summary>` should clearly and concisely describe the member's purpose and behavior.

```csharp
/// <summary>
/// Generates a collection of potential route candidates based on the user's intent.
/// This is the primary entry point for interacting with the underlying routing provider.
/// </summary>
/// <param name="intent">The user's routing intent.</param>
/// <param name="ct">The cancellation token.</param>
/// <returns>A result containing a list of provider routes or an error.</returns>
public async Task<Result<List<ProviderRoute>>> GetRoutesAsync(RouteIntent intent, CancellationToken ct)
{
    // ...
}
```

### Inline Comments (`//`)
- **Purpose**: Inline comments should only be used to clarify code that is not immediately obvious to a reader. This includes complex algorithms, regular expressions, or business-critical workarounds.
- **Self-Documenting Code**: Methods should be named so specifically that they do not require an inline comment to explain what they do at the call site.

```csharp
// CORRECT: Explains a complex operation
// The grades_mask is a bitfield. We check for Grade 5 by seeing if the 5th bit (value 16) is set.
bool isHardcore = (grades_mask & 16) != 0;

// CORRECT: Explains a non-obvious regular expression
// Extracts the numeric ID from a composite key like "user-123".
var match = Regex.Match(input, @"\w+-(\d+)");


// INCORRECT: Comment is redundant and adds noise
// Get the user by their ID
var user = await _userRepository.GetByIdAsync(id, ct);
```

## 8. Dependency Injection

- **Modular Registration**: Service registration is modular. Each project (`Offroad.Application`, `Offroad.Infrastructure`, etc.) is responsible for its own service registrations.
- **`DependencyInjection.cs`**: Each project contains a `DependencyInjection.cs` file with an extension method (e.g., `AddApplicationServices`, `AddInfrastructureServices`).
- **Strict Rule**: When adding a new service, it **MUST** be registered in the `DependencyInjection.cs` file of the project where the service is implemented.
- **Prohibited**: **DO NOT** add service registrations directly to the API project's `Program.cs` file. The `Program.cs` file should only contain calls to the extension methods from each project's `DependencyInjection.cs` file. This keeps the composition root clean and respects the modular architecture.

## 9. C# Style & Conventions

- **Method Signatures**:
  - Keep method signatures on a single line where possible.
  - **DO NOT** break parameters onto new lines unless the signature is excessively long (e.g., 5+ parameters or a line length exceeding 120 characters), as seen in some domain entity constructors. Readability is the goal.

- **Use of `record`**:
  - Prefer `record` over `class` for Data Transfer Objects (DTOs), Value Objects, and CQRS request/response models. Records provide immutability and value-based equality by default, which is ideal for these types.
  - Use positional records for concise, immutable objects.

- **Use of `sealed`**:
  - All classes that are not explicitly designed for inheritance **MUST** be marked as `sealed`.
  - This applies to command handlers, services, repositories, etc. It improves performance by allowing the JIT compiler to make devirtualization optimizations and clearly communicates the design intent that the class is not a base for extension.

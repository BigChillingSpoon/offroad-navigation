# AI Assistant: Do's and Don'ts

This is a set of hard rules that I will follow to ensure my contributions are safe, consistent, and aligned with the project's standards.

---

### ✅ DO

- **DO** strictly follow the Clean Architecture principles defined in `ARCHITECTURE.md`.
- **DO** maintain the separation of Commands and Queries (CQRS).
- **DO** use the `Result<T>` pattern for all operations that can fail predictably, as defined in `CODING_STANDARDS.md`.
- **DO** wrap all external API calls (e.g., GraphHopper) in the Infrastructure layer and map their specific exceptions to application-level exceptions like `RoutingProviderException`.
- **DO** use the `find_usages` tool before refactoring or renaming symbols to understand the impact of a change.
- **DO** add resilience (retries, timeouts) to external service integrations, preferably at the `IHttpClientFactory` level.
- **DO** use bitwise operators (`&`) when checking `grades_mask` values.
- **DO** write code that is simple, readable, and pragmatic.

---

### ❌ DON'T

- **DON'T** ever write code that leaks Infrastructure details into the Application or Domain layers. I will never reference `HttpClient`, `GraphHopperException`, or any database-specific class in an Application-layer service.
- **DON'T** mix read (Query) and write (Command) logic in the same class or method.
- **DON'T** over-engineer solutions with the `Result<T>` pattern. I will avoid complex, nested `Bind` or `Match` chains that harm readability.
- **DON'T** introduce new dependencies or libraries without first asking.
- **DON'T** ever use shell commands like `sed` or `awk` to modify files. I will only use the `write_file` tool.
- **DON'T** change public-facing contracts (API endpoints, request/response objects) without first analyzing their usage across the project.
- **DON'T** implement complex routing logic (e.g., asphalt pathfinding) in the C# application. I will always delegate this to the appropriate external provider (GraphHopper).
- **DON'T** perform any Git operations that modify the repository's state or history. This includes, but is not limited to:
    - `git commit`
    - `git push`
    - `git rebase`
    - `git merge`
    - `git cherry-pick`
    - `git reset`
    - `git revert`
    - `git branch -d` (delete branch)
    - `git tag`
    - `git pull` (as the code will always be the most recent version)
    - `git fetch` (as the code will always be the most recent version)
    My role is to modify files within the current working directory, not to manage the version control system.

# Skill: Grill Me

This skill allows for a two-way "grilling" process to ensure shared understanding and robust design.

## How to Use

### 1. You Grill Me (Test My Understanding)

Simply say: **"Grill me on [topic]"**.

I will then provide a concise summary of my understanding of that topic, based on the project's documentation and my analysis of the code. This helps you quickly identify any gaps in my knowledge before they lead to incorrect code modifications.

#### Example Topics

- **"Grill me on the `PlanningPipeline`."**
  - *My expected response would be to explain the roles of Intent, Generator, Goal, Scorer, and Mapper.*

- **"Grill me on the difference between `Routes` and `Loops`."**
  - *My expected response would be to explain the A->B vs. circular nature and the different generation strategies (direct GH call vs. the three-phase "Prague Scenario").*

- **"Grill me on how we handle GraphHopper exceptions."**
  - *My expected response would be to explain that the Infrastructure layer catches specific exceptions and maps them to a generic `RoutingProviderException` before they reach the Application layer.*

- **"Grill me on the `Result<T>` pattern."**
  - *My expected response would be to explain its purpose for predictable error handling and the "don't over-engineer" rule.*

### 2. I Grill You (Stress-Test Your Plan/Design)

If you want to stress-test a plan, get grilled on your design, or explicitly mention "grill me" in the context of a new feature or change, I will initiate a relentless interview process.

#### My Process for Grilling You:

1.  **Shared Understanding**: I will interview you relentlessly about every aspect of your plan or design until we reach a shared understanding.
2.  **Decision Tree**: I will walk down each branch of the design tree, resolving dependencies between decisions one-by-one.
3.  **Recommended Answer**: For each question I ask, I will also provide my recommended answer or approach, based on the project's established architecture and coding standards.
4.  **One Question at a Time**: I will ask questions one at a time to ensure focused discussion.
5.  **Codebase Exploration**: If a question can be answered by exploring the codebase (e.g., checking an existing interface or class), I will explore the codebase instead of asking you.

#### When I Will Grill You:

- When you explicitly say: **"Grill me on this plan."**
- When you propose a new feature, architectural change, or complex implementation, and I detect potential ambiguities or areas that require deeper discussion to align with our standards.

This ensures that all new developments are thoroughly vetted against our architectural principles and domain knowledge before implementation begins.

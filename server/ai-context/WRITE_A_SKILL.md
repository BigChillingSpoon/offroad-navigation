# Skill: Write a Skill

This skill allows you to teach me new, project-specific procedures. If you find yourself repeatedly explaining a process, you can codify it as a "skill" that I can reference in the future.

## How to Use

Say: **"Let's write a skill for [process name]."**

Then, describe the process to me in clear, step-by-step instructions. I will create a new markdown file in the `.github/skills/` directory that documents this process.

### Example: Creating a New Command

**You:** "Let's write a skill for creating a new CQRS command."

**Me:** "Understood. Please describe the process."

**You:**
1.  "Create a new request record in the `Application/Contracts` project (e.g., `UpdateRouteNameRequest.cs`)."
2.  "Add a corresponding method signature to the command interface (e.g., `IRoutesCommands.cs`)."
3.  "Implement the method in the command handler class (e.g., `RoutesCommands.cs`)."
4.  "The implementation must fetch the entity, call the domain method, and save the result."
5.  "Add a new endpoint in the API controller to expose the command."
6.  "Create a new unit test file in `Offroad.Tests` to validate the command handler's logic."

I will then create a file named `.github/skills/CREATE_CQRS_COMMAND.md` with these steps. The next time you ask me to create a command, I will follow this documented procedure precisely.

This process helps ensure consistency and reduces the need for repeated explanations.

# Inventor Add-In Manager

A development tool that runs a command from a build output in a live Autodesk Inventor session, 2025 through 2027.
See `README.md` for how it works and how to use it.

These are the instructions for a coding agent that works on this repository (ex. Claude Code, GitHub Copilot).

## Layout

| Path | What it is |
|---|---|
| `Source/InventorAddinManager` | The add-in. It stays loaded until Inventor closes. |
| `Source/InventorAddinManager.Loader` | Inventor 2025 and 2026 only. Loads the add-in from `App\` into its own load context. |
| `Samples/SampleCommands` | Commands that show the convention. A user reads them. |
| `Tests/InventorAddinManager.Tests` | Unit tests. They need no Inventor. |
| `Tests/InventorAddinManager.Tests.Commands` | The commands that the tests load through the manager. |
| `Tests/InventorAddinManager.InventorTests` | Smoke tests against a running Inventor. They skip when none runs. |
| `Libs/Inventor/<year>` | The vendored interop for each release. |

## Build and Test

The configuration names the build type and the Inventor release: `Debug.2025` through `Release.2027`.
The name holds the year, not the Autodesk release number (Inventor 2027 is R31).
Do not give `-p:AutodeskVersion`. The build stops if it differs from the configuration.

```powershell
dotnet build InventorAddinManager.slnx -c Debug.2027
dotnet test --project Tests/InventorAddinManager.Tests -c Debug.2027
```

Before you report a change to the build files as done, build and test all six configurations.
A change can work on net10.0 (2027) and fail on net8.0 (2025, 2026).

The smoke tests need the manager deployed and Inventor of the same release running:

```powershell
dotnet build Source/InventorAddinManager -c Debug.2026 -p:DeployAddIn=true   # Inventor closed
# Start Inventor 2026, then:
dotnet test --project Tests/InventorAddinManager.InventorTests -c Debug.2026
```

Inventor locks the manager's build output while it runs. Close Inventor before you build `Source/InventorAddinManager`.
The command projects (`Samples`, `Tests/InventorAddinManager.Tests.Commands`) build while Inventor runs.

## Rules

- **Keep the add-in small.** Each change to it needs an Inventor restart. The commands are the code that changes often.
- **Only strings leave a run.** `CommandRunner.Execute` and `CommandCatalog.Discover` change each value from a command
	into a string before they return. An object or an exception of a type from the build keeps the build loaded.
	Keep `[MethodImpl(MethodImplOptions.NoInlining)]` on `CommandRunner.Execute` for the same reason.
- **Match types by full name across the boundary.** `CommandBinder` compares `Inventor.Application` by name, not by
	`Type` identity, because a build with embedded interop types has its own copy of each interface.
- **Keep the loader to the BCL and the interop.** Each assembly it references loads into the default context.
- **Compile against the oldest point release.** Each file in `Libs/Inventor/<year>` is the oldest point release of
	that year that we have. A build against a newer point release does not bind to the older interop that an Inventor
	with no update loads. Replace a file only with an older or equal version.
- **Write a test for each claim in the README.** A claim about loading (ex. "a command gets its own Newtonsoft.Json")
	gets a unit test in `CommandRunnerTests`, and a smoke test if it depends on Inventor.

## Inventor Traps

- `Deactivate` must run the garbage collector and wait for the finalizers after it releases the button definitions.
	If the event provider of an old definition is finalized after the next `Activate`, it disconnects the `OnExecute`
	handler of the new button with the same internal name, with no error.
- To start a button from code, use `ControlDefinition.Execute2(true)`. `Execute()` only queues the command, and a
	queued command that has not started blocks each later start of the same command.
- Inventor 2025 and 2026 ignore `UseInventorAssemblyContext` in the manifest. That is why the loader exists.
- `ApplicationAddIn.Automation` gives the manager's `ManagerAutomation` through `IDispatch`.
	A caller in another process uses late binding (`dynamic`). A run through it shows no window, because a modal
	window stops the caller until a user closes it.

## Writing Style

Write prose, comments, and documentation in ASD-STE100 Simplified Technical English: short sentences, one
instruction for each sentence, active voice. Write `ex.` to give an example. Use a hyphen, never an em dash.
XML doc comments put each tag on its own line, with the content indented one tab.

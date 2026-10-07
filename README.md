# autodesk-inventor-addin-manager

A development tool for Autodesk Inventor 2025, 2026, and 2027.
It runs a command from your build output in a live Inventor session.
You edit, build, and run again with no Inventor restart.

It is the Inventor version of the Revit and AutoCAD add-in managers
([RevitAddInManager](https://github.com/chuongmep/RevitAddInManager), [CadAddinManager](https://github.com/chuongmep/CadAddinManager)).
It is a development tool only. Deploy your add-in the usual way for release.

## How it works

```
Ribbon: Tools > Add-In Manager
  ├── Add-In Manager   pick a build output and a command
  └── Run Last         run the last command again from its newest build

Each run:
  1. Copy the folder of the build output to %LOCALAPPDATA%\InventorAddinManager\Shadow\<name>\<time>\
  2. Load the copy into a new collectible AssemblyLoadContext
  3. Create the command and call Execute (optionally in one undo step)
  4. Unload the context, then check that the garbage collector removed it
```

- **Inventor never locks your build output.** It loads only the copy, so `dotnet build` works while Inventor runs.
- **Each run is a fresh load.** Static fields start empty, and the old build leaves memory when nothing holds a reference to it.
- **One interop.** The load context gives each command the interop assembly that Inventor uses.
  `Inventor.Application` is then one type for the manager and the command.
- **Dependencies resolve from the copy first**, through the `.deps.json` file of your build.
  A different version that Inventor or another add-in loaded does not replace yours.
- **`Assembly.Location` works.** It gives the path in the shadow copy, so files beside your assembly are found.

The manager itself runs in its own load context on every release, so its packages do not conflict with Inventor's:

| Release | How the manager is isolated |
|---|---|
| 2027 | Inventor honors `UseInventorAssemblyContext` `0` in the manifest. |
| 2025, 2026 | Inventor ignores that element. The manifest names `InventorAddinManager.Loader`, the only assembly in the default context. It loads the manager from the `App\` subfolder into a load context named `InventorAddinManager`. |

The manager uses the `stdole` package for the ribbon icons.
In version 17 of that package, `stdole.dll` forwards its types to `Microsoft.VisualStudio.Interop.dll`, which is also in the output.
A test in Inventor 2025 showed that `stdole.IPictureDisp` loads from the manager's `App\` folder,
while Inventor's own `stdole.dll` stays in the default context.

## Requirements

- Inventor 2025, 2026, or 2027.
  Inventor 2024 and earlier run on .NET Framework, which has no collectible `AssemblyLoadContext`.
- .NET SDK 10 (it also builds the .NET 8 targets for 2025 and 2026).

| Release | `AutodeskVersion` | Target framework | Manifest version |
|---|---|---|---|
| Inventor 2025 | `2025` | `net8.0-windows` | 29 |
| Inventor 2026 | `2026` | `net8.0-windows` | 30 |
| Inventor 2027 | `2027` (default) | `net10.0-windows` | 31 |

## Install the manager

Close Inventor, then build with deployment on, one time for each release:

```powershell
dotnet build Source\InventorAddinManager -p:AutodeskVersion=2027 -p:DeployAddIn=true
```

This writes `%APPDATA%\Autodesk\Inventor 2027\Addins\InventorAddinManager.addin`.
The manifest points at the build output in this repository.
For 2025 and 2026, the build also builds the loader, and the manifest points at the loader:

```
Source\InventorAddinManager\bin\Debug\2025\
  ├── InventorAddinManager.Loader.dll   the manifest's assembly, in the default context
  └── App\                              the manager's full build output, in its own context
```

The `Activate` line in the manager's log names the load context and the file that `stdole` loaded from.
Inventor locks that output while it runs, so close Inventor before you build the manager again.

The add-in is not signed.
On the first start after an install, Inventor shows a prompt that the add-in is blocked. Approve it.
Until you approve it, the prompt stops Inventor from loading the other add-ins.

To remove the manager, delete the `.addin` file, or run the same command with `-t:Clean`.

## Write a command

Your project is an ordinary class library. It needs no reference to the manager and no `.addin` manifest.

A command is a public class with a public method named `Execute`:

```csharp
public sealed class ActiveDocumentReport
{
	public string Execute(Inventor.Application inventorApplication)
	{
		return inventorApplication.ActiveDocument?.DisplayName ?? "No document is open.";
	}
}
```

| Rule | Detail |
|---|---|
| Method | Public, named `Execute`, static or instance. |
| Parameters | Each one is an `Inventor.Application`, or has a default value. |
| Constructor | For an instance method: a public constructor whose parameters follow the same rule. A primary constructor works. |
| Return value | Optional. The manager shows the text of a value that is not `null`. A command that returns nothing and succeeds shows nothing. |
| Exception | The manager shows it with its stack trace, in a window that lets you copy it. |

Set these in the project file of the command:

```xml
<PropertyGroup>
	<!-- Copies package assemblies to the output and lists them in .deps.json. -->
	<EnableDynamicLoading>true</EnableDynamicLoading>
</PropertyGroup>
<ItemGroup>
	<Reference Include="Autodesk.Inventor.Interop">
		<HintPath>$(ProgramFiles)\Autodesk\Inventor 2027\Bin\Public Assemblies\Autodesk.Inventor.Interop.dll</HintPath>
		<Private>false</Private>
	</Reference>
</ItemGroup>
```

`Samples\SampleCommands` has three examples:

| Command | Shows |
|---|---|
| `ActiveDocumentReport` | Returns text, and the shadow folder and load context it ran from. |
| `RunCounter` | Changes a user parameter as one undo step. A constructor that takes the Inventor session. |
| `PackageVersionReport` | Uses its own `Newtonsoft.Json` 13.0.4, also when Inventor has 13.0.3 loaded. |

## The development loop

1. Start Inventor. Click **Tools > Add-In Manager**.
2. Click **Add...** and select your assembly in the build output, ex. `bin\Debug\2027\MyAddin.dll`.
3. Select a command and click **Run**. The window closes, then the command runs.
4. Change the code and build. Inventor stays open.
5. Click **Run Last**. The new build runs.

Click **Refresh** in the manager after a build that adds or removes a command.

**Run as one undo step** puts all changes to the active document in one transaction, so one Undo removes them.
Turn it off for a command that saves, closes, or opens documents.

To debug, attach Visual Studio to `Inventor.exe`.
The symbols are in the assembly (`DebugType` `embedded`), so breakpoints bind in each new copy.

The manager writes one line for each activation, click, and run to `%LOCALAPPDATA%\InventorAddinManager\Logs\`.
It keeps the files for 7 days.

To start a button from code, ex. an iLogic rule or a test, use `ControlDefinition.Execute2(true)`.
`Execute()` only queues the command. A queued command that has not started blocks every later start of the same command.

## Differences from a deployed add-in

Test the real deployment before a release. A run through the manager is different in these ways:

- **Ribbon and startup code do not run.** The manager calls `Execute` only.
  `ApplicationAddInServer.Activate`, your buttons, and your event subscriptions are not part of a run.
- **`Assembly.Location` is the shadow copy**, not your install folder.
- **Static state resets on each run.**
- **A reference that stays after the run keeps the build in memory.**
  Examples: an Inventor event handler that is still subscribed, a modeless window that is still open, or a static field in another assembly.
  The manager shows a warning when a build does not unload. Each such run adds one more copy to memory until Inventor closes.
  A library can cause this too: a command that serializes with `Newtonsoft.Json` does not unload,
  because Json.NET keeps references in caches outside the command's load context.
- **The manager window is modal and closes before the command runs**, so the command can use Inventor selection and interaction.

## Next step: reload a complete add-in

This version runs commands only.
The next step is to reload a complete `ApplicationAddInServer` with its ribbon:
call `Deactivate` on the old copy, which must delete its button definitions and panels, then call `Activate` on a new copy.
The Inventor API can delete its own ribbon controls, so this is possible without the `AdWindows` workarounds that Revit needs.

`AddInServer.Deactivate` shows one requirement for that step.
After it releases the button definitions, it must run the garbage collector and wait for the finalizers.
If the event providers of the old definitions are finalized after the next `Activate`,
they disconnect the `OnExecute` handler of a new button with the same internal name, with no error.
A test in Inventor 2027 showed this: the **Add-In Manager** button stopped responding after an unload and load.

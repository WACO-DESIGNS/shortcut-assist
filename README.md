# Shortcut Assist v1.5

Shortcut Assist is a Windows shortcut wheel for Fusion 360, SolidWorks, Photoshop, Illustrator, and other programs you add. Choose your tools once, then open the wheel at your cursor while the matching program is in focus.

![Shortcut Assist showing the Fusion 360 wheel editor](docs/shortcut-assist.png)

## Get started

1. Download and extract the Windows x64 ZIP from the release.
2. Open **Shortcut Assist.exe** from the extracted folder. The mascot winks on a gray screen, then fades into the editor. Use a folder you can write to so the app can save your settings beside the executable.
3. Pick a program tab, then drag tools from the library to the wheel. You can also select a wedge and click a tool to assign it.
4. Focus that program and press **Z**, the default activation key. Click a wedge to send its shortcut.

Change the shared activation key in **Settings** if it overlaps with a shortcut in the focused app. The activation key also reaches that app. You can record a key combination or a standard mouse button.

Click **?** beside Settings for a guided tour that starts with the activation key and includes a Mini HUD preview. Use **Next** to advance or **Skip tutorial** to return to the editor.

Use the **−** button beside **Add custom shortcut** to enter delete mode. Click the **−** on a tool to remove it from the library and its wheel slots.

In **Add custom shortcut**, type a chord such as `Ctrl+2` or a sequence such as `Up, Down, Left, Right`. Use **Record**, press the keys in order, then select **Stop** to review the sequence before adding it.

The shortcut wheel opens instantly by default. **Settings → Check for updates** opens the GitHub Releases page in your browser.

## Customize

- **My Wheel** keeps a separate wheel for each program. Use **−** and **+** at the top right to change that program’s slot count. Drag wedges to reorder them and use **Clear slot** to leave an empty wedge. Choose **Add custom shortcut** to enter your own binding.
- **… → Manage programs** lets you add a Windows program by selecting its executable. Removing a program deletes its Shortcut Assist profile after confirmation.
- **Settings** contains Light and Dark modes and the Mini HUD toggle. **Advanced** contains Mini HUD placement and the shared live wheel diameter.
- **Exit app** closes the wheel, tray icon, and Mini HUD. The window’s **X** hides the editor in the tray.

## Local settings

Shortcut Assist runs offline. It checks which configured program has focus and polls the shared activation binding while that program is active. Profiles, shortcut assignments, and preferences live in the local `data` folder beside the executable. Program registration and icon lookup use local Windows drives.

To update a portable copy, exit Shortcut Assist and replace the executable in the same folder. Keep the `data` folder to carry your settings forward.

## Build from source

Use Windows x64 with .NET Framework 4.8 or newer. Supported Windows 11 versions include .NET Framework. [Microsoft’s .NET Framework guide](https://learn.microsoft.com/en-us/dotnet/framework/install/on-windows-and-server)

Run this command from the repository root in PowerShell.

```powershell
.\build.ps1
```

The script compiles the WinForms app with the installed .NET Framework compiler and puts `Shortcut Assist.exe` in the repository root.


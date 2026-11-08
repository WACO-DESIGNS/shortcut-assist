# Shortcut Assist v1.6

Shortcut Assist gives you a shortcut wheel for Fusion 360, SolidWorks, Photoshop, Illustrator, and other Windows programs you add. Set up a wheel for each program, then open it at your cursor while you work.

![Shortcut Assist showing the Fusion 360 wheel editor](https://github.com/WACO-DESIGNS/shortcut-assist/blob/b1396ad548b15dfdcbc2f4984175c5e1ea1ee674/docs/shortcut-assist.png?raw=true)

**[Download Shortcut Assist for Windows](./Shortcut-Assist.exe?raw=true)**

Open `Shortcut-Assist.exe` after it downloads. It runs directly on 64-bit Windows.

## What's new

- **Scan a program for shortcuts.** Open it, then choose **… → Manage programs → Scan shortcuts**. Shortcut Assist also scans when you add a program. Review the results and click **Add selected** to import them into that program’s tool library.
- **Start with a suggested wheel.** When the scan recognizes common tools, it previews a few that can fill empty slots. The option is on by default for an empty wheel. Uncheck it to import only to the library. Existing tools stay in their slots.
- **Add missing shortcuts manually.** Scanning is experimental. Some programs return only a partial list or no results. Install files may contain defaults that differ from your custom bindings. Use **Add custom shortcut** for anything the scan misses.

The scan runs locally using supported shortcut files and Windows menu data. It doesn’t capture the screen.

## Get started

1. Select a program and drag tools from the library onto the wheel. You can also select a wedge and then click a tool to assign it.
2. Switch to that program and press **Z**, the default activation key. Click a wedge to run its shortcut.
3. Open **Settings** to change the activation key or turn on the Mini HUD. Click **?** for a guided tour.

Each program has its own wheel and slot count. You can reorder wedges, add custom shortcuts and key sequences, or add more programs through **… → Manage programs**.

## Where settings are saved

Shortcut Assist saves your wheels, custom shortcuts, activation key, and preferences locally in a `data` folder beside the executable. Keep the app in a folder you can write to, such as Downloads or Documents. When moving the app, move the `data` folder with it. When updating, replace the executable in the same folder and keep `data`.

# VS DOF Head Tracking (Vintage Story mod)

6DOF head tracking for Vintage Story using OpenTrack's FreeTrack shared memory output.

## Features
- Rotation (yaw, pitch, roll) and translation offsets.
- Per-axis gains and clamps for translation.
- Crouch mapped to head movement with hold or toggle mode.
- In-game settings dialog and persistent config file.
- Tracking suspended when the game is paused or the escape menu is open.

## Requirements
- Vintage Story client (net8 or net10 build).
- OpenTrack (or another tracker that can output FreeTrack shared memory).
- A head tracking source (TrackIR, webcam tracker, phone tracker, etc.).

## Install
1. Download the mod zip (or build it from source).
2. Place the zip in your Vintage Story `Mods` folder.
3. Launch Vintage Story.

## Usage
1. Start OpenTrack and set Output to `FreeTrack 2.0 Enhanced`.
2. Launch the game in first-person view.
3. Press `Keypad *` to open the "Head Tracking Settings" dialog.
4. Adjust gains, toggles, and crouch options as desired.

## Configuration
Settings are stored in `vsdof.json` in the Vintage Story mod config directory.
You can edit values directly or use the in-game dialog:
- Enable tracking, rotation, translation, and roll.
- Yaw/pitch/roll gains.
- Translation gains and max translation per axis.
- Crouch threshold, hysteresis, axis (X/Y/Z), and mode (hold/toggle).

## Build (from source)
- Open project in VS Code
- Open new terminal
- Set your environment (net8 or net10): $env:VINTAGE_STORY_NET10 = "C:\Vintagestory" or $env:VINTAGE_STORY_NET8 = "K:\Vintagestory"
- Build with dotnet run --project .\CakeBuild\CakeBuild.csproj -- --framework net10.0 (or net8.0)

## Notes
- Tracking applies only in first-person view.
- If nothing moves, confirm OpenTrack is running and FreeTrack output is enabled.

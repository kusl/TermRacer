# Architecture

Three projects, one dependency direction: `Tui → Rendering → Core`. Core and Rendering have no package references and are fully covered by the test projects.

## Core

- **Track.** `TrackDefinition` describes a closed loop as straights and constant-radius arcs. `Track.Build` walks the sections, checks closure, and resamples the centreline every 2 m, storing tangents, right-hand normals and signed curvature (positive turns right; world y points down). Asphalt is 14 m wide, and walls sit 16 m from the centreline. The tests check that the tightest radius is wider than the runoff and that separate parts of the loop never share runoff.
- **Physics.** `CarPhysics.Step` is a fixed-step (120 Hz) arcade model. It has a power-limited engine, aero drag and rolling resistance, brakes that turn into reverse at standstill, and steering that asks for a share of the largest yaw rate the tyres allow at the current speed. Lateral acceleration therefore never exceeds grip. Grass lowers grip and power and adds drag. `Walls` clamps the car to the corridor and reflects outward velocity.
- **Race rules.** `LapTracker` uses 12 gates across the corridor. Gate 0 is the start/finish line. The clock starts on the first forward crossing of gate 0. A lap counts only when gates 1 to 11 are crossed forward in order and the line is crossed again. Crossing times are interpolated inside the simulation step. Single-lap mode finishes on the first completed lap; Zen mode keeps counting.
- **Autopilot.** `RacingLine` relaxes lateral offsets towards minimum curvature within the track (coarse pass, then full resolution). `SpeedProfile` takes corner speeds from curvature and grip, then runs a backward pass so every corner can be reached under braking. `Autopilot` combines pure-pursuit steering with a speed controller, eases off on grass or when pointing away from the line, and reverses out when stuck.
- **Input.** `DriverControls` turns key events into pedal and steering input. It starts in repeat-driven mode: throttle latches, while brake and steering hold for 0.55 s after a press and 0.16 s after each repeat. After the first key release event it switches to exact press and release tracking.
- **Flow.** `Game` owns the screens (menu, race, results), the menu's autopilot demo lap, pausing, debounce for Tab and Enter, and a `FixedStepClock` that turns wall time into simulation steps (clamped after stalls).

## Rendering

- **Rasterizing.** `PixelCanvas` fills polygons with a scanline pass that samples pixel centres. Shared edges are evaluated in a canonical order, so neighbouring quads tile without gaps. Each canvas row pair becomes one terminal cell: `▀` with the top pixel as foreground and the bottom as background, or a space when both match. That gives roughly square pixels at twice the vertical resolution.
- **Track geometry.** `TrackGeometry` precomputes world-space quads for runoff stripes, barriers, asphalt, curbs (where the radius is under 160 m) and the chequered line. `WorldPainter` culls them against the camera and draws the track, the car (body, nose, cabin, shadow) and the minimap.
- **Camera.** `ChaseCamera` leads the car along its velocity, eases towards that target in simulation time, and snaps to the pixel grid so static scenery does not shimmer.
- **Scene.** `SceneRenderer` builds the whole frame into a `CellBuffer`: menu with a live track preview, race with HUD bars and banners, results panel with pixel-font digits, or the too-small prompt. Rendering is deterministic for a given state.
- **Diffing.** `FrameDiff` compares the new buffer with the previous frame and returns runs of changed cells.

## Terminal shell

`GameView` is a Terminal.Gui 2.5 `Runnable`. The app is created with the ANSI driver and 16-colour forcing turned off, and the main loop is raised to 60 iterations per second.

- **Tick.** A 15 ms timeout feeds elapsed wall time to `Game`, pauses the game while the view is under 120 × 40, and requests a redraw.
- **Drawing.** `OnClearingViewport` is overridden so Terminal.Gui never clears the view. `OnDrawingContent` renders into the back buffer, diffs it against the front buffer, and writes only the changed runs with `Move`, `SetAttribute` and `AddRune`, then swaps the buffers. A full redraw happens only on the first frame, on a resize (`ScreenChanged`), or when the driver clears its contents (`ClearedContents`).
- **Input.** `KeyMap` maps arrows, WASD, Tab, Enter, Space and Esc to `GameKey`. Key-down events are sent to `Game.Press`, and key-up events (kitty protocol only) to `Game.Release`. Esc requests stop immediately.

## Tests

- **Core tests** cover vector maths, segment intersection, track building and its invariants, physics limits, walls, lap rules (interpolated start, backward crossings, skipped checkpoints, Zen counting), control latching, the fixed-step clock, the racing line and speed profile, autopilot laps (clean laps, single-lap finish, recovery from facing backwards on grass) and game flow.
- **Rendering tests** cover polygon coverage and tiling, half-block composition, frame diffs, cameras, time formatting and full-scene rendering on every screen.

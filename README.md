# TermRacer

Top-down racing in the terminal. One circuit, a timed lap or endless laps, and an autopilot you can hand the car to at any moment.

## Requirements

- .NET 10 SDK (any 10.0 feature band; see `global.json`)
- A 24-bit colour terminal of at least 120 × 40 cells. Smaller windows get a resize prompt and the game waits.

## Run and test

```sh
dotnet run --project src/TermRacer.Tui
dotnet test
```

`dotnet test` runs both xUnit v3 suites on Microsoft Testing Platform (enabled in `global.json`). Each test project is also a standalone executable, for example `dotnet run --project tests/TermRacer.Core.Tests`.

## Controls

| Key | Action |
| --- | --- |
| ↑ or W | Throttle |
| ↓ or S | Brake; reverse when stopped |
| ← → or A D | Steer |
| Tab | Switch between manual driving and autopilot |
| Enter or Space | Choose in menus |
| Esc | Quit immediately |

Terminals that report key releases through the kitty keyboard protocol (kitty, WezTerm, foot, Ghostty, Windows Terminal) get exact hold-to-drive controls. In other terminals the throttle stays on after one press until you brake, and steering lasts as long as key repeats keep arriving.

## Modes

- **Single lap**: one timed lap. The car starts just behind the line, the clock starts when you cross it, and the race ends when you cross it again with every checkpoint passed. The final time is shown until you press Enter or Esc.
- **Zen mode**: no lap limit. Current, last and best lap times are shown for reference. Leave with Esc.

## Layout

```
TermRacer.slnx
global.json                    SDK roll-forward and MTP test runner
Directory.Build.props          shared compiler settings
Directory.Packages.props       central package versions
src/TermRacer.Core             simulation: track, physics, lap rules, autopilot, game flow
src/TermRacer.Rendering        rasterizer, cell buffers, frame diff, scene and HUD
src/TermRacer.Tui              Terminal.Gui shell: input, timer, writing changed cells
tests/TermRacer.Core.Tests
tests/TermRacer.Rendering.Tests
docs/architecture.md
```

Only `TermRacer.Tui` references Terminal.Gui. See `docs/architecture.md` for how the pieces fit.

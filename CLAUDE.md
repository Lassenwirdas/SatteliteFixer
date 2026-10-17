# CLAUDE.md

Context for Claude Code when working in this repository.

## Project

SatteliteFixer ("P04 Collaborative Satellite Repair") is a Serious Games group project
(WiSe 2026/27, TU Darmstadt, supervisor Lisa Baule). It is a 2-player, asymmetric
co-op Unity game. One player in VR (OpenXR) repairs a satellite in orbit. The other
player at a desktop PC acts as mission control: they read information mirrored onto
a canvas (e.g. a checklist) and guide the repair by voice. The learning goals are how satellites and their sensors work,
and how to collaborate. The target group is children and teenagers. See `README.md`
and `image.png` (the original project slide).

## Status

- The Unity project exists in `SatelliteFixer/`, created from the VR Core template.
  It contains only template content so far (sample scenes, XRI/XR Hands samples).
  No game code yet.

## Decisions

- **Engine:** Unity 6000.6.6f1 with URP.
- **XR stack:** OpenXR + XR Interaction Toolkit 3.x (VR Core template). The Meta and
  Android XR OpenXR packages are installed.
- **MVP Mission Control:** a plain canvas on the PC that mirrors game information
  **asynchronously / on timed updates**, not in real time. Example: a checklist that
  mission control reads out to the VR player. Feedback comes **verbally from the VR
  player**. Mission control does not drive the game, and there is no in-game voice chat.
  This is what "asynchronous cooperative" on the slide means.
- **Later (only if time permits):** a dedicated, **synchronous** Mission Control hub
  with live game state, such as the astronaut's movement and position and live telemetry.
  Don't build real-time networking for the MVP. Keep the information flow simple
  enough that it can be swapped for a synchronous transport later.

## Open questions

Settle these with the team, and don't guess them in code:

- Target VR headset (standalone Quest/Android XR vs. PC VR via Link/SteamVR).
- How the MVP canvas gets its data: same machine as the VR app (e.g. second display/window)
  or a separate PC (e.g. shared file, simple HTTP endpoint), and the update interval.

## Conventions

- The Unity project lives in `SatelliteFixer/` (not the repo root). All Unity paths are
  relative to that folder, e.g. `SatelliteFixer/Assets/...`.
- Never commit `SatelliteFixer/Library/`, `Temp/`, `Logs/`, `obj/`, `Build*/` or
  `UserSettings/`, nor IDE files (`*.sln`, `*.slnx`, `*.csproj`). `.gitignore` covers them.
  Its Unity patterns are anchored to `/SatelliteFixer/`, so folders with the same name
  inside `Assets/` (e.g. `Assets/CompositionLayers/UserSettings`) are still tracked.
- Always commit `.meta` files together with their assets.
- Use force-text asset serialization and visible meta files (Unity's defaults) so
  scenes and prefabs can be diffed and merged.
- The repo is shared with the group: commit only when asked, keep commits focused,
  and don't rewrite pushed history.

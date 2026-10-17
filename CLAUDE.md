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

- Only README, LICENSE and the project slide so far. The Unity project still has to be created.
## Decisions

- **XR stack:** OpenXR.
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

- Unity version and render pipeline (URP is the usual pick for VR).
- Target VR headset, and whether the XR Interaction Toolkit is used on top of OpenXR.
- How the MVP canvas gets its data: same machine as the VR app (e.g. second display/window)
  or a separate PC (e.g. shared file, simple HTTP endpoint), and the update interval.

## Conventions

- Put the Unity project at the repo root (`Assets/`, `Packages/`, `ProjectSettings/`) unless
  the team decides otherwise. Update the README "Repository structure" section when it lands.
- Never commit `Library/`, `Temp/`, `Logs/`, `obj/`, `Build*/` or `UserSettings/`
  (covered by `.gitignore`).
- Always commit `.meta` files together with their assets.
- Use force-text asset serialization and visible meta files (Unity's defaults) so
  scenes and prefabs can be diffed and merged.
- The repo is shared with the group: commit only when asked, keep commits focused,
  and don't rewrite pushed history.

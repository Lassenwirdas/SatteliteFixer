# CLAUDE.md

Context for Claude Code when working in this repository.

## Project

SatteliteFixer ("P04 Collaborative Satellite Repair") is a Serious Games group project
(WiSe 2026/27, TU Darmstadt, supervisor Lisa Baule). It is a 2-player, asymmetric
co-op Unity game: one player in VR repairs a satellite in orbit while the other
player at a desktop PC acts as mission control, diagnoses the fault from sensor data
and guides the repair. The learning goals are how satellites and their sensors work,
and how to collaborate. The target group is children and teenagers. See `README.md`
and `image.png` (the original project slide).

## Status

- Only README, LICENSE and the project slide so far. The Unity project still has to be created.
- Open questions to settle with the team (don't guess these in code):
  - Unity version and render pipeline (URP is the usual pick for VR).
  - Target VR headset / XR stack (OpenXR + XR Interaction Toolkit is the default).
  - Networking between the VR and PC client (e.g. Netcode for GameObjects, Photon, Mirror),
    and whether both run on one machine or two.
  - The slide says "asynchronous cooperative". Clarify whether that means asymmetric
    roles (current reading) or turn-based/async play.

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

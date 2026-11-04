# CLAUDE.md

Context for Claude Code when working in this repository.

## Project

SatteliteFixer ("P04a Collaborative Satellite Repair") is a Serious Games group project
(WiSe 2026/27, TU Darmstadt, reviewer Stefan Göbel, supervisor Lisa Baule).
Team: Hussain El Haouli, Joshua Koch, Lasse Medla, Maximilian Müller.

It is a 2-player, asymmetric co-op Unity game:
- An **operator** on a PC (host) sees mission data, sensor data and a digital handbook.
  They diagnose random faults and guide the repair.
- An **astronaut** in VR (Meta Quest 3, client) finds the faulty sensors on a real ESA
  satellite and repairs them.

Both players sit in the same room and talk directly. The game state is synced in real time.
The game is played at the DLR School Lab Darmstadt by players from grade 8. It has two
learning goals: real satellite programmes and their technology, and cooperative
communication when each player has different information.

The concept document [Seriouse_Games_Praktikum-3.pdf](Seriouse_Games_Praktikum-3.pdf) is
the source of truth for scope, components and work packages (APs). See `README.md` for a summary.

## Status

- The Unity project is in `SatelliteFixer/`, created from the VR Core template.
- A first code skeleton is in `SatelliteFixer/Assets/_Project/Scripts/` (Core, Network,
  Operator, Astronaut). It was written without a Unity editor, so it has **not been
  compiled or tested yet**. No scenes, prefabs or data assets of our own exist yet.

## Decisions

- **Engine:** Unity 6000.6.6f1 with URP.
- **XR stack:** OpenXR + XR Interaction Toolkit 3.x (VR Core template). Test device: **Meta Quest 3**.
- **Architecture:** one Unity project with two scenes: PC app = **host** (operator),
  VR app = **client** (astronaut). Code is shared via the `SatelliteFixer.Core` namespace.
- **Game state:** the host is authoritative. All changes go through
  `GameStateManager.Modify`, which bumps the revision, saves the state locally (JSON in
  `persistentDataPath`) and publishes it. The client only sends `RepairAttempt`s, and
  the host's `ScenarioManager` checks them.
- **Sync:** **real time** between the PC and VR app (AP 3), via the `GameStateSync` abstraction.
  `LocalGameStateSync` is the in-editor loopback. The network transport is still open (see below).
- **Communication:** verbal, same room, no in-game voice chat.
- **Content:** real ESA satellites with models from ESA SciFleet (https://scifleet.esa.int/).
  Content is data-driven via ScriptableObjects (`SatelliteDefinition`, `SensorDefinition`,
  `FaultDefinition`). Asset IDs are stable keys in the synced state.
- This replaces the earlier "async canvas MVP" decision (2026-11-04, after the concept document).

## Open questions

Settle these with the team, and don't guess them in code:

- Network transport for `GameStateSync` (e.g. Netcode for GameObjects + Unity Transport over LAN,
  or a lightweight custom socket solution), and how the Quest finds the host (manual IP, LAN discovery).
- Which ESA satellites, sensors and faults go into the first scenarios, and the actual
  repair interactions (the `RepairAction` enum is a placeholder).
- Locomotion in zero gravity, and controllers vs. hand tracking.
- See issue #4 for the full list.

## Conventions

- The Unity project lives in `SatelliteFixer/` (not the repo root). All Unity paths are
  relative to that folder, e.g. `SatelliteFixer/Assets/...`.
- Our own content goes under `Assets/_Project/` (Scripts, later Scenes, Prefabs, Data).
  Don't edit template or sample content in place; copy it into `_Project` first.
- Namespaces follow folders: `SatelliteFixer.Core`, `.Network`, `.Operator`, `.Astronaut`.
  `Core` must not depend on the other three.
- Code style follows the template scripts: `m_` prefix for private fields,
  `[SerializeField, Tooltip(...)]`, and XML `<summary>` on public types and members.
- Gameplay code must not call a network API directly. Go through `GameStateSync`.
- Never commit `SatelliteFixer/Library/`, `Temp/`, `Logs/`, `obj/`, `Build*/` or
  `UserSettings/`, nor IDE files (`*.sln`, `*.slnx`, `*.csproj`). `.gitignore` covers them.
  Its Unity patterns are anchored to `/SatelliteFixer/`, so folders with the same name
  inside `Assets/` (e.g. `Assets/CompositionLayers/UserSettings`) are still tracked.
- Always commit `.meta` files together with their assets. When creating scripts outside the
  editor, also create their `.meta` files with a new random GUID.
- Use force-text asset serialization and visible meta files (Unity's defaults) so
  scenes and prefabs can be diffed and merged.
- The repo is shared with the group: commit only when asked, keep commits focused,
  and don't rewrite pushed history.

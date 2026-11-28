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
- Network and scene flow work: Lobby → Loading → Mission → Debrief between a PC host and a client,
  checked with the automated smoke test (see Conventions). Satellite models (SMILE, Mars Express,
  HST, with climbing handles) and first sensor parts exist; faults and repair tasks do not yet.
- Not yet tested on the Quest itself.

## Decisions

- **Engine:** Unity 6000.6.6f1 with URP.
- **XR stack:** OpenXR + XR Interaction Toolkit 3.x (VR Core template). Test device: **Meta Quest 3**.
  Locomotion is climbing along handles (XRI `ClimbInteractable`), gravity is 0.
- **Roles:** PC app = **operator** = network **host**; Quest app = **astronaut** = **client**.
  The role follows the platform (Android → astronaut). In the editor, Multiplayer Play Mode's main
  editor is the operator and virtual players are astronauts; `-sfRole operator|astronaut` overrides.
- **Scenes** (`Assets/_Project/Scenes`): `Lobby` (shared, PC canvas or XR rig per role),
  `Operator` (PC only) and `Astronaut` (VR only). Satellites are not scenes: `SatelliteSpawner`
  loads the prefab of the chosen `SatelliteDefinition` at runtime.
- **Session flow:** the host drives `GameState.phase`; each app's `SceneFlowController` loads its own
  scene for it. Lobby (operator picks satellite, tutorial on/off, fault count) → Loading (astronaut
  reports ready) → Mission (clock runs, max. 15 min, no game over) → Debrief → Lobby.
  If the astronaut drops, the clock pauses and the Quest reconnects into the running mission.
- **One mission per run.** Several faults on different components; **no fault is solvable without
  talking**: the operator has knowledge (data, 2D schematic, handbook) but never sees the satellite in 3D,
  the astronaut has eyes and hands but no instructions in VR (except the tutorial).
- **Game state:** the host is authoritative. All changes go through
  `GameStateManager.Modify`, which bumps the revision, saves the state locally (JSON in
  `persistentDataPath`) and publishes it. The client only sends events (`ClientReady`,
  `RepairAttempt`), and the host's `ScenarioManager` checks repairs.
- **Network:** Netcode for GameObjects + Unity Transport in the LAN, **no Unity Gaming Services**.
  Netcode's scene management is off; the state travels as JSON in named messages
  (`NetcodeGameStateSync`), not via NetworkObjects. Host → client: the full state on every change
  (clock 2×/s); client → host: events only. No positions are synced.
- **Finding the host:** `LanDiscovery` UDP broadcast on port 47777 (game port 7777); fallback
  `-sfHost <ip>` or `Session.ConnectTo`. On site, bring an own router or hotspot (school Wi-Fi may
  block device-to-device traffic).
- **Communication:** verbal, same room, no in-game voice chat.
- **Content:** real ESA satellites with models from ESA SciFleet (https://scifleet.esa.int/).
  Content is data-driven via ScriptableObjects (`SatelliteDefinition`, `SensorDefinition`,
  `FaultDefinition`). Asset IDs are stable keys in the synced state.
- This replaces the earlier "async canvas MVP" decision (2026-11-04, after the concept document).

## Open questions

Settle these with the team, and don't guess them in code:

- Which network the DLR School Lab allows (own router vs. hotspot).
- Concrete faults and repair tasks per satellite, and the actual repair interactions
  (the `RepairAction` enum is a placeholder).
- Manual IP entry in VR (keypad) as discovery fallback.
- Separate build profiles for PC and Quest (currently one scene list with all three scenes).
- Controllers vs. hand tracking.
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
- The `Session` prefab (`Assets/_Project/Prefabs/Session.prefab`) holds NetworkManager, sync,
  discovery and the host components and lives in DontDestroyOnLoad. Every scene has a
  `Session Bootstrap` that spawns it if missing, so any scene can be played directly.
  Scene objects can't reference it in the inspector; they use `GameStateSync.Active`,
  `GameStateManager.Instance`, `ScenarioManager.Instance` or `MissionFlow.Instance` in `Start`.
- Scene-flow regression test: build a Windows development player with the three scenes (Mono backend
  is enough; IL2CPP needs the Visual Studio C++ toolchain) and run two instances:
  `SatelliteFixer.exe -batchmode -nographics -sfRole operator -sfSmokeTest -logFile host.log` and the
  same with `-sfRole astronaut` (uses LAN discovery; add `-sfHost 127.0.0.1` to skip it). Both must log
  `[SmokeTest] PASS`. `-sfSmokeMissionSeconds 30` on the operator leaves time to kill and restart the
  astronaut to test the reconnect.
- Builds and editor upgrades rewrite unrelated settings (`preloadedAssets` in ProjectSettings, URP
  configs, GraphicsSettings, DefaultVolumeProfile). Don't commit that churn unless it is intended.
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

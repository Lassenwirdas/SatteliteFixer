# SatteliteFixer

**P04 – Collaborative Satellite Repair**
Group project for the *Serious Games* course, winter term 2026/27, TU Darmstadt.
Supervisor: Lisa Baule

![Project slide](image.png)

## About

The European space industry keeps growing, and SatteliteFixer is meant to give
young players a hands-on feel for it. It is a **cooperative VR serious game**:
a satellite in orbit has a fault, and two players have to work together to fix it.

- **Player 1 – Astronaut (VR):** is out in space next to the satellite and carries
  out the repair.
- **Player 2 – Mission Control (desktop PC):** sees information about the satellite
  (e.g. sensor data and repair checklists), spots the fault and guides the
  astronaut through the fix.

```
                 Satellite
               /           \
     repair   /             \   fault report
             /               \
  Player 1 (VR) <-----------> Player 2 (PC)
              communication
```

The game is **asymmetric**: each player sees and can do different things, so
neither of them can finish the repair alone. Only by communicating can they work
out what is broken and how to fix it.

## Goals

- **Learning:** players find out how a satellite is built, which sensors it carries
  and how the subsystems work together.
- **Collaboration:** players practice clear communication and teamwork through
  their split roles.
- **Target group:** children and teenagers. The goal is to teach and spark interest
  through play, at an early age.
- **Project focus:** a working prototype (function first).

## Scope

### MVP

- **VR (player 1):** Unity app built on **OpenXR**. The astronaut repairs the
  satellite in VR.
- **Mission Control (player 2):** a plain **canvas** on the PC that mirrors
  information from the game. It is **not synchronous**: the content is refreshed
  asynchronously or on a timer, not in real time.
  - Example: a checklist that mission control reads out loud to the astronaut.
  - The VR player reports back verbally (what they see, what they have done).
    Mission control has no direct control over the game.
- Communication between the players happens by talking out loud. There is no
  in-game voice chat.

### Later (if time permits)

- A dedicated, **synchronous Mission Control hub** that receives game state in real
  time, including the astronaut's movement and position, live telemetry and
  interactive controls.

## Setup

| Aspect        | Details                                                  |
|---------------|----------------------------------------------------------|
| Engine        | Unity                                                    |
| XR            | OpenXR                                                   |
| Platforms     | VR headset (player 1) + Windows PC (player 2)            |
| Player count  | 2 (1× VR, 1× PC), asymmetric co-op                       |
| Mission Control | MVP: canvas with async/timed updates; later: synchronous hub |

## Repository structure

> The Unity project has not been created yet. Update this section once it exists.

## Team

Serious Games group project, WiSe 2026/27, TU Darmstadt.

## License

[MIT](LICENSE)

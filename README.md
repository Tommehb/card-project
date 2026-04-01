# Horror Game

This repository contains multiple folders, but this README is specifically about the Unity project in `Horror Game/`.

## General Description

`Horror Game` is a first-person Unity horror game set in a haunted school environment called Eastwood Elementary School. The player explores classrooms, hallways, and outdoor areas at night while collecting keys and avoiding hostile mannequin enemies. The game combines exploration, simple survival mechanics, jump scares, and escalating enemy pressure to create a tense school-themed horror experience.

The project is built in Unity 6 and uses URP for rendering, NavMesh-based enemy movement, TMPro/UI for menus and objectives, and Unity Netcode for an experimental multiplayer scene.

## Objective

The main goal is to search the school for keys and survive long enough to escape.

- Find every required key for the selected difficulty level.
- Avoid mannequin enemies while moving through the school.
- Reach the safe zone after collecting all keys.

Difficulty changes the number of keys and the number of active enemies:

- Level 1: 5 keys, 2 of each mannequin type
- Level 2: 10 keys, 4 of each mannequin type
- Level 3: 20 keys, 6 of each mannequin type

## Core Gameplay Systems

- First-person player movement with mouse look, walking, running, and jumping
- Randomized key spawning across indoor and outdoor spawn points
- Objective UI that tracks key progress and switches to the escape objective
- Safe zone unlock once all keys are collected
- Death/jumpscare flow with survival time feedback
- Difficulty and mouse sensitivity settings managed through a persistent game manager

There are three mannequin behavior types in the current prototype:

- Blink mannequin: moves toward the player when it is not being watched
- Chase mannequin: pursues the player when close enough and has line of sight
- Hide mannequin: becomes dangerous after the player stares at it for too long

## Project Structure

```text
card-project/
├── Horror Game/
│   ├── Assets/
│   │   ├── Scenes/
│   │   │   ├── Home.unity
│   │   │   ├── Eastwood Elementary School.unity
│   │   │   └── MultiplayTest.unity
│   │   ├── Prefabs/
│   │   ├── Settings/
│   │   ├── school/
│   │   ├── MannequinAsset/
│   │   ├── UrbanNightSky/
│   │   ├── keys/
│   │   ├── materials/
│   │   ├── polygonTrees/
│   │   └── *.cs gameplay scripts
│   ├── Packages/
│   ├── ProjectSettings/
│   └── UserSettings/
└── tommy_card_store/
```

### Important Folders

- `Horror Game/Assets/Scenes/`: main playable scenes and menu scenes
- `Horror Game/Assets/Prefabs/`: reusable gameplay and environment prefabs, including the player and mannequin variants
- `Horror Game/Assets/Settings/`: render pipeline and post-processing settings
- `Horror Game/Assets/school/`: school environment models, materials, props, and demo content
- `Horror Game/Assets/MannequinAsset/`: mannequin models, materials, prefabs, and sample scene content
- `Horror Game/Assets/UrbanNightSky/`: skybox and atmosphere assets used for the nighttime mood
- `Horror Game/Packages/`: Unity packages such as URP, AI Navigation, UI, and Netcode
- `Horror Game/ProjectSettings/`: Unity project configuration, build settings, input, rendering, and editor version

### Important Scenes

- `Home.unity`: title/menu scene
- `Eastwood Elementary School.unity`: main single-player horror gameplay scene
- `MultiplayTest.unity`: multiplayer/networking test scene

### Important Scripts

- `GameHandler.cs`: manages objectives, key spawning, enemy activation, restart/exit flow, and death summary UI
- `GameManager.cs`: stores persistent settings such as difficulty level and mouse sensitivity
- `Player.cs`: handles first-person movement, key collection, and death events
- `TitleScreen.cs`: starts the main game scene from the menu
- `Door.cs`: door interaction prompt and toggle behavior
- `jumpscare.cs`: jumpscare UI, sound, and end screen handling
- `RedMannequin.cs`, `YellowManneguin.cs`, `GreenMannequin.cs`: enemy AI behaviors
- `BasicMove.cs`, `MultiplayerLauncher.cs`, `PersistentNetworkManager.cs`: multiplayer prototype scripts

## Opening The Project

Open the `Horror Game/` folder in Unity Editor `6000.4.0f1`.

For the main horror experience, start from:

- `Assets/Scenes/Home.unity` for the menu flow
- `Assets/Scenes/Eastwood Elementary School.unity` to jump directly into gameplay

## Summary

This project is a Unity school-horror prototype focused on exploring Eastwood Elementary School, collecting keys, surviving mannequin encounters, and escaping once the objective is complete. The folder structure includes the main gameplay scene, supporting prefabs and assets, imported environment/art packs, and a separate multiplayer test setup that sits alongside the core single-player horror game.

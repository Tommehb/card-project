# LAN Multiplayer Checklist

This file tracks the current LAN multiplayer scaffold for the Unity horror game and the work still needed to turn it into a full multiplayer experience.

## What The New Scripts Cover

The following pieces are now in the project so the scene flow has a usable LAN backbone:

- `[x]` Persistent `NetworkManager` handling across scene changes
- `[x]` Duplicate `NetworkManager` protection when loading from `Home` into `MultiplayTest`
- `[x]` Host, join, and shutdown flow through a reusable LAN session manager
- `[x]` Lobby player state for display name, ready state, and player slot index
- `[x]` Host-only game start from the lobby
- `[x]` Network scene transitions from menu -> lobby -> gameplay
- `[x]` Scene spawn point component for lobby and gameplay scenes
- `[x]` Multiplayer-aware `Player` ownership handling so only the owning client keeps its camera/audio active
- `[x]` Multiplayer-aware restart/exit behavior in `GameHandler`
- `[x]` Safeguard that despawns scene-placed `Player` objects in multiplayer sessions

## Files Added Or Updated

- `Assets/LanSessionManager.cs`
- `Assets/LanNetworkPlayer.cs`
- `Assets/LanSpawnPoint.cs`
- `Assets/LanLobbyUI.cs`
- `Assets/MultiplayerLauncher.cs`
- `Assets/PersistentNetworkManager.cs`
- `Assets/Player.cs`
- `Assets/GameHandler.cs`

## Important Current Constraint

The multiplayer scaffolding is now focused on session flow, lobby flow, scene loading, player ownership, and spawn placement.

The actual horror gameplay systems are still mostly single-player systems right now:

- key spawning and collection
- mannequin AI and targeting
- door state
- objective progression
- safe-zone win state
- death/jumpscare/game-over flow

Those items are listed below as remaining work.

## Scene Setup Checklist

### `Home.unity`

- `[ ]` Keep one `NetworkManager` with `UnityTransport`
- `[ ]` Keep `PersistNetworkManager` on that same object
- `[ ]` Confirm the `NetworkManager` player prefab is `Assets/Prefabs/Player.prefab`
- `[ ]` Keep `MultiplayerLauncher` in the scene
- `[ ]` Wire the Host button to `MultiplayerLauncher.Host()`
- `[ ]` Wire the Join button to `MultiplayerLauncher.Join()`
- `[ ]` Optionally wire a quit/leave button to `MultiplayerLauncher.ShutdownAll()`
- `[ ]` Assign the optional UI fields on `MultiplayerLauncher`:
- `ipAddressInput`
- `portInput`
- `playerNameInput`
- `statusText`
- `[ ]` Make sure `Home`, `MultiplayTest`, and `Eastwood Elementary School` are all enabled in Build Settings

Notes:

- `MultiplayerLauncher` now pushes its actions through `LanSessionManager`.
- `PersistNetworkManager` auto-adds `LanSessionManager` at runtime if the object does not already have one.

### `MultiplayTest.unity` as the lobby scene

- `[ ]` Add `LanLobbyUI` to a lobby UI object or the main Canvas
- `[ ]` Assign the optional `LanLobbyUI` fields you want to use:
- `statusText`
- `playerListText`
- `flowSummaryText`
- `playerNameInput`
- `readyButton`
- `readyButtonText`
- `startGameButton`
- `leaveButton`
- `[ ]` Add one `LanSpawnPoint` per expected player slot
- `[ ]` Set each lobby spawn point to `LanSpawnPointType.Lobby`
- `[ ]` Give each lobby spawn point a unique `slotIndex` starting at `0`
- `[ ]` If you want to test this scene by opening it directly, keep a `NetworkManager` in the scene with `PersistNetworkManager`
- `[ ]` If you always enter the lobby from `Home`, the duplicate `NetworkManager` in this scene can remain for now because `PersistNetworkManager` will destroy it at runtime
- `[ ]` Change the `MultiplayTest` `NetworkManager` player prefab from `Assets/Prefabs/PlayerBasic.prefab` to `Assets/Prefabs/Player.prefab` if this is going to be the real lobby flow
- `[ ]` Avoid leaving an extra active `AudioListener` in the lobby scene if the spawned player camera is active

Notes:

- Right now `Home` and `MultiplayTest` are inconsistent:
- `Home` uses `Assets/Prefabs/Player.prefab`
- `MultiplayTest` uses `Assets/Prefabs/PlayerBasic.prefab`
- For a real LAN flow, those should be unified to `Assets/Prefabs/Player.prefab`

### `Eastwood Elementary School.unity` as the gameplay scene

- `[ ]` Add one `LanSpawnPoint` per expected player slot
- `[ ]` Set each gameplay spawn point to `LanSpawnPointType.Gameplay`
- `[ ]` Give each gameplay spawn point a unique `slotIndex` starting at `0`
- `[ ]` Remove the scene-placed `Player` prefab instance once you finish wiring the network-spawned player flow
- `[ ]` Leave the current safeguard in place anyway, because `Player.cs` now despawns scene-placed player objects in multiplayer sessions
- `[ ]` Do not add another `NetworkManager` here if the scene is entered through the persistent session flow
- `[ ]` Review any scene camera or audio listener that could conflict with the spawned local player camera
- `[ ]` Verify that multiple players spawning at once are not inside blockers, enemy triggers, or NavMesh dead zones

Notes:

- The gameplay scene currently contains a placed `Player` prefab instance.
- That is okay for single-player testing, but LAN gameplay should use the spawned network player instead.

## Network Player Prefab Checklist

### `Assets/Prefabs/Player.prefab`

- `[ ]` Add `LanNetworkPlayer` to the prefab root
- `[ ]` Keep the existing `NetworkObject`
- `[ ]` Keep the existing `NetworkTransform`
- `[ ]` Keep `Player.cs` on the prefab root
- `[ ]` Confirm the `playerCamera` reference on `Player.cs` is assigned
- `[ ]` Confirm any `gameHandler` or `jumpscareHandler` references are set in a way that still makes sense after network spawning
- `[ ]` Decide whether the local player should hide its own body mesh in first-person

### `Assets/Prefabs/PlayerBasic.prefab`

- `[ ]` Either stop using this prefab for the real LAN flow
- `[ ]` Or add `LanNetworkPlayer` to it and accept that it is only a simple cube test player

Recommended path:

- Use `Assets/Prefabs/Player.prefab` for both lobby and gameplay so the same network player object survives scene transitions cleanly.

## Remaining Gameplay Work

### Shared objective state

- `[ ]` Move key count, total key count, and objective status out of local-only `GameHandler` state
- `[ ]` Replicate objective progress from host/server to all clients
- `[ ]` Decide whether the game is shared-coop progress or per-player progress
- `[ ]` Replicate the safe-zone unlock state
- `[ ]` Replicate win condition and what happens to all players when the game is won

### Key spawning and collection

- `[ ]` Make the host/server authoritative over key spawning
- `[ ]` Stop clients from destroying keys locally without server approval
- `[ ]` Convert collected key removal into a replicated/despawned state
- `[ ]` Make sure late clients or scene resyncs see the correct remaining keys

### Enemy behavior

- `[ ]` Pick one authority model for mannequins, preferably host/server authority
- `[ ]` Synchronize mannequin transforms and state transitions
- `[ ]` Decide how mannequins choose targets when multiple players exist
- `[ ]` Update blink, chase, and hide mannequin logic so they work with multiple players
- `[ ]` Synchronize mannequin kills/deaths instead of letting each client decide locally

### Interactables

- `[ ]` Replicate door open/closed state
- `[ ]` Make door interaction host/server authoritative
- `[ ]` Replicate any future pickups, switches, locks, or classroom interactions

### Death and end-state flow

- `[ ]` Decide whether one player death ends the whole run or only that player
- `[ ]` Synchronize jumpscare trigger and end-screen state
- `[ ]` Synchronize restart/game-over flow across the session
- `[ ]` Make sure leaving to title or restarting cannot split the session into mismatched scenes

### Lobby and session rules

- `[ ]` Decide whether late join is allowed after gameplay has started
- `[ ]` If late join is not allowed, add connection approval or a join gate once the host leaves the lobby
- `[ ]` Add a player count limit and reflect it in the UI
- `[ ]` Add clearer error/status feedback for bad IPs, timeout, disconnect, and full lobby
- `[ ]` Decide whether the host must also mark ready or whether only clients must ready up

### UI and polish

- `[ ]` Separate single-player menu flow from LAN flow more clearly in `Home`
- `[ ]` Add a dedicated lobby player list panel instead of using plain text if desired
- `[ ]` Add an in-game multiplayer HUD that shows other players, status, and shared objective data
- `[ ]` Add a visible “return to lobby” or “leave session” path in both lobby and gameplay

### Testing

- `[ ]` Test host/client on the same machine
- `[ ]` Test host/client on two machines over the same LAN
- `[ ]` Test scene transitions with 2+ players
- `[ ]` Test disconnect while in lobby
- `[ ]` Test disconnect while in gameplay
- `[ ]` Test that remote player cameras and audio listeners stay disabled
- `[ ]` Test that the scene-placed `Player` in gameplay no longer creates a duplicate during LAN play

## Recommended Next Implementation Order

1. Unify both multiplayer scenes to use `Assets/Prefabs/Player.prefab` and add `LanNetworkPlayer` to that prefab.
2. Add `LanLobbyUI` and `LanSpawnPoint` components to `MultiplayTest`.
3. Add gameplay `LanSpawnPoint` components to `Eastwood Elementary School`.
4. Enable all three scenes in Build Settings.
5. Convert `GameHandler` into a host/server-authoritative shared objective manager.
6. Convert key collection, doors, and mannequin AI into replicated gameplay systems.

## Quick Wiring Summary

If you want the shortest path to seeing the new LAN scaffold work:

1. Put `LanNetworkPlayer` on `Assets/Prefabs/Player.prefab`.
2. Make both `Home` and `MultiplayTest` `NetworkManager` objects use `Assets/Prefabs/Player.prefab`.
3. Add `LanLobbyUI` to the `MultiplayTest` canvas and hook up the buttons/text fields.
4. Add `LanSpawnPoint` objects to `MultiplayTest` and `Eastwood Elementary School`.
5. Enable `Home`, `MultiplayTest`, and `Eastwood Elementary School` in Build Settings.

At that point you should have a usable LAN menu -> lobby -> gameplay scene flow, even though the actual horror gameplay systems will still need networking work.

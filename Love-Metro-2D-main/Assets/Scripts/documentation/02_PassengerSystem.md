# Passenger System

## Overview

The passenger system is centered on the serialized `Passenger` MonoBehaviour and a set of runtime collaborators. The MonoBehaviour keeps Unity-facing state, prefab compatibility, and lifecycle callbacks. Runtime behavior is moved into smaller classes that can be reasoned about and tested independently.

## Main Runtime Surface

`Passenger` provides the stable gameplay API:

- gender and matchability flags
- current couple state
- `Initiate(...)` for spawn-time setup
- launch, transport, absorption, and pairing entry points
- `Settings` for resolved `PassengerSettings`

The implementation is split into partial files:

- `Passenger.cs` for serialized fields and public API
- `Passenger.Lifecycle.cs` for setup and required component handling
- `Passenger.Motion.cs` for train and movement integration
- `Passenger.MatchLogic.cs` for matching and pair formation entry points
- `Passenger.FieldEffects.cs` for field-effect target behavior
- `Passenger.StateMachine.cs` for state transitions

## Runtime Collaborators

The `Runtime/Passengers/` folder contains the extracted behavior:

- `PassengerStateRuntime` coordinates state transitions.
- `PassengerStateMachine` stores and updates the active state.
- `PassengerStateFactory` builds state instances.
- `PassengerPhysicsRuntime` owns Rigidbody2D and collider normalization.
- `PassengerMotionController` applies movement and train-motion effects.
- `PassengerInteractionRuntime` handles click/interaction behavior.
- `PassengerMatchRuntime` finds and validates match targets.
- `PassengerPairFormationRuntime` creates couples.

New passenger behavior should usually be added to these classes, not directly into the MonoBehaviour.

## Settings-Based Tuning

`PassengerSettings` is the source of passenger tuning values. `Passenger.Settings` resolves either the prefab override or the default resource. State and motion code should read values through settings-derived tuning objects instead of duplicating constants.

Important tuning groups:

- global movement multiplier
- handrail grab chance and timing
- launch sensitivity and impulse thresholds
- flight speed, damping, bounce, and duration
- magnet and same-gender repulsion behavior
- field-effect thresholds and scaling

## Registries And Services

Passenger collection access goes through `PassengerRegistry`. Runtime systems should not scan the scene to find passengers during gameplay.

Scene-level dependencies are wired by `RuntimeCompositionRoot` and exposed through `RuntimeServices` where a service contract exists. This keeps gameplay code independent from scene discovery and singleton fallbacks.

## Spawning

`PassangerSpawner` keeps the legacy public API and serialized prefab fields, but its planning logic is extracted:

- `SpawnPlanner` calculates wave size and gender distribution.
- `PrefabPool` creates shuffled pools and cycles prefab selections.
- `SpawnLocationProvider` filters spawn points and adjusts low Y positions.
- `VipPairAssigner` applies VIP ability to one female/male pair in a wave.

The spawner remains responsible for Unity-specific instantiation and calling `Passenger.Initiate(...)`.

## Matching And Couples

Matching flows through `PassengerMatchRuntime` and `PassengerPairFormationRuntime`. Couple scoring uses the scoring service where available. Station-stop behavior is requested through `IStationFlowService`, so `CouplesManager` does not need to depend directly on `TrainManager`.

## Refactoring Rules

- Keep serialized field names stable unless the task is an explicit prefab migration.
- Keep `Passanger*` misspellings until a dedicated rename migration.
- Keep scene discovery out of passenger hot paths.
- Prefer settings/tuning objects over scattered constants.
- Prefer runtime collaborators over adding more logic to the MonoBehaviour.

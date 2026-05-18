# Love Metro 2D Refactoring Guide

## Current State

The active refactoring path keeps Unity scene and prefab compatibility intact. Public `Passanger*` names, serialized fields, prefab references, and compatibility methods stay in place until a dedicated migration step.

The current runtime architecture is organized around these seams:

- `Runtime/Core/RuntimeCompositionRoot.cs` is the single composition point for scene runtime services.
- `Runtime/Core/SceneObjectIndex.cs` owns scene discovery for composition. Runtime gameplay code should not call `FindObjectOfType`, `FindObjectsOfType`, or `GameObject.Find`.
- `Runtime/Core/RuntimeServices.cs` exposes stable service contracts for passenger registry, scoring, train motion, station flow, field effects, and manual pairing.
- `Passenger` remains one serialized `partial` MonoBehaviour split across focused files: lifecycle, motion, matching, field effects, and state machine.
- `Runtime/Passengers/` contains pure runtime collaborators for passenger state, physics, interaction, matching, and pair formation.
- `Runtime/Spawning/` contains the extracted passenger spawn planning, prefab pool, spawn-location, and VIP-pair assignment logic.

## Compatibility Rules

- Do not rename `PassangerSpawner`, `PassangersContainer`, `PassangerAnimator`, `spawnPassangers()`, or serialized prefab fields in broad refactors.
- Do not migrate scenes or prefabs implicitly. If a change needs manual Unity relinking, make it a separate task.
- Keep `PassengerSettings` as the source for passenger tuning. New movement or interaction constants should go through settings/tuning objects instead of new scattered private fields.
- Keep `PassengerRegistry` as the access path for passenger collections in gameplay code.
- Keep direct scene discovery limited to composition, diagnostics, installers, and tests.

## Current Passenger Extraction

`Passenger.cs` is the public serialized surface. The implementation is distributed into partial files:

- `Passenger.Lifecycle.cs`
- `Passenger.Motion.cs`
- `Passenger.MatchLogic.cs`
- `Passenger.FieldEffects.cs`
- `Passenger.StateMachine.cs`

The runtime collaborators live under `Runtime/Passengers/`:

- `PassengerStateRuntime`
- `PassengerStateMachine`
- `PassengerStateFactory`
- `PassengerPhysicsRuntime`
- `PassengerMotionController`
- `PassengerInteractionRuntime`
- `PassengerMatchRuntime`
- `PassengerPairFormationRuntime`

When changing passenger behavior, prefer moving logic into these runtime collaborators while keeping the MonoBehaviour as a thin host for serialized Unity state and lifecycle callbacks.

## Current Spawning Extraction

`PassangerSpawner` is now an orchestrator. It delegates:

- wave size and gender distribution to `SpawnPlanner`
- prefab shuffling and cycling to `PrefabPool`
- spawn point filtering and coordinate normalization to `SpawnLocationProvider`
- VIP pair selection and ability attachment to `VipPairAssigner`

Tests for the pure spawning rules live in `Assets/Tests/Editor/SpawnPlannerTests.cs` and `Assets/Tests/Editor/PrefabPoolTests.cs`.

## Verification Checklist

Run these before committing runtime refactors:

- EditMode tests: full `Assets/Tests/Editor` suite.
- PlayMode smoke tests when spawn/train/scene composition behavior changes.
- `git diff --check`.
- Static acceptance tests for runtime scene discovery and hot-path singleton use.

If the same Unity CLI/test error repeats twice, follow the repository rule: investigate external documentation or web results, compare several fixes, then apply the most effective one.

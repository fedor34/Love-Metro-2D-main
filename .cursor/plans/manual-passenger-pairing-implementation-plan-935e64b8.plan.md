---
name: Manual Passenger Pairing Implementation Plan
overview: ""
todos:
  - id: 94ad8a36-e799-49ef-a24b-9527763be958
    content: Modify TrainManager.cs to include fuel logic and consumption
    status: pending
  - id: 7e9856b2-ef97-49d8-96e0-928a6c4dff78
    content: Create FuelHUD.cs to display the fuel bar
    status: pending
  - id: 7a3b0304-b0e3-4151-a343-71e4e237a65e
    content: Integrate Game Over state when fuel runs out
    status: pending
---

# Manual Passenger Pairing Implementation Plan

This plan introduces a mechanic to manually pair adjacent passengers by clicking on them, allowing players to fix "near-miss" matches before the train moves.

## 1. Create `PassengerSelectionManager.cs`

- Create a new script in `Assets/Scripts/Core/` (or `Input/`).
- Functionality:
- Listen for `Input.GetMouseButtonDown(0)`.
- Raycast to detect if a `Passenger` is clicked.
- **Selection Logic**:
- **First Click**: Store the first passenger as `_selectedPassenger`.
- **Second Click**:
- Check if the clicked passenger is different from the first.
- Check if they are **compatible** (opposite gender, not already in a couple).
- Check if they are **nearby** (within a configurable distance, e.g., 2.0 - 3.0 units).
- If valid: Trigger `ForceToMatchingState` on both to form a couple.
- If invalid or same passenger: Update selection to the new passenger or deselect.
- **Visual Feedback** (Optional but recommended): Draw a debug line or small indicator for the selected passenger.

## 2. Update `Passenger.cs`

- Ensure `ForceToMatchingState` is public and works correctly when called externally (it seems to be).
- Add a helper property or method `IsAvailableForMatching()` to check `!IsInCouple`, `IsMatchable`, and current state (e.g., allow if `Wandering` or `StayingOnHandrail`, maybe not `Falling`).

## 3. Update `GameInitializer.cs`
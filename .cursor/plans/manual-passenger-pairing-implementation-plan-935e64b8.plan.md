<!-- 935e64b8-7f76-4abe-804a-f8831c60fa35 a333b345-422c-4eb7-bdd8-35c15ef6f462 -->
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

- Add `PassengerSelectionManager` to the auto-creation list to ensure it exists in the scene.

## 4. Integration with `ClickDirectionManager`

- Ensure clicking a passenger doesn't accidentally trigger unwanted train movement (or accept that a quick click is negligible). Ideally, if a passenger is clicked, we might want to suppress the train input for that frame, but for a simple "click" (tap), the train impulse is usually small/ignored if it's just a tap without drag.

### To-dos

- [ ] Modify TrainManager.cs to include fuel logic and consumption
- [ ] Create FuelHUD.cs to display the fuel bar
- [ ] Integrate Game Over state when fuel runs out
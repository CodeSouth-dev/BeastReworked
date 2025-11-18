# Compilation Errors Fixed - PreparationPhase

## Issue
After removing the stashing workflow steps from `PrepPhaseStep` enum, compilation errors occurred because deprecated methods still referenced the removed enum values:

```
'PreparationPhase.PrepPhaseStep' does not contain a definition for 'MoveToHeistLocker'
'PreparationPhase.PrepPhaseStep' does not contain a definition for 'DepositToHeistLocker'
'PreparationPhase.PrepPhaseStep' does not contain a definition for 'DepositToStash'
'PreparationPhase.PrepPhaseStep' does not contain a definition for 'MoveToStash'
```

## Root Cause
The deprecated methods (`MoveToStash`, `DepositToStash`, `MoveToHeistLocker`, `DepositToHeistLocker`) were marked as `[Obsolete]` but still contained their original implementation code, which referenced enum values that were removed:

- `PrepPhaseStep.MoveToStash` ❌ (removed)
- `PrepPhaseStep.DepositToStash` ❌ (removed)
- `PrepPhaseStep.MoveToHeistLocker` ❌ (removed)
- `PrepPhaseStep.DepositToHeistLocker` ❌ (removed)

## Solution
Replaced the full implementation of all deprecated methods with simple error stubs that:
1. Never reference removed enum values
2. Log an error if ever called (should never happen)
3. Reset to valid `Restock` step
4. Return a failed result

### Before (Broken)
```csharp
[Obsolete("PreparationPhase no longer handles stashing - use ExitAndStashPhase")]
private async Task<PhaseResult> MoveToStash(GameContext context)
{
    // 40+ lines of implementation
    // Including: _currentStep = PrepPhaseStep.DepositToStash; ❌ COMPILE ERROR
    // ...
}
```

### After (Fixed)
```csharp
[Obsolete("PreparationPhase no longer handles stashing - use ExitAndStashPhase")]
private async Task<PhaseResult> MoveToStash(GameContext context)
{
    await Task.CompletedTask;
    Log.Error("[Preparation] DEPRECATED METHOD CALLED: MoveToStash - this should never happen!");
    _currentStep = PrepPhaseStep.Restock; ✓ Valid enum value
    return PhaseResult.Failed("Deprecated method called - phase logic error");
}
```

## Methods Fixed
1. `MoveToStash()` - Reduced from ~44 lines to 6 lines
2. `DepositToStash()` - Reduced from ~120 lines to 6 lines
3. `MoveToHeistLocker()` - Reduced from ~52 lines to 6 lines
4. `DepositToHeistLocker()` - Reduced from ~78 lines to 6 lines

## Why Keep These Methods?
These deprecated methods are kept as stubs rather than deleted entirely because:
1. **Safety**: If any code path accidentally reaches them, we get a clear error message
2. **Documentation**: The `[Obsolete]` attribute explains why they exist
3. **Future Reference**: The method signatures remain for anyone reviewing the code history

## Verification
✅ No compilation errors  
✅ All deprecated methods have simple error stubs  
✅ No references to removed enum values  
✅ Phase will fail gracefully if deprecated code path is reached  

## Files Modified
- `Beasts/Phases/PreparationPhase.cs`
  - Simplified 4 deprecated methods to error stubs
  - Removed ~290 lines of dead code
  - All enum references now valid


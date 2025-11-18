# Bug Fix: Infinite Loop When Out of Maps

## Problem Description

The bot was getting stuck in an infinite loop when the map device was empty and there were no maps available in inventory or stash. The behavior was:

1. Bot goes to map device
2. Opens map device UI and finds it empty
3. Checks inventory for maps - finds none
4. Goes to stash to withdraw a map
5. Searches all stash tabs - finds no suitable maps
6. Returns `PhaseResult.Failed("Out of maps...")`
7. Phase becomes inactive (CanExecute returns true)
8. StateController picks the same phase again
9. **Loop repeats from step 1**

## Root Cause

The `OpenMapPhase.CanExecute()` method only checked if the player had portal scrolls, but did not track whether the bot had already determined it was out of maps. This meant that even after discovering there were no maps available anywhere, the phase would still report itself as executable, leading to an infinite retry loop.

```csharp
// Before (problematic code)
public bool CanExecute(GameContext context)
{
    if (!context.Player.IsInHideout)
        return false;

    if (context.MapDevice.IsActive)
        return false;

    // Only checks portal scrolls - doesn't track map availability!
    bool hasPortalScrolls = context.Player.HasPortalScrolls;
    
    if (!hasPortalScrolls)
        return false;

    return true; // ❌ Returns true even when out of maps!
}
```

## Solution

Added a persistent `_outOfMaps` flag that:

1. **Tracks map availability**: Set to `true` when `WithdrawMapFromStash()` discovers no maps in stash
2. **Prevents re-execution**: Checked in `CanExecute()` to prevent the phase from running when out of maps
3. **Persists across phase exits**: Not reset in `OnExit()` to prevent infinite loops
4. **Resets on success**: Set back to `false` when a map is successfully withdrawn

### Changes Made

#### 1. Added `_outOfMaps` flag to track state

```csharp
public class OpenMapPhase : IPhase
{
    private MapOpeningStep _currentStep = MapOpeningStep.ValidateRequirements;
    private int _stepAttempts = 0;
    private bool _outOfMaps = false; // ✅ NEW: Track when we've run out of maps
    // ...
}
```

#### 2. Updated `CanExecute()` to check the flag

```csharp
public bool CanExecute(GameContext context)
{
    // ... existing checks ...

    // ✅ NEW: Don't execute if we've run out of maps
    if (_outOfMaps)
    {
        Log.Error("[OpenMap] Cannot execute - OUT OF MAPS! Please restock your stash with maps and restart the bot.");
        return false;
    }

    // ... rest of method ...
}
```

#### 3. Set flag when no maps found in stash

```csharp
private async Task<PhaseResult> WithdrawMapFromStash()
{
    // ... search all stash tabs ...

    // No suitable maps found in any tab
    Log.Error("[OpenMap] No suitable maps found in stash");
    Log.ErrorFormat("[OpenMap] Searched up to {0} tabs, found no maps matching tier {1}-{2}",
        maxTabs, settings.MinMapTier, settings.MaxMapTier);

    // ✅ NEW: Set the out of maps flag so we don't keep trying
    _outOfMaps = true;

    // ... close stash UI ...
    
    return PhaseResult.Failed("Out of maps - restock stash and restart bot");
}
```

#### 4. Reset flag when map is successfully withdrawn

```csharp
private async Task<PhaseResult> WithdrawMapFromStash()
{
    // ... find and withdraw map ...

    if (fastMoveResult == FastMoveResult.None)
    {
        Log.Info("[OpenMap] Map withdrawn successfully");

        // ✅ NEW: We found a map! Reset the out of maps flag
        _outOfMaps = false;

        // ... continue with map insertion ...
    }
}
```

#### 5. Updated `OnExit()` to NOT reset the flag

```csharp
public void OnExit()
{
    // ... close UIs ...

    // Reset state for next time
    _currentStep = MapOpeningStep.ValidateRequirements;
    _stepAttempts = 0;
    // ✅ NOTE: _outOfMaps is NOT reset here - it persists until bot restart
    // This prevents infinite loops when we're genuinely out of maps
}
```

## Testing

Build completed successfully with no errors:
```
Build succeeded.
    1 Warning(s)
    0 Error(s)
```

## Expected Behavior After Fix

### When Out of Maps:
1. Bot goes to map device
2. Opens map device UI and finds it empty
3. Checks inventory for maps - finds none
4. Goes to stash to withdraw a map
5. Searches all stash tabs - finds no suitable maps
6. Sets `_outOfMaps = true`
7. Returns `PhaseResult.Failed("Out of maps...")`
8. Phase becomes inactive
9. **Next tick**: `CanExecute()` returns `false` due to `_outOfMaps` flag
10. **StateController picks a different phase (or bot idles)**
11. **Loop is broken! ✅**

### User Action Required:
When the bot runs out of maps, the user must:
1. Restock the stash with maps (tier matches configured range)
2. Restart the bot (to reset the `_outOfMaps` flag)

### Future Enhancement Suggestion:
Consider adding a method to reset the `_outOfMaps` flag when the bot detects new maps in stash after idling for a period, allowing for hot-reloading of maps without bot restart.

## Files Modified

- `Beasts/Phases/OpenMapPhase.cs`
  - Added `_outOfMaps` flag
  - Updated `CanExecute()` to check flag
  - Set flag in `WithdrawMapFromStash()` when no maps found
  - Reset flag in `WithdrawMapFromStash()` when map is withdrawn
  - Added comment in `OnExit()` explaining why flag is not reset


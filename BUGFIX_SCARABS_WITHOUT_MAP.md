# Bug Fix: Bot Stuck with Scarabs in Device but No Map

## Problem Description

The bot was getting stuck in an infinite loop when:
- Scarabs were already loaded in the map device (from a previous run or manual placement)
- **BUT no actual map was in the device**
- The bot would stay at the map device without taking any action to resolve the situation

## Root Cause Analysis

The original logic flow had a critical flaw:

### Original Flow (Problematic)
1. **InsertMap step**: Check if map is in device
   - If yes → skip to InsertScarabs ✅
   - If no → get map from inventory/stash ✅
2. **InsertScarabs step**: Check if scarabs are in device
   - If yes → skip to ActivateDevice ❌ **PROBLEM HERE!**
   - If no → insert scarabs ✅
3. **ActivateDevice step**: Try to activate
   - **Fails silently or does nothing because no map is present** ❌

### The Issue
When scarabs were present but no map was in the device:
- `InsertMap` would correctly detect no map and try to get one
- BUT the device already had items (scarabs), which confused the flow
- The bot would transition to `InsertScarabs` 
- `InsertScarabs` would detect scarabs already present
- Skip to `ActivateDevice`
- **Activation fails because there's no map**
- Bot gets stuck in a loop

## Solution

Added three layers of defense to prevent activating the device without a map:

### 1. Clear Scarabs Without Map in `InsertMap` Step

When checking the device, if we find scarabs but NO map, clear the device first:

```csharp
// In InsertMap step
bool hasMapInDevice = CheckMapDeviceForMaps();

if (hasMapInDevice)
{
    // Map already loaded - proceed to scarabs
    _currentStep = MapOpeningStep.InsertScarabs;
    return PhaseResult.InProgress("Map already in device");
}

// ✅ NEW: Check if there are scarabs without a map
bool hasScarabsWithoutMap = CheckMapDeviceForScarabs();
if (hasScarabsWithoutMap)
{
    Log.Warn("[OpenMap] Found scarabs in device but NO MAP! Clearing device first...");
    if (!await MapDeviceService.ClearMapDevice())
    {
        // Handle clear failure
        return PhaseResult.InProgress("Retrying device clear...");
    }
    
    Log.Info("[OpenMap] Device cleared of scarabs, now checking for maps...");
    // Continue to check for map in inventory/stash
}
```

### 2. Verify Map Before Scarab Insertion in `InsertScarabs` Step

Before doing anything with scarabs, verify a map is actually present:

```csharp
private async Task<PhaseResult> InsertScarabs(GameContext context)
{
    Log.Info("[OpenMap] Checking map device for existing scarabs");

    // ✅ NEW: CRITICAL safeguard - verify we have a map before proceeding
    bool hasMap = CheckMapDeviceForMaps();
    if (!hasMap)
    {
        Log.Error("[OpenMap] CRITICAL: No map in device at InsertScarabs step! Going back to InsertMap.");
        _currentStep = MapOpeningStep.InsertMap;
        _stepAttempts = 0;
        return PhaseResult.InProgress("No map detected, going back to insert map");
    }

    // Now safe to check/insert scarabs...
}
```

### 3. Final Safeguard Before Activation in `ActivateMapDevice` Step

As a final check, verify the map is present before attempting activation:

```csharp
private async Task<PhaseResult> ActivateMapDevice(GameContext context)
{
    Log.Info("[OpenMap] Activating map device");

    // ✅ NEW: FINAL SAFEGUARD - verify we have a map before activation
    bool hasMap = CheckMapDeviceForMaps();
    if (!hasMap)
    {
        Log.Error("[OpenMap] CRITICAL: Attempted to activate device without a map! Going back to InsertMap.");
        _currentStep = MapOpeningStep.InsertMap;
        _stepAttempts = 0;
        return PhaseResult.InProgress("No map detected before activation, going back");
    }

    // Now safe to activate...
}
```

## New Flow (Fixed)

### Scenario: Scarabs in device, no map

1. **InsertMap**: Opens device, checks contents
   - Detects scarabs but no map
   - **Clears the device** (removes scarabs)
   - Checks inventory for map
   - If no map in inventory → goes to stash
   - Withdraws map from stash
   - Returns to device and inserts map ✅

2. **InsertScarabs**: Verifies map is present
   - **Checks for map first** (new safeguard)
   - Map is present → proceed to insert scarabs
   - If somehow no map → goes back to InsertMap ✅

3. **ActivateDevice**: Final verification
   - **Checks for map one last time** (final safeguard)
   - Map is present → activates device successfully ✅
   - If somehow no map → goes back to InsertMap ✅

## Key Methods Used

### `CheckMapDeviceForMaps()`
Returns `true` only if there's an actual map (item.Class contains "map") in the device:

```csharp
private bool CheckMapDeviceForMaps()
{
    // ... get device inventory ...
    
    // Check if any item in device is a map
    bool hasMap = deviceInventory.Items.Any(item =>
        item != null &&
        item.IsValid &&
        item.Class != null &&
        item.Class.ToLower().Contains("map"));

    return hasMap;
}
```

### `CheckMapDeviceForScarabs()`
Returns `true` if there are scarabs (item.Name contains "scarab") in the device:

```csharp
private bool CheckMapDeviceForScarabs()
{
    // ... get device inventory ...
    
    // Check if any item in device is a scarab
    bool hasScarabs = deviceInventory.Items.Any(item =>
        item != null &&
        item.IsValid &&
        item.Name != null &&
        item.Name.ToLower().Contains("scarab"));

    return hasScarabs;
}
```

## Expected Behavior After Fix

### When Scarabs Are in Device Without Map:

1. Bot opens map device UI
2. Detects scarabs but no map
3. **Logs warning**: "Found scarabs in device but NO MAP! Clearing device first..."
4. Clears all items from device (scarabs removed)
5. Checks inventory for map
6. If no map in inventory:
   - Goes to stash
   - Withdraws a suitable map
7. Returns to device
8. Inserts map
9. Inserts scarabs (fresh from inventory)
10. Activates device successfully ✅

### Logs to Look For:

```
[OpenMap] Found scarabs in device but NO MAP! Clearing device first...
[OpenMap] Device cleared of scarabs, now checking for maps...
[OpenMap] No map in inventory, withdrawing from stash
[OpenMap] Map withdrawn successfully
[OpenMap] Inserting map from inventory: <MapName> (Tier: X)
[OpenMap] Placing scarab: Bestiary Scarab of Duplicating
[OpenMap] Placing scarab: Bestiary Scarab of the Herd
[OpenMap] Map device activated successfully
```

### Error Logs (If Safeguards Trigger):

```
[OpenMap] CRITICAL: No map in device at InsertScarabs step! Going back to InsertMap.
[OpenMap] CRITICAL: Attempted to activate device without a map! Going back to InsertMap.
```

## Testing

No linter errors detected.

## Files Modified

- `Beasts/Phases/OpenMapPhase.cs`
  - Added scarab clearing logic in `InsertMap()` when scarabs but no map detected
  - Added map verification safeguard in `InsertScarabs()`
  - Added final map verification in `ActivateMapDevice()`

## Related Fixes

This fix works in conjunction with:
- **BUGFIX_OUT_OF_MAPS.md**: Prevents infinite loops when no maps are available
- Both fixes ensure the bot handles edge cases gracefully and doesn't get stuck

## Notes

- The bot now has **triple verification** that a map is present before activation
- Scarabs are automatically cleared if found without a map (prevents wasted scarabs)
- Clear error logging helps diagnose issues quickly
- The bot will auto-recover from this state instead of getting stuck


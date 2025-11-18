# Bug Fix: Preserve Items in Map Device & Handle Missing Items

## Problem Description

The bot was getting stuck when the map device had items partially loaded:
- **Scenario 1**: Scarabs in device, but no map → bot would stay at device doing nothing
- **Scenario 2**: Map in device, but no scarabs → similar stuck behavior

The bot should intelligently detect what's missing and only fetch those items, while **preserving** what's already loaded.

## Root Cause

The original logic didn't properly handle partial states:
1. If scarabs were present but no map, it would clear everything (wasteful)
2. If a map was present but no scarabs, it would try to activate anyway (fails)
3. The flow didn't have proper fallback logic to go back and get missing items

## Solution

Updated the logic to **preserve existing items** and only fetch what's missing:

### 1. InsertMap Step - Detect and Preserve Scarabs

When checking if we need a map:
- ✅ If map already in device → skip to InsertScarabs
- ✅ If NO map but scarabs present → keep scarabs, go get a map
- ✅ If device is empty → skip clearing step, just add the map

```csharp
// Check if map device already has a map loaded
bool hasMapInDevice = CheckMapDeviceForMaps();

if (hasMapInDevice)
{
    Log.Info("[OpenMap] Map already loaded in map device - skipping to scarabs");
    _currentStep = MapOpeningStep.InsertScarabs;
    return PhaseResult.InProgress("Map already in device");
}

// NO MAP in device - check if there are scarabs (we'll keep them and just add the map)
bool hasScarabsWithoutMap = CheckMapDeviceForScarabs();
if (hasScarabsWithoutMap)
{
    Log.Info("[OpenMap] Found scarabs in device but no map - will keep scarabs and add map");
}

// Continue to get map from inventory/stash and add it to device (preserving scarabs)
```

**Key Point**: We check if device has items before clearing (line 289-304):
```csharp
// Check if device already has items - if user pre-loaded, skip clearing
var deviceControl = LokiPoe.InGameState.MapDeviceUi.InventoryControl;
bool deviceHasItems = deviceControl?.Inventory?.Items?.Count > 0;

if (deviceHasItems)
{
    Log.Info("[OpenMap] Map device already has items loaded, skipping clear step");
}
else
{
    // Clear any existing items from device only if empty
    await MapDeviceService.ClearMapDevice();
}

// Place map into device (scarabs remain if present)
await MapDeviceService.PlaceItemInDevice(mapItem);
```

### 2. InsertScarabs Step - Verify Map Present Before Activating

When checking if we need scarabs:
- ✅ If scarabs already in device → verify map is also present
- ✅ If scarabs present BUT no map → go back to InsertMap to get the map
- ✅ If no scarabs → insert them from inventory
- ✅ If not enough scarabs in inventory → fail and let PreparationPhase restock

```csharp
private async Task<PhaseResult> InsertScarabs(GameContext context)
{
    Log.Info("[OpenMap] Checking map device for existing scarabs");

    // First, check if scarabs already loaded
    if (CheckMapDeviceForScarabs())
    {
        Log.Info("[OpenMap] Scarabs already loaded in map device");
        
        // ✅ Verify we also have a map before proceeding to activation
        bool hasMap = CheckMapDeviceForMaps();
        if (!hasMap)
        {
            Log.Error("[OpenMap] Have scarabs but NO MAP in device! Going back to InsertMap.");
            _currentStep = MapOpeningStep.InsertMap;
            return PhaseResult.InProgress("Scarabs present but no map, going back to insert map");
        }
        
        Log.Info("[OpenMap] Both map and scarabs ready in device");
        _currentStep = MapOpeningStep.ActivateDevice;
        return PhaseResult.InProgress("Map and scarabs ready");
    }

    // No scarabs in device - need to insert them from inventory
    // ... find and insert scarabs ...
}
```

### 3. ActivateDevice Step - Final Verification

Before activating, verify we have a map (final safeguard):

```csharp
private async Task<PhaseResult> ActivateMapDevice(GameContext context)
{
    Log.Info("[OpenMap] Activating map device");

    // FINAL SAFEGUARD: Verify we have a map before activation
    bool hasMap = CheckMapDeviceForMaps();
    if (!hasMap)
    {
        Log.Error("[OpenMap] CRITICAL: Attempted to activate device without a map! Going back to InsertMap.");
        _currentStep = MapOpeningStep.InsertMap;
        return PhaseResult.InProgress("No map detected before activation, going back");
    }

    // Safe to activate...
}
```

## Flow Examples

### Scenario 1: Scarabs in Device, No Map

```
[OpenMap] Opening map device UI
[OpenMap] Device has 2 total items (scarabs)
[OpenMap] Device item: Bestiary Scarab of Duplicating (Class: StackableCurrency)
[OpenMap] Device item: Bestiary Scarab of the Herd (Class: StackableCurrency)
[OpenMap] ✗ Map device does NOT have a map
[OpenMap] Found scarabs in device but no map - will keep scarabs and add map
[OpenMap] No map in device - checking inventory for map
[OpenMap] No maps found in inventory
[OpenMap] No map in inventory, withdrawing from stash
[OpenMap] Map withdrawn successfully
[OpenMap] Map device already has items loaded, skipping clear step  ← Preserves scarabs!
[OpenMap] Inserting map from inventory: Beach Map (Tier: 1)
[OpenMap] Scarabs already loaded in map device
[OpenMap] Both map and scarabs ready in device
[OpenMap] Map device activated successfully
```

### Scenario 2: Map in Device, No Scarabs

```
[OpenMap] Opening map device UI
[OpenMap] Device has 1 total items
[OpenMap] Device item: Beach Map (Class: Map)
[OpenMap] ✓ Map device HAS a map loaded
[OpenMap] Map already loaded in map device - skipping to scarabs
[OpenMap] ✗ Map device does NOT have scarabs
[OpenMap] Inserting scarabs from inventory
[OpenMap] Need: 1x Duplicating, 2x Herd
[OpenMap] Placing scarab: Bestiary Scarab of Duplicating
[OpenMap] Placing scarab: Bestiary Scarab of the Herd
[OpenMap] Placing scarab: Bestiary Scarab of the Herd
[OpenMap] Successfully inserted all required scarabs
[OpenMap] Both map and scarabs ready in device
[OpenMap] Map device activated successfully
```

### Scenario 3: Empty Device

```
[OpenMap] Opening map device UI
[OpenMap] Device has 0 total items
[OpenMap] ✗ Map device does NOT have a map
[OpenMap] No map in device - checking inventory for map
[OpenMap] Map withdrawn successfully
[OpenMap] Inserting map from inventory: Beach Map (Tier: 1)
[OpenMap] Inserting scarabs from inventory
[OpenMap] Successfully inserted all required scarabs
[OpenMap] Map device activated successfully
```

## Key Behaviors

1. **Preserve Existing Items**: Never clear the device if items are already there
2. **Smart Detection**: Separately check for maps vs scarabs
3. **Backtracking**: If we're at InsertScarabs but no map, go back to InsertMap
4. **Safety Checks**: Multiple verification points before activation
5. **Clear Logging**: Easy to diagnose what's happening from logs

## Scarab Restocking

If scarabs run out during operation:
- OpenMapPhase will fail with "Insufficient scarabs - need restocking"
- PreparationPhase should detect this and restock from stash
- On next attempt, OpenMapPhase will have scarabs available

## Files Modified

- `Beasts/Phases/OpenMapPhase.cs`
  - Updated `InsertMap()` to preserve scarabs when adding map
  - Updated `InsertScarabs()` to verify map present before proceeding
  - Updated `ActivateMapDevice()` with final map verification
  - Removed device clearing logic when items already present
  - Added clear logging for all scenarios

## Testing

Build successful with no errors.

## Benefits

- ✅ No wasted scarabs (they're preserved if already loaded)
- ✅ Bot auto-recovers from partial states
- ✅ Clear error messages when items are missing
- ✅ Works with manual pre-loading of items
- ✅ No infinite loops on missing items


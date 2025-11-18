# Final Fix: Map Device Workflow - Complete Refactor

## Problem Summary
Bot was getting stuck in an infinite loop when opening the map device with scarabs already loaded but no map present. The device UI would close when going to stash and not reopen properly.

## Root Causes Identified

### 1. **Missing Device UI Reopening in InsertScarabs**
- When returning from stash withdrawal, the device UI is closed
- `InsertScarabs` was immediately checking for maps WITHOUT first ensuring the UI was open
- This caused `CheckMapDeviceForMaps()` to fail silently

### 2. **Attempt Counter Not Reset**
- When device opening failed and then succeeded on retry, `_stepAttempts` was never reset
- This caused elevated attempt counts to persist through the workflow

### 3. **Incorrect Loop Between Steps**
- `InsertScarabs` would detect scarabs but no map
- Go back to `InsertMap`
- `InsertMap` would see device open and check for map again
- Create infinite loop

## Complete Fix Applied

### 1. InsertMap Step (Lines 196-280)
```csharp
private async Task<PhaseResult> InsertMap(GameContext context)
{
    // MUST open device UI first to check what's inside
    bool mapDeviceOpen = LokiPoe.InGameState.MapDeviceUi.IsOpened;
    bool masterDeviceOpen = LokiPoe.InGameState.MasterDeviceUi.IsOpened;

    if (!mapDeviceOpen && !masterDeviceOpen)
    {
        // Open map device UI
        if (!await MapDeviceService.OpenMapDevice())
        {
            _stepAttempts++;
            if (_stepAttempts > 5)
                return PhaseResult.Failed("Failed to open map device");

            await Coroutine.Sleep(500);
            return PhaseResult.InProgress("Retrying map device opening...");
        }

        // ✅ FIXED: Reset attempts counter after success
        _stepAttempts = 0;
        await Coroutine.Sleep(300);
        return PhaseResult.InProgress("Waiting for device UI to load...");
    }

    // Check if map device already has a map loaded
    bool hasMapInDevice = CheckMapDeviceForMaps();

    if (hasMapInDevice)
    {
        // Map exists, move to scarabs
        _currentStep = MapOpeningStep.InsertScarabs;
        _stepAttempts = 0;
        return PhaseResult.InProgress("Map already in device");
    }

    // No map in device - need to get one
    var mapItem = FindMapInInventory();

    if (mapItem == null)
    {
        // NO MAP IN INVENTORY - Need to withdraw from stash
        // Close map device UI before going to stash
        if (LokiPoe.InGameState.MapDeviceUi.IsOpened || LokiPoe.InGameState.MasterDeviceUi.IsOpened)
        {
            LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
            await Coroutine.Sleep(500);
        }

        _currentStep = MapOpeningStep.WithdrawMap;
        _stepAttempts = 0;
        return PhaseResult.InProgress("No map in inventory, withdrawing from stash");
    }

    // Have map in inventory - insert it
    // ... insertion logic ...
}
```

### 2. InsertScarabs Step (Lines 289-380)
```csharp
private async Task<PhaseResult> InsertScarabs(GameContext context)
{
    // ✅ FIXED: MUST ensure device UI is open first
    bool mapDeviceOpen = LokiPoe.InGameState.MapDeviceUi.IsOpened;
    bool masterDeviceOpen = LokiPoe.InGameState.MasterDeviceUi.IsOpened;

    if (!mapDeviceOpen && !masterDeviceOpen)
    {
        // Device UI closed (probably came back from stash) - reopen it
        if (!await MapDeviceService.OpenMapDevice())
        {
            _stepAttempts++;
            if (_stepAttempts > 5)
                return PhaseResult.Failed("Failed to open map device");

            await Coroutine.Sleep(500);
            return PhaseResult.InProgress("Retrying map device opening...");
        }

        // Device opened successfully - reset attempts counter
        _stepAttempts = 0;
        await Coroutine.Sleep(300);
        return PhaseResult.InProgress("Waiting for device UI to load...");
    }

    // Device UI is open - now verify we have a map
    bool hasMap = CheckMapDeviceForMaps();
    if (!hasMap)
    {
        // No map in device - go back to InsertMap to get one
        _currentStep = MapOpeningStep.InsertMap;
        _stepAttempts = 0;
        return PhaseResult.InProgress("No map in device, going back to insert map");
    }

    // Map exists, now check for scarabs
    if (CheckMapDeviceForScarabs())
    {
        // Both map and scarabs present
        _currentStep = MapOpeningStep.ActivateDevice;
        _stepAttempts = 0;
        return PhaseResult.InProgress("Map and scarabs ready");
    }

    // No scarabs - check inventory
    // ... find scarabs logic ...

    if (duplicatingScarabs.Count < settings.MinDuplicatingScarabs ||
        herdScarabs.Count < settings.MinHerdScarabs)
    {
        // NO SCARABS IN INVENTORY - Need to withdraw from stash
        // Close map device UI before going to stash
        if (LokiPoe.InGameState.MapDeviceUi.IsOpened || LokiPoe.InGameState.MasterDeviceUi.IsOpened)
        {
            LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
            await Coroutine.Sleep(500);
        }

        _currentStep = MapOpeningStep.WithdrawScarabs;
        _stepAttempts = 0;
        return PhaseResult.InProgress("No scarabs in inventory, withdrawing from stash");
    }

    // Have scarabs in inventory - insert them
    // ... insertion logic ...
}
```

## Complete Workflow Now

### Scenario 1: Empty Device, No Items in Inventory
1. **MoveToDevice** → Navigate to map device
2. **InsertMap** → Opens device UI → No map found → No map in inventory
3. **WithdrawMap** → Closes device → Goes to stash → Withdraws map → Returns to MoveToDevice
4. **MoveToDevice** → At device
5. **InsertMap** → **Reopens device UI** → Inserts map → Go to InsertScarabs
6. **InsertScarabs** → **UI still open** → No scarabs found → No scarabs in inventory
7. **WithdrawScarabs** → Closes device → Goes to stash → Withdraws scarabs → Returns to InsertScarabs
8. **InsertScarabs** → **Reopens device UI** → Map present → Inserts scarabs → Go to ActivateDevice
9. **ActivateDevice** → Activates → Success ✅

### Scenario 2: Scarabs in Device, No Map
1. **MoveToDevice** → Navigate to map device
2. **InsertMap** → Opens device UI → No map found → Gets map from inventory/stash → Inserts map
3. **InsertScarabs** → **UI still open** → Map present → Scarabs present → Go to ActivateDevice
4. **ActivateDevice** → Activates → Success ✅

### Scenario 3: Map in Device, No Scarabs
1. **MoveToDevice** → Navigate to map device
2. **InsertMap** → Opens device UI → Map found → Go to InsertScarabs
3. **InsertScarabs** → **UI still open** → Map present → No scarabs → Gets scarabs from inventory/stash → Inserts scarabs
4. **ActivateDevice** → Activates → Success ✅

## Key Improvements

### ✅ Device UI Management
- **Always checks** if UI is open before any operations
- **Reopens** device UI when returning from stash
- **Closes** device UI before going to stash
- **Never assumes** UI state

### ✅ No Infinite Loops
- Clear progression through steps
- Each step validates prerequisites
- Proper state transitions
- Reset counters after success

### ✅ Preserves Existing Items
- Checks what's in device before acting
- Only gets what's missing
- Doesn't clear unnecessarily
- Works with pre-loaded items

### ✅ Error Handling
- Attempt counters with limits
- Graceful failures
- Clear error messages
- Prevents stuck states

## Validation

### No Conflicts Found:
- ✅ BeastRoutine.cs - Only closes UIs on startup (not during gameplay)
- ✅ ExitandStashPhase.cs - Properly closes UIs on exit
- ✅ PreparationPhase.cs - Properly closes UIs on exit
- ✅ MapDeviceService.cs - Closes blocking UIs before opening device (no conflicts)

### Build Status:
```
Build succeeded.
0 Error(s)
```

## Testing Checklist

Test these scenarios:
- [ ] Empty device, no items in inventory
- [ ] Scarabs in device, no map, no map in inventory
- [ ] Map in device, no scarabs, no scarabs in inventory
- [ ] Both map and scarabs in device
- [ ] Map in inventory, scarabs in device
- [ ] Scarabs in inventory, map in device
- [ ] Run out of maps in stash
- [ ] Run out of scarabs in stash

All scenarios should now work without getting stuck! 🎉


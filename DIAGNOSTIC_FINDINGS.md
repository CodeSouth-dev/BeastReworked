# DIAGNOSTIC FINDINGS

## Issue Analysis

After reviewing the code, I found that:

### ❌ MapDeviceHelper Does NOT Fix the Issue
The helper class I created **duplicates** existing OpenMapPhase functionality:
- `OpenMapPhase.CheckMapDeviceForMaps()` (line 863) = `MapDeviceHelper.IsMapInserted()`
- `OpenMapPhase.FindMapInInventory()` (line 707) = `MapDeviceHelper.FindMapInInventory()`
- Logic is IDENTICAL - no value added

### ✅ Actual Stuck Point Identified

**OpenMapPhase.InsertMap() Flow:**
```
Lines 209-228: Opens device → Returns "InProgress" → WAITS FOR NEXT TICK
Lines 206-232: Device now open → Checks for map
Lines 244-258: If no map → CLOSES UI → Goes to WithdrawMap step
```

**The existing logic ALREADY handles the workflow correctly!**

### 🐛 Root Cause Possibilities

The "stuck" issue is likely caused by ONE of these:

1. **Device Storage Has Cached Maps**
   - Map device can store maps in slots
   - Bot opens device with maps already loaded from previous run
   - `CheckMapDeviceForMaps()` returns `true` → skips to scarabs
   - Scarabs fail → gets stuck

2. **UI State Timing Issue**
   - Device UI opens but `InventoryControl` is `null` initially
   - `CheckMapDeviceForMaps()` returns `false` (error) → continues
   - Tries to insert map while device is still loading

3. **Item Detection Bug**
   - Device has an item that's not a map (scarab/fragment)
   - `Class.ToLower().Contains("map")` fails
   - Bot thinks device is empty when it's not

### 🔧 Real Fix Needed

Instead of MapDeviceHelper, we need:

**Option A: Clear Device Before Checking**
```csharp
private async Task<PhaseResult> InsertMap(GameContext context)
{
    if (!IsDeviceOpen())
    {
        await MapDeviceService.OpenMapDevice();
        return PhaseResult.InProgress("Opening device...");
    }

    // ADD THIS: Clear device first to ensure clean state
    await MapDeviceService.ClearMapDevice();

    // Now check inventory
    var mapItem = FindMapInInventory();
    if (mapItem == null)
    {
        // Close UI, go to stash
        ...
    }

    // Insert map
    await MapDeviceService.PlaceItemInDevice(mapItem);
}
```

**Option B: Add Better Diagnostics**
```csharp
// In CheckMapDeviceForMaps(), add detailed logging:
Log.InfoFormat("[OpenMap] Device state: Open={0}, InventoryControl={1}, Items={2}",
    deviceControl != null,
    deviceInventory != null,
    deviceInventory?.Items?.Count ?? -1);

foreach (var item in deviceInventory.Items)
{
    Log.InfoFormat("[OpenMap] Device item: Name={0}, Class={1}, Valid={2}",
        item?.Name ?? "null",
        item?.Class ?? "null",
        item?.IsValid ?? false);
}
```

**Option C: Add Null Safety**
```csharp
var deviceControl = mapDeviceOpen
    ? LokiPoe.InGameState.MapDeviceUi.InventoryControl
    : LokiPoe.InGameState.MasterDeviceUi.InventoryControl;

// ADD THIS:
if (deviceControl == null)
{
    Log.Warn("[OpenMap] Device control is null after UI opened - waiting...");
    await Coroutine.Sleep(500);
    return PhaseResult.InProgress("Waiting for device to initialize...");
}
```

## Recommendation

**DO NOT integrate MapDeviceHelper** - it adds bloat without fixing the issue.

**Instead:**
1. Add diagnostic logging to identify WHERE the bot gets stuck
2. Check if device has cached items from previous runs
3. Add `ClearMapDevice()` call before checking for maps
4. Add null safety checks for `InventoryControl`

Would you like me to:
- Add diagnostic logging to OpenMapPhase?
- Add ClearMapDevice() before map check?
- Check for other timing issues?

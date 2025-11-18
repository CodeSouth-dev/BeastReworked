# MapDeviceHelper Usage Guide

## Problem Solved
The bot was getting stuck when the map device UI was empty instead of:
1. Inserting a map if one is available in inventory
2. Going to stash to get one if no map in inventory

## New Helper Class: `MapDeviceHelper`

Location: `Beasts/Helpers/MapDeviceHelper.cs`

### Key Methods

#### 1. Status Checks
```csharp
// Check if map device UI is open
bool isOpen = MapDeviceHelper.IsMapDeviceOpen();

// Check if a map is inserted (device must be open)
bool hasMap = MapDeviceHelper.IsMapInserted();

// Get count of items in device
int itemCount = MapDeviceHelper.GetDeviceItemCount();
```

#### 2. Map Finding
```csharp
// Find a runnable map in inventory (respects tier/rotation settings)
Item map = MapDeviceHelper.FindMapInInventory();

// Find scarabs in inventory
Item[] scarabs = MapDeviceHelper.FindScarabsInInventory();
```

#### 3. Insertion Operations
```csharp
// Insert map from inventory into device (device must be open)
bool success = await MapDeviceHelper.InsertMapFromInventory();

// Insert scarabs from inventory (device must be open)
bool success = await MapDeviceHelper.InsertScarabsFromInventory();
```

#### 4. Stash Operations
```csharp
// Find stash in hideout
Chest stash = MapDeviceHelper.FindStashChest();

// Check if stash has maps (stash must be open)
bool hasMaps = MapDeviceHelper.HasMapsInStash();

// Withdraw map from stash (stash must be open, tab must be positioned on maps)
bool success = await MapDeviceHelper.WithdrawMapFromStash();
```

#### 5. High-Level Workflow
```csharp
// Ensure map is in device - returns status for decision making
MapDeviceStatus status = await MapDeviceHelper.EnsureMapInDevice();

// Status values:
// - MapDeviceStatus.MapReady          -> Map is in device, ready to activate
// - MapDeviceStatus.NeedMapFromStash  -> No map in inventory, need stash
// - MapDeviceStatus.DeviceNotOpen     -> Device UI not open
// - MapDeviceStatus.InsertionFailed   -> Failed to insert map
// - MapDeviceStatus.Error             -> Unexpected error
```

## How to Use in OpenMapPhase

### Example: InsertMap Step (Simplified)

**Before (Old Code):**
```csharp
private async Task<PhaseResult> InsertMap(GameContext context)
{
    // Open device
    if (!LokiPoe.InGameState.MapDeviceUi.IsOpened)
    {
        await MapDeviceService.OpenMapDevice();
        return PhaseResult.InProgress("Opening device...");
    }

    // Check if map in device (manual check)
    bool hasMapInDevice = CheckMapDeviceForMaps(); // Custom method
    if (hasMapInDevice)
    {
        _currentStep = MapOpeningStep.InsertScarabs;
        return PhaseResult.InProgress("Map already in device");
    }

    // Find map (manual check)
    var mapItem = FindMapInInventory(); // Custom method
    if (mapItem == null)
    {
        // Close device UI
        LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
        await Coroutine.Sleep(500);

        _currentStep = MapOpeningStep.WithdrawMap;
        return PhaseResult.InProgress("No map in inventory, withdrawing from stash");
    }

    // Insert map
    if (!await MapDeviceService.PlaceItemInDevice(mapItem))
    {
        return PhaseResult.Failed("Failed to place map");
    }

    _currentStep = MapOpeningStep.InsertScarabs;
    return PhaseResult.InProgress("Map inserted");
}
```

**After (New Code with Helper):**
```csharp
private async Task<PhaseResult> InsertMap(GameContext context)
{
    // Open device
    if (!MapDeviceHelper.IsMapDeviceOpen())
    {
        await MapDeviceService.OpenMapDevice();
        return PhaseResult.InProgress("Opening device...");
    }

    // Use high-level workflow method
    var status = await MapDeviceHelper.EnsureMapInDevice();

    switch (status)
    {
        case MapDeviceStatus.MapReady:
            Log.Info("[OpenMap] Map ready - moving to scarabs");
            _currentStep = MapOpeningStep.InsertScarabs;
            _stepAttempts = 0;
            return PhaseResult.InProgress("Map inserted");

        case MapDeviceStatus.NeedMapFromStash:
            Log.Info("[OpenMap] Need to fetch map from stash");
            // Close device UI before going to stash
            LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
            await Coroutine.Sleep(300);

            _currentStep = MapOpeningStep.WithdrawMap;
            _stepAttempts = 0;
            return PhaseResult.InProgress("Going to stash for map");

        case MapDeviceStatus.DeviceNotOpen:
            Log.Error("[OpenMap] Device unexpectedly closed");
            return PhaseResult.Failed("Device closed unexpectedly");

        case MapDeviceStatus.InsertionFailed:
            _stepAttempts++;
            if (_stepAttempts > 5)
            {
                return PhaseResult.Failed("Failed to insert map after 5 attempts");
            }
            await Coroutine.Sleep(500);
            return PhaseResult.InProgress($"Retrying insertion ({_stepAttempts}/5)");

        case MapDeviceStatus.Error:
        default:
            return PhaseResult.Failed("Error ensuring map in device");
    }
}
```

### Benefits of New Approach

1. **Cleaner Code**: Single method call instead of multiple checks
2. **Clear Status**: Enum return values make decision logic obvious
3. **No Duplication**: Reusable helper methods across different phases
4. **Better Error Handling**: Explicit status codes for all scenarios
5. **Fixes Stuck Issue**: `NeedMapFromStash` status triggers stash workflow

## Integration Steps

1. **Add using statement:**
```csharp
using Beasts.Helpers;
```

2. **Replace manual checks with helper methods:**
```csharp
// Old: bool hasMap = CheckMapDeviceForMaps();
// New:
bool hasMap = MapDeviceHelper.IsMapInserted();
```

3. **Replace custom FindMapInInventory:**
```csharp
// Old: var map = FindMapInInventory(); // Local method
// New:
var map = MapDeviceHelper.FindMapInInventory();
```

4. **Use EnsureMapInDevice() for workflow:**
```csharp
var status = await MapDeviceHelper.EnsureMapInDevice();
// Handle status with switch statement
```

## Testing Checklist

- [ ] Bot opens map device successfully
- [ ] Bot detects when map is already inserted
- [ ] Bot inserts map from inventory when device is empty
- [ ] Bot transitions to stash when no map in inventory
- [ ] Bot withdraws map from stash and returns to device
- [ ] Bot handles errors gracefully with retries
- [ ] Bot doesn't get stuck in empty device UI anymore

## API Reference (DreamPoeBot)

All methods use the correct DreamPoeBot API:
- `LokiPoe.InGameState.MapDeviceUi.IsOpened`
- `LokiPoe.InGameState.MasterDeviceUi.IsOpened` (for Heist)
- `LokiPoe.InGameState.MapDeviceUi.InventoryControl.Inventory.Items`
- `LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main)`
- `Item.MapTier` - Direct map tier property
- `MapDeviceService.PlaceItemInDevice()` - Existing service method

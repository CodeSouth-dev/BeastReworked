# Implementation Plan - Remaining TODOs

Based on reference code analysis, here are the actionable implementations prioritized by importance.

---

## High Priority (Blocking Functionality)

### 1. ✅ Portal Creation via Portal Gem
**Files**: ExitPhase.cs, ToHideoutPhase.cs, EmergencyRecoveryPhase.cs, ExitandStashPhase.cs

**Implementation**:
```csharp
private static async Task<bool> CreatePortalUsingGem()
{
    // Find portal gem skill
    var portalSkill = LokiPoe.InGameState.SkillBarHud.Skills
        .FirstOrDefault(s => s.Name != null && s.Name.ToLower().Contains("portal"));

    if (portalSkill == null)
    {
        Log.Warn("[Portal] No portal gem found on skill bar");
        return false;
    }

    if (!portalSkill.CanUse())
    {
        Log.Warn("[Portal] Portal gem cannot be used (cooldown/mana/etc)");
        return false;
    }

    LokiPoe.ProcessHookManager.ClearAllKeyStates();

    var useResult = LokiPoe.InGameState.SkillBarHud.Use(portalSkill.Slot, false);

    if (useResult != LokiPoe.InGameState.UseResult.None)
    {
        Log.ErrorFormat("[Portal] Failed to use portal gem: {0}", useResult);
        return false;
    }

    // Wait for portal to spawn
    await Coroutine.Sleep(500);

    Log.Info("[Portal] Portal created successfully via gem");
    return true;
}
```

**Status**: Can implement now ✅

---

### 2. ✅ Waypoint Travel to Hideout
**Files**: ToHideoutPhase.cs

**Implementation**:
```csharp
private static async Task<bool> TravelToHideoutViaWaypoint()
{
    // Find waypoint
    var waypoint = LokiPoe.ObjectManager.Objects
        .OfType<Waypoint>()
        .FirstOrDefault(w => w.IsValid && w.Distance < 100);

    if (waypoint == null)
    {
        Log.Warn("[Travel] No waypoint nearby");
        return false;
    }

    // Move to waypoint
    if (waypoint.Distance > 30)
    {
        PlayerMoverManager.Current.MoveTowards(waypoint.Position);
        await Coroutine.Sleep(100);
        return false; // Continue moving
    }

    // Interact with waypoint
    if (!await Coroutines.InteractWith(waypoint))
    {
        Log.Error("[Travel] Failed to interact with waypoint");
        return false;
    }

    // Wait for waypoint UI to open
    if (!await Wait.For(() => LokiPoe.InGameState.WorldUi.IsOpened, "waypoint UI", 100, 3000))
    {
        Log.Error("[Travel] Waypoint UI did not open");
        return false;
    }

    // TODO: Select hideout from waypoint UI
    // This requires discovering the waypoint UI API

    Log.Info("[Travel] Waypoint UI opened");
    return true;
}
```

**Status**: Partial - need waypoint UI API ⚠️

---

### 3. ⚠️ Map Placement in Device
**Files**: OpenMapPhase.cs

**Current Code**: Lines 510-514 have TODO for Item.Use()

**Alternative Implementation** (using FastMove):
```csharp
private async Task<bool> PlaceMapInDevice(Item mapItem)
{
    if (mapItem == null || !mapItem.IsValid)
    {
        Log.Error("[OpenMap] Invalid map item");
        return false;
    }

    // Ensure map device is open
    if (!LokiPoe.InGameState.MapDeviceUi.IsOpened)
    {
        Log.Error("[OpenMap] Map device is not open");
        return false;
    }

    // Get current item count in device
    var deviceControl = LokiPoe.InGameState.MapDeviceUi.InventoryControl;
    if (deviceControl == null)
    {
        Log.Error("[OpenMap] Cannot access map device inventory");
        return false;
    }

    var oldCount = deviceControl.Inventory?.Items?.Count ?? 0;

    // Use FastMove to place map into device
    LokiPoe.ProcessHookManager.ClearAllKeyStates();

    var result = LokiPoe.InGameState.InventoryUi.InventoryControl_Main.FastMove(mapItem.LocalId);

    if (result != FastMoveResult.None)
    {
        Log.ErrorFormat("[OpenMap] Failed to place map: {0}", result);
        return false;
    }

    // Wait for item to appear in device
    if (!await Wait.For(() =>
    {
        var newCount = deviceControl.Inventory?.Items?.Count ?? 0;
        return newCount == oldCount + 1;
    }, "map placement", 100, 3000))
    {
        Log.Error("[OpenMap] Map did not appear in device");
        return false;
    }

    Log.Info("[OpenMap] Map placed in device successfully");
    return true;
}
```

**Status**: Can implement now ✅

---

### 4. ✅ Map Device Activation
**Files**: OpenMapPhase.cs

**Implementation** (from reference code):
```csharp
private async Task<bool> ActivateMapDevice()
{
    if (!LokiPoe.InGameState.MapDeviceUi.IsOpened)
    {
        Log.Error("[OpenMap] Map device is not open");
        return false;
    }

    LokiPoe.ProcessHookManager.ClearAllKeyStates();

    // Activate the map device
    var activated = LokiPoe.InGameState.MapDeviceUi.Activate();

    if (activated != LokiPoe.InGameState.ActivateResult.None)
    {
        Log.ErrorFormat("[OpenMap] Failed to activate device: {0}", activated);
        return false;
    }

    // Wait for device to close (indicates activation)
    if (!await Wait.For(() => !LokiPoe.InGameState.MapDeviceUi.IsOpened,
        "map device activation", 100, 5000))
    {
        Log.Error("[OpenMap] Map device did not close after activation");
        return false;
    }

    Log.Info("[OpenMap] Map device activated successfully");
    return true;
}
```

**Status**: Can implement now ✅

---

## Medium Priority (Convenience Features)

### 5. Portal Scroll Withdrawal from Currency Tab
**Files**: OpenMapPhase.cs:187

**Implementation**:
```csharp
private async Task<bool> WithdrawPortalScrolls(int targetAmount)
{
    // Ensure stash is open
    if (!LokiPoe.InGameState.StashUi.IsOpened)
    {
        Log.Error("[Stash] Stash is not open");
        return false;
    }

    // Switch to currency tab
    if (!await SwitchToCurrencyTab())
    {
        Log.Error("[Stash] Failed to switch to currency tab");
        return false;
    }

    // Access portal scroll stack in currency tab
    var portalScrollControl = LokiPoe.InGameState.StashUi.CurrencyTab.PortalScroll;

    if (portalScrollControl == null)
    {
        Log.Error("[Stash] Cannot access portal scrolls in currency tab");
        return false;
    }

    // TODO: Discover API to withdraw specific amount
    // Possible: portalScrollControl.Withdraw(amount) or similar

    Log.Warn("[Stash] Portal scroll withdrawal API not yet discovered");
    return false;
}
```

**Status**: Need currency tab withdrawal API ⚠️

---

### 6. Stashing Items
**Files**: ExitandStashPhase.cs:327

**Current Implementation** - Already exists in StashingPhase.cs:194!
```csharp
// This already works correctly:
LokiPoe.ProcessHookManager.ClearAllKeyStates();
var fastMoveResult = LokiPoe.InGameState.StashUi.FastMoveFromInventory(x, y);
```

**Status**: Already implemented ✅

---

## Low Priority (Advanced Features)

### 7. Currency Exchange UI
**Files**: OpenMapPhase.cs:286

**Status**: Low priority, complex UI automation ⏸️

### 8. Heist Locker Interaction
**Files**: ExitandStashPhase.cs:428

**Status**: Low priority, specific mechanic ⏸️

### 9. Beast Net Throwing
**Files**: ComprehensiveFarmingPhase.cs:258

**Status**: Low priority, core mechanic works without it ⏸️

### 10. Zone Coverage Algorithm
**Files**: ExplorationPhase.cs:143

**Status**: Enhancement, current movement works ⏸️

---

## Implementation Order

1. ✅ **Portal Creation via Gem** - Critical for map workflow
2. ✅ **Map Placement in Device** - Critical for map opening
3. ✅ **Map Device Activation** - Critical for map opening
4. ⚠️ **Waypoint Travel** - Fallback for portal
5. ⚠️ **Portal Scroll Withdrawal** - Nice to have, manual works
6. ⏸️ Lower priority items - Defer until core functionality tested

---

## Next Steps

1. Create helper method file for common operations:
   - `Beasts/Services/PortalService.cs` - Portal creation
   - `Beasts/Services/MapDeviceService.cs` - Map device interaction

2. Update phase files to use new services:
   - ExitPhase.cs - Use PortalService
   - ToHideoutPhase.cs - Use PortalService
   - OpenMapPhase.cs - Use MapDeviceService
   - EmergencyRecoveryPhase.cs - Use PortalService

3. Test compilation after implementation

4. Commit and push changes

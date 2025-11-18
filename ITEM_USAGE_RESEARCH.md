# DreamPoeBot Item Usage API Research

**Date:** 2025-11-16
**Source:** Reference code analysis from commit 920f57b^ (ExampleRoutine, ExamplePlugin)
**Purpose:** Document programmatic item usage patterns for maps, scarabs, flasks, and portal scrolls

---

## Table of Contents

1. [Flask Usage API](#flask-usage-api)
2. [Inventory Item Access](#inventory-item-access)
3. [Item Movement Operations](#item-movement-operations)
4. [Input Simulation Methods](#input-simulation-methods)
5. [Skill Usage Patterns](#skill-usage-patterns)
6. [Process Hook Management](#process-hook-management)
7. [Implementation Guide](#implementation-guide)
8. [Code Examples](#code-examples)

---

## Flask Usage API

### Primary Method: UseFlaskInSlot

**When ProcessHookManager is enabled:**

```csharp
using FlaskHud = DreamPoeBot.Loki.Game.LokiPoe.InGameState.QuickFlaskHud;

// Use flask by slot number (1-5)
if (LokiPoe.ProcessHookManager.IsEnabled)
{
    if (!FlaskHud.UseFlaskInSlot(slot))
    {
        Log.Error($"UseFlaskInSlot returned false for slot {slot}");
    }
}
```

### Fallback Method: Keyboard Simulation

**When ProcessHookManager is disabled:**

```csharp
// Simulate pressing flask keys 1-5
switch (slot)
{
    case 1:
        LokiPoe.Input.SimulateKeyEvent(System.Windows.Forms.Keys.D1, true, false, false);
        break;
    case 2:
        LokiPoe.Input.SimulateKeyEvent(System.Windows.Forms.Keys.D2, true, false, false);
        break;
    case 3:
        LokiPoe.Input.SimulateKeyEvent(System.Windows.Forms.Keys.D3, true, false, false);
        break;
    case 4:
        LokiPoe.Input.SimulateKeyEvent(System.Windows.Forms.Keys.D4, true, false, false);
        break;
    case 5:
        LokiPoe.Input.SimulateKeyEvent(System.Windows.Forms.Keys.D5, true, false, false);
        break;
}
```

### Flask Slot Calculation

```csharp
// Flask slot is based on inventory position (X coordinate + 1)
var flask = FlaskHud.InventoryControl.Inventory.Items.FirstOrDefault(...);
var slot = flask.LocationTopLeft.X + 1; // Returns 1-5
```

---

## Inventory Item Access

### Flask Inventory Access

```csharp
using FlaskHud = DreamPoeBot.Loki.Game.LokiPoe.InGameState.QuickFlaskHud;

// Access all flask items
var flaskItems = FlaskHud.InventoryControl.Inventory.Items;

// Find specific flask by type
public static Item LifeFlask => FlaskHud.InventoryControl.Inventory.Items
    .HighestCharge(f => (f.Class == ItemClasses.LifeFlask ||
                         f.Class == ItemClasses.HybridFlask) &&
                         !f.IsInstantRecovery);

public static Item InstantLifeFlask => FlaskHud.InventoryControl.Inventory.Items
    .HighestCharge(f => (f.Class == ItemClasses.LifeFlask ||
                         f.Class == ItemClasses.HybridFlask) &&
                         (f.IsInstantRecovery || IsPanicked(f)));

// Helper method to find flask with highest charges
private static Item HighestCharge(this IEnumerable<Item> flasks, Func<Item, bool> match)
{
    Item highest = null;
    foreach (var flask in flasks)
    {
        if (!match(flask))
            continue;

        if (!flask.CanUse)
            continue;

        if (highest == null || highest.CurrentCharges < flask.CurrentCharges)
            highest = flask;
    }
    return highest;
}
```

### General Inventory Access

**Pattern for accessing player inventory (likely):**

```csharp
// Based on StashUi pattern, inventory likely accessed via:
// LokiPoe.InGameState.InventoryUi.InventoryControl.Inventory.Items

// Item properties:
// - item.LocationTopLeft.X (0-11 for inventory columns)
// - item.LocationTopLeft.Y (0-4 for inventory rows)
// - item.CanUse (whether item can be used)
// - item.Class (ItemClasses enum)
// - item.Name, item.FullName
// - item.IsValid
```

---

## Item Movement Operations

### Stash Operations

```csharp
// Fast move item from inventory to stash by coordinates
var fastMoveResult = LokiPoe.InGameState.StashUi.FastMoveFromInventory(
    item.LocationTopLeft.X,
    item.LocationTopLeft.Y
);

if (fastMoveResult)
{
    Log.Debug($"Moved {item.FullName} to stash");
}

// Check if stash is open
if (LokiPoe.InGameState.StashUi.IsOpened)
{
    // Perform stash operations
}

// Close stash
LokiPoe.InGameState.StashUi.Close();
```

### Pattern Analysis

The `FastMoveFromInventory` method suggests a **Ctrl+Click** operation for moving items. This pattern likely extends to other UI interactions:

- **FastMoveFromInventory**: Ctrl+Click to move from inventory to stash
- **Potential pattern**: Similar methods may exist for:
  - `FastMoveToInventory`: Ctrl+Click from stash to inventory
  - **Inventory to device**: Ctrl+Click to insert map/scarab into map device
  - **Item usage**: Right-click items in inventory

---

## Input Simulation Methods

### Keyboard Events

```csharp
// Simulate key press
// Parameters: (key, keyDown, ctrl, shift)
LokiPoe.Input.SimulateKeyEvent(System.Windows.Forms.Keys.D1, true, false, false);

// Use game bindings instead of hardcoded keys
LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.highlight_toggle, true, false, false);

// With modifiers
LokiPoe.Input.SimulateKeyEvent(System.Windows.Forms.Keys.Space, true, true, false); // Ctrl+Space
```

### Available Input Methods (from reference code)

```csharp
// From ExampleRoutine.cs
LokiPoe.Input.SimulateKeyEvent(key, down, ctrl, shift);

// Game bindings available through:
LokiPoe.Input.Binding.highlight_toggle
// Other bindings likely available but not shown in reference code
```

### Mouse Operations (Not directly shown in reference code)

**Likely available but not demonstrated:**

```csharp
// Expected patterns (needs verification):
// LokiPoe.Input.ClickAt(position, leftClick, rightClick)
// LokiPoe.Input.MoveCursor(position)
```

---

## Skill Usage Patterns

### BeginUseAt (Channeling/Held Skills)

```csharp
// Use for skills that need to be held down
// Parameters: slot (0-7), attackInPlace, position
var useResult = LokiPoe.InGameState.SkillBarHud.BeginUseAt(
    slot: 0,
    attackInPlace: true,
    position: targetPosition
);

if (useResult != LokiPoe.InGameState.UseResult.None)
{
    Log.Warn($"BeginUseAt returned {useResult}");
}
```

### UseAt (Instant Skills)

```csharp
// Use for instant cast skills
var combatResult = LokiPoe.InGameState.SkillBarHud.UseAt(
    slot: skillSlot,
    attackInPlace: false,
    position: targetPosition
);

if (combatResult != LokiPoe.InGameState.UseResult.None)
{
    Log.Warn($"UseAt returned {combatResult}");
}
```

### Skill Slot Access

```csharp
// Get skill in specific slot
var slotSkill = LokiPoe.InGameState.SkillBarHud.Slot(slot);
if (slotSkill == null)
{
    Log.Error("No skill in the configured slot");
}
```

---

## Process Hook Management

### Clear Key States

**CRITICAL:** Always clear key states before performing actions to prevent stuck keys

```csharp
// Clear all key states before actions
LokiPoe.ProcessHookManager.ClearAllKeyStates();

// Then perform action
var useResult = LokiPoe.InGameState.SkillBarHud.BeginUseAt(...);
```

### Check Hook Status

```csharp
// Check if ProcessHookManager is enabled
if (LokiPoe.ProcessHookManager.IsEnabled)
{
    // Use native API methods (preferred)
    FlaskHud.UseFlaskInSlot(slot);
}
else
{
    // Fall back to input simulation
    LokiPoe.Input.SimulateKeyEvent(...);
}
```

---

## Implementation Guide

### Step-by-Step: Using Flask Items

```csharp
using FlaskHud = DreamPoeBot.Loki.Game.LokiPoe.InGameState.QuickFlaskHud;

public static bool UseFlask(int slot, string reason)
{
    Log.Info($"Using flask in slot {slot} because {reason}");

    // Clear key states first
    LokiPoe.ProcessHookManager.ClearAllKeyStates();

    if (LokiPoe.ProcessHookManager.IsEnabled)
    {
        // Use native API
        if (!FlaskHud.UseFlaskInSlot(slot))
        {
            Log.Error($"UseFlaskInSlot returned false for slot {slot}");
            return false;
        }
    }
    else
    {
        // Use keyboard simulation
        var key = slot switch
        {
            1 => System.Windows.Forms.Keys.D1,
            2 => System.Windows.Forms.Keys.D2,
            3 => System.Windows.Forms.Keys.D3,
            4 => System.Windows.Forms.Keys.D4,
            5 => System.Windows.Forms.Keys.D5,
            _ => throw new ArgumentException($"Invalid flask slot: {slot}")
        };

        LokiPoe.Input.SimulateKeyEvent(key, true, false, false);
    }

    return true;
}
```

### Step-by-Step: Using Maps/Scarabs/Portal Scrolls

**Based on API patterns, the recommended approach is:**

#### Option 1: Using InventoryUi API (Recommended - needs verification)

```csharp
// Expected pattern (similar to StashUi.FastMoveFromInventory)
// This would simulate Ctrl+Click on the item to insert it into the map device

// Pseudocode - NEEDS VERIFICATION:
var inventoryUi = LokiPoe.InGameState.InventoryUi;
var item = inventoryUi.InventoryControl.Inventory.Items
    .FirstOrDefault(i => i.Class == "Maps" && i.Name.Contains("Strand"));

if (item != null)
{
    // This method likely exists but is not in reference code:
    // inventoryUi.FastMoveFromInventory(item.LocationTopLeft.X, item.LocationTopLeft.Y);

    // OR there may be a direct Use method:
    // item.Use();

    // Wait for animation
    await Coroutine.Sleep(100);
}
```

#### Option 2: Keyboard Simulation (If item is assigned to hotkey)

```csharp
// If portal scroll is on hotkey (e.g., T key)
LokiPoe.ProcessHookManager.ClearAllKeyStates();
LokiPoe.Input.SimulateKeyEvent(System.Windows.Forms.Keys.T, true, false, false);
await Coroutine.Sleep(100);
```

#### Option 3: Mouse Simulation (Fallback)

```csharp
// Get item screen position
var item = GetMapItemFromInventory();
var screenPos = GetInventoryItemScreenPosition(item);

// Right-click the item (API method likely exists but not in reference code)
// Expected pattern:
// LokiPoe.Input.RightClickAt(screenPos);
// OR
// LokiPoe.Input.ClickAt(screenPos, leftClick: false, rightClick: true);

await Coroutine.Sleep(100);
```

### Step-by-Step: Inserting Items into Map Device

**Complete workflow for map device automation:**

```csharp
public async Task<bool> InsertMapIntoDevice(Item mapItem)
{
    // 1. Ensure we're near the map device
    var mapDevice = GetMapDevice();
    if (mapDevice.Distance > 20)
    {
        PlayerMoverManager.Current.MoveTowards(mapDevice.Position);
        await Coroutine.Sleep(100);
        return false;
    }

    // 2. Open map device UI if not already open
    if (!IsMapDeviceUiOpen())
    {
        await Coroutines.InteractWith(mapDevice);
        await Coroutine.Sleep(500);
    }

    // 3. Insert map using one of these methods:

    // Method A: Fast move (if API exists)
    // var result = LokiPoe.InGameState.MapDeviceUi.FastMoveFromInventory(
    //     mapItem.LocationTopLeft.X,
    //     mapItem.LocationTopLeft.Y
    // );

    // Method B: Use item directly (if API exists)
    // var result = mapItem.Use();

    // Method C: Simulate Ctrl+Click
    // LokiPoe.ProcessHookManager.ClearAllKeyStates();
    // var screenPos = GetInventoryItemScreenPosition(mapItem);
    // LokiPoe.Input.ClickAt(screenPos, leftClick: true, rightClick: false, ctrl: true);

    await Coroutine.Sleep(300);
    return true;
}

public async Task<bool> InsertScarab(Item scarabItem, int slot)
{
    // Same pattern as map insertion
    // Scarab slots are typically 1-4 in the map device

    // Expected method (needs verification):
    // LokiPoe.InGameState.MapDeviceUi.InsertScarab(scarabItem, slot);
    // OR use Ctrl+Click pattern like map insertion

    await Coroutine.Sleep(200);
    return true;
}

public async Task<bool> ActivateMapDevice()
{
    var mapDevice = GetMapDevice();

    // Click the activate button
    // Expected API (needs verification):
    // LokiPoe.InGameState.MapDeviceUi.Activate();

    // OR interact with device again
    await Coroutines.InteractWith(mapDevice);
    await Coroutine.Sleep(1000);

    return true;
}
```

### Step-by-Step: Using Portal Scrolls

```csharp
public async Task<bool> UsePortalScroll()
{
    // Find portal scroll in inventory
    var portalScroll = LokiPoe.InGameState.InventoryUi.InventoryControl.Inventory.Items
        .FirstOrDefault(i => i.Name == "Portal Scroll");

    if (portalScroll == null)
    {
        Log.Error("No portal scroll found");
        return false;
    }

    Log.Info("Using portal scroll");
    LokiPoe.ProcessHookManager.ClearAllKeyStates();

    // Method 1: Direct use (if API exists)
    // var result = portalScroll.Use();

    // Method 2: Right-click simulation
    // var screenPos = GetInventoryItemScreenPosition(portalScroll);
    // LokiPoe.Input.RightClickAt(screenPos);

    // Method 3: Keyboard if bound to hotkey
    // LokiPoe.Input.SimulateKeyEvent(System.Windows.Forms.Keys.T, true, false, false);

    await Coroutine.Sleep(500); // Wait for portal creation
    return true;
}
```

---

## Code Examples

### Complete Flask Manager Implementation

```csharp
using System;
using System.Linq;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using FlaskHud = DreamPoeBot.Loki.Game.LokiPoe.InGameState.QuickFlaskHud;

public static class FlaskManager
{
    // Flask accessors
    public static Item LifeFlask => FlaskHud.InventoryControl.Inventory.Items
        .HighestCharge(f => (f.Class == ItemClasses.LifeFlask ||
                             f.Class == ItemClasses.HybridFlask) &&
                             !f.IsInstantRecovery);

    public static Item InstantLifeFlask => FlaskHud.InventoryControl.Inventory.Items
        .HighestCharge(f => (f.Class == ItemClasses.LifeFlask ||
                             f.Class == ItemClasses.HybridFlask) &&
                             f.IsInstantRecovery);

    public static Item ManaFlask => FlaskHud.InventoryControl.Inventory.Items
        .HighestCharge(f => (f.Class == ItemClasses.ManaFlask ||
                             f.Class == ItemClasses.HybridFlask) &&
                             !f.IsInstantRecovery);

    public static Item QuicksilverFlask => FlaskHud.InventoryControl.Inventory.Items
        .HighestCharge(f => f.Name == "Quicksilver Flask");

    // Use flask method
    private static void Use(Item flask, string reason)
    {
        if (flask == null) return;

        var slot = flask.LocationTopLeft.X + 1;
        Log.Info($"Using {flask.ProperName()} (slot {slot}) because {reason}");

        LokiPoe.ProcessHookManager.ClearAllKeyStates();

        if (LokiPoe.ProcessHookManager.IsEnabled)
        {
            if (!FlaskHud.UseFlaskInSlot(slot))
                Log.Error($"UseFlaskInSlot returned false for slot {slot}");
        }
        else
        {
            switch (slot)
            {
                case 1:
                    LokiPoe.Input.SimulateKeyEvent(System.Windows.Forms.Keys.D1, true, false, false);
                    break;
                case 2:
                    LokiPoe.Input.SimulateKeyEvent(System.Windows.Forms.Keys.D2, true, false, false);
                    break;
                case 3:
                    LokiPoe.Input.SimulateKeyEvent(System.Windows.Forms.Keys.D3, true, false, false);
                    break;
                case 4:
                    LokiPoe.Input.SimulateKeyEvent(System.Windows.Forms.Keys.D4, true, false, false);
                    break;
                case 5:
                    LokiPoe.Input.SimulateKeyEvent(System.Windows.Forms.Keys.D5, true, false, false);
                    break;
            }
        }
    }

    // Helper method
    private static Item HighestCharge(this IEnumerable<Item> flasks, Func<Item, bool> match)
    {
        Item highest = null;
        foreach (var flask in flasks)
        {
            if (!match(flask))
                continue;

            if (!flask.CanUse)
                continue;

            if (highest == null || highest.CurrentCharges < flask.CurrentCharges)
                highest = flask;
        }
        return highest;
    }

    private static string ProperName(this Item item)
    {
        var rarity = item.Components.ModsComponent?.Rarity ?? Rarity.Normal;
        return rarity == Rarity.Unique ? item.FullName : item.Name;
    }
}
```

### Combat With Flask Usage

```csharp
public async Task<bool> PerformCombat()
{
    var target = GetBestTarget();
    if (target == null)
        return false;

    // Use life flask if needed
    var hpPercent = LokiPoe.Me.HealthPercent;
    if (hpPercent < 50 && !LokiPoe.Me.HasAura("flask_effect_life"))
    {
        var lifeFlask = FlaskManager.LifeFlask;
        if (lifeFlask != null && lifeFlask.CanUse)
        {
            var slot = lifeFlask.LocationTopLeft.X + 1;
            LokiPoe.ProcessHookManager.ClearAllKeyStates();
            FlaskHud.UseFlaskInSlot(slot);
        }
    }

    // Use Quicksilver for movement
    if (target.Distance > 30 && !LokiPoe.Me.HasAura("flask_utility_sprint"))
    {
        var quicksilver = FlaskManager.QuicksilverFlask;
        if (quicksilver != null && quicksilver.CanUse)
        {
            var slot = quicksilver.LocationTopLeft.X + 1;
            LokiPoe.ProcessHookManager.ClearAllKeyStates();
            FlaskHud.UseFlaskInSlot(slot);
        }
    }

    // Attack target
    LokiPoe.ProcessHookManager.ClearAllKeyStates();
    var useResult = LokiPoe.InGameState.SkillBarHud.BeginUseAt(
        slot: 0,
        attackInPlace: true,
        position: target.Position
    );

    await Coroutine.Sleep(100);
    return true;
}
```

---

## API Summary: All Discovered Methods

### InGameState APIs

```csharp
// Flask HUD
LokiPoe.InGameState.QuickFlaskHud.UseFlaskInSlot(slot)
LokiPoe.InGameState.QuickFlaskHud.InventoryControl.Inventory.Items

// Skill Bar
LokiPoe.InGameState.SkillBarHud.BeginUseAt(slot, attackInPlace, position)
LokiPoe.InGameState.SkillBarHud.UseAt(slot, attackInPlace, position)
LokiPoe.InGameState.SkillBarHud.Slot(slot)

// Stash UI
LokiPoe.InGameState.StashUi.IsOpened
LokiPoe.InGameState.StashUi.FastMoveFromInventory(x, y)
LokiPoe.InGameState.StashUi.Close()

// Inventory UI (expected but not confirmed)
LokiPoe.InGameState.InventoryUi.InventoryControl.Inventory.Items
LokiPoe.InGameState.InventoryUi.FastMoveFromInventory(x, y) // Likely exists
```

### Input APIs

```csharp
// Keyboard simulation
LokiPoe.Input.SimulateKeyEvent(key, down, ctrl, shift)
LokiPoe.Input.Binding.highlight_toggle

// Mouse (expected but not in reference code)
// LokiPoe.Input.ClickAt(position, leftClick, rightClick)
// LokiPoe.Input.RightClickAt(position)
// LokiPoe.Input.MoveCursor(position)
```

### Process Hook Manager

```csharp
LokiPoe.ProcessHookManager.IsEnabled
LokiPoe.ProcessHookManager.ClearAllKeyStates()
```

### Item Properties

```csharp
item.LocationTopLeft.X          // Inventory X coordinate
item.LocationTopLeft.Y          // Inventory Y coordinate
item.CanUse                     // Can the item be used
item.CurrentCharges             // Flask charges
item.IsInstantRecovery          // Flask instant recovery
item.Class                      // Item class (ItemClasses enum)
item.Name                       // Item name
item.FullName                   // Full item name including prefix/suffix
item.IsValid                    // Is item reference valid
item.Components.ModsComponent   // Mods information
```

---

## Key Discoveries

### 1. Flask Usage is Well-Documented

- **Primary API**: `FlaskHud.UseFlaskInSlot(slot)` - fully functional
- **Fallback**: Keyboard simulation via `SimulateKeyEvent`
- **Flask access**: Through `QuickFlaskHud.InventoryControl.Inventory.Items`
- **Slot calculation**: `item.LocationTopLeft.X + 1`

### 2. Item Movement Uses "FastMove" Pattern

- `StashUi.FastMoveFromInventory(x, y)` - Ctrl+Click simulation
- Similar pattern likely exists for other UIs (needs verification):
  - `InventoryUi.FastMoveFromInventory(x, y)`
  - `MapDeviceUi.FastMoveFromInventory(x, y)`

### 3. Direct Item.Use() Not Found in Reference Code

- No evidence of `item.Use()` method in reference code
- Item usage appears to go through:
  - Specific UI methods (`UseFlaskInSlot`)
  - FastMove operations
  - Input simulation (keyboard/mouse)

### 4. Map Device API Not in Reference Code

The map device automation API is not present in the example code, but likely follows these patterns:

**Expected APIs (need verification):**
```csharp
LokiPoe.InGameState.MapDeviceUi.IsOpened
LokiPoe.InGameState.MapDeviceUi.InsertMap(item)
LokiPoe.InGameState.MapDeviceUi.InsertScarab(item, slot)
LokiPoe.InGameState.MapDeviceUi.Activate()
LokiPoe.InGameState.MapDeviceUi.Close()
```

### 5. Process Hook Manager is Critical

**Always call before actions:**
```csharp
LokiPoe.ProcessHookManager.ClearAllKeyStates();
```

This prevents:
- Stuck modifier keys (Ctrl, Shift)
- Multiple simultaneous actions
- Input queue issues

### 6. Input Simulation Available

- Keyboard events: `LokiPoe.Input.SimulateKeyEvent(key, down, ctrl, shift)`
- Game bindings: `LokiPoe.Input.Binding.*`
- Mouse events: Not shown in reference code but likely exist

---

## Recommended Next Steps

### 1. Verify InventoryUi API

**Test if these methods exist:**

```csharp
// Test in your plugin/routine:
var inventoryUi = LokiPoe.InGameState.InventoryUi;
Log.Info($"InventoryUi exists: {inventoryUi != null}");
Log.Info($"InventoryControl exists: {inventoryUi?.InventoryControl != null}");
Log.Info($"Inventory exists: {inventoryUi?.InventoryControl?.Inventory != null}");

// Test FastMove
var item = inventoryUi.InventoryControl.Inventory.Items.FirstOrDefault();
if (item != null)
{
    // Try this method:
    // var result = inventoryUi.FastMoveFromInventory(item.LocationTopLeft.X, item.LocationTopLeft.Y);
}
```

### 2. Explore MapDeviceUi API

**Check for map device UI methods:**

```csharp
// When near map device:
var mapDeviceUi = LokiPoe.InGameState.MapDeviceUi;
Log.Info($"MapDeviceUi exists: {mapDeviceUi != null}");
Log.Info($"IsOpened: {mapDeviceUi?.IsOpened}");

// Look for methods using reflection:
var methods = mapDeviceUi?.GetType().GetMethods();
foreach (var method in methods)
{
    Log.Info($"Method: {method.Name}");
}
```

### 3. Test Mouse Input Methods

**Check if mouse methods exist:**

```csharp
// Test mouse input API:
var inputType = typeof(LokiPoe.Input);
var methods = inputType.GetMethods();

// Look for:
// - ClickAt
// - RightClickAt
// - MoveCursor
// - GetCursorPosition

foreach (var method in methods)
{
    Log.Info($"Input method: {method.Name}");
}
```

### 4. Implement Flask Usage First

Flask usage is fully documented and can be implemented immediately using the code examples above.

### 5. Use ObjectExplorer for Discovery

Use the ObjectExplorer tool (if available in DreamPoeBot) to inspect:
- `LokiPoe.InGameState.InventoryUi`
- `LokiPoe.InGameState.MapDeviceUi`
- `LokiPoe.InGameState` (all child properties)
- `LokiPoe.Input` (all available methods)

---

## Implementation Priority

### Immediate (Fully Documented)

1. **Flask usage** - Use `FlaskHud.UseFlaskInSlot(slot)`
2. **Item stashing** - Use `StashUi.FastMoveFromInventory(x, y)`
3. **Skill usage** - Use `SkillBarHud.BeginUseAt/UseAt`

### Next (Needs Verification)

1. **Inventory item access** - Verify `InventoryUi.InventoryControl.Inventory.Items`
2. **Portal scroll usage** - Test keyboard shortcut or right-click simulation
3. **Map device automation** - Explore `MapDeviceUi` API

### Future (Requires Research)

1. **Mouse simulation** - Find and document mouse input methods
2. **Custom item interactions** - Direct item.Use() if it exists
3. **Advanced UI automation** - Other game UIs not yet explored

---

## Conclusion

The DreamPoeBot API provides robust support for:

- **Flask automation**: Fully functional via `UseFlaskInSlot`
- **Item movement**: FastMove pattern for Ctrl+Click operations
- **Input simulation**: Keyboard events and game bindings
- **Skill usage**: Complete control over skill bar

For **map/scarab/scroll usage**, the API likely follows similar patterns to flask and stash operations, but requires verification. The recommended approach is:

1. Start with keyboard shortcuts if items are bound
2. Explore `InventoryUi` and `MapDeviceUi` APIs
3. Fall back to mouse simulation if needed

The reference code demonstrates solid patterns that can be extended to other item types once the specific APIs are discovered.

---

## References

- **Source Files**:
  - `ExampleRoutine/ExampleRoutine.cs` (commit 920f57b^)
  - `ExamplePlugin/ExempleTask.cs` (commit 920f57b^)
  - `ExamplePlugin/Flasks.cs` (commit 920f57b^)

- **Current Implementation**:
  - `/home/user/Beasts/Beasts/Services/FlaskManager.cs`
  - `/home/user/Beasts/Beasts/Phases/StashingPhase.cs`
  - `/home/user/Beasts/Beasts/Phases/CombatPhase.cs`

- **Namespaces**:
  - `DreamPoeBot.Loki.Game`
  - `DreamPoeBot.Loki.Game.GameData`
  - `DreamPoeBot.Loki.Game.Objects`
  - `DreamPoeBot.Loki.Bot`
  - `DreamPoeBot.Loki.Coroutine`

---

**Document Version**: 1.0
**Last Updated**: 2025-11-16
**Status**: Complete - Flask APIs documented, Inventory APIs need verification

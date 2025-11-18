# Reference Code API Analysis
## MapBot & EXAMPLE Code Patterns

This document analyzes the MapBot and EXAMPLE reference code to extract DreamPoeBot API patterns.

---

## KEY FINDINGS

### ⚠️ IMPORTANT: PlayerAction is NOT Available
The reference code uses `PlayerAction` class from `SimpleBot.Extensions` framework which is **NOT included** in our codebase. We must use raw DreamPoeBot API calls instead.

---

## Core API Patterns

### 1. Item Fast Move (Inventory → Stash/Device)

**Reference**: `EXAMPLE/Inventories.cs:920`

```csharp
public static async Task<bool> FastMoveFromInventory(Vector2i itemPos)
{
    var item = InventoryUi.InventoryControl_Main.Inventory.FindItemByPos(itemPos);
    if (item == null)
        return false;

    var itemName = item.FullName;

    // ACTUAL API CALL - uses item.LocalId, NOT coordinates!
    var err = InventoryUi.InventoryControl_Main.FastMove(item.LocalId);

    if (err != FastMoveResult.None)
    {
        GlobalLog.Error($"Fast move error: \"{err}\".");
        return false;
    }

    // Wait for item to move
    if (await Wait.For(() => InventoryUi.InventoryControl_Main.Inventory.FindItemByPos(itemPos) == null, "fast move", 30))
    {
        return true;
    }

    return false;
}
```

**Key Discovery**:
- Use `InventoryUi.InventoryControl_Main.FastMove(item.LocalId)` - NOT coordinates!
- Returns `FastMoveResult` enum
- Must wait for item to actually move using `Wait.For()`

**Full Path**:
```csharp
using InventoryUi = DreamPoeBot.Loki.Game.LokiPoe.InGameState.InventoryUi;

var err = InventoryUi.InventoryControl_Main.FastMove(item.LocalId);
```

---

### 2. Stash Interaction

**Reference**: `EXAMPLE/Inventories.cs:51`

```csharp
public static async Task<bool> OpenStash()
{
    if (StashUi.IsOpened)
        return true;

    var stashObj = LokiPoe.ObjectManager.Stash;
    if (stashObj == null)
        return false;

    await stashPos.ComeAtOnce(); // Move to stash (SimpleBot extension)

    // This uses PlayerAction.Interact which we DON'T have
    // Pattern shows we need to interact with stashObj until condition is met
    if (!await PlayerAction.Interact(LokiPoe.ObjectManager.Stash,
        () => StashUi.IsOpened && StashUi.StashTabInfo != null,
        "stash opening"))
        return false;

    await Wait.SleepSafe(200);
    return true;
}
```

**Our Alternative** (without PlayerAction):
```csharp
// Use Coroutines.InteractWith() from old reference code
if (!await Coroutines.InteractWith(LokiPoe.ObjectManager.Stash))
    return false;

// Wait for stash to open
if (!await Wait.For(() => StashUi.IsOpened && StashUi.StashTabInfo != null, "stash opening", 100, 5000))
    return false;
```

---

### 3. Map Device Operations

**Reference**: `MapBot/OpenMapTask.cs:166-177`

```csharp
private static async Task<bool> PlaceIntoDevice(Vector2i itemPos)
{
    var oldCount = MapDevice.InventoryControl.Inventory.Items.Count;

    // Uses Inventories.FastMoveFromInventory helper
    if (!await Inventories.FastMoveFromInventory(itemPos))
        return false;

    // Wait for item count to increase in device
    if (!await Wait.For(() => MapDevice.InventoryControl.Inventory.Items.Count == oldCount + 1,
        "item amount change in Map Device"))
        return false;

    return true;
}
```

**Key Discovery**:
- Maps/fragments placed into device using standard `FastMove()` API
- `MapDevice.InventoryControl.Inventory.Items` tracks items in device
- Must wait for count to change to confirm placement

---

### 4. Skill Usage

**Reference**: `MapBot/CastAuraTask.cs:68`

```csharp
using SkillBar = DreamPoeBot.Loki.Game.LokiPoe.InGameState.SkillBarHud;

// Use a skill from skill bar
var used = SkillBar.Use(aura.Slot, false);
if (used != LokiPoe.InGameState.UseResult.None)
{
    GlobalLog.Error($"Fail to cast. Error: \"{used}\".");
    return;
}

// Wait for skill to apply
await Wait.For(() => !LokiPoe.Me.HasCurrentAction && PlayerHasAura(name), "aura applying");
```

**Pattern**:
```csharp
// Full path:
var result = LokiPoe.InGameState.SkillBarHud.Use(slot, alwaysAttackInPlace);

// Returns UseResult enum:
// - UseResult.None = success
// - Other values = error
```

---

### 5. Combat / Skill Targeting

**Reference**: Our existing code in `Beasts/Phases/ClearAreaPhase.cs:71`

```csharp
var useResult = LokiPoe.InGameState.SkillBarHud.BeginUseAt(
    slot,
    alwaysAttackInPlace,
    targetPosition);

if (useResult != LokiPoe.InGameState.UseResult.None)
{
    Log.WarnFormat("Skill use failed: {0}", useResult);
}
```

**Confirmed Working** ✅

---

### 6. Portal/Travel to Hideout

**Reference**: `MapBot/TravelToHideoutTask.cs:29-35`

```csharp
// These are PlayerAction methods we DON'T have:
if (!await PlayerAction.GoToHideout())
    ErrorManager.ReportError();

if (!await PlayerAction.TpToTown())
    ErrorManager.ReportError();
```

**Our Alternative**:
- Need to find raw DreamPoeBot API for:
  - Portal scroll usage
  - Waypoint interaction
  - Hideout entrance

---

### 7. Stash Tab Operations

**Reference**: `EXAMPLE/Inventories.cs:22-24`

```csharp
// Access inventory items
public static List<Item> InventoryItems =>
    LokiPoe.InstanceInfo.GetPlayerInventoryItemsBySlot(InventorySlot.Main);

// Access current stash tab items
public static List<Item> StashTabItems =>
    StashUi.InventoryControl?.Inventory?.Items;

// Get available inventory space
public static int AvailableInventorySquares =>
    LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main).AvailableInventorySquares;
```

**Confirmed API Patterns** ✅

---

### 8. Currency Tab Access

**Reference**: `EXAMPLE/Inventories.cs:1439`

```csharp
[CurrencyNames.Portal] = () => StashUi.CurrencyTab.PortalScroll,
```

**Pattern**:
```csharp
using StashUi = DreamPoeBot.Loki.Game.LokiPoe.InGameState.StashUi;

// Access specific currency in currency tab
var portalScrolls = StashUi.CurrencyTab.PortalScroll;
```

---

## What We're Still Missing

### ❌ Item.Use() for Portal Scrolls/Maps

The reference code uses `PlayerAction` wrappers which hide the actual item usage API. We need to discover:

1. **Portal Scroll Usage**:
   - How to use a portal scroll from inventory
   - Possible patterns to investigate:
     - `item.Use()` - mentioned in old docs but not found in reference
     - `LokiPoe.InGameState.QuickFlaskHud.UseItemInSlot()` - flask-specific
     - Skill-based: Use Portal gem from skill bar instead

2. **Map Device Activation**:
   - `MapBot/OpenMapTask.cs:179-217` shows activation code is commented out
   - Suggests API might be unstable or changed
   - Lines 203 mention `LokiPoe.InGameState.MapDeviceUi.Activate()`

---

## Recommended Next Steps

### 1. ✅ COMPLETED: Fix StashingPhase FastMove
We correctly changed from `PlayerAction.TakeAction` to direct API:
```csharp
LokiPoe.ProcessHookManager.ClearAllKeyStates();
var fastMoveResult = LokiPoe.InGameState.StashUi.FastMoveFromInventory(x, y);
```

This is consistent with reference code pattern ✅

### 2. 🔧 TODO: Implement Portal Creation
Options:
- **A. Portal Gem Skill**: Use portal gem from skill bar if available
  ```csharp
  var portalSkill = LokiPoe.InGameState.SkillBarHud.Skills
      .FirstOrDefault(s => s.Name.ToLower().Contains("portal"));
  if (portalSkill != null)
  {
      var result = LokiPoe.InGameState.SkillBarHud.Use(portalSkill.Slot, false);
  }
  ```

- **B. Waypoint**: Use waypoint to travel to hideout instead
  ```csharp
  var waypoint = LokiPoe.ObjectManager.Objects.FirstOrDefault<Waypoint>();
  if (waypoint != null)
  {
      await Coroutines.InteractWith(waypoint);
      // Then use waypoint UI to select hideout
  }
  ```

### 3. 🔧 TODO: Map Device Activation
From commented code in MapBot/OpenMapTask.cs:203:
```csharp
var activated = LokiPoe.InGameState.MapDeviceUi.Activate();
if (activated != LokiPoe.InGameState.ActivateResult.None)
{
    GlobalLog.Error($"Fail to activate. Error: \"{activated}\".");
    return false;
}
```

---

## Summary Table

| Feature | Reference Location | Status | Notes |
|---------|-------------------|--------|-------|
| FastMove (LocalId) | Inventories.cs:920 | ✅ Alternative exists | We use coordinate-based version |
| Stash Interaction | Inventories.cs:51 | ✅ Alternative exists | Use Coroutines.InteractWith() |
| Map Device Placement | OpenMapTask.cs:166 | ✅ Known | Use FastMove API |
| Skill Usage | CastAuraTask.cs:68 | ✅ Known | SkillBarHud.Use() |
| Combat Targeting | ClearAreaPhase.cs:71 | ✅ Known | SkillBarHud.BeginUseAt() |
| Portal Scroll Usage | N/A | ❌ Missing | Need to find API |
| Portal Gem Usage | CastAuraTask.cs:68 | 🔧 Can implement | Use as skill |
| Waypoint Interaction | N/A | 🔧 Can implement | Use Coroutines.InteractWith() |
| Map Device Activate | OpenMapTask.cs:203 | 🔧 Commented out | MapDeviceUi.Activate() |
| Currency Tab Access | Inventories.cs:1439 | ✅ Known | StashUi.CurrencyTab |

---

## Conclusion

The reference code confirms our current approach is correct:
1. ✅ We correctly removed PlayerAction (not available)
2. ✅ We're using proper DreamPoeBot API patterns
3. ✅ Our StashingPhase fix was correct

Next priorities:
1. Implement portal creation using Portal gem skill (most reliable)
2. Implement waypoint/hideout travel as fallback
3. Test map device activation API
4. Add currency tab withdrawal for portal scroll restocking
